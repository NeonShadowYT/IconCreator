using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    public static class PresetDrawer
    {
        #pragma warning disable UDR0001 // Domain Reload Analyzer
        private static Vector2 _presetScrollPosition;
        private static string _newPresetName = "Новый пресет";
        #pragma warning restore UDR0001 // Domain Reload Analyzer
        private static readonly Dictionary<string, Texture2D> _previewCache = new Dictionary<string, Texture2D>();

        public static void Draw(ref bool showPresetSettings, PresetManager presetManager, 
            ref string presetsFolder, TextureSettings textureSettings, CameraSettings cameraSettings, 
            LightSettings lightSettings, ShadowSettings shadowSettings, string directory,
            List<Object> targets, string cameraTag, string objectsLayer, string scenePath, EditorStyleManager styleManager)
        {
            EditorGUILayout.BeginVertical("box");
            showPresetSettings = EditorGUILayout.Foldout(showPresetSettings, "💾 Пресеты", 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showPresetSettings)
            {
                EditorGUI.indentLevel++;
                DrawPresetsFolderField(ref presetsFolder, presetManager);
                EditorGUILayout.Space(10f);
                DrawPresetManagement(presetManager, textureSettings, cameraSettings, lightSettings, 
                    shadowSettings, directory, cameraTag, objectsLayer, targets, scenePath);
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawPresetsFolderField(ref string presetsFolder, PresetManager presetManager)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Папка пресетов", GUILayout.Width(120));
            string newFolder = EditorGUILayout.TextField(presetsFolder);
            if (newFolder != presetsFolder)
            {
                presetsFolder = newFolder;
                presetManager.SetPresetsFolder(presetsFolder);
                ClearCache();
            }
            if (GUILayout.Button("Обзор", GUILayout.Width(60)))
            {
                string path = EditorUtility.SaveFolderPanel("Выберите папку для пресетов", "Assets", "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    presetsFolder = "Assets" + path.Substring(Application.dataPath.Length);
                    presetManager.SetPresetsFolder(presetsFolder);
                    ClearCache();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawPresetManagement(PresetManager presetManager, 
            TextureSettings textureSettings, CameraSettings cameraSettings, 
            LightSettings lightSettings, ShadowSettings shadowSettings, string directory,
            string cameraTag, string objectsLayer, List<Object> targets, string scenePath)
        {
            DrawSavePresetSection(presetManager, textureSettings, cameraSettings, 
                lightSettings, shadowSettings, directory, cameraTag, objectsLayer);
            
            EditorGUILayout.Space(10f);
            DrawPresetsList(presetManager, targets, scenePath);
        }

        private static void DrawSavePresetSection(PresetManager presetManager, 
            TextureSettings textureSettings, CameraSettings cameraSettings, 
            LightSettings lightSettings, ShadowSettings shadowSettings, string directory,
            string cameraTag, string objectsLayer)
        {
            EditorGUILayout.LabelField("Сохранить текущие настройки", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _newPresetName = EditorGUILayout.TextField("Имя пресета", _newPresetName);
            
            GUI.enabled = !string.IsNullOrEmpty(_newPresetName);
            if (GUILayout.Button("💾 Сохранить", GUILayout.Width(100)))
            {
                PresetData preset = new PresetData
                {
                    presetName = _newPresetName,
                    textureSettings = CloneTextureSettings(textureSettings),
                    cameraSettings = CloneCameraSettings(cameraSettings),
                    lightSettings = CloneLightSettings(lightSettings),
                    shadowSettings = CloneShadowSettings(shadowSettings),
                    directory = directory,
                    cameraTag = cameraTag,
                    objectsLayer = objectsLayer
                };

                presetManager.SavePreset(preset);
                _newPresetName = "Новый пресет";
                ClearCache();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawPresetsList(PresetManager presetManager, List<Object> targets, string scenePath)
        {
            EditorGUILayout.LabelField("Сохраненные пресеты", EditorStyles.boldLabel);
            
            List<PresetData> presets = presetManager.LoadAllPresets();
            if (presets.Count == 0)
            {
                EditorGUILayout.HelpBox("Пресеты не найдены", MessageType.Info);
                return;
            }

            foreach (PresetData preset in presets)
            {
                if (!_previewCache.ContainsKey(preset.presetName))
                {
                    _previewCache[preset.presetName] = presetManager.GeneratePreviewForPreset(preset, targets, scenePath);
                }
            }

            _presetScrollPosition = EditorGUILayout.BeginScrollView(_presetScrollPosition, 
                GUILayout.Height(Mathf.Min(presets.Count * 140, 400)));

            foreach (PresetData preset in presets)
            {
                DrawPresetItem(preset, presetManager, targets, scenePath);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawPresetItem(PresetData preset, PresetManager presetManager, List<Object> targets, string scenePath)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.BeginVertical(GUILayout.Width(100));
            Texture2D preview = _previewCache.ContainsKey(preset.presetName) ? _previewCache[preset.presetName] : null;
            if (preview != null)
            {
                GUILayout.Box(preview, GUILayout.Width(80), GUILayout.Height(80));
            }
            else
            {
                GUILayout.Box("", GUILayout.Width(80), GUILayout.Height(80));
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            
            EditorGUILayout.LabelField(preset.presetName, EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📏 {preset.textureSettings.Size}px", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField($"📦 {preset.textureSettings.Compression}", EditorStyles.miniLabel, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"💡 {preset.lightSettings.Type}", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField($"👥 {(preset.shadowSettings.Enabled ? "Вкл" : "Выкл")}", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"🏷️ {preset.cameraTag}", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField($"📊 {preset.objectsLayer}", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical(GUILayout.Width(120));
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 25,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            
            if (GUILayout.Button("🔄 Загрузить", buttonStyle))
            {
                ApplyPreset(preset, presetManager);
            }
            
            if (GUILayout.Button("🗑️ Удалить", buttonStyle))
            {
                if (EditorUtility.DisplayDialog("Удаление пресета", 
                    $"Удалить пресет '{preset.presetName}'?", "Да", "Нет"))
                {
                    presetManager.DeletePreset(preset.presetName);
                    _previewCache.Remove(preset.presetName);
                    RepaintAllWindows();
                }
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5f);
        }

        private static void ApplyPreset(PresetData preset, PresetManager presetManager)
        {
            IconsCreatorWindow[] windows = Resources.FindObjectsOfTypeAll<IconsCreatorWindow>();
            foreach (IconsCreatorWindow window in windows)
            {
                window.ApplyPreset(preset);
            }
        }

        private static void RepaintAllWindows()
        {
            IconsCreatorWindow[] windows = Resources.FindObjectsOfTypeAll<IconsCreatorWindow>();
            foreach (IconsCreatorWindow window in windows)
            {
                window.Repaint();
            }
        }

        public static void ClearCache()
        {
            foreach (Texture2D texture in _previewCache.Values)
            {
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
            }
            _previewCache.Clear();
        }

        private static TextureSettings CloneTextureSettings(TextureSettings original)
        {
            return new TextureSettings
            {
                Compression = original.Compression,
                FilterMode = original.FilterMode,
                AnisoLevel = original.AnisoLevel,
                Size = original.Size
            };
        }

        private static CameraSettings CloneCameraSettings(CameraSettings original)
        {
            return new CameraSettings
            {
                Rotation = original.Rotation,
                Padding = original.Padding,
                RenderShadows = original.RenderShadows
            };
        }

        private static LightSettings CloneLightSettings(LightSettings original)
        {
            LightSettings clone = new LightSettings
            {
                Type = original.Type,
                DirectionalRotation = original.DirectionalRotation,
                DirectionalColor = original.DirectionalColor,
                DirectionalIntensity = original.DirectionalIntensity
            };

            for (int i = 0; i < original.PointLights.Length; i++)
            {
                clone.PointLights[i].Position = original.PointLights[i].Position;
                clone.PointLights[i].Color = original.PointLights[i].Color;
                clone.PointLights[i].Intensity = original.PointLights[i].Intensity;
            }

            return clone;
        }

        private static ShadowSettings CloneShadowSettings(ShadowSettings original)
        {
            return new ShadowSettings
            {
                Enabled = original.Enabled,
                Color = original.Color,
                Offset = original.Offset,
                Scale = original.Scale
            };
        }
    }
}