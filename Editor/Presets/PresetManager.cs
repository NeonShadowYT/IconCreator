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

        /// <summary>
        /// Генерирует превью пресета в изолированной сцене иконок.
        /// Не использует объекты из текущей сцены пользователя — только куб-заглушку
        /// (для меш-пресетов) или собственные текстуры пресета (для текстурных).
        /// </summary>
        public Texture2D GeneratePreviewForPreset(PresetData preset, string scenePath,
            string objectsLayer, string cameraTag)
        {
            if (EditorApplication.isPlaying) return null;
            if (preset == null || preset.textureSettings == null) return null;

            // Страховка от старых/битых пресетов
            if (preset.textureSettings.PreviewSize <= 0 || preset.textureSettings.PreviewSize > 1024)
                preset.textureSettings.PreviewSize = 512;

            Texture2D preview = null;
            IconSceneService sceneService = new IconSceneService();
            IconCameraService cameraService = new IconCameraService();

            try
            {
                cameraService.Initialize(preset.textureSettings, preset.cameraSettings,
                    preset.shadowSettings, cameraTag);

                sceneService.BeginBatch(cameraTag, objectsLayer, scenePath, preset.lightSettings);
                try
                {
                    if (preset.iconType == IconType.Mesh)
                    {
                        GameObject previewTarget = CreatePreviewPrimitive(objectsLayer);
                        try
                        {
                            sceneService.ProcessMeshTargetInBatch(previewTarget, "PresetPreview",
                                preset.cameraSettings.RenderShadows, objectsLayer, container =>
                            {
                                if (container == null) return;
                                cameraService.SetupForTarget(container);
                                preview = cameraService.CaptureView(isPreview: true, applyShadow: true);
                            });
                        }
                        finally
                        {
                            if (previewTarget != null)
                                UnityEngine.Object.DestroyImmediate(previewTarget);
                        }
                    }
                    else
                    {
                        sceneService.ProcessTextureTargetsInBatch(preset.textureObjects, objectsLayer, () =>
                        {
                            cameraService.SetupForTexture(objectsLayer);
                            preview = cameraService.CaptureView(isPreview: true, applyShadow: true);
                        });
                    }
                }
                finally
                {
                    sceneService.EndBatch();
                }

                return preview;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to generate preview for preset {preset.presetName}: {ex.Message}");
                if (preview != null) UnityEngine.Object.DestroyImmediate(preview);
                return null;
            }
            finally
            {
                cameraService.Cleanup();
            }
        }

        private GameObject CreatePreviewPrimitive(string objectsLayer)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "PresetPreviewCube";
            cube.transform.position = Vector3.zero;
            cube.transform.rotation = Quaternion.identity;
            cube.transform.localScale = Vector3.one * 0.5f;

            int layer = LayerMask.NameToLayer(objectsLayer);
            if (layer == -1) layer = LayerMask.NameToLayer("Default");
            cube.layer = layer;

            Collider col = cube.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

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