using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    public static class PresetDrawer
    {
        private static Vector2 _presetScrollPosition;
        private static string _newPresetName = "Новый пресет";
        private static readonly Dictionary<string, Texture2D> _previewCache = new Dictionary<string, Texture2D>();

        public static void Draw(ref bool showPresetSettings, PresetManager presetManager,
            ref string presetsFolder, TextureSettings textureSettings, CameraSettings cameraSettings,
            LightSettings lightSettings, ShadowSettings shadowSettings,
            string objectsLayer, string cameraTag, string scenePath,
            EditorStyleManager styleManager, Color headerColor,
            IconType currentIconType, List<TextureObjectData> textureObjects)
        {
            EditorGUILayout.BeginVertical("box");
            string colorHex = ColorUtility.ToHtmlStringRGB(headerColor);
            string label = $"💾 <color=#{colorHex}>Пресеты</color>";
            showPresetSettings = EditorGUILayout.Foldout(showPresetSettings, label,
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);

            if (showPresetSettings)
            {
                EditorGUI.indentLevel++;
                DrawPresetsFolderField(ref presetsFolder, presetManager);
                EditorGUILayout.Space(10f);

                DrawSavePresetSection(presetManager, textureSettings, cameraSettings,
                    lightSettings, shadowSettings, currentIconType, textureObjects);

                EditorGUILayout.Space(10f);
                DrawPresetsList(presetManager, scenePath, objectsLayer, cameraTag);

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
                    presetsFolder = "Assets" + path[Application.dataPath.Length..];
                    presetManager.SetPresetsFolder(presetsFolder);
                    ClearCache();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSavePresetSection(PresetManager presetManager,
            TextureSettings textureSettings, CameraSettings cameraSettings,
            LightSettings lightSettings, ShadowSettings shadowSettings,
            IconType currentIconType, List<TextureObjectData> textureObjects)
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
                    iconType = currentIconType,
                    textureObjects = CloneTextureObjects(textureObjects)
                };

                presetManager.SavePreset(preset);
                _newPresetName = "Новый пресет";
                ClearCache();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawPresetsList(PresetManager presetManager, string scenePath,
            string objectsLayer, string cameraTag)
        {
            EditorGUILayout.LabelField("Сохраненные пресеты", EditorStyles.boldLabel);

            List<PresetData> presets = presetManager.LoadAllPresets();
            if (presets.Count == 0)
            {
                EditorGUILayout.HelpBox("Пресеты не найдены", MessageType.Info);
                return;
            }

            // Генерируем превью ТОЛЬКО один раз на пресет. Дальше — из кэша.
            // Кэш не зависит от текущих настроек окна и не сбрасывается при смене targets.
            foreach (PresetData preset in presets)
            {
                if (preset == null) continue;
                if (_previewCache.ContainsKey(preset.presetName)) continue;

                _previewCache[preset.presetName] = presetManager.GeneratePreviewForPreset(
                    preset, scenePath, objectsLayer, cameraTag);
            }

            _presetScrollPosition = EditorGUILayout.BeginScrollView(_presetScrollPosition,
                GUILayout.Height(Mathf.Min(presets.Count * 180, 500)));

            foreach (PresetData preset in presets)
            {
                if (preset == null) continue;
                DrawPresetItem(preset, presetManager);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawPresetItem(PresetData preset, PresetManager presetManager)
        {
            if (preset == null) return;
            if (preset.textureSettings == null || preset.cameraSettings == null ||
                preset.lightSettings == null || preset.shadowSettings == null)
            {
                EditorGUILayout.HelpBox($"Пресет '{preset.presetName}' повреждён", MessageType.Error);
                return;
            }

            const float PreviewSize = 160f;
            const float ButtonHeight = 26f;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            // ============================================================
            // Левая колонка: превью
            // ============================================================
            Rect previewRect = GUILayoutUtility.GetRect(
                PreviewSize, PreviewSize,
                GUILayout.Width(PreviewSize),
                GUILayout.Height(PreviewSize),
                GUILayout.ExpandWidth(false),
                GUILayout.ExpandHeight(false));

            EditorGUI.DrawRect(previewRect, new Color(0f, 0f, 0f, 0.25f));

            if (_previewCache.TryGetValue(preset.presetName, out var cached) && cached != null)
                GUI.DrawTexture(previewRect, cached, ScaleMode.ScaleToFit);
            else
                EditorGUI.LabelField(previewRect, "…", EditorStyles.centeredGreyMiniLabel);

            // Обводка превью для чёткости
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y, previewRect.width, 1f),
                new Color(1f, 1f, 1f, 0.15f));
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.yMax - 1f, previewRect.width, 1f),
                new Color(0f, 0f, 0f, 0.3f));

            GUILayout.Space(10f);

            // ============================================================
            // Правая колонка: инфо + кнопки
            // ============================================================
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // --- Заголовок ---
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(preset.presetName, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            string typeIcon = preset.iconType == IconType.Mesh ? "🧊 MESH" : "🖼️ TEX";
            EditorGUILayout.LabelField(typeIcon, EditorStyles.miniBoldLabel, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            // --- TEXTURE ---
            DrawCompactRow(new[]
            {
                ("Size", $"{preset.textureSettings.Size}px"),
                ("Render", $"{preset.textureSettings.RenderSize}px"),
                ("Preview", $"{preset.textureSettings.PreviewSize}px")
            });

            DrawCompactRow(new[]
            {
                ("Compress", preset.textureSettings.Compression.ToString()),
                ("Filter", preset.textureSettings.FilterMode.ToString()),
                ("Aniso", preset.textureSettings.AnisoLevel.ToString())
            });

            if (preset.iconType == IconType.Mesh)
            {
                // --- CAMERA ---
                Vector3 r = preset.cameraSettings.Rotation;
                DrawCompactRow(new[]
                {
                    ("Camera", $"({r.x:F0}°,{r.y:F0}°,{r.z:F0}°)"),
                    ("Padding", $"{preset.cameraSettings.Padding:F2}")
                });

                // --- LIGHT ---
                if (preset.lightSettings.Type == LightType.Directional)
                {
                    EditorGUILayout.BeginHorizontal();
                    DrawKvInline("Light", preset.lightSettings.Type.ToString(), 100);
                    EditorGUILayout.LabelField("Color", EditorStyles.miniLabel, GUILayout.Width(40));
                    EditorGUILayout.ColorField(GUIContent.none, preset.lightSettings.DirectionalColor,
                        false, false, false, GUILayout.Width(36));
                    DrawKvInline("⚡", $"{preset.lightSettings.DirectionalIntensity:F2}", 60);
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    DrawCompactRow(new[]
                    {
                        ("Light", preset.lightSettings.Type.ToString()),
                        ("Points", preset.lightSettings.PointLights.Length.ToString())
                    });
                }

                // --- SHADOWS ---
                EditorGUILayout.BeginHorizontal();
                DrawStatusPill("Scene shadows", preset.cameraSettings.RenderShadows);
                GUILayout.Space(10f);
                DrawStatusPill("Fake shadow", preset.shadowSettings.Enabled);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                int texCount = preset.textureObjects?.Length ?? 0;
                DrawCompactRow(new[] { ("Textures", texCount.ToString()) });
            }

            GUILayout.Space(4f);

            // --- Кнопки под инфой ---
            EditorGUILayout.BeginHorizontal();

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = ButtonHeight,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            if (GUILayout.Button("🔄 Загрузить", buttonStyle, GUILayout.Width(120)))
                ApplyPreset(preset);

            if (GUILayout.Button("🗑️ Удалить", buttonStyle, GUILayout.Width(110)))
            {
                if (EditorUtility.DisplayDialog("Удаление пресета",
                    $"Удалить пресет '{preset.presetName}'?", "Да", "Нет"))
                {
                    presetManager.DeletePreset(preset.presetName);
                    if (_previewCache.TryGetValue(preset.presetName, out var tex) && tex != null)
                        Object.DestroyImmediate(tex);
                    _previewCache.Remove(preset.presetName);
                    RepaintAllWindows();
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        // ============================================================
        // Helpers
        // ============================================================

        private static void DrawCompactRow((string key, string value)[] items)
        {
            EditorGUILayout.BeginHorizontal();
            foreach (var (key, value) in items)
                DrawKvInline(key, value, 0);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawKvInline(string key, string value, float width)
        {
            EditorGUILayout.LabelField(key, EditorStyles.miniLabel, GUILayout.Width(60));
            if (width > 0)
                EditorGUILayout.LabelField(value, EditorStyles.miniLabel, GUILayout.Width(width));
            else
                EditorGUILayout.LabelField(value, EditorStyles.miniLabel, GUILayout.Width(90));
            GUILayout.Space(8f);
        }

        private static void DrawStatusPill(string label, bool value)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(100));
            Color prev = GUI.color;
            GUI.color = value ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
            EditorGUILayout.LabelField(value ? "● ON" : "● OFF", EditorStyles.miniBoldLabel, GUILayout.Width(45));
            GUI.color = prev;
        }

        private static void ApplyPreset(PresetData preset)
        {
            if (preset == null) return;
            IconsCreatorWindow[] windows = Resources.FindObjectsOfTypeAll<IconsCreatorWindow>();
            foreach (IconsCreatorWindow window in windows)
                window.ApplyPreset(preset);
        }

        private static void RepaintAllWindows()
        {
            IconsCreatorWindow[] windows = Resources.FindObjectsOfTypeAll<IconsCreatorWindow>();
            foreach (IconsCreatorWindow window in windows)
                window.Repaint();
        }

        public static void ClearCache()
        {
            foreach (Texture2D texture in _previewCache.Values)
            {
                if (texture != null)
                    Object.DestroyImmediate(texture);
            }
            _previewCache.Clear();
        }

        // ============================================================
        // Clones
        // ============================================================

        private static TextureSettings CloneTextureSettings(TextureSettings original) => new TextureSettings
        {
            Compression = original.Compression,
            FilterMode = original.FilterMode,
            AnisoLevel = original.AnisoLevel,
            Size = original.Size,
            RenderSize = original.RenderSize,
            PreviewSize = original.PreviewSize
        };

        private static CameraSettings CloneCameraSettings(CameraSettings original) => new CameraSettings
        {
            Rotation = original.Rotation,
            Padding = original.Padding,
            RenderShadows = original.RenderShadows
        };

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

        private static ShadowSettings CloneShadowSettings(ShadowSettings original) => new ShadowSettings
        {
            Enabled = original.Enabled,
            Color = original.Color,
            Offset = original.Offset,
            Scale = original.Scale
        };

        private static TextureObjectData[] CloneTextureObjects(List<TextureObjectData> list)
        {
            if (list == null) return new TextureObjectData[0];
            TextureObjectData[] clone = new TextureObjectData[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                clone[i] = new TextureObjectData
                {
                    texture = list[i].texture,
                    position = list[i].position,
                    rotation = list[i].rotation,
                    scale = list[i].scale,
                    color = list[i].color
                };
            }
            return clone;
        }
    }
}