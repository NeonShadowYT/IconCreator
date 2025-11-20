using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    public class PresetManager
    {
        private string _presetsFolder;
        private readonly IconCreatorService _iconCreator;

        public PresetManager(IconCreatorService iconCreator, string presetsFolder)
        {
            _iconCreator = iconCreator;
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
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Cannot save presets in Play Mode");
                return;
            }

            try
            {
                string filePath = Path.Combine(_presetsFolder, $"{presetData.presetName}.json");
                string json = JsonUtility.ToJson(presetData, true);
                File.WriteAllText(filePath, json);
                AssetDatabase.Refresh();
                
                // Сохраняем имя текущего пресета
                EditorPrefs.SetString("currentPresetName", presetData.presetName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save preset: {e.Message}");
            }
        }

        public List<PresetData> LoadAllPresets()
        {
            var presets = new List<PresetData>();
            
            if (!Directory.Exists(_presetsFolder))
                return presets;

            string[] jsonFiles = Directory.GetFiles(_presetsFolder, "*.json");
            
            foreach (string filePath in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    PresetData preset = JsonUtility.FromJson<PresetData>(json);
                    presets.Add(preset);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load preset from {filePath}: {e.Message}");
                }
            }

            return presets;
        }

        public PresetData LoadPreset(string presetName)
        {
            string filePath = Path.Combine(_presetsFolder, $"{presetName}.json");
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    return JsonUtility.FromJson<PresetData>(json);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load preset {presetName}: {e.Message}");
                return null;
                }
            }
            return null;
        }

        public void DeletePreset(string presetName)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Cannot delete presets in Play Mode");
                return;
            }

            string filePath = Path.Combine(_presetsFolder, $"{presetName}.json");
            string metaFilePath = filePath + ".meta";
            
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                
                if (File.Exists(metaFilePath))
                {
                    File.Delete(metaFilePath);
                }
                
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete preset {presetName}: {e.Message}");
            }
        }

        public Texture2D GeneratePreviewForPreset(PresetData preset, List<Object> targets)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Cannot generate previews in Play Mode");
                return null;
            }

            GameObject previewTarget = null;
            
            try
            {
                previewTarget = GetPreviewTarget(targets);
                
                var tempData = new IconsCreatorData(
                    preset.textureSettings,
                    preset.cameraSettings,
                    preset.lightSettings,
                    preset.shadowSettings,
                    preset.directory,
                    new List<Object> { previewTarget }
                );

                _iconCreator.SetData(tempData);
                
                Texture2D preview = null;
                var sceneService = new IconSceneService();
                var cameraService = new IconCameraService();
                
                cameraService.Initialize(preset.textureSettings, preset.cameraSettings, preset.shadowSettings);
                
                sceneService.ExecuteWithTarget(previewTarget, preset.lightSettings, preset.cameraSettings.RenderShadows, target =>
                {
                    if (target != null)
                    {
                        cameraService.SetupForTarget(target);
                        preview = cameraService.CaptureView();
                    }
                });

                return preview;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate preview for preset {preset.presetName}: {e.Message}");
                return null;
            }
            finally
            {
                // Удаляем временный объект если он был создан
                if (previewTarget != null && previewTarget.name == "PreviewCube")
                {
                    UnityEngine.Object.DestroyImmediate(previewTarget);
                }
            }
        }

        private GameObject GetPreviewTarget(List<Object> targets)
        {
            if (targets != null && targets.Count > 0)
            {
                foreach (var target in targets)
                {
                    if (target is GameObject gameObject && gameObject != null)
                    {
                        return gameObject;
                    }
                }
            }
            
            // Если нет валидных целей, создаем куб
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "PreviewCube";
            return cube;
        }

        private void EnsurePresetsFolderExists()
        {
            if (EditorApplication.isPlaying) return;

            try
            {
                if (!Directory.Exists(_presetsFolder))
                {
                    Directory.CreateDirectory(_presetsFolder);
                    AssetDatabase.Refresh();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create presets folder: {e.Message}");
            }
        }

        public string GetCurrentPresetName()
        {
            return EditorPrefs.GetString("currentPresetName", "");
        }
    }
}