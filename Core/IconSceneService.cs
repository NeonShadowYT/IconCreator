using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NeonImperium.IconsCreation
{
    public class IconSceneService
    {
        private const string SCENE_NAME = "Icons_Creation";
        private readonly string _scenePath = $"Assets/Starve Neon/Script/Extension/Editor/IconsCreator/Scenes/{SCENE_NAME}.unity";

        private Scene _previousScene;
        private LightSettings _lightSettings;
        private bool _isOperating = false;
        private GameObject _currentTargetInstance;
        private Camera _sceneCamera;
        private GameObject[] _createdLights;

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
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                scene.name = SCENE_NAME;
                
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

        public void ExecuteWithTarget(GameObject target, string targetName, LightSettings lightSettings, bool renderShadows, 
                                    string cameraTag, string objectsLayer, Action<GameObject> action)
        {
            if (EditorApplication.isPlaying || _isOperating)
            {
                return;
            }

            Scene scene = default;
            _isOperating = true;
            
            try
            {
                _lightSettings = lightSettings;
                scene = OpenScene();
                
                if (!scene.IsValid()) return;

                CleanupPreviousTarget();
                SetupSceneCamera(cameraTag);
                
                _currentTargetInstance = InstantiateTarget(target, targetName);
                if (_currentTargetInstance == null) return;

                SetupTargetLayer(_currentTargetInstance, objectsLayer, renderShadows);
                SetupSceneLighting(objectsLayer);
                
                action?.Invoke(_currentTargetInstance);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to execute with target: {e.Message}");
            }
            finally
            {
                CloseScene(scene);
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

        private GameObject InstantiateTarget(GameObject original, string targetName)
        {
            if (EditorApplication.isPlaying) return null;
            
            Scene currentScene = EditorSceneManager.GetActiveScene();
            if (currentScene.name != SCENE_NAME) return null;

            try
            {
                GameObject instance = UnityEngine.Object.Instantiate(original);
                instance.name = targetName;
                return instance;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to instantiate target: {e.Message}");
                return null;
            }
        }

        private void CleanupPreviousTarget()
        {
            if (_currentTargetInstance != null)
            {
                UnityEngine.Object.DestroyImmediate(_currentTargetInstance);
                _currentTargetInstance = null;
            }
            
            CleanupLights();
        }

        private void CleanupLights()
        {
            if (_createdLights != null)
            {
                foreach (GameObject light in _createdLights)
                {
                    if (light != null)
                        UnityEngine.Object.DestroyImmediate(light);
                }
                _createdLights = null;
            }
        }

        private void SetupSceneCamera(string cameraTag)
        {
            if (_sceneCamera == null || !_sceneCamera.CompareTag(cameraTag))
            {
                _sceneCamera = null;
                foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (root.TryGetComponent<Camera>(out Camera camera) && camera.CompareTag(cameraTag))
                    {
                        _sceneCamera = camera;
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
                }
            }
        }

        private void SetupTargetLayer(GameObject target, string objectsLayer, bool renderShadows)
        {
            int layer = LayerMask.NameToLayer(objectsLayer);
            if (layer == -1) layer = LayerMask.NameToLayer("Default");

            if (layer != -1)
            {
                target.layer = layer;

                foreach (Transform child in target.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = layer;
                    
                    if (child.TryGetComponent<MeshRenderer>(out MeshRenderer renderer))
                    {
                        renderer.shadowCastingMode = renderShadows ? 
                            ShadowCastingMode.On : ShadowCastingMode.Off;
                        renderer.receiveShadows = renderShadows;
                    }
                }
            }
        }

        private void SetupSceneLighting(string objectsLayer)
        {
            int cullingMask = LayerMask.GetMask(objectsLayer);
            if (cullingMask == 0) cullingMask = LayerMask.GetMask("Default");
            
            CleanupLights();
            
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
            
            _createdLights = new GameObject[] { lightGo };
        }

        private void CreatePointLights(int cullingMask)
        {
            _createdLights = new GameObject[_lightSettings.PointLights.Length];
            
            for (int i = 0; i < _lightSettings.PointLights.Length; i++)
            {
                LightSettings.PointLightData pointLight = _lightSettings.PointLights[i];
                GameObject lightGo = new GameObject($"IconsCreator_PointLight_{i + 1}");
                Light light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = pointLight.Color;
                light.intensity = pointLight.Intensity;
                light.transform.position = pointLight.Position;
                light.cullingMask = cullingMask;
                light.range = 10f;
                light.shadows = LightShadows.Soft;
                
                _createdLights[i] = lightGo;
            }
        }

        private void SetupCameraCullingMask(int cullingMask)
        {
            if (_sceneCamera != null)
                _sceneCamera.cullingMask = cullingMask;
        }

        private void CloseScene(Scene scene)
        {
            try
            {
                CleanupPreviousTarget();
                
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