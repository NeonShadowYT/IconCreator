using System;
using System.IO;
using NeonImperium.IconsCreation.Helpers;
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

        public void EnsureSceneExists()
        {
            if (File.Exists(Path.GetFullPath(_scenePath))) return;
            CreateScene();
        }

        private void CreateScene()
        {
            Scene previous = EditorSceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            scene.name = SCENE_NAME;
            
            ulong layerMask = (ulong)LayerMask.GetMask(TARGET_LAYER);
            EditorSceneManager.SetSceneCullingMask(scene, layerMask);

            ConfigureSceneObjects(scene);
            ConfigureSceneLighting();
            
            EditorSceneManager.SaveScene(scene, _scenePath);
            EditorSceneManager.CloseScene(scene, true);
            EditorSceneManager.SetActiveScene(previous);
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

                if (root.TryGetComponent<Light>(out var light))
                {
                    light.useColorTemperature = false;
                    light.color = Color.white;
                }
            }
        }

        private void ConfigureSceneLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientSkyColor = new Color(0.73f, 0.73f, 0.73f);
        }

        public void ExecuteWithTarget(GameObject target, bool renderShadows, Action<GameObject> action)
        {
            Scene scene = default;
            
            try
            {
                LayersHelper.CreateLayer(TARGET_LAYER);
                scene = OpenScene();
                
                GameObject targetInstance = InstantiateTarget(target);
                SetupTargetLayer(targetInstance, renderShadows);
                SetupSceneLighting();
                
                action?.Invoke(targetInstance);
            }
            finally
            {
                CloseScene(scene);
                LayersHelper.RemoveLayer(TARGET_LAYER);
            }
        }

        private Scene OpenScene()
        {
            _previousScene = EditorSceneManager.GetActiveScene();
            
            foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                light.cullingMask &= ~LayerMask.GetMask(TARGET_LAYER);
            }

            Scene scene = EditorSceneManager.OpenScene(_scenePath, OpenSceneMode.Additive);
            EditorSceneManager.SetActiveScene(scene);
            return scene;
        }

        private GameObject InstantiateTarget(GameObject original)
        {
            if (EditorSceneManager.GetActiveScene().name != SCENE_NAME)
            {
                Debug.LogWarning("Target can only be placed in the internal scene!");
                return null;
            }

            return UnityEngine.Object.Instantiate(original);
        }

        private void SetupTargetLayer(GameObject target, bool renderShadows)
        {
            int layer = LayerMask.NameToLayer(TARGET_LAYER);
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
            
            foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.TryGetComponent<Light>(out var light))
                    light.cullingMask = cullingMask;
                    
                if (root.TryGetComponent<Camera>(out var camera))
                    camera.cullingMask = cullingMask;
            }
        }

        private void CloseScene(Scene scene)
        {
            EditorSceneManager.SetActiveScene(_previousScene);
            if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }
    }
}