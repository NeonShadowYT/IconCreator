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
            _presetsFolder = ValidateFolderPath(presetsFolder);
            EnsurePresetsFolderExists();
        }

        public void SetPresetsFolder(string folder)
        {
            _presetsFolder = ValidateFolderPath(folder);
            EnsurePresetsFolderExists();
        }

        private string ValidateFolderPath(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return "Assets/Plugins/NeonImperium/IconCreator/Presets";

            folder = folder.Replace('\\', '/');
            if (!folder.StartsWith("Assets/"))
                folder = "Assets/" + folder.TrimStart('/');

            return folder;
        }

        public void SavePreset(PresetData presetData)
        {
            if (EditorApplication.isPlaying) return;
            if (string.IsNullOrEmpty(_presetsFolder)) return;

            string filePath = Path.Combine(_presetsFolder, $"{presetData.presetName}.json");
            string json = JsonUtility.ToJson(presetData, true);
            File.WriteAllText(filePath, json);
            AssetDatabase.Refresh();
            
            EditorPrefs.SetString("currentPresetName", presetData.presetName);
        }

        public List<PresetData> LoadAllPresets()
        {
            List<PresetData> presets = new List<PresetData>();
            
            if (string.IsNullOrEmpty(_presetsFolder) || !Directory.Exists(_presetsFolder))
                return presets;

            string[] jsonFiles = Directory.GetFiles(_presetsFolder, "*.json");
            
            foreach (string filePath in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    PresetData preset = JsonUtility.FromJson<PresetData>(json);
                    if (preset != null && !string.IsNullOrEmpty(preset.presetName))
                    {
                        presets.Add(preset);
                    }
                    else
                    {
                        Debug.LogWarning($"Preset file {filePath} contains invalid data and will be skipped.");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to load preset from {filePath}: {ex.Message}. Skipping.");
                }
            }

            return presets;
        }

        public PresetData LoadPreset(string presetName)
        {
            if (string.IsNullOrEmpty(_presetsFolder) || string.IsNullOrEmpty(presetName))
                return null;

            string filePath = Path.Combine(_presetsFolder, $"{presetName}.json");
            if (!File.Exists(filePath))
                return null;

            try
            {
                string json = File.ReadAllText(filePath);
                PresetData preset = JsonUtility.FromJson<PresetData>(json);
                if (preset != null && !string.IsNullOrEmpty(preset.presetName))
                {
                    return preset;
                }
                else
                {
                    Debug.LogWarning($"Preset file {filePath} contains invalid data.");
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load preset {presetName}: {ex.Message}");
                return null;
            }
        }

        public void DeletePreset(string presetName)
        {
            if (EditorApplication.isPlaying) return;
            if (string.IsNullOrEmpty(_presetsFolder) || string.IsNullOrEmpty(presetName)) return;

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
            
            Texture2D preview = null;
            try
            {
                IconsCreatorData tempData = new IconsCreatorData(
                    preset.textureSettings,
                    preset.cameraSettings,
                    preset.lightSettings,
                    preset.shadowSettings,
                    "Assets/Icons/",
                    new List<Object> { previewTarget },
                    "EditorOnly",
                    "Default"
                );

                IconSceneService sceneService = new IconSceneService();
                IconCameraService cameraService = new IconCameraService();
                
                cameraService.Initialize(preset.textureSettings, preset.cameraSettings, preset.shadowSettings, "EditorOnly");
                
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
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to generate preview for preset {preset.presetName}: {ex.Message}");
                if (preview != null)
                {
                    UnityEngine.Object.DestroyImmediate(preview);
                }
                return null;
            }
            finally
            {
                if (previewTarget.name == "PreviewCube")
                {
                    UnityEngine.Object.DestroyImmediate(previewTarget);
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
            if (string.IsNullOrEmpty(_presetsFolder)) return;

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