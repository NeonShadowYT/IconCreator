using System.Collections.Generic;
using System.Linq;
using NeonImperium.IconsCreation.Extensions;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    public class IconsCreatorWindow : EditorWindow
    {   
        [SerializeField] private string directory = "Assets/Icons/";
        [SerializeField] private TextureSettings textureSettings = new();
        [SerializeField] private CameraSettings cameraSettings = new();
        [SerializeField] private ShadowSettings shadowSettings = new();
        [SerializeField] private List<Object> targets = new();

        private const int PREVIEW_SIZE = 150;
        private const int MAX_PREVIEWS_PER_ROW = 3;
        private readonly IconCreatorService _iconCreator = new();
        private Vector2 _scrollPosition;
        private Vector2 _previewScrollPosition;
        
        private EditorStyleManager _styleManager;
        private bool _showHelpBoxes = false;
        private bool _showSpawnSettings = true;
        private bool _showShadowSettings = false;
        private bool _showSpriteSettings = false;
        private bool _showObjectsSettings = true;
        private bool _showPreview = true;

        private bool HasValidTargets => targets.ExtractAllGameObjects().Where(g => g.HasVisibleMesh()).Any();

        [MenuItem("Neon Imperium/Создатель иконок")]
        private static void OpenWindow() 
        {
            var window = GetWindow<IconsCreatorWindow>("Создатель иконок");
            window.minSize = new Vector2(400, 600);
        }

        private void OnEnable()
        {
            _styleManager = new EditorStyleManager();
            _iconCreator.InitializeEnvironment();
            LoadSettings();
        }

        private void OnDisable() 
        {
            SaveSettings();
            _iconCreator.Dispose();
            
            if (_styleManager != null)
            {
                _styleManager.Dispose();
                _styleManager = null;
            }
        }

        private void LoadSettings()
        {
            directory = EditorPrefs.GetString(nameof(directory), "Assets/Icons/");
            textureSettings.Size = EditorPrefs.GetInt("textureSize", 512);
            cameraSettings.Padding = EditorPrefs.GetFloat("padding", 0.1f);
            _showHelpBoxes = EditorPrefs.GetBool("showHelpBoxes", false);
            textureSettings.Compression = (TextureImporterCompression)EditorPrefs.GetInt("compression", (int)TextureImporterCompression.CompressedHQ);
            textureSettings.FilterMode = (FilterMode)EditorPrefs.GetInt("filterMode", (int)FilterMode.Point);
            textureSettings.AnisoLevel = EditorPrefs.GetInt("anisoLevel", 0);
            
            cameraSettings.Rotation = LoadVector3("cameraRotation", new Vector3(45f, -45f, 0f));
            
            shadowSettings.Enabled = EditorPrefs.GetBool("shadowEnabled", false);
            shadowSettings.Color = LoadColor("shadowColor", new Color(0f, 0f, 0f, 0.5f));
            shadowSettings.Offset = LoadVector2("shadowOffset", new Vector2(0.05f, -0.05f));
            shadowSettings.Scale = EditorPrefs.GetFloat("shadowScale", 0.95f);
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString(nameof(directory), directory);
            EditorPrefs.SetInt("textureSize", textureSettings.Size);
            EditorPrefs.SetFloat("padding", cameraSettings.Padding);
            EditorPrefs.SetBool("showHelpBoxes", _showHelpBoxes);
            EditorPrefs.SetInt("compression", (int)textureSettings.Compression);
            EditorPrefs.SetInt("filterMode", (int)textureSettings.FilterMode);
            EditorPrefs.SetInt("anisoLevel", textureSettings.AnisoLevel);
            
            SaveVector3("cameraRotation", cameraSettings.Rotation);
            
            EditorPrefs.SetBool("shadowEnabled", shadowSettings.Enabled);
            SaveColor("shadowColor", shadowSettings.Color);
            SaveVector2("shadowOffset", shadowSettings.Offset);
            EditorPrefs.SetFloat("shadowScale", shadowSettings.Scale);
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
            if (_styleManager == null)
            {
                _styleManager = new EditorStyleManager();
            }

            _styleManager.InitializeStyles();
            
            try
            {
                _styleManager.UpdateStyles(new Color(0.2f, 0.6f, 1f));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to update styles: {e.Message}");
            }
            
            using (var scroll = new GUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;
                
                DrawHeader();
                DrawDisplaySettings();
                DrawSpawnSettings();
                DrawShadowSettings();
                DrawSpriteSettings();
                DrawObjectsSettings();
                DrawPreviewSection();
                DrawActionButtons();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🖼️ Создатель иконок", GetHeaderStyle());
            EditorGUILayout.LabelField("Профессиональный инструмент для создания иконок", GetCenteredLabelStyle());
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10f);
        }

        private void DrawDisplaySettings()
        {
            EditorGUILayout.BeginVertical("box");
            _showHelpBoxes = GUILayout.Toggle(_showHelpBoxes, 
                new GUIContent(" 📚 Показать подсказки", "Включает/выключает подробные подсказки"), 
                EditorStyles.miniButton, GUILayout.Height(22));
            
            if (_showHelpBoxes)
                DrawHelpBox("💡 <b>Режим подсказок активен</b>. Наводите курсор на названия настроек для получения информации.");
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private void DrawSpawnSettings()
        {
            EditorGUILayout.BeginVertical("box");
            _showSpawnSettings = EditorGUILayout.Foldout(_showSpawnSettings, "⚙️ Настройки иконки", GetFoldoutStyle());
            
            if (_showSpawnSettings)
            {
                EditorGUI.indentLevel++;
                
                DrawDirectoryField();
                DrawSizeSlider();
                DrawPaddingSlider();
                DrawRotationField();
                DrawShadowsToggle();
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private void DrawShadowSettings()
        {
            EditorGUILayout.BeginVertical("box");
            _showShadowSettings = EditorGUILayout.Foldout(_showShadowSettings, "👥 Настройки тени", GetFoldoutStyle());
            
            if (_showShadowSettings)
            {
                EditorGUI.indentLevel++;
                
                shadowSettings.Enabled = EditorGUILayout.Toggle(
                    new GUIContent("Включить тень", "Добавляет тень к иконке"), 
                    shadowSettings.Enabled);

                if (shadowSettings.Enabled)
                {
                    shadowSettings.Color = EditorGUILayout.ColorField(
                        new GUIContent("Цвет тени", "Цвет и прозрачность тени"), 
                        shadowSettings.Color);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Смещение тени", "Смещение тени относительно иконки"), GUILayout.Width(120));
                    shadowSettings.Offset = EditorGUILayout.Vector2Field("", shadowSettings.Offset);
                    EditorGUILayout.EndHorizontal();

                    shadowSettings.Scale = EditorGUILayout.Slider(
                        new GUIContent("Масштаб тени", "Размер тени относительно иконки"), 
                        shadowSettings.Scale, 0.5f, 1.2f);

                    if (_showHelpBoxes)
                        DrawHelpBox("💡 <b>Тень добавляется</b> к текстуре иконки и не зависит от освещения сцены");
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private void DrawDirectoryField()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Папка сохранения", "Папка для сохранения созданных иконок"), GUILayout.Width(120));
            directory = EditorGUILayout.TextField(directory);
            if (GUILayout.Button("Обзор", GUILayout.Width(60)))
            {
                string path = EditorUtility.SaveFolderPanel("Выберите папку для иконок", "Assets", "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                    directory = path.Substring(Application.dataPath.Length + 1);
            }
            EditorGUILayout.EndHorizontal();

            if (_showHelpBoxes)
                DrawHelpBox("💡 <b>Папка должна находиться внутри Assets</b>");
        }

        private void DrawSizeSlider()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Размер иконки", "Размер иконки в пикселях"), GUILayout.Width(120));
            textureSettings.Size = EditorGUILayout.IntSlider(textureSettings.Size, 32, 2048);
            EditorGUILayout.LabelField($"{textureSettings.Size}px", GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            if (_showHelpBoxes)
                DrawHelpBox("💡 <b>Рекомендуемые размеры:</b> 512px - стандарт, 256px - для UI, 1024px - HD");
        }

        private void DrawPaddingSlider()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Внутренний отступ", "Отступ от краев объекта"), GUILayout.Width(120));
            cameraSettings.Padding = EditorGUILayout.Slider(cameraSettings.Padding, 0f, 0.5f);
            EditorGUILayout.LabelField($"{cameraSettings.Padding:P0}", GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            if (_showHelpBoxes)
                DrawHelpBox("💡 <b>Отступ помогает</b> предотвратить обрезку краев объекта");
        }

        private void DrawRotationField()
        {
            EditorGUILayout.LabelField("Поворот камеры");
            cameraSettings.Rotation = EditorGUILayout.Vector3Field("", cameraSettings.Rotation);

            if (_showHelpBoxes)
                DrawHelpBox("💡 <b>Стандартные значения:</b> (45, -45, 0) - изометрический вид");
        }

        private void DrawShadowsToggle()
        {
            cameraSettings.RenderShadows = EditorGUILayout.Toggle(
                new GUIContent("Отображать тени", "Включает отображение теней на иконке"), 
                cameraSettings.RenderShadows);

            if (_showHelpBoxes)
                DrawHelpBox("💡 <b>Тени добавляют</b> глубину и реализм, но могут увеличить время рендера");
        }

        private void DrawSpriteSettings()
        {
            EditorGUILayout.BeginVertical("box");
            _showSpriteSettings = EditorGUILayout.Foldout(_showSpriteSettings, "🖌️ Настройки спрайта", GetFoldoutStyle());
            
            if (_showSpriteSettings)
            {
                EditorGUI.indentLevel++;

                textureSettings.Compression = (TextureImporterCompression)EditorGUILayout.EnumPopup(
                    new GUIContent("Сжатие", "Настройка сжатия текстуры"), textureSettings.Compression);
                if (_showHelpBoxes) DrawHelpBox("💡 <b>CompressedHQ</b> - высокое качество, <b>Compressed</b> - баланс, <b>Uncompressed</b> - без сжатия");

                EditorGUILayout.Space(5f);

                textureSettings.FilterMode = (FilterMode)EditorGUILayout.EnumPopup(
                    new GUIContent("Filter Mode", "Метод фильтрации текстуры"), textureSettings.FilterMode);
                if (_showHelpBoxes) DrawHelpBox("💡 <b>Point</b> - пиксельный вид, <b>Bilinear</b> - сглаживание, <b>Trilinear</b> - лучшее сглаживание");

                EditorGUILayout.Space(5f);

                textureSettings.AnisoLevel = EditorGUILayout.IntSlider(
                    new GUIContent("Aniso Level", "Уровень анизотропной фильтрации"), textureSettings.AnisoLevel, 0, 16);
                if (_showHelpBoxes) DrawHelpBox("💡 <b>Улучшает качество</b> текстур под углом. 0 - отключено");

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private void DrawObjectsSettings()
        {
            EditorGUILayout.BeginVertical("box");
            _showObjectsSettings = EditorGUILayout.Foldout(_showObjectsSettings, "🎯 Объекты для иконок", GetFoldoutStyle());
            
            if (_showObjectsSettings)
            {
                EditorGUI.indentLevel++;

                SerializedObject serializedObject = new SerializedObject(this);
                SerializedProperty targetsProperty = serializedObject.FindProperty("targets");
                EditorGUILayout.PropertyField(targetsProperty, new GUIContent("Список объектов", "Добавьте объекты для создания иконок"), true);
                serializedObject.ApplyModifiedProperties();

                foreach (var target in targets.Where(t => t != null).OfType<GameObject>())
                {
                    if (!target.HasVisibleMesh())
                        EditorGUILayout.HelpBox($"Объект '{target.name}' не имеет видимых мешей!", MessageType.Warning);
                }

                if (_showHelpBoxes)
                    DrawHelpBox("💡 <b>Можно добавлять:</b> префабы, объекты на сцене, папки с префабами");

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private void DrawPreviewSection()
        {
            if (!HasValidTargets) return;

            EditorGUILayout.BeginVertical("box");
            _showPreview = EditorGUILayout.Foldout(_showPreview, $"👁️ Предпросмотр всех моделей ({targets.ExtractAllGameObjects().Count(g => g.HasVisibleMesh())})", GetFoldoutStyle());
            
            if (_showPreview)
            {
                EditorGUI.indentLevel++;

                var previews = _iconCreator.CameraPreviews;
                if (previews != null && previews.Length > 0 && previews[0] != null)
                {
                    int previewCount = previews.Length;
                    int rows = Mathf.CeilToInt((float)previewCount / MAX_PREVIEWS_PER_ROW);
                    float totalHeight = rows * PREVIEW_SIZE + (rows - 1) * 5f + 40f;

                    _previewScrollPosition = EditorGUILayout.BeginScrollView(_previewScrollPosition, GUILayout.Height(Mathf.Min(totalHeight, 400f)));
                    
                    for (int i = 0; i < previewCount; i++)
                    {
                        if (previews[i] == null) continue;

                        int row = i / MAX_PREVIEWS_PER_ROW;
                        int col = i % MAX_PREVIEWS_PER_ROW;

                        if (col == 0) EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.BeginVertical(GUILayout.Width(PREVIEW_SIZE), GUILayout.Height(PREVIEW_SIZE + 25f));
                        
                        if (_data != null && i < _data.Targets.Length)
                        {
                            EditorGUILayout.LabelField(_data.Targets[i].name, EditorStyles.miniLabel, GUILayout.Width(PREVIEW_SIZE));
                        }
                        else
                        {
                            EditorGUILayout.LabelField($"Объект {i + 1}", EditorStyles.miniLabel, GUILayout.Width(PREVIEW_SIZE));
                        }

                        Rect previewRect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE);
                        GUI.Box(new Rect(previewRect.x - 1, previewRect.y - 1, previewRect.width + 2, previewRect.height + 2), "");
                        GUI.DrawTexture(previewRect, previews[i], ScaleMode.ScaleToFit);
                        
                        EditorGUILayout.EndVertical();

                        if (col < MAX_PREVIEWS_PER_ROW - 1 && i < previewCount - 1)
                        {
                            GUILayout.Space(5f);
                        }

                        if (col == MAX_PREVIEWS_PER_ROW - 1 || i == previewCount - 1)
                        {
                            EditorGUILayout.EndHorizontal();
                            if (i < previewCount - 1) GUILayout.Space(5f);
                        }
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.HelpBox("Предпросмотр будет отображен после обновления настроек", MessageType.Info);
                    
                    if (GUILayout.Button("Обновить предпросмотр"))
                    {
                        UpdateIconCreator();
                    }
                }

                if (_showHelpBoxes)
                    DrawHelpBox("💡 <b>Предпросмотр обновляется</b> автоматически при изменении настроек");

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);

            if (GUI.changed) UpdateIconCreator();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginVertical("box");

            if (!HasValidTargets)
            {
                EditorGUILayout.HelpBox("Добавьте хотя бы один объект для создания иконок", MessageType.Warning);
            }
            else
            {
                int targetCount = targets.ExtractAllGameObjects().Count(g => g.HasVisibleMesh());
                string buttonText = targetCount > 1 ? $"Создать {targetCount} иконок" : "Создать иконку";

                var buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fixedHeight = 35,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                if (GUILayout.Button($"🖼️ {buttonText}", buttonStyle))
                    CreateIcons();

                if (_showHelpBoxes)
                    DrawHelpBox($"💡 <b>Будет создано:</b> {targetCount} иконок в папке {directory}");

                EditorGUILayout.Space(5f);
                if (GUILayout.Button("🔄 Обновить предпросмотр всех моделей"))
                {
                    UpdateIconCreator();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private IconsCreatorData _data;

        private GUIStyle GetHeaderStyle()
        {
            return _styleManager?.HeaderStyle ?? EditorStyles.boldLabel;
        }

        private GUIStyle GetFoldoutStyle()
        {
            return _styleManager?.FoldoutStyle ?? EditorStyles.foldout;
        }

        private GUIStyle GetCenteredLabelStyle()
        {
            return _styleManager?.CenteredLabelStyle ?? EditorStyles.label;
        }

        private void DrawHelpBox(string message)
        {
            var helpBoxStyle = _styleManager?.HelpBoxStyle ?? EditorStyles.helpBox;
            var miniLabelStyle = _styleManager?.MiniLabelStyle ?? EditorStyles.miniLabel;

            EditorGUILayout.BeginVertical(helpBoxStyle);
            EditorGUILayout.LabelField(message, miniLabelStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3f);
        }

        private void UpdateIconCreator()
        {
            if (!HasValidTargets) return;

            _data = new IconsCreatorData(textureSettings, cameraSettings, shadowSettings, directory, targets);
            _iconCreator.SetData(_data);
            Repaint();
        }

        private void CreateIcons()
        {
            if (!HasValidTargets) return;
            UpdateIconCreator();
            _iconCreator.CreateIcons();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Иконки созданы", 
                $"Успешно создано {_data.Targets.Length} иконок в папке {directory}", "OK");
        }
    }
}