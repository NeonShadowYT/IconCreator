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
        [SerializeField] private string scenePath = "Assets/Plugins/NeonImperium/IconCreator/Scenes/Icons_Creation.unity";
        [SerializeField] private string presetsFolder = "Assets/Plugins/NeonImperium/IconCreator/Presets";
        [SerializeField] private TextureSettings textureSettings = new TextureSettings();
        [SerializeField] private CameraSettings cameraSettings = new CameraSettings();
        [SerializeField] private LightSettings lightSettings = new LightSettings();
        [SerializeField] private ShadowSettings shadowSettings = new ShadowSettings();
        [SerializeField] private List<Object> targets = new List<Object>();
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

        private bool HasValidTargets => targets.ExtractAllGameObjects().Any(g => g.HasVisibleMesh());
        private IconsCreatorData _data;
        private bool _previewNeedsUpdate = false;
        private bool _presetApplied = false;

        private PresetData _currentSettingsState;
        private bool _settingsChangedSinceLastPreview = false;
        private bool _firstPreviewUpdate = true;
        private List<Object> _previousTargets = new List<Object>();
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
            #pragma warning disable UDR0004 // Domain Reload Analyzer
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            #pragma warning restore UDR0004 // Domain Reload Analyzer

            if (!EditorApplication.isPlaying && !BuildPipeline.isBuildingPlayer)
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
                    if (!EditorApplication.isPlaying && !BuildPipeline.isBuildingPlayer)
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
            SettingsStorage.LoadAll(ref directory, ref scenePath, ref presetsFolder,
                ref textureSettings, ref cameraSettings, ref lightSettings, ref shadowSettings,
                ref cameraTag, ref objectsLayer);

            _iconCreator.InitializeEnvironment(scenePath);

            _presetManager = new PresetManager(presetsFolder);
            LoadCurrentPreset();

            #pragma warning disable UDR0004 // Domain Reload Analyzer
            EditorApplication.update += OnEditorUpdate;
            #pragma warning restore UDR0004 // Domain Reload Analyzer
            CreateCurrentSettingsState();
            _previousTargets = new List<Object>(targets);
            _previewNeedsUpdate = true;
            _firstPreviewUpdate = true;
        }

        private void CleanupResources()
        {
            SettingsStorage.SaveAll(directory, scenePath, presetsFolder,
                textureSettings, cameraSettings, lightSettings, shadowSettings,
                cameraTag, objectsLayer);

            _iconCreator.Dispose();
            _styleManager?.Dispose();
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
                _previousTargets = new List<Object>(targets);
                PresetDrawer.ClearCache();
                _iconCreator.MarkPreviewDirty();
                _previewNeedsUpdate = true;
                Repaint();
            }
        }

        private bool TargetsChanged(List<Object> oldTargets, List<Object> newTargets)
        {
            if (oldTargets.Count != newTargets.Count) return true;

            for (int i = 0; i < oldTargets.Count; i++)
            {
                if (oldTargets[i] != newTargets[i]) return true;
            }

            return false;
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
            Color headerColor = new Color(0.2f, 0.6f, 1f);
            _styleManager.UpdateStyles(headerColor);

            using (GUILayout.ScrollViewScope scroll = new GUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;

                DrawHeader(headerColor);

                PresetDrawer.Draw(ref _showPresetSettings, _presetManager, ref presetsFolder, textureSettings, cameraSettings,
                    lightSettings, shadowSettings, targets, scenePath, _styleManager, headerColor);

                SpawnSettingsDrawer.Draw(ref _showSpawnSettings, ref directory, ref cameraTag, ref objectsLayer,
                    ref scenePath, textureSettings, cameraSettings, _styleManager, headerColor);

                LightSettingsDrawer.Draw(ref _showLightSettings, lightSettings, _styleManager, headerColor);
                ShadowSettingsDrawer.Draw(ref _showShadowSettings, shadowSettings, _styleManager, headerColor);
                SpriteSettingsDrawer.Draw(ref _showSpriteSettings, textureSettings, _styleManager, headerColor);
                ObjectsSettingsDrawer.Draw(ref _showObjectsSettings, targets, _styleManager, new SerializedObject(this), headerColor);

                if (HasValidTargets)
                {
                    PreviewDrawer.Draw(_previewScrollPosition, _iconCreator.CameraPreviews, _data, HasValidTargets);
                }

                ActionButtonsDrawer.Draw(targets, HasValidTargets, _iconCreator.IsGenerating, CreateIcons);
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
                shadowSettings = CloneShadowSettings(shadowSettings)
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
                   shadowSettings.Scale == _currentSettingsState.shadowSettings.Scale;
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

        private void DrawHeader(Color headerColor)
        {
            EditorGUILayout.BeginVertical("box");
            string colorHex = ColorUtility.ToHtmlStringRGB(headerColor);
            string headerText = $"🖼️ <color=#{colorHex}>Создатель иконок</color>";
            EditorGUILayout.LabelField(headerText, _styleManager?.HeaderStyle ?? EditorStyles.boldLabel);
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