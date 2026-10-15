using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NeonImperium.IconsCreation
{
    public class IconSceneService
    {
        // --- Scene state ---
        private Scene _previousScene;
        private Scene _iconScene;
        private Scene _cachedScene;
        private bool _isOperating;
        private LightSettings _lightSettings;

        // --- Scene objects ---
        private Camera _sceneCamera;
        private GameObject[] _createdLights;

        // --- Reusable container & slots ---
        private GameObject _container;
        private readonly List<RenderSlot> _slots = new List<RenderSlot>();
        private readonly List<SpriteSlot> _spriteSlots = new List<SpriteSlot>();
        private readonly Dictionary<Texture2D, Sprite> _spriteCache = new Dictionary<Texture2D, Sprite>();

        private MaterialPropertyBlock _propertyBlock;

        private static readonly Vector3 ParkedPosition = new Vector3(0f, -100000f, 0f);

        // Уникальный бит для изоляции сцены иконок от сцен пользователя.
        // Берём старший бит — он вряд ли используется Unity для чего-то ещё.
        private const ulong IconSceneCullingBit = 1UL << 62;

        // Сохраняем исходные маски сцен, чтобы восстановить после батча
        private readonly Dictionary<Scene, ulong> _savedSceneMasks = new Dictionary<Scene, ulong>();

        private MaterialPropertyBlock PropertyBlock
        {
            get
            {
                if (_propertyBlock == null)
                    _propertyBlock = new MaterialPropertyBlock();
                return _propertyBlock;
            }
        }

        private class RenderSlot
        {
            public GameObject go;
            public MeshFilter mf;
            public MeshRenderer mr;
        }

        private class SpriteSlot
        {
            public GameObject go;
            public SpriteRenderer sr;
        }

        // ============================================================
        // Public API
        // ============================================================

        public void EnsureSceneExists(string scenePath)
        {
            if (EditorApplication.isPlaying || BuildPipeline.isBuildingPlayer)
                return;

            if (string.IsNullOrEmpty(scenePath) || !scenePath.StartsWith("Assets/"))
                return;

            if (File.Exists(Path.GetFullPath(scenePath)))
                return;

            try
            {
                CreateScene(scenePath);
            }
            catch (Exception e)
            {
                Debug.unityLogger.Log(LogType.Warning, $"Не удалось создать сцену: {e.Message}");
            }
        }

        public void BeginBatch(string cameraTag, string objectsLayer, string scenePath, LightSettings lightSettings)
        {
            if (EditorApplication.isPlaying || BuildPipeline.isBuildingPlayer)
                return;

            if (_isOperating)
            {
                Debug.LogWarning("IconsCreator: предыдущий батч не завершился, принудительная очистка.");
                try { EndBatch(); } catch { }
            }

            _isOperating = true;

            try
            {
                _lightSettings = lightSettings;
                OpenScene(scenePath);

                if (!_iconScene.IsValid())
                {
                    Debug.LogWarning("IconsCreator: сцена невалидна, операция прервана.");
                    _isOperating = false;
                    return;
                }

                ApplySceneIsolation();
                PurgeLeftovers();
                CreateContainer();
                SetupSceneCamera(cameraTag);
                SetupSceneLighting(objectsLayer);
            }
            catch (Exception e)
            {
                Debug.unityLogger.Log(LogType.Warning, $"Ошибка при открытии сцены: {e.Message}");
                _isOperating = false;
            }
        }

        public void EndBatch()
        {
            if (!_isOperating) return;

            try
            {
                DestroyContainer();
                CleanupLights();
                RestoreSceneIsolation();
                CloseScene();
            }
            finally
            {
                _lightSettings = null;
                _isOperating = false;
            }
        }

        public bool ProcessMeshTargetInBatch(GameObject target, string targetName, bool renderShadows,
            string objectsLayer, Action<GameObject> action)
        {
            if (!_isOperating || !_iconScene.IsValid() || _container == null)
            {
                action?.Invoke(null);
                return false;
            }

            try
            {
                int layer = LayerMask.NameToLayer(objectsLayer);
                if (layer == -1) layer = LayerMask.NameToLayer("Default");

                int slotIndex = 0;

                MeshFilter[] sourceFilters = target.GetComponentsInChildren<MeshFilter>(true);
                foreach (MeshFilter srcFilter in sourceFilters)
                {
                    if (srcFilter == null || srcFilter.sharedMesh == null) continue;
                    if (!srcFilter.gameObject.activeSelf) continue;

                    MeshRenderer srcRenderer = srcFilter.GetComponent<MeshRenderer>();
                    if (srcRenderer == null || !srcRenderer.enabled) continue;

                    RenderSlot slot = GetOrCreateMeshSlot(slotIndex);

                    Transform srcT = srcFilter.transform;
                    slot.go.transform.SetPositionAndRotation(srcT.position, srcT.rotation);
                    slot.go.transform.localScale = srcT.lossyScale;

                    slot.mf.sharedMesh = srcFilter.sharedMesh;

                    Material[] mats = srcRenderer.sharedMaterials;
                    if (mats == null || mats.Length == 0)
                    {
                        Material fallback = new Material(Shader.Find("Sprites/Default"));
                        mats = new[] { fallback };
                    }
                    slot.mr.sharedMaterials = mats;

                    var pb = PropertyBlock;
                    srcRenderer.GetPropertyBlock(pb);
                    slot.mr.SetPropertyBlock(pb);

                    slot.mr.shadowCastingMode = renderShadows
                        ? ShadowCastingMode.On : ShadowCastingMode.Off;
                    slot.mr.receiveShadows = renderShadows;
                    slot.mr.renderingLayerMask = uint.MaxValue;

                    slot.go.layer = layer;
                    slot.mr.enabled = true;
                    if (!slot.go.activeSelf) slot.go.SetActive(true);

                    slotIndex++;
                }

                ParkUnusedMeshSlots(slotIndex);

                if (!_container.activeSelf) _container.SetActive(true);

                action?.Invoke(_container);
                return true;
            }
            catch (Exception e)
            {
                Debug.unityLogger.Log(LogType.Warning, $"Ошибка при обработке цели: {e.Message}");
                action?.Invoke(null);
                return false;
            }
        }

        public bool ProcessTextureTargetsInBatch(TextureObjectData[] textureObjects, string objectsLayer, Action action)
        {
            if (!_isOperating || !_iconScene.IsValid() || _container == null)
            {
                action?.Invoke();
                return false;
            }

            try
            {
                int layer = LayerMask.NameToLayer(objectsLayer);
                if (layer == -1) layer = LayerMask.NameToLayer("Default");

                int slotIndex = 0;
                foreach (TextureObjectData obj in textureObjects)
                {
                    if (obj == null || obj.texture == null) continue;

                    SpriteSlot slot = GetOrCreateSpriteSlot(slotIndex);

                    slot.go.layer = layer;
                    if (!slot.go.activeSelf) slot.go.SetActive(true);
                    if (!slot.sr.enabled) slot.sr.enabled = true;

                    slot.sr.sprite = GetOrCreateSprite(obj.texture);
                    slot.sr.color = obj.color;

                    slot.go.transform.position = obj.position;
                    slot.go.transform.rotation = Quaternion.Euler(0f, 0f, obj.rotation);
                    slot.go.transform.localScale = new Vector3(obj.scale.x, obj.scale.y, 1f);

                    slotIndex++;
                }

                ParkUnusedSpriteSlots(slotIndex);

                int cullingMask = LayerMask.GetMask(objectsLayer);
                if (cullingMask == 0) cullingMask = LayerMask.GetMask("Default");
                if (_sceneCamera != null) _sceneCamera.cullingMask = cullingMask;

                _sceneCamera.orthographic = true;
                _sceneCamera.orthographicSize = 1f;
                _sceneCamera.transform.rotation = Quaternion.identity;
                _sceneCamera.transform.position = new Vector3(0f, 0f, -10f);
                _sceneCamera.clearFlags = CameraClearFlags.SolidColor;
                _sceneCamera.backgroundColor = Color.clear;
                _sceneCamera.gameObject.SetActive(true);

                action?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.unityLogger.Log(LogType.Warning, $"Ошибка при создании текстурной иконки: {e.Message}");
                action?.Invoke();
                return false;
            }
        }

        // Совместимость со старым API (используется PresetManager).
        public void ExecuteWithTarget(GameObject target, string targetName, LightSettings lightSettings,
            bool renderShadows, string cameraTag, string objectsLayer, string scenePath,
            Action<GameObject> action)
        {
            BeginBatch(cameraTag, objectsLayer, scenePath, lightSettings);
            try
            {
                ProcessMeshTargetInBatch(target, targetName, renderShadows, objectsLayer, action);
            }
            finally
            {
                EndBatch();
            }
        }

        public void ExecuteWithTextureTargets(TextureObjectData[] textureObjects, string cameraTag,
            string objectsLayer, string scenePath, Action action)
        {
            BeginBatch(cameraTag, objectsLayer, scenePath, new LightSettings());
            try
            {
                ProcessTextureTargetsInBatch(textureObjects, objectsLayer, action);
            }
            finally
            {
                EndBatch();
            }
        }

        // ============================================================
        // Slots
        // ============================================================

        private RenderSlot GetOrCreateMeshSlot(int index)
        {
            while (_slots.Count <= index)
            {
                int newIndex = _slots.Count;
                GameObject go = new GameObject($"Slot_{newIndex}");
                go.transform.SetParent(_container.transform, false);
                go.transform.position = ParkedPosition;
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                MeshFilter mf = go.AddComponent<MeshFilter>();
                MeshRenderer mr = go.AddComponent<MeshRenderer>();

                mr.renderingLayerMask = uint.MaxValue;
                mr.enabled = false;
                mr.sharedMaterials = Array.Empty<Material>();

                go.SetActive(false);

                _slots.Add(new RenderSlot { go = go, mf = mf, mr = mr });
            }
            return _slots[index];
        }

        private SpriteSlot GetOrCreateSpriteSlot(int index)
        {
            while (_spriteSlots.Count <= index)
            {
                int newIndex = _spriteSlots.Count;
                GameObject go = new GameObject($"SpriteSlot_{newIndex}");
                go.transform.SetParent(_container.transform, false);
                go.transform.position = ParkedPosition;
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.renderingLayerMask = uint.MaxValue;
                sr.enabled = false;

                go.SetActive(false);

                _spriteSlots.Add(new SpriteSlot { go = go, sr = sr });
            }
            return _spriteSlots[index];
        }

        private void ParkUnusedMeshSlots(int fromIndex)
        {
            for (int i = fromIndex; i < _slots.Count; i++)
            {
                RenderSlot slot = _slots[i];
                slot.mr.enabled = false;
                slot.mf.sharedMesh = null;
                slot.mr.sharedMaterials = Array.Empty<Material>();
                slot.mr.SetPropertyBlock(null);
                slot.go.transform.position = ParkedPosition;
                slot.go.transform.rotation = Quaternion.identity;
                slot.go.transform.localScale = Vector3.one;
                if (slot.go.activeSelf) slot.go.SetActive(false);
            }
        }

        private void ParkUnusedSpriteSlots(int fromIndex)
        {
            for (int i = fromIndex; i < _spriteSlots.Count; i++)
            {
                SpriteSlot slot = _spriteSlots[i];
                slot.sr.enabled = false;
                slot.sr.sprite = null;
                slot.go.transform.position = ParkedPosition;
                slot.go.transform.rotation = Quaternion.identity;
                slot.go.transform.localScale = Vector3.one;
                if (slot.go.activeSelf) slot.go.SetActive(false);
            }
        }

        private Sprite GetOrCreateSprite(Texture2D texture)
        {
            if (texture == null) return null;
            if (_spriteCache.TryGetValue(texture, out Sprite cached) && cached != null)
                return cached;

            Sprite sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            _spriteCache[texture] = sprite;
            return sprite;
        }

        // ============================================================
        // Container lifecycle
        // ============================================================

        private void CreateContainer()
        {
            _container = new GameObject("IconsCreator_Container");
            _container.transform.position = Vector3.zero;
            _container.transform.rotation = Quaternion.identity;
            _container.transform.localScale = Vector3.one;
            SceneManager.MoveGameObjectToScene(_container, _iconScene);
            _container.SetActive(true);

            _slots.Clear();
            _spriteSlots.Clear();
        }

        private void DestroyContainer()
        {
            if (_container != null)
            {
                UnityEngine.Object.DestroyImmediate(_container);
                _container = null;
            }

            _slots.Clear();
            _spriteSlots.Clear();

            foreach (Sprite s in _spriteCache.Values)
            {
                if (s != null) UnityEngine.Object.DestroyImmediate(s);
            }
            _spriteCache.Clear();
        }

        // ============================================================
        // Scene isolation
        // ============================================================

        private void ApplySceneIsolation()
        {
            _savedSceneMasks.Clear();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (!s.IsValid() || !s.isLoaded) continue;

                ulong oldMask = EditorSceneManager.GetSceneCullingMask(s);
                _savedSceneMasks[s] = oldMask;

                if (s == _iconScene)
                {
                    EditorSceneManager.SetSceneCullingMask(s, IconSceneCullingBit);
                }
                else
                {
                    EditorSceneManager.SetSceneCullingMask(s, oldMask & ~IconSceneCullingBit);
                }
            }
        }

        private void RestoreSceneIsolation()
        {
            foreach (var kv in _savedSceneMasks)
            {
                if (kv.Key.IsValid() && kv.Key.isLoaded)
                    EditorSceneManager.SetSceneCullingMask(kv.Key, kv.Value);
            }
            _savedSceneMasks.Clear();
        }

        // ============================================================
        // Scene management
        // ============================================================

        private void OpenScene(string scenePath)
        {
            if (EditorApplication.isPlaying || BuildPipeline.isBuildingPlayer)
                return;

            try
            {
                _previousScene = EditorSceneManager.GetActiveScene();

                Scene existing = SceneManager.GetSceneByPath(scenePath);
                if (existing.IsValid() && existing.isLoaded)
                {
                    _iconScene = existing;
                    EditorSceneManager.SetActiveScene(_iconScene);
                    return;
                }

                _iconScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                EditorSceneManager.SetActiveScene(_iconScene);
            }
            catch (Exception e)
            {
                Debug.unityLogger.Log(LogType.Warning, $"Не удалось открыть сцену: {e.Message}");
                _iconScene = default;
                _isOperating = false;
            }
        }

        private void CloseScene()
        {
            try
            {
                if (_previousScene.IsValid() && _previousScene != _iconScene)
                    EditorSceneManager.SetActiveScene(_previousScene);

                if (_iconScene.IsValid())
                    EditorSceneManager.CloseScene(_iconScene, true);

                _sceneCamera = null;
                _iconScene = default;
                _cachedScene = default;
            }
            catch (Exception e)
            {
                Debug.unityLogger.Log(LogType.Warning, $"Ошибка при закрытии сцены: {e.Message}");
                _iconScene = default;
                _sceneCamera = null;
                _cachedScene = default;
            }
        }

        private void CreateScene(string scenePath)
        {
            if (EditorApplication.isPlaying || BuildPipeline.isBuildingPlayer)
                return;

            string directory = Path.GetDirectoryName(scenePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            Scene previous = EditorSceneManager.GetActiveScene();
            Scene scene = default;

            try
            {
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                scene.name = sceneName;
                EditorSceneManager.SaveScene(scene, scenePath);
            }
            catch (Exception e)
            {
                Debug.unityLogger.Log(LogType.Warning, $"Ошибка при сохранении сцены: {e.Message}");
            }
            finally
            {
                if (scene.IsValid())
                    EditorSceneManager.CloseScene(scene, true);
                EditorSceneManager.SetActiveScene(previous);
            }
        }

        private void PurgeLeftovers()
        {
            if (!_iconScene.IsValid()) return;

            GameObject cameraGO = _sceneCamera != null ? _sceneCamera.gameObject : null;

            foreach (GameObject root in _iconScene.GetRootGameObjects())
            {
                if (root == null) continue;
                if (root == cameraGO) continue;

                string n = root.name;

                if (n == "IconsCreator_Camera" ||
                    n.StartsWith("IconsCreator_PointLight_") ||
                    n.StartsWith("IconsCreator_DirectionalLight"))
                    continue;

                if (n == "IconsCreator_Container" ||
                    n.StartsWith("Slot_") ||
                    n.StartsWith("SpriteSlot_") ||
                    n.StartsWith("Group_") ||
                    n.StartsWith("Sprite_") ||
                    n == "PreviewCube")
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        // ============================================================
        // Camera & lights
        // ============================================================

        private void SetupSceneCamera(string cameraTag)
        {
            if (_sceneCamera != null
                && _sceneCamera.CompareTag(cameraTag)
                && _sceneCamera.gameObject.scene == _iconScene)
            {
                _sceneCamera.overrideSceneCullingMask = IconSceneCullingBit;
                return;
            }

            _sceneCamera = null;

            foreach (GameObject root in _iconScene.GetRootGameObjects())
            {
                if (root.TryGetComponent<Camera>(out Camera cam) && cam.CompareTag(cameraTag))
                {
                    _sceneCamera = cam;
                    break;
                }
            }

            if (_sceneCamera == null)
            {
                GameObject cameraGO = new GameObject("IconsCreator_Camera");
                _sceneCamera = cameraGO.AddComponent<Camera>();
                _sceneCamera.tag = cameraTag;
                _sceneCamera.clearFlags = CameraClearFlags.SolidColor;
                _sceneCamera.backgroundColor = Color.clear;
                _sceneCamera.orthographic = true;
                cameraGO.SetActive(false);
                SceneManager.MoveGameObjectToScene(cameraGO, _iconScene);
            }

            _sceneCamera.overrideSceneCullingMask = IconSceneCullingBit;
        }

        private void SetupSceneLighting(string objectsLayer)
        {
            int cullingMask = LayerMask.GetMask(objectsLayer);
            if (cullingMask == 0) cullingMask = LayerMask.GetMask("Default");

            CleanupLights();

            if (_lightSettings == null)
            {
                if (_sceneCamera != null) _sceneCamera.cullingMask = cullingMask;
                return;
            }

            if (_lightSettings.Type == LightType.Directional)
                CreateDirectionalLight(cullingMask);
            else if (_lightSettings.Type == LightType.Point)
                CreatePointLights(cullingMask);

            if (_sceneCamera != null) _sceneCamera.cullingMask = cullingMask;
        }

        private void CreateDirectionalLight(int cullingMask)
        {
            GameObject lightGo = new GameObject("IconsCreator_DirectionalLight");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = _lightSettings.DirectionalColor;
            light.intensity = _lightSettings.DirectionalIntensity;
            light.transform.rotation = Quaternion.Euler(_lightSettings.DirectionalRotation);
            light.cullingMask = cullingMask;
            light.shadows = LightShadows.Soft;
            SceneManager.MoveGameObjectToScene(lightGo, _iconScene);

            _createdLights = new[] { lightGo };
        }

        private void CreatePointLights(int cullingMask)
        {
            var lights = _lightSettings.PointLights;
            _createdLights = new GameObject[lights.Length];

            for (int i = 0; i < lights.Length; i++)
            {
                LightSettings.PointLightData data = lights[i];
                GameObject lightGo = new GameObject($"IconsCreator_PointLight_{i + 1}");
                Light light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = data.Color;
                light.intensity = data.Intensity;
                light.transform.position = data.Position;
                light.cullingMask = cullingMask;
                light.range = 10f;
                light.shadows = LightShadows.Soft;
                SceneManager.MoveGameObjectToScene(lightGo, _iconScene);

                _createdLights[i] = lightGo;
            }
        }

        private void CleanupLights()
        {
            if (_createdLights == null) return;

            foreach (GameObject lightGo in _createdLights)
            {
                if (lightGo != null)
                    UnityEngine.Object.DestroyImmediate(lightGo);
            }
            _createdLights = null;
        }
    }
}