using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class PresetDrawer
    {
        #pragma warning disable UDR0001 // Domain Reload Analyzer
        private static Vector2 _presetScrollPosition;
        private static string _newPresetName = "Новый пресет";
        private static Dictionary<string, Texture2D> _previewCache = new Dictionary<string, Texture2D>();
        #pragma warning restore UDR0001 // Domain Reload Analyzer

        public static void Draw(ref bool showPresetSettings, PresetManager presetManager, 
            ref string presetsFolder, TextureSettings textureSettings, CameraSettings cameraSettings, 
            LightSettings lightSettings, ShadowSettings shadowSettings, string directory,
            List<Object> targets, bool showHelpBoxes, EditorStyleManager styleManager)
        {
            EditorGUILayout.BeginVertical("box");
            showPresetSettings = EditorGUILayout.Foldout(showPresetSettings, "💾 Управление пресетами", 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showPresetSettings)
            {
                EditorGUI.indentLevel++;
                DrawPresetsFolderField(ref presetsFolder, presetManager, showHelpBoxes, styleManager);
                EditorGUILayout.Space(10f);
                DrawPresetManagement(presetManager, textureSettings, cameraSettings, lightSettings, 
                    shadowSettings, directory, targets, showHelpBoxes, styleManager);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawPresetsFolderField(ref string presetsFolder, PresetManager presetManager, 
            bool showHelpBoxes, EditorStyleManager styleManager)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Папка пресетов", "Папка для сохранения и загрузки пресетов"), GUILayout.Width(120));
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

            if (showHelpBoxes)
                DisplaySettingsDrawer.DrawHelpBox("💡 <b>Папка должна находиться внутри Assets</b>", styleManager);
        }

        private static void DrawPresetManagement(PresetManager presetManager, 
            TextureSettings textureSettings, CameraSettings cameraSettings, 
            LightSettings lightSettings, ShadowSettings shadowSettings, string directory,
            List<Object> targets, bool showHelpBoxes, EditorStyleManager styleManager)
        {
            DrawSavePresetSection(presetManager, textureSettings, cameraSettings, 
                lightSettings, shadowSettings, directory);
            
            EditorGUILayout.Space(10f);
            DrawPresetsList(presetManager, targets, textureSettings, cameraSettings, 
                lightSettings, shadowSettings, directory);
            
            if (showHelpBoxes)
                DisplaySettingsDrawer.DrawHelpBox("💡 <b>Пресеты позволяют</b> сохранять и загружать настройки для быстрого доступа", styleManager);
        }

        private static void DrawSavePresetSection(PresetManager presetManager, 
            TextureSettings textureSettings, CameraSettings cameraSettings, 
            LightSettings lightSettings, ShadowSettings shadowSettings, string directory)
        {
            EditorGUILayout.LabelField("Сохранить текущие настройки", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _newPresetName = EditorGUILayout.TextField("Имя пресета", _newPresetName);
            
            GUI.enabled = !string.IsNullOrEmpty(_newPresetName);
            if (GUILayout.Button("Сохранить", GUILayout.Width(80)))
            {
                var preset = new PresetData
                {
                    presetName = _newPresetName,
                    textureSettings = CloneTextureSettings(textureSettings),
                    cameraSettings = CloneCameraSettings(cameraSettings),
                    lightSettings = CloneLightSettings(lightSettings),
                    shadowSettings = CloneShadowSettings(shadowSettings),
                    directory = directory
                };

                presetManager.SavePreset(preset);
                _newPresetName = "Новый пресет";
                ClearCache();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawPresetsList(PresetManager presetManager, List<Object> targets,
            TextureSettings textureSettings, CameraSettings cameraSettings,
            LightSettings lightSettings, ShadowSettings shadowSettings, string directory)
        {
            EditorGUILayout.LabelField("Сохраненные пресеты", EditorStyles.boldLabel);
            
            var presets = presetManager.LoadAllPresets();
            if (presets.Count == 0)
            {
                EditorGUILayout.HelpBox("Пресеты не найдены", MessageType.Info);
                return;
            }

            _presetScrollPosition = EditorGUILayout.BeginScrollView(_presetScrollPosition, 
                GUILayout.Height(Mathf.Min(presets.Count * 140, 400)));

            foreach (var preset in presets)
            {
                DrawPresetItem(preset, presetManager, targets, textureSettings, cameraSettings,
                    lightSettings, shadowSettings, directory);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawPresetItem(PresetData preset, PresetManager presetManager, List<Object> targets,
            TextureSettings textureSettings, CameraSettings cameraSettings,
            LightSettings lightSettings, ShadowSettings shadowSettings, string directory)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            
            // Генерируем или получаем превью из кэша
            if (!_previewCache.ContainsKey(preset.presetName))
            {
                _previewCache[preset.presetName] = presetManager.GeneratePreviewForPreset(preset, targets);
            }
            
            Texture2D preview = _previewCache[preset.presetName];
            
            // Левая часть: превью и основная информация
            EditorGUILayout.BeginVertical(GUILayout.Width(100));
            if (preview != null)
            {
                GUILayout.Box(preview, GUILayout.Width(80), GUILayout.Height(80));
            }
            else
            {
                GUILayout.Box("Нет превью", GUILayout.Width(80), GUILayout.Height(80));
            }
            EditorGUILayout.EndVertical();
            
            // Центральная часть: детальная информация о пресете
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            
            EditorGUILayout.LabelField(preset.presetName, EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Размер: {preset.textureSettings.Size}px", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField($"Сжатие: {preset.textureSettings.Compression}", EditorStyles.miniLabel, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Освещение: {preset.lightSettings.Type}", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField($"Тень: {(preset.shadowSettings.Enabled ? "Вкл" : "Выкл")}", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Фильтр: {preset.textureSettings.FilterMode}", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField($"Aniso: {preset.textureSettings.AnisoLevel}", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Камера: {preset.cameraSettings.Rotation.x:F0}°, {preset.cameraSettings.Rotation.y:F0}°", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            // Правая часть: кнопки действий (вертикально) - увеличенные
            EditorGUILayout.BeginVertical(GUILayout.Width(120));
            
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 25,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            
            if (GUILayout.Button("🔄 Загрузить", buttonStyle))
            {
                ApplyPreset(preset, textureSettings, cameraSettings, lightSettings, shadowSettings, directory);
            }
            
            if (GUILayout.Button("📷 Обновить превью", buttonStyle))
            {
                _previewCache.Remove(preset.presetName);
                RepaintAllWindows();
            }
            
            if (GUILayout.Button("🗑️ Удалить", buttonStyle))
            {
                if (EditorUtility.DisplayDialog("Удаление пресета", 
                    $"Вы уверены, что хотите удалить пресет '{preset.presetName}'?", "Да", "Нет"))
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

        private static void ApplyPreset(PresetData preset, TextureSettings textureSettings, 
            CameraSettings cameraSettings, LightSettings lightSettings, 
            ShadowSettings shadowSettings, string directory)
        {
            // Копируем данные из пресета в текущие настройки
            CopyTextureSettings(preset.textureSettings, textureSettings);
            CopyCameraSettings(preset.cameraSettings, cameraSettings);
            CopyLightSettings(preset.lightSettings, lightSettings);
            CopyShadowSettings(preset.shadowSettings, shadowSettings);
            directory = preset.directory;
            
            // Сохраняем имя текущего пресета
            EditorPrefs.SetString("currentPresetName", preset.presetName);
            
            RepaintAllWindows();
        }

        private static void RepaintAllWindows()
        {
            var windows = Resources.FindObjectsOfTypeAll<IconsCreatorWindow>();
            foreach (var window in windows)
            {
                window.Repaint();
            }
        }

        public static void ClearCache()
        {
            foreach (var texture in _previewCache.Values)
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

        private static void CopyTextureSettings(TextureSettings source, TextureSettings destination)
        {
            destination.Compression = source.Compression;
            destination.FilterMode = source.FilterMode;
            destination.AnisoLevel = source.AnisoLevel;
            destination.Size = source.Size;
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

        private static void CopyCameraSettings(CameraSettings source, CameraSettings destination)
        {
            destination.Rotation = source.Rotation;
            destination.Padding = source.Padding;
            destination.RenderShadows = source.RenderShadows;
        }

        private static LightSettings CloneLightSettings(LightSettings original)
        {
            var clone = new LightSettings
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

        private static void CopyLightSettings(LightSettings source, LightSettings destination)
        {
            destination.Type = source.Type;
            destination.DirectionalRotation = source.DirectionalRotation;
            destination.DirectionalColor = source.DirectionalColor;
            destination.DirectionalIntensity = source.DirectionalIntensity;

            for (int i = 0; i < source.PointLights.Length; i++)
            {
                destination.PointLights[i].Position = source.PointLights[i].Position;
                destination.PointLights[i].Color = source.PointLights[i].Color;
                destination.PointLights[i].Intensity = source.PointLights[i].Intensity;
            }
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

        private static void CopyShadowSettings(ShadowSettings source, ShadowSettings destination)
        {
            destination.Enabled = source.Enabled;
            destination.Color = source.Color;
            destination.Offset = source.Offset;
            destination.Scale = source.Scale;
        }
    }
}