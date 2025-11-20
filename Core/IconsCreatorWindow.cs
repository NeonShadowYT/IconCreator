﻿using System.Collections.Generic;
using System.Linq;
using NeonImperium.IconsCreation.Extensions;
using NeonImperium.IconsCreation.SettingsDrawers;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    public class IconsCreatorWindow : EditorWindow
    {   
        [SerializeField] private string directory = "Assets/Icons/";
        [SerializeField] private string presetsFolder = "Assets/Starve Neon/Script/Extension/Editor/IconsCreator/Presets";
        [SerializeField] private TextureSettings textureSettings = new();
        [SerializeField] private CameraSettings cameraSettings = new();
        [SerializeField] private LightSettings lightSettings = new();
        [SerializeField] private ShadowSettings shadowSettings = new();
        [SerializeField] private List<Object> targets = new();

        private readonly IconCreatorService _iconCreator = new();
        private PresetManager _presetManager;
        private Vector2 _scrollPosition;
        private Vector2 _previewScrollPosition;
        
        private EditorStyleManager _styleManager;
        private bool _showHelpBoxes = false;
        private bool _showSpawnSettings = true;
        private bool _showLightSettings = true;
        private bool _showShadowSettings = false;
        private bool _showSpriteSettings = false;
        private bool _showObjectsSettings = true;
        private bool _showPreview = true;
        private bool _showPresetSettings = true;

        private bool HasValidTargets => targets.ExtractAllGameObjects().Where(g => g.HasVisibleMesh()).Any();
        private int TargetCount => targets.ExtractAllGameObjects().Count(g => g.HasVisibleMesh());

        [MenuItem("Neon Imperium/Создатель иконок")]
        private static void OpenWindow() 
        {
            var window = GetWindow<IconsCreatorWindow>("Создатель иконок");
            window.minSize = new Vector2(400, 600);
        }

        private void OnEnable()
        {
            _styleManager = new EditorStyleManager();
            
            // Подписываемся на события изменения режима воспроизведения
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // Инициализируем только если не в Play Mode
            if (!EditorApplication.isPlaying)
            {
                InitializeServices();
            }
        }

        private void OnDisable()
        {
            // Отписываемся от событий
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            // Очищаем ресурсы
            CleanupResources();
        }
        
        private void OnDestroy() 
        {
            // Отписываемся от событий
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
            // Очищаем ресурсы
            CleanupResources();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    // При выходе из Edit Mode очищаем ресурсы
                    CleanupResources();
                    break;
                    
                case PlayModeStateChange.EnteredEditMode:
                    // При возвращении в Edit Mode инициализируем сервисы
                    if (!EditorApplication.isPlaying)
                    {
                        InitializeServices();
                    }
                    break;
                    
                case PlayModeStateChange.EnteredPlayMode:
                    // При входе в Play Mode очищаем ресурсы
                    CleanupResources();
                    Repaint();
                    break;
            }
        }

        private void InitializeServices()
        {
            try
            {
                _iconCreator.InitializeEnvironment();
                LoadSettings();
                
                // Инициализируем PresetManager после загрузки настроек
                _presetManager = new PresetManager(_iconCreator, presetsFolder);
                
                // Загружаем последний использованный пресет если есть
                LoadCurrentPreset();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to initialize Icons Creator: {e.Message}");
            }
        }

        private void CleanupResources()
        {
            try
            {
                SaveSettings();
                _iconCreator?.Dispose();
                _styleManager?.Dispose();
                _styleManager = null;
                
                // Очищаем кэш превью
                PresetDrawer.ClearCache();
                
                // Очищаем превью иконок
                if (_iconCreator?.CameraPreviews != null)
                {
                    foreach (var preview in _iconCreator.CameraPreviews)
                    {
                        if (preview != null)
                            UnityEngine.Object.DestroyImmediate(preview);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during cleanup: {e.Message}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                directory = EditorPrefs.GetString(nameof(directory), "Assets/Icons/");
                presetsFolder = EditorPrefs.GetString("presetsFolder", "Assets/Starve Neon/Script/Extension/Editor/IconsCreator/Presets");
                textureSettings.Size = EditorPrefs.GetInt("textureSize", 512);
                cameraSettings.Padding = EditorPrefs.GetFloat("padding", 0.1f);
                _showHelpBoxes = EditorPrefs.GetBool("showHelpBoxes", false);
                textureSettings.Compression = (TextureImporterCompression)EditorPrefs.GetInt("compression", (int)TextureImporterCompression.CompressedHQ);
                textureSettings.FilterMode = (FilterMode)EditorPrefs.GetInt("filterMode", (int)FilterMode.Point);
                textureSettings.AnisoLevel = EditorPrefs.GetInt("anisoLevel", 0);
                
                cameraSettings.Rotation = LoadVector3("cameraRotation", new Vector3(45f, -45f, 0f));
                
                lightSettings.Type = (LightType)EditorPrefs.GetInt("lightType", (int)LightType.Directional);
                lightSettings.DirectionalRotation = LoadVector3("directionalRotation", new Vector3(50f, -30f, 0f));
                lightSettings.DirectionalColor = LoadColor("directionalColor", Color.white);
                lightSettings.DirectionalIntensity = EditorPrefs.GetFloat("directionalIntensity", 1f);
                
                for (int i = 0; i < lightSettings.PointLights.Length; i++)
                {
                    lightSettings.PointLights[i].Position = LoadVector3($"pointLight{i}Position", new Vector3(1, 0.5f, -0.5f));
                    lightSettings.PointLights[i].Color = LoadColor($"pointLight{i}Color", Color.white);
                    lightSettings.PointLights[i].Intensity = EditorPrefs.GetFloat($"pointLight{i}Intensity", 1f);
                }
                
                shadowSettings.Enabled = EditorPrefs.GetBool("shadowEnabled", false);
                shadowSettings.Color = LoadColor("shadowColor", new Color(0f, 0f, 0f, 0.5f));
                shadowSettings.Offset = LoadVector2("shadowOffset", new Vector2(0.05f, -0.05f));
                shadowSettings.Scale = EditorPrefs.GetFloat("shadowScale", 0.95f);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load settings: {e.Message}");
            }
        }

        private void LoadCurrentPreset()
        {
            try
            {
                string currentPresetName = _presetManager?.GetCurrentPresetName();
                if (!string.IsNullOrEmpty(currentPresetName))
                {
                    var preset = _presetManager.LoadPreset(currentPresetName);
                    if (preset != null)
                    {
                        ApplyPreset(preset);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load current preset: {e.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                EditorPrefs.SetString(nameof(directory), directory);
                EditorPrefs.SetString("presetsFolder", presetsFolder);
                EditorPrefs.SetInt("textureSize", textureSettings.Size);
                EditorPrefs.SetFloat("padding", cameraSettings.Padding);
                EditorPrefs.SetBool("showHelpBoxes", _showHelpBoxes);
                EditorPrefs.SetInt("compression", (int)textureSettings.Compression);
                EditorPrefs.SetInt("filterMode", (int)textureSettings.FilterMode);
                EditorPrefs.SetInt("anisoLevel", textureSettings.AnisoLevel);
                
                SaveVector3("cameraRotation", cameraSettings.Rotation);
                
                EditorPrefs.SetInt("lightType", (int)lightSettings.Type);
                SaveVector3("directionalRotation", lightSettings.DirectionalRotation);
                SaveColor("directionalColor", lightSettings.DirectionalColor);
                EditorPrefs.SetFloat("directionalIntensity", lightSettings.DirectionalIntensity);
                
                for (int i = 0; i < lightSettings.PointLights.Length; i++)
                {
                    SaveVector3($"pointLight{i}Position", lightSettings.PointLights[i].Position);
                    SaveColor($"pointLight{i}Color", lightSettings.PointLights[i].Color);
                    EditorPrefs.SetFloat($"pointLight{i}Intensity", lightSettings.PointLights[i].Intensity);
                }
                
                EditorPrefs.SetBool("shadowEnabled", shadowSettings.Enabled);
                SaveColor("shadowColor", shadowSettings.Color);
                SaveVector2("shadowOffset", shadowSettings.Offset);
                EditorPrefs.SetFloat("shadowScale", shadowSettings.Scale);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save settings: {e.Message}");
            }
        }

        private Color LoadColor(string key, Color defaultValue)
        {
            return new Color(
                EditorPrefs.GetFloat($"{key}_r", defaultValue.r),
                EditorPrefs.GetFloat($"{key}_g", defaultValue.g),
                EditorPrefs.GetFloat($"{key}_b", defaultValue.b),
                EditorPrefs.GetFloat($"{key}_a", defaultValue.a)
            );
        }

        private void SaveColor(string key, Color color)
        {
            EditorPrefs.SetFloat($"{key}_r", color.r);
            EditorPrefs.SetFloat($"{key}_g", color.g);
            EditorPrefs.SetFloat($"{key}_b", color.b);
            EditorPrefs.SetFloat($"{key}_a", color.a);
        }

        private Vector2 LoadVector2(string key, Vector2 defaultValue)
        {
            return new Vector2(
                EditorPrefs.GetFloat($"{key}_x", defaultValue.x),
                EditorPrefs.GetFloat($"{key}_y", defaultValue.y)
            );
        }

        private void SaveVector2(string key, Vector2 vector)
        {
            EditorPrefs.SetFloat($"{key}_x", vector.x);
            EditorPrefs.SetFloat($"{key}_y", vector.y);
        }

        private Vector3 LoadVector3(string key, Vector3 defaultValue)
        {
            return new Vector3(
                EditorPrefs.GetFloat($"{key}_x", defaultValue.x),
                EditorPrefs.GetFloat($"{key}_y", defaultValue.y),
                EditorPrefs.GetFloat($"{key}_z", defaultValue.z)
            );
        }

        private void SaveVector3(string key, Vector3 vector)
        {
            EditorPrefs.SetFloat($"{key}_x", vector.x);
            EditorPrefs.SetFloat($"{key}_y", vector.y);
            EditorPrefs.SetFloat($"{key}_z", vector.z);
        }

        private void OnGUI()
        {
            // Блокировка интерфейса в Play Mode
            if (EditorApplication.isPlaying)
            {
                DrawPlayModeBlockedMessage();
                return;
            }

            // Проверка инициализации сервисов
            if (_styleManager == null) 
                _styleManager = new EditorStyleManager();

            if (_presetManager == null)
                _presetManager = new PresetManager(_iconCreator, presetsFolder);

            try
            {
                _styleManager.InitializeStyles();
                _styleManager.UpdateStyles(new Color(0.2f, 0.6f, 1f));
                
                using (var scroll = new GUILayout.ScrollViewScope(_scrollPosition))
                {
                    _scrollPosition = scroll.scrollPosition;
                    
                    DrawHeader();
                    DisplaySettingsDrawer.Draw(ref _showHelpBoxes, _styleManager);
                    PresetDrawer.Draw(ref _showPresetSettings, _presetManager, ref presetsFolder, textureSettings, cameraSettings, 
                        lightSettings, shadowSettings, directory, targets, _showHelpBoxes, _styleManager);
                    SpawnSettingsDrawer.Draw(ref _showSpawnSettings, ref directory, textureSettings, cameraSettings, _showHelpBoxes, _styleManager);
                    LightSettingsDrawer.Draw(ref _showLightSettings, lightSettings, _showHelpBoxes, _styleManager);
                    ShadowSettingsDrawer.Draw(ref _showShadowSettings, shadowSettings, _showHelpBoxes, _styleManager);
                    SpriteSettingsDrawer.Draw(ref _showSpriteSettings, textureSettings, _showHelpBoxes, _styleManager);
                    ObjectsSettingsDrawer.Draw(ref _showObjectsSettings, targets, _showHelpBoxes, _styleManager, new SerializedObject(this));
                    PreviewDrawer.Draw(ref _showPreview, ref _previewScrollPosition, _iconCreator.CameraPreviews, _data, HasValidTargets, TargetCount, _showHelpBoxes, UpdateIconCreator, _styleManager);
                    SettingsDrawers.ActionButtonsDrawer.Draw(targets, directory, HasValidTargets, CreateIcons, UpdateIconCreator);
                }

                if (GUI.changed) 
                    UpdateIconCreator();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in Icons Creator GUI: {e.Message}");
                EditorGUILayout.HelpBox($"Произошла ошибка: {e.Message}", MessageType.Error);
            }
        }

        private void DrawPlayModeBlockedMessage()
        {
            EditorGUILayout.BeginVertical("box");
            
            var warningStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow }
            };
            
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("🚫 Создатель иконок недоступен", warningStyle);
            EditorGUILayout.Space(10);
            
            var messageStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            
            EditorGUILayout.LabelField("Инструмент отключен во время Play Mode для предотвращения конфликтов со сценой.", messageStyle);
            EditorGUILayout.LabelField("Пожалуйста, выйдите из Play Mode для использования создателя иконок.", messageStyle);
            
            EditorGUILayout.Space(20);
            
            if (GUILayout.Button("Выйти из Play Mode", GUILayout.Height(30)))
            {
                EditorApplication.isPlaying = false;
            }
            
            EditorGUILayout.Space(20);
            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🖼️ Создатель иконок", _styleManager?.HeaderStyle ?? EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Профессиональный инструмент для создания иконок", _styleManager?.CenteredLabelStyle ?? EditorStyles.label);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10f);
        }

        private IconsCreatorData _data;

        private void UpdateIconCreator()
        {
            if (!HasValidTargets) return;

            try
            {
                _data = new IconsCreatorData(textureSettings, cameraSettings, lightSettings, shadowSettings, directory, targets);
                _iconCreator.SetData(_data);
                Repaint();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to update icon creator: {e.Message}");
            }
        }

        private void CreateIcons()
        {
            if (!HasValidTargets) return;
            
            try
            {
                UpdateIconCreator();
                _iconCreator.CreateIcons();
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("Иконки созданы", 
                    $"Успешно создано {_data.Targets.Length} иконок в папке {directory}", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create icons: {e.Message}");
                EditorUtility.DisplayDialog("Ошибка", 
                    $"Не удалось создать иконки: {e.Message}", "OK");
            }
        }

        public void ApplyPreset(PresetData preset)
        {
            try
            {
                // Копируем данные из пресета в текущие настройки
                CopyTextureSettings(preset.textureSettings, textureSettings);
                CopyCameraSettings(preset.cameraSettings, cameraSettings);
                CopyLightSettings(preset.lightSettings, lightSettings);
                CopyShadowSettings(preset.shadowSettings, shadowSettings);
                directory = preset.directory;
                
                // Сохраняем имя текущего пресета
                EditorPrefs.SetString("currentPresetName", preset.presetName);
                
                UpdateIconCreator();
                Repaint();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to apply preset: {e.Message}");
            }
        }

        private void CopyTextureSettings(TextureSettings source, TextureSettings destination)
        {
            destination.Compression = source.Compression;
            destination.FilterMode = source.FilterMode;
            destination.AnisoLevel = source.AnisoLevel;
            destination.Size = source.Size;
        }

        private void CopyCameraSettings(CameraSettings source, CameraSettings destination)
        {
            destination.Rotation = source.Rotation;
            destination.Padding = source.Padding;
            destination.RenderShadows = source.RenderShadows;
        }

        private void CopyLightSettings(LightSettings source, LightSettings destination)
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

        private void CopyShadowSettings(ShadowSettings source, ShadowSettings destination)
        {
            destination.Enabled = source.Enabled;
            destination.Color = source.Color;
            destination.Offset = source.Offset;
            destination.Scale = source.Scale;
        }
    }
}