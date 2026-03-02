using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    public class PresetManager
    {
        private string _presetsFolder;

        public PresetManager(string presetsFolder)
        {
            _presetsFolder = presetsFolder;
            EnsurePresetsFolderExists();
        }

        public void SetPresetsFolder(string folder)
        {
            _presetsFolder = folder;
            EnsurePresetsFolderExists();
        }

        public void SavePreset(PresetData presetData)
        {
            if (EditorApplication.isPlaying) return;

            string filePath = Path.Combine(_presetsFolder, $"{presetData.presetName}.json");
            string json = JsonUtility.ToJson(presetData, true);
            File.WriteAllText(filePath, json);
            AssetDatabase.Refresh();
            
            EditorPrefs.SetString("currentPresetName", presetData.presetName);
        }

        public List<PresetData> LoadAllPresets()
        {
            List<PresetData> presets = new();
            
            if (!Directory.Exists(_presetsFolder))
                return presets;

            string[] jsonFiles = Directory.GetFiles(_presetsFolder, "*.json");
            
            foreach (string filePath in jsonFiles)
            {
                string json = File.ReadAllText(filePath);
                PresetData preset = JsonUtility.FromJson<PresetData>(json);
                presets.Add(preset);
            }

            return presets;
        }

        public PresetData LoadPreset(string presetName)
        {
            string filePath = Path.Combine(_presetsFolder, $"{presetName}.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<PresetData>(json);
            }
            return null;
        }

        public void DeletePreset(string presetName)
        {
            if (EditorApplication.isPlaying) return;

            string filePath = Path.Combine(_presetsFolder, $"{presetName}.json");
            string metaFilePath = filePath + ".meta";
            
            if (File.Exists(filePath))
                File.Delete(filePath);
            
            if (File.Exists(metaFilePath))
                File.Delete(metaFilePath);
            
            AssetDatabase.Refresh();
        }

        public Texture2D GeneratePreviewForPreset(PresetData preset, List<Object> targets, string scenePath)
        {
            if (EditorApplication.isPlaying) return null;

            GameObject previewTarget = GetPreviewTarget(targets);
            if (previewTarget == null) return null;
            
            try
            {
                IconsCreatorData tempData = new(
                    preset.textureSettings,
                    preset.cameraSettings,
                    preset.lightSettings,
                    preset.shadowSettings,
                    "Assets/Icons/", // directory - не используется в превью
                    new List<Object> { previewTarget },
                    "EditorOnly", // cameraTag - не используется в превью
                    "Default" // objectsLayer - не используется в превью
                );

                IconSceneService sceneService = new();
                IconCameraService cameraService = new();
                
                cameraService.Initialize(preset.textureSettings, preset.cameraSettings, preset.shadowSettings, "EditorOnly");
                
                Texture2D preview = null;
                sceneService.ExecuteWithTarget(previewTarget, previewTarget.name, preset.lightSettings, 
                    preset.cameraSettings.RenderShadows, "EditorOnly", "Default", scenePath, target =>
                {
                    if (target != null)
                    {
                        cameraService.SetupForTarget(target);
                        preview = cameraService.CaptureView();
                    }
                });

                cameraService.Cleanup();
                
                return preview;
            }
            finally
            {
                if (previewTarget.name == "PreviewCube")
                {
                    Object.DestroyImmediate(previewTarget);
                }
            }
        }

        private GameObject GetPreviewTarget(List<Object> targets)
        {
            if (targets != null && targets.Count > 0)
            {
                foreach (Object target in targets)
                {
                    if (target is GameObject gameObject && gameObject != null)
                    {
                        return gameObject;
                    }
                }
            }
            
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "PreviewCube";
            cube.transform.localScale = Vector3.one * 0.5f;
            return cube;
        }

        private void EnsurePresetsFolderExists()
        {
            if (EditorApplication.isPlaying) return;

            if (!Directory.Exists(_presetsFolder))
            {
                Directory.CreateDirectory(_presetsFolder);
                AssetDatabase.Refresh();
            }
        }

        public string GetCurrentPresetName()
        {
            return EditorPrefs.GetString("currentPresetName", "");
        }
    }
}