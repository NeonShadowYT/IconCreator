using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    public class IconCreatorService
    {
        private IconsCreatorData _data;
        private readonly IconSceneService _sceneService;
        private readonly IconCameraService _cameraService;
        private readonly IconSaverService _saverService;
        private EditorCoroutine _generationCoroutine;
        private EditorCoroutine _previewCoroutine;
        private bool _previewDirty = true;
        private string _scenePath;
        private bool _isProcessing = false;

        public Texture2D[] CameraPreviews { get; private set; }
        public bool IsGenerating { get; private set; }
        public float GenerationProgress { get; private set; }
        public string GenerationStatus { get; private set; }

        public event System.Action PreviewsUpdated;

        public IconCreatorService()
        {
            _sceneService = new IconSceneService();
            _cameraService = new IconCameraService();
            _saverService = new IconSaverService();
        }

        public void InitializeEnvironment(string scenePath)
        {
            if (EditorApplication.isPlaying) return;
            _scenePath = scenePath;
            _sceneService.EnsureSceneExists(_scenePath);
        }

        public void SetData(IconsCreatorData data)
        {
            if (EditorApplication.isPlaying) return;

            if (_data == null || !DataEquals(_data, data))
            {
                _data = data;
                _previewDirty = true;

                _cameraService.Initialize(data.Texture, data.Camera, data.Shadow, data.CameraTag);
                _saverService.Initialize(data.Directory, data.Texture);
            }
        }

        private bool DataEquals(IconsCreatorData a, IconsCreatorData b)
        {
            if (a == null || b == null) return false;
            if (a.Type != b.Type) return false;

            if (a.Type == IconType.Mesh)
            {
                if (a.MeshTargets == null || b.MeshTargets == null) return false;
                if (a.MeshTargets.Length != b.MeshTargets.Length) return false;
                for (int i = 0; i < a.MeshTargets.Length; i++)
                    if (a.MeshTargets[i] != b.MeshTargets[i]) return false;
            }
            else
            {
                if (a.TextureTargets == null || b.TextureTargets == null) return false;
                if (a.TextureTargets.Length != b.TextureTargets.Length) return false;
                for (int i = 0; i < a.TextureTargets.Length; i++)
                {
                    var x = a.TextureTargets[i];
                    var y = b.TextureTargets[i];
                    if (x.texture != y.texture ||
                        x.position != y.position ||
                        x.rotation != y.rotation ||
                        x.scale != y.scale ||
                        x.color != y.color) return false;
                }
            }

            return a.Directory == b.Directory &&
                   a.CameraTag == b.CameraTag &&
                   a.ObjectsLayer == b.ObjectsLayer &&
                   a.Texture.Size == b.Texture.Size &&
                   a.Texture.RenderSize == b.Texture.RenderSize &&
                   a.Texture.PreviewSize == b.Texture.PreviewSize &&
                   a.Texture.Compression == b.Texture.Compression &&
                   a.Texture.FilterMode == b.Texture.FilterMode &&
                   a.Texture.AnisoLevel == b.Texture.AnisoLevel &&
                   a.Camera.Padding == b.Camera.Padding &&
                   a.Camera.Rotation == b.Camera.Rotation &&
                   a.Camera.RenderShadows == b.Camera.RenderShadows &&
                   a.Light.Type == b.Light.Type &&
                   a.Light.DirectionalRotation == b.Light.DirectionalRotation &&
                   a.Light.DirectionalColor == b.Light.DirectionalColor &&
                   a.Light.DirectionalIntensity == b.Light.DirectionalIntensity &&
                   a.Light.PointLights[0].Position == b.Light.PointLights[0].Position &&
                   a.Light.PointLights[0].Color == b.Light.PointLights[0].Color &&
                   a.Light.PointLights[0].Intensity == b.Light.PointLights[0].Intensity &&
                   a.Light.PointLights[1].Position == b.Light.PointLights[1].Position &&
                   a.Light.PointLights[1].Color == b.Light.PointLights[1].Color &&
                   a.Light.PointLights[1].Intensity == b.Light.PointLights[1].Intensity &&
                   a.Shadow.Enabled == b.Shadow.Enabled &&
                   a.Shadow.Color == b.Shadow.Color &&
                   a.Shadow.Offset == b.Shadow.Offset &&
                   a.Shadow.Scale == b.Shadow.Scale;
        }

        public void RequestPreviewUpdate(bool forceUpdate = false)
        {
            if (IsGenerating) return;
            if (!HasValidTargets) return;

            if (_previewCoroutine != null)
            {
                EditorCoroutine.Stop(_previewCoroutine);
                _previewCoroutine = null;
            }

            _isProcessing = false;

            if (_previewDirty || forceUpdate)
            {
                _previewDirty = false;
                _isProcessing = true;
                _previewCoroutine = EditorCoroutine.Start(UpdatePreviewCoroutine());
            }
        }

        private IEnumerator UpdatePreviewCoroutine()
        {
            try
            {
                yield return null;

                ClearPreviews();

                if (_data == null)
                    yield break;

                if (_data.Type == IconType.Mesh)
                {
                    if (_data.MeshTargets == null || _data.MeshTargets.Length == 0)
                        yield break;

                    CameraPreviews = new Texture2D[_data.MeshTargets.Length];

                    _sceneService.BeginBatch(_data.CameraTag, _data.ObjectsLayer, _scenePath, _data.Light);
                    try
                    {
                        for (int i = 0; i < _data.MeshTargets.Length; i++)
                        {
                            GameObject target = _data.MeshTargets[i];
                            if (target == null) { CameraPreviews[i] = null; continue; }

                            string targetName = CleanObjectName(target.name);
                            Texture2D captured = null;

                            bool processed = _sceneService.ProcessMeshTargetInBatch(
                                target, targetName, _data.Camera.RenderShadows, _data.ObjectsLayer,
                                container =>
                                {
                                    if (container != null)
                                    {
                                        _cameraService.SetupForTarget(container);
                                        captured = _cameraService.CaptureView(isPreview: true, applyShadow: true);
                                    }
                                });

                            if (!processed)
                                Debug.LogWarning($"Не удалось обработать {targetName}.");

                            CameraPreviews[i] = captured;

                            if (i % 2 == 0)
                                yield return null;
                        }
                    }
                    finally
                    {
                        _sceneService.EndBatch();
                    }
                }
                else // Texture
                {
                    if (_data.TextureTargets == null || _data.TextureTargets.Length == 0)
                        yield break;

                    Texture2D captured = null;

                    _sceneService.BeginBatch(_data.CameraTag, _data.ObjectsLayer, _scenePath, _data.Light);
                    try
                    {
                        _sceneService.ProcessTextureTargetsInBatch(_data.TextureTargets, _data.ObjectsLayer, () =>
                        {
                            _cameraService.SetupForTexture(_data.ObjectsLayer);
                            captured = _cameraService.CaptureView(isPreview: true, applyShadow: true);
                        });
                    }
                    finally
                    {
                        _sceneService.EndBatch();
                    }

                    CameraPreviews = captured != null ? new[] { captured } : null;
                }
            }
            finally
            {
                _isProcessing = false;
                PreviewsUpdated?.Invoke();
            }
        }

        public void CreateIconsAsync()
        {
            if (_isProcessing) return;
            if (!HasValidTargets || IsGenerating) return;

            StopGeneration();
            _isProcessing = true;
            _generationCoroutine = EditorCoroutine.Start(CreateIconsCoroutine());
        }

        private IEnumerator CreateIconsCoroutine()
        {
            try
            {
                IsGenerating = true;
                GenerationProgress = 0f;
                GenerationStatus = "Подготовка...";

                _saverService.BeginBatch();
                try
                {
                    if (_data.Type == IconType.Mesh)
                    {
                        GameObject[] targets = _data.MeshTargets;
                        if (targets == null || targets.Length == 0)
                            yield break;

                        int totalTargets = targets.Length;

                        _sceneService.BeginBatch(_data.CameraTag, _data.ObjectsLayer, _scenePath, _data.Light);
                        try
                        {
                            for (int i = 0; i < totalTargets; i++)
                            {
                                if (targets[i] == null) continue;

                                GenerationProgress = (float)i / totalTargets;
                                GenerationStatus = $"Обработка {i + 1} из {totalTargets}";

                                if (i % 5 == 0 || i == totalTargets - 1)
                                    EditorUtility.DisplayProgressBar("Создание иконок", GenerationStatus, GenerationProgress);

                                string targetName = CleanObjectName(targets[i].name);

                                _sceneService.ProcessMeshTargetInBatch(
                                    targets[i], targetName, _data.Camera.RenderShadows, _data.ObjectsLayer,
                                    container =>
                                    {
                                        if (container == null) return;
                                        _cameraService.SetupForTarget(container);
                                        Texture2D icon = _cameraService.CaptureView(isPreview: false, applyShadow: true);
                                        if (icon != null)
                                            _saverService.SaveIcon(icon, targetName);
                                    });

                                yield return null;
                            }
                        }
                        finally
                        {
                            _sceneService.EndBatch();
                        }
                    }
                    else // Texture
                    {
                        if (_data.TextureTargets == null || _data.TextureTargets.Length == 0)
                            yield break;

                        EditorUtility.DisplayProgressBar("Создание иконок", "Рендеринг текстурной иконки...", 0.5f);

                        string iconName = "TextureComposite";

                        _sceneService.BeginBatch(_data.CameraTag, _data.ObjectsLayer, _scenePath, _data.Light);
                        try
                        {
                            _sceneService.ProcessTextureTargetsInBatch(_data.TextureTargets, _data.ObjectsLayer, () =>
                            {
                                _cameraService.SetupForTexture(_data.ObjectsLayer);
                                Texture2D icon = _cameraService.CaptureView(isPreview: false, applyShadow: true);
                                if (icon != null)
                                    _saverService.SaveIcon(icon, iconName);
                            });
                        }
                        finally
                        {
                            _sceneService.EndBatch();
                        }
                    }

                    GenerationStatus = "Сохранение...";
                    EditorUtility.DisplayProgressBar("Создание иконок", GenerationStatus, 0.95f);
                }
                finally
                {
                    _saverService.EndBatch();
                }

                AssetDatabase.Refresh();
                yield return null;

                EditorUtility.DisplayDialog("Готово", "Иконки созданы успешно", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                IsGenerating = false;
                GenerationProgress = 0f;
                GenerationStatus = "";
                _isProcessing = false;
            }
        }

        public void StopGeneration()
        {
            if (_generationCoroutine != null)
            {
                EditorCoroutine.Stop(_generationCoroutine);
                _generationCoroutine = null;
            }

            if (_previewCoroutine != null)
            {
                EditorCoroutine.Stop(_previewCoroutine);
                _previewCoroutine = null;
            }

            IsGenerating = false;
            GenerationProgress = 0f;
            GenerationStatus = "";
            _isProcessing = false;
        }

        public void Dispose()
        {
            if (_generationCoroutine != null)
            {
                EditorCoroutine.Stop(_generationCoroutine);
                _generationCoroutine = null;
            }

            if (_previewCoroutine != null)
            {
                EditorCoroutine.Stop(_previewCoroutine);
                _previewCoroutine = null;
            }

            try { _sceneService.EndBatch(); } catch { }

            ClearPreviews();
            _cameraService.Cleanup();
            _saverService.Dispose();
            PreviewsUpdated = null;
            _isProcessing = false;
            IsGenerating = false;
        }

        private void ClearPreviews()
        {
            if (CameraPreviews != null)
            {
                foreach (Texture2D preview in CameraPreviews)
                {
                    if (preview != null)
                        UnityEngine.Object.DestroyImmediate(preview);
                }
                CameraPreviews = null;
            }
        }

        private bool HasValidTargets
        {
            get
            {
                if (_data == null) return false;
                if (_data.Type == IconType.Mesh)
                    return _data.MeshTargets?.Any(t => t != null) ?? false;
                else
                    return _data.TextureTargets?.Any(t => t.texture != null) ?? false;
            }
        }

        private string CleanObjectName(string name)
        {
            return name.Replace("(Clone)", "").Replace("(clone)", "").Trim();
        }

        public void MarkPreviewDirty()
        {
            _previewDirty = true;
        }
    }

    public class EditorCoroutine
    {
        private readonly IEnumerator _routine;

        public static EditorCoroutine Start(IEnumerator routine)
        {
            EditorCoroutine coroutine = new EditorCoroutine(routine);
            coroutine.Start();
            return coroutine;
        }

        private EditorCoroutine(IEnumerator routine)
        {
            _routine = routine;
        }

        private void Start()
        {
            EditorApplication.update += Update;
        }

        private void Update()
        {
            bool finished;
            try
            {
                finished = !_routine.MoveNext();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"EditorCoroutine exception: {e}");
                finished = true;
            }

            if (finished)
                Stop();
        }

        public static void Stop(EditorCoroutine coroutine)
        {
            if (coroutine != null)
                coroutine.Stop();
        }

        private void Stop()
        {
            EditorApplication.update -= Update;

            if (_routine is System.IDisposable disposable)
            {
                try { disposable.Dispose(); }
                catch (System.Exception e) { Debug.LogError($"EditorCoroutine dispose: {e}"); }
            }
        }
    }
}