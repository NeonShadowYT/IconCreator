using System;
using System.IO;
using NeonImperium.IconsCreation.Helpers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NeonImperium.IconsCreation
{
    public class IconSceneService
    {
        private const string TARGET_LAYER = "IconsCreatorTargets";
        private const string SCENE_NAME = "Icons_Creation";
        private readonly string _scenePath = $"Assets/Starve Neon/Script/Extension/Editor/IconsCreator/Scenes/{SCENE_NAME}.unity";

        private Scene _previousScene;
        private LightSettings _lightSettings;
        private bool _isOperating = false;

        public void EnsureSceneExists()
        {
            if (EditorApplication.isPlaying) return;
            if (File.Exists(Path.GetFullPath(_scenePath))) return;
            
            try
            {
                CreateScene();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create scene: {e.Message}");
            }
        }

        private void CreateScene()
        {
            if (EditorApplication.isPlaying) return;

            Scene previous = EditorSceneManager.GetActiveScene();
            Scene scene = default;

            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
                scene.name = SCENE_NAME;
                
                ulong layerMask = (ulong)LayerMask.GetMask(TARGET_LAYER);
                EditorSceneManager.SetSceneCullingMask(scene, layerMask);

                ConfigureSceneObjects(scene);
                ConfigureSceneLighting();
                
                EditorSceneManager.SaveScene(scene, _scenePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create scene: {e.Message}");
            }
            finally
            {
                if (scene.IsValid())
                    EditorSceneManager.CloseScene(scene, true);
                EditorSceneManager.SetActiveScene(previous);
            }
        }

        private void ConfigureSceneObjects(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent<Camera>(out var camera))
                {
                    camera.tag = "IconsCreationCamera";
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = Color.clear;
                    camera.orthographic = true;
                }
                
                // Удаляем стандартный Directional Light из сцены иконок
                if (root.TryGetComponent<Light>(out var light) && light.type == LightType.Directional)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private void ConfigureSceneLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientSkyColor = new Color(0.73f, 0.73f, 0.73f);
        }

        public void ExecuteWithTarget(GameObject target, LightSettings lightSettings, bool renderShadows, Action<GameObject> action)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Cannot execute icon creation operations in Play Mode");
                return;
            }

            if (_isOperating)
            {
                Debug.LogWarning("Icon scene service is already operating");
                return;
            }

            Scene scene = default;
            _isOperating = true;
            
            try
            {
                _lightSettings = lightSettings;
                LayersHelper.CreateLayer(TARGET_LAYER);
                scene = OpenScene();
                
                if (!scene.IsValid())
                {
                    Debug.LogError("Failed to open icon creation scene");
                    return;
                }

                GameObject targetInstance = InstantiateTarget(target);
                if (targetInstance == null)
                {
                    Debug.LogError("Failed to instantiate target");
                    return;
                }

                SetupTargetLayer(targetInstance, renderShadows);
                SetupSceneLighting();
                
                action?.Invoke(targetInstance);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during icon scene operation: {e.Message}");
            }
            finally
            {
                CloseScene(scene);
                LayersHelper.RemoveLayer(TARGET_LAYER);
                _lightSettings = null;
                _isOperating = false;
            }
        }

        private Scene OpenScene()
        {
            if (EditorApplication.isPlaying) return default;

            try
            {
                _previousScene = EditorSceneManager.GetActiveScene();
                Scene scene = EditorSceneManager.OpenScene(_scenePath, OpenSceneMode.Additive);
                EditorSceneManager.SetActiveScene(scene);
                return scene;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to open scene: {e.Message}");
                return default;
            }
        }

        private GameObject InstantiateTarget(GameObject original)
        {
            if (EditorApplication.isPlaying) return null;
            
            Scene currentScene = EditorSceneManager.GetActiveScene();
            if (currentScene.name != SCENE_NAME)
            {
                Debug.LogWarning("Target can only be placed in the internal scene!");
                return null;
            }

            try
            {
                GameObject instance = UnityEngine.Object.Instantiate(original);
                instance.name = original.name;
                return instance;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to instantiate target: {e.Message}");
                return null;
            }
        }

        private void SetupTargetLayer(GameObject target, bool renderShadows)
        {
            int layer = LayerMask.NameToLayer(TARGET_LAYER);
            if (layer == -1)
            {
                Debug.LogError($"Layer {TARGET_LAYER} not found");
                return;
            }

            target.layer = layer;

            foreach (Transform child in target.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = layer;
                
                if (child.TryGetComponent<MeshRenderer>(out var renderer))
                {
                    renderer.shadowCastingMode = renderShadows ? 
                        ShadowCastingMode.On : ShadowCastingMode.Off;
                }
            }
        }

        private void SetupSceneLighting()
        {
            int cullingMask = LayerMask.GetMask(TARGET_LAYER);
            
            ClearExistingLights();
            
            if (_lightSettings.Type == LightType.Directional)
            {
                CreateDirectionalLight(cullingMask);
            }
            else if (_lightSettings.Type == LightType.Point)
            {
                CreatePointLights(cullingMask);
            }
            
            SetupCameraCullingMask(cullingMask);
        }

        private void ClearExistingLights()
        {
            var existingLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var light in existingLights)
            {
                if (light.gameObject.scene.name == SCENE_NAME && !light.CompareTag("IconsCreationCamera"))
                {
                    UnityEngine.Object.DestroyImmediate(light.gameObject);
                }
            }
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
        }

        private void CreatePointLights(int cullingMask)
        {
            for (int i = 0; i < _lightSettings.PointLights.Length; i++)
            {
                var pointLight = _lightSettings.PointLights[i];
                GameObject lightGo = new GameObject($"IconsCreator_PointLight_{i + 1}");
                Light light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = pointLight.Color;
                light.intensity = pointLight.Intensity;
                light.transform.position = pointLight.Position;
                light.cullingMask = cullingMask;
                light.range = 10f;
            }
        }

        private void SetupCameraCullingMask(int cullingMask)
        {
            foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.TryGetComponent<Camera>(out var camera))
                    camera.cullingMask = cullingMask;
            }
        }

        private void CloseScene(Scene scene)
        {
            try
            {
                if (_previousScene.IsValid())
                {
                    EditorSceneManager.SetActiveScene(_previousScene);
                }
                
                if (scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to close scene: {e.Message}");
            }
        }
    }
}