﻿﻿﻿using System.Collections.Generic;
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
        [SerializeField] private IconType iconType = IconType.Mesh;
        [SerializeField] private List<TextureObjectData> textureObjects = new List<TextureObjectData>();

        private IconCreatorService _iconCreator;
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
        private bool _showTextureObjectsSettings = false;

        private bool HasValidTargets => iconType == IconType.Mesh
            ? targets.ExtractAllGameObjects().Any(g => g.HasVisibleMesh())
            : textureObjects.Any(t => t.texture != null);

        private IconsCreatorData _data;
        private bool _previewNeedsUpdate = false;
        private bool _presetApplied = false;
        private PresetData _currentSettingsState;
        private bool _settingsChangedSinceLastPreview = false;
        private bool _firstPreviewUpdate = true;
        private List<Object> _previousTargets = new List<Object>();
        private bool _targetsChanged = false;
        private List<TextureObjectData> _previousTextureObjects = new List<TextureObjectData>();
        private bool _textureObjectsChanged = false;

        private SerializedObject _serializedObject;
        private SerializedProperty _targetsProperty;
        private SerializedProperty _textureObjectsProperty;

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
            if (!EditorApplication.isPlaying && !BuildPipeline.isBuildingPlayer)
                InitializeServices();

            _serializedObject = new SerializedObject(this);
            _targetsProperty = _serializedObject.FindProperty("targets");
            _textureObjectsProperty = _serializedObject.FindProperty("textureObjects");
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
                ref cameraTag, ref objectsLayer, ref iconType, ref textureObjects);

            // Создаём сервис здесь, а не в конструкторе окна —
            // иначе Unity сыпет CreateImpl-is-not-allowed и прочими native-ошибками.
            if (_iconCreator == null)
                _iconCreator = new IconCreatorService();

            _iconCreator.InitializeEnvironment(scenePath);
            _iconCreator.PreviewsUpdated += OnPreviewsUpdated;

            _presetManager = new PresetManager(presetsFolder);
            LoadCurrentPreset();

            EditorApplication.update += OnEditorUpdate;

            CreateCurrentSettingsState();
            _previousTargets = new List<Object>(targets);
            _previousTextureObjects = new List<TextureObjectData>(textureObjects);
            _previewNeedsUpdate = true;
            _firstPreviewUpdate = true;
        }

        private void CleanupResources()
        {
            SettingsStorage.SaveAll(directory, scenePath, presetsFolder,
                textureSettings, cameraSettings, lightSettings, shadowSettings,
                cameraTag, objectsLayer, iconType, textureObjects);

            if (_iconCreator != null)
            {
                _iconCreator.PreviewsUpdated -= OnPreviewsUpdated;
                _iconCreator.Dispose();
                _iconCreator = null;
            }

            _styleManager?.Dispose();
            _styleManager = null;

            PresetDrawer.ClearCache();

            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnPreviewsUpdated()
        {
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (this == null) return;
            if (_iconCreator == null) return;

            if (!hasFocus && !_iconCreator.IsGenerating)
                return;

            CheckTargetsChanged();

            if (_previewNeedsUpdate)
            {
                _previewNeedsUpdate = false;
                UpdateData();
                bool forceUpdate = _firstPreviewUpdate || _settingsChangedSinceLastPreview
                                || _targetsChanged || _textureObjectsChanged;
                _iconCreator.RequestPreviewUpdate(forceUpdate);

                if (_firstPreviewUpdate) _firstPreviewUpdate = false;
                if (_settingsChangedSinceLastPreview) _settingsChangedSinceLastPreview = false;
                if (_targetsChanged) _targetsChanged = false;
                if (_textureObjectsChanged) _textureObjectsChanged = false;
            }
        }

        /// <summary>
        /// Unity вызывает это ~10 раз в секунду. Используем для лёгкой перерисовки
        /// прогресс-бара и статуса, не нагружая OnGUI каждый кадр.
        /// </summary>
        private void OnInspectorUpdate()
        {
            if (_iconCreator == null) return;

            // Перерисовываем окно только если идёт генерация или превью-апдейт,
            // и только если окно в фокусе — иначе редактор начинает дёргаться.
            if (_iconCreator.IsGenerating && hasFocus)
            {
                Repaint();
            }
        }

        private void CheckTargetsChanged()
        {
            if (iconType == IconType.Mesh)
            {
                if (TargetsChanged(_previousTargets, targets))
                {
                    _targetsChanged = true;
                    _previousTargets = new List<Object>(targets);
                    // PresetDrawer.ClearCache() здесь НЕ нужен — превью пресетов
                    // не зависят от текущих targets окна. Их кэш живёт до смены папки/пресета.
                    _iconCreator.MarkPreviewDirty();
                    _previewNeedsUpdate = true;
                    Repaint();
                }
            }
            else
            {
                if (TextureObjectsChanged(_previousTextureObjects, textureObjects))
                {
                    _textureObjectsChanged = true;
                    _previousTextureObjects = new List<TextureObjectData>(textureObjects);
                    PresetDrawer.ClearCache();
                    _iconCreator.MarkPreviewDirty();
                    _previewNeedsUpdate = true;
                    Repaint();
                }
            }
        }

        private bool TargetsChanged(List<Object> oldTargets, List<Object> newTargets)
        {
            if (oldTargets.Count != newTargets.Count) return true;
            for (int i = 0; i < oldTargets.Count; i++)
                if (oldTargets[i] != newTargets[i]) return true;
            return false;
        }

        private bool TextureObjectsChanged(List<TextureObjectData> oldList, List<TextureObjectData> newList)
        {
            if (oldList.Count != newList.Count) return true;
            for (int i = 0; i < oldList.Count; i++)
            {
                var a = oldList[i];
                var b = newList[i];
                if (a.texture != b.texture ||
                    a.position != b.position ||
                    a.rotation != b.rotation ||
                    a.scale != b.scale ||
                    a.color != b.color) return true;
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

            // Если сервисы ещё не поднялись — ничего не рисуем, чтобы не сыпать NRE.
            if (_iconCreator == null || _presetManager == null)
                return;

            if (_styleManager == null)
                _styleManager = new EditorStyleManager();

            _styleManager.InitializeStyles();

            Color headerColor = new Color(0.2f, 0.6f, 1f);
            _styleManager.UpdateStyles(headerColor);

            _serializedObject.Update();
            _targetsProperty = _serializedObject.FindProperty("targets");
            _textureObjectsProperty = _serializedObject.FindProperty("textureObjects");

            using (GUILayout.ScrollViewScope scroll = new GUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;

                DrawHeader(headerColor);

                PresetDrawer.Draw(ref _showPresetSettings, _presetManager, ref presetsFolder, textureSettings, cameraSettings,
                    lightSettings, shadowSettings, objectsLayer, cameraTag, scenePath, _styleManager, headerColor, iconType, textureObjects);

                SpawnSettingsDrawer.Draw(ref _showSpawnSettings, ref directory, ref cameraTag, ref objectsLayer,
                    ref scenePath, textureSettings, cameraSettings, _styleManager, headerColor, ref iconType);

                if (iconType == IconType.Texture)
                    TextureObjectsSettingsDrawer.Draw(ref _showTextureObjectsSettings, _textureObjectsProperty, _styleManager, headerColor);
                else
                {
                    LightSettingsDrawer.Draw(ref _showLightSettings, lightSettings, _styleManager, headerColor);
                    ShadowSettingsDrawer.Draw(ref _showShadowSettings, shadowSettings, _styleManager, headerColor);
                }

                SpriteSettingsDrawer.Draw(ref _showSpriteSettings, textureSettings, _styleManager, headerColor);

                if (iconType == IconType.Mesh)
                    ObjectsSettingsDrawer.Draw(ref _showObjectsSettings, targets, _styleManager, _serializedObject, headerColor);

                if (HasValidTargets)
                    PreviewDrawer.Draw(ref _previewScrollPosition, _iconCreator.CameraPreviews, _data, HasValidTargets, iconType);

                ActionButtonsDrawer.Draw(targets, HasValidTargets, _iconCreator.IsGenerating, CreateIcons);
            }

            _serializedObject.ApplyModifiedProperties();

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
                    _presetApplied = false;

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
                iconType = iconType,
                textureObjects = CloneTextureObjects(textureObjects)
            };
        }

        private bool SettingsEqualsCurrentState()
        {
            if (_currentSettingsState == null) return false;

            if (iconType != _currentSettingsState.iconType) return false;

            if (iconType == IconType.Texture)
            {
                if (textureObjects.Count != _currentSettingsState.textureObjects.Length) return false;
                for (int i = 0; i < textureObjects.Count; i++)
                {
                    var a = textureObjects[i];
                    var b = _currentSettingsState.textureObjects[i];
                    if (a.texture != b.texture ||
                        a.position != b.position ||
                        a.rotation != b.rotation ||
                        a.scale != b.scale ||
                        a.color != b.color) return false;
                }
            }
            else
            {
                if (textureSettings.Size != _currentSettingsState.textureSettings.Size) return false;
                if (textureSettings.RenderSize != _currentSettingsState.textureSettings.RenderSize) return false;
                if (textureSettings.PreviewSize != _currentSettingsState.textureSettings.PreviewSize) return false;
                if (textureSettings.Compression != _currentSettingsState.textureSettings.Compression) return false;
                if (textureSettings.FilterMode != _currentSettingsState.textureSettings.FilterMode) return false;
                if (textureSettings.AnisoLevel != _currentSettingsState.textureSettings.AnisoLevel) return false;
                if (cameraSettings.Rotation != _currentSettingsState.cameraSettings.Rotation) return false;
                if (cameraSettings.Padding != _currentSettingsState.cameraSettings.Padding) return false;
                if (cameraSettings.RenderShadows != _currentSettingsState.cameraSettings.RenderShadows) return false;
                if (lightSettings.Type != _currentSettingsState.lightSettings.Type) return false;
                if (lightSettings.DirectionalRotation != _currentSettingsState.lightSettings.DirectionalRotation) return false;
                if (lightSettings.DirectionalColor != _currentSettingsState.lightSettings.DirectionalColor) return false;
                if (lightSettings.DirectionalIntensity != _currentSettingsState.lightSettings.DirectionalIntensity) return false;
                if (lightSettings.PointLights[0].Position != _currentSettingsState.lightSettings.PointLights[0].Position) return false;
                if (lightSettings.PointLights[0].Color != _currentSettingsState.lightSettings.PointLights[0].Color) return false;
                if (lightSettings.PointLights[0].Intensity != _currentSettingsState.lightSettings.PointLights[0].Intensity) return false;
                if (lightSettings.PointLights[1].Position != _currentSettingsState.lightSettings.PointLights[1].Position) return false;
                if (lightSettings.PointLights[1].Color != _currentSettingsState.lightSettings.PointLights[1].Color) return false;
                if (lightSettings.PointLights[1].Intensity != _currentSettingsState.lightSettings.PointLights[1].Intensity) return false;
                if (shadowSettings.Enabled != _currentSettingsState.shadowSettings.Enabled) return false;
                if (shadowSettings.Color != _currentSettingsState.shadowSettings.Color) return false;
                if (shadowSettings.Offset != _currentSettingsState.shadowSettings.Offset) return false;
                if (shadowSettings.Scale != _currentSettingsState.shadowSettings.Scale) return false;
            }

            return true;
        }

        private static TextureSettings CloneTextureSettings(TextureSettings original)
        {
            return new TextureSettings
            {
                Compression = original.Compression,
                FilterMode = original.FilterMode,
                AnisoLevel = original.AnisoLevel,
                Size = original.Size,
                RenderSize = original.RenderSize,
                PreviewSize = original.PreviewSize
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

        private TextureObjectData[] CloneTextureObjects(List<TextureObjectData> list)
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

            if (iconType == IconType.Mesh)
            {
                _data = new IconsCreatorData(textureSettings, cameraSettings, lightSettings, shadowSettings,
                    directory, targets, cameraTag, objectsLayer);
            }
            else
            {
                _data = new IconsCreatorData(textureSettings, cameraSettings, lightSettings, shadowSettings,
                    directory, textureObjects, cameraTag, objectsLayer);
            }
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

            iconType = preset.iconType;
            if (preset.textureObjects != null)
            {
                textureObjects.Clear();
                foreach (var obj in preset.textureObjects)
                {
                    textureObjects.Add(new TextureObjectData
                    {
                        texture = obj.texture,
                        position = obj.position,
                        rotation = obj.rotation,
                        scale = obj.scale,
                        color = obj.color
                    });
                }
            }

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