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
        [SerializeField] private string presetsFolder = "Assets/Plugins/Editor/IconsCreator/Presets";
        [SerializeField] private TextureSettings textureSettings = new TextureSettings();
        [SerializeField] private CameraSettings cameraSettings = new CameraSettings();
        [SerializeField] private LightSettings lightSettings = new LightSettings();
        [SerializeField] private ShadowSettings shadowSettings = new ShadowSettings();
        [SerializeField] private List<UnityEngine.Object> targets = new List<UnityEngine.Object>();
        [SerializeField] private string cameraTag = "EditorOnly";
        [SerializeField] private string objectsLayer = "TransparentFX";

        private readonly IconCreatorService _iconCreator = new IconCreatorService();
        private PresetManager _presetManager;
        private Vector2 _scrollPosition;
        private Vector2 _previewScrollPosition;
        
        private EditorStyleManager _styleManager;
        private bool _showSpawnSettings = true;
        private bool _showLightSettings = false;
        private bool _showShadowSettings = false;
        private bool _showSpriteSettings = false;
        private bool _showObjectsSettings = true;
        private bool _showPresetSettings = false;

        private bool HasValidTargets => targets.ExtractAllGameObjects().Where(g => g.HasVisibleMesh()).Any();
        private int TargetCount => targets.ExtractAllGameObjects().Count(g => g.HasVisibleMesh());
        private IconsCreatorData _data;
        private bool _previewNeedsUpdate = false;
        private bool _presetApplied = false;

        private PresetData _currentSettingsState;
        private bool _settingsChangedSinceLastPreview = false;
        private bool _firstPreviewUpdate = true;
        private List<UnityEngine.Object> _previousTargets = new List<UnityEngine.Object>();
        private bool _targetsChanged = false;

        [MenuItem("Neon Imperium/Создатель иконок")]
        private static void OpenWindow() 
        {
            IconsCreatorWindow window = GetWindow<IconsCreatorWindow>("Создатель иконок");
            window.minSize = new Vector2(400, 600);
        }

        private void OnEnable()
        {
            _styleManager = new EditorStyleManager();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            if (!EditorApplication.isPlaying)
            {
                InitializeServices();
            }
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            CleanupResources();
        }
        
        private void OnDestroy() 
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            CleanupResources();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    CleanupResources();
                    break;
                    
                case PlayModeStateChange.EnteredEditMode:
                    if (!EditorApplication.isPlaying)
                    {
                        InitializeServices();
                    }
                    break;
                    
                case PlayModeStateChange.EnteredPlayMode:
                    CleanupResources();
                    Repaint();
                    break;
            }
        }

        private void InitializeServices()
        {
            _iconCreator.InitializeEnvironment();
            LoadSettings();
            
            _presetManager = new PresetManager(presetsFolder);
            LoadCurrentPreset();
            
            EditorApplication.update += OnEditorUpdate;
            CreateCurrentSettingsState();
            _previousTargets = new List<UnityEngine.Object>(targets);
            _previewNeedsUpdate = true;
            _firstPreviewUpdate = true;
        }

        private void CleanupResources()
        {
            SaveSettings();
            _iconCreator.Dispose();
            _styleManager.Dispose();
            _styleManager = null;
            
            PresetDrawer.ClearCache();
            
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            CheckTargetsChanged();
            
            if (_previewNeedsUpdate)
            {
                _previewNeedsUpdate = false;
                UpdateData();
                bool forceUpdate = _firstPreviewUpdate || _settingsChangedSinceLastPreview || _targetsChanged;
                _iconCreator.RequestPreviewUpdate(forceUpdate);
                if (_firstPreviewUpdate) _firstPreviewUpdate = false;
                if (_settingsChangedSinceLastPreview) _settingsChangedSinceLastPreview = false;
                if (_targetsChanged) _targetsChanged = false;
            }
        }

        private void CheckTargetsChanged()
        {
            if (TargetsChanged(_previousTargets, targets))
            {
                _targetsChanged = true;
                _previousTargets = new List<UnityEngine.Object>(targets);
                PresetDrawer.ClearCache();
                _iconCreator.MarkPreviewDirty();
                _previewNeedsUpdate = true;
                Repaint();
            }
        }

        private bool TargetsChanged(List<UnityEngine.Object> oldTargets, List<UnityEngine.Object> newTargets)
        {
            if (oldTargets.Count != newTargets.Count) return true;
            
            for (int i = 0; i < oldTargets.Count; i++)
            {
                if (oldTargets[i] != newTargets[i]) return true;
            }
            
            return false;
        }

        private void LoadSettings()
        {
            directory = EditorPrefs.GetString(nameof(directory), "Assets/Icons/");
            presetsFolder = EditorPrefs.GetString("presetsFolder", "Assets/Starve Neon/Script/Extension/Editor/IconsCreator/Presets");
            textureSettings.Size = EditorPrefs.GetInt("textureSize", 512);
            cameraSettings.Padding = EditorPrefs.GetFloat("padding", 0.1f);
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
            
            cameraTag = EditorPrefs.GetString("cameraTag", "EditorOnly");
            objectsLayer = EditorPrefs.GetString("objectsLayer", "TransparentFX");
        }

        private void LoadCurrentPreset()
        {
            string currentPresetName = _presetManager.GetCurrentPresetName();
            if (!string.IsNullOrEmpty(currentPresetName))
            {
                PresetData preset = _presetManager.LoadPreset(currentPresetName);
                if (preset != null)
                {
                    ApplyPreset(preset);
                }
            }
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString(nameof(directory), directory);
            EditorPrefs.SetString("presetsFolder", presetsFolder);
            EditorPrefs.SetInt("textureSize", textureSettings.Size);
            EditorPrefs.SetFloat("padding", cameraSettings.Padding);
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
            
            EditorPrefs.SetString("cameraTag", cameraTag);
            EditorPrefs.SetString("objectsLayer", objectsLayer);
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
            if (EditorApplication.isPlaying)
            {
                DrawPlayModeBlockedMessage();
                return;
            }

            if (_styleManager == null) 
                _styleManager = new EditorStyleManager();

            if (_presetManager == null)
                _presetManager = new PresetManager(presetsFolder);

            _styleManager.InitializeStyles();
            _styleManager.UpdateStyles(new Color(0.2f, 0.6f, 1f));
            
            using (GUILayout.ScrollViewScope scroll = new GUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;
                
                DrawHeader();
                
                PresetDrawer.Draw(ref _showPresetSettings, _presetManager, ref presetsFolder, textureSettings, cameraSettings, 
                    lightSettings, shadowSettings, directory, targets, cameraTag, objectsLayer, _styleManager);
                    
                SpawnSettingsDrawer.Draw(ref _showSpawnSettings, ref directory, ref cameraTag, ref objectsLayer, 
                    textureSettings, cameraSettings, _styleManager);
                    
                LightSettingsDrawer.Draw(ref _showLightSettings, lightSettings, _styleManager);
                ShadowSettingsDrawer.Draw(ref _showShadowSettings, shadowSettings, _styleManager);
                SpriteSettingsDrawer.Draw(ref _showSpriteSettings, textureSettings, _styleManager);
                ObjectsSettingsDrawer.Draw(ref _showObjectsSettings, targets, _styleManager, new SerializedObject(this));
                
                if (HasValidTargets)
                {
                    PreviewDrawer.Draw(_previewScrollPosition, _iconCreator.CameraPreviews, 
                        _data, HasValidTargets, TargetCount, _styleManager);
                }
                
                SettingsDrawers.ActionButtonsDrawer.Draw(targets, HasValidTargets, _iconCreator.IsGenerating, CreateIcons);
            }

            if (GUI.changed && !_iconCreator.IsGenerating)
            {
                if (!SettingsEqualsCurrentState())
                {
                    _settingsChangedSinceLastPreview = true;
                    CreateCurrentSettingsState();
                    _previewNeedsUpdate = true;
                    _iconCreator.MarkPreviewDirty();
                }
                
                if (_presetApplied)
                {
                    _presetApplied = false;
                }
                
                UpdateData();
            }
        }

        private void CreateCurrentSettingsState()
        {
            _currentSettingsState = new PresetData
            {
                presetName = "CurrentSettingsState",
                textureSettings = CloneTextureSettings(textureSettings),
                cameraSettings = CloneCameraSettings(cameraSettings),
                lightSettings = CloneLightSettings(lightSettings),
                shadowSettings = CloneShadowSettings(shadowSettings),
                directory = directory,
                cameraTag = cameraTag,
                objectsLayer = objectsLayer
            };
        }

        private bool SettingsEqualsCurrentState()
        {
            if (_currentSettingsState == null) return false;
            
            return textureSettings.Size == _currentSettingsState.textureSettings.Size &&
                   textureSettings.Compression == _currentSettingsState.textureSettings.Compression &&
                   textureSettings.FilterMode == _currentSettingsState.textureSettings.FilterMode &&
                   textureSettings.AnisoLevel == _currentSettingsState.textureSettings.AnisoLevel &&
                   cameraSettings.Rotation == _currentSettingsState.cameraSettings.Rotation &&
                   cameraSettings.Padding == _currentSettingsState.cameraSettings.Padding &&
                   cameraSettings.RenderShadows == _currentSettingsState.cameraSettings.RenderShadows &&
                   lightSettings.Type == _currentSettingsState.lightSettings.Type &&
                   lightSettings.DirectionalRotation == _currentSettingsState.lightSettings.DirectionalRotation &&
                   lightSettings.DirectionalColor == _currentSettingsState.lightSettings.DirectionalColor &&
                   lightSettings.DirectionalIntensity == _currentSettingsState.lightSettings.DirectionalIntensity &&
                   lightSettings.PointLights[0].Position == _currentSettingsState.lightSettings.PointLights[0].Position &&
                   lightSettings.PointLights[0].Color == _currentSettingsState.lightSettings.PointLights[0].Color &&
                   lightSettings.PointLights[0].Intensity == _currentSettingsState.lightSettings.PointLights[0].Intensity &&
                   lightSettings.PointLights[1].Position == _currentSettingsState.lightSettings.PointLights[1].Position &&
                   lightSettings.PointLights[1].Color == _currentSettingsState.lightSettings.PointLights[1].Color &&
                   lightSettings.PointLights[1].Intensity == _currentSettingsState.lightSettings.PointLights[1].Intensity &&
                   shadowSettings.Enabled == _currentSettingsState.shadowSettings.Enabled &&
                   shadowSettings.Color == _currentSettingsState.shadowSettings.Color &&
                   shadowSettings.Offset == _currentSettingsState.shadowSettings.Offset &&
                   shadowSettings.Scale == _currentSettingsState.shadowSettings.Scale &&
                   directory == _currentSettingsState.directory &&
                   cameraTag == _currentSettingsState.cameraTag &&
                   objectsLayer == _currentSettingsState.objectsLayer;
        }

        private TextureSettings CloneTextureSettings(TextureSettings original)
        {
            return new TextureSettings
            {
                Compression = original.Compression,
                FilterMode = original.FilterMode,
                AnisoLevel = original.AnisoLevel,
                Size = original.Size
            };
        }

        private CameraSettings CloneCameraSettings(CameraSettings original)
        {
            return new CameraSettings
            {
                Rotation = original.Rotation,
                Padding = original.Padding,
                RenderShadows = original.RenderShadows
            };
        }

        private LightSettings CloneLightSettings(LightSettings original)
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

        private ShadowSettings CloneShadowSettings(ShadowSettings original)
        {
            return new ShadowSettings
            {
                Enabled = original.Enabled,
                Color = original.Color,
                Offset = original.Offset,
                Scale = original.Scale
            };
        }

        private void DrawPlayModeBlockedMessage()
        {
            EditorGUILayout.BeginVertical("box");
            
            GUIStyle warningStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow }
            };
            
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("🚫 Создатель иконок недоступен", warningStyle);
            EditorGUILayout.Space(10);
            
            GUIStyle messageStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            
            EditorGUILayout.LabelField("Инструмент отключен во время Play Mode.", messageStyle);
            
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
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10f);
        }

        private void UpdateData()
        {
            if (!HasValidTargets) return;
            
            _data = new IconsCreatorData(textureSettings, cameraSettings, lightSettings, shadowSettings, 
                directory, targets, cameraTag, objectsLayer);
            _iconCreator.SetData(_data);
        }

        private void CreateIcons()
        {
            if (!HasValidTargets) return;
            
            UpdateData();
            _iconCreator.CreateIconsAsync();
        }

        public void ApplyPreset(PresetData preset)
        {
            textureSettings.Compression = preset.textureSettings.Compression;
            textureSettings.FilterMode = preset.textureSettings.FilterMode;
            textureSettings.AnisoLevel = preset.textureSettings.AnisoLevel;
            textureSettings.Size = preset.textureSettings.Size;
            
            cameraSettings.Rotation = preset.cameraSettings.Rotation;
            cameraSettings.Padding = preset.cameraSettings.Padding;
            cameraSettings.RenderShadows = preset.cameraSettings.RenderShadows;
            
            lightSettings.Type = preset.lightSettings.Type;
            lightSettings.DirectionalRotation = preset.lightSettings.DirectionalRotation;
            lightSettings.DirectionalColor = preset.lightSettings.DirectionalColor;
            lightSettings.DirectionalIntensity = preset.lightSettings.DirectionalIntensity;

            for (int i = 0; i < preset.lightSettings.PointLights.Length; i++)
            {
                lightSettings.PointLights[i].Position = preset.lightSettings.PointLights[i].Position;
                lightSettings.PointLights[i].Color = preset.lightSettings.PointLights[i].Color;
                lightSettings.PointLights[i].Intensity = preset.lightSettings.PointLights[i].Intensity;
            }
            
            shadowSettings.Enabled = preset.shadowSettings.Enabled;
            shadowSettings.Color = preset.shadowSettings.Color;
            shadowSettings.Offset = preset.shadowSettings.Offset;
            shadowSettings.Scale = preset.shadowSettings.Scale;
            
            directory = preset.directory;
            cameraTag = preset.cameraTag;
            objectsLayer = preset.objectsLayer;
            
            EditorPrefs.SetString("currentPresetName", preset.presetName);
            
            _previewNeedsUpdate = true;
            _iconCreator.MarkPreviewDirty();
            _settingsChangedSinceLastPreview = true;
            CreateCurrentSettingsState();
            _presetApplied = true;
            Repaint();
        }
    }
}