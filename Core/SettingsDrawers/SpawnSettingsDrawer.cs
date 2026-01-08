using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class SpawnSettingsDrawer
    {
        private static readonly int[] SIZE_OPTIONS = { 32, 64, 128, 256, 512, 1024, 2048 };
        private static readonly string[] SIZE_OPTIONS_STR = { "32px", "64px", "128px", "256px", "512px", "1024px", "2048px" };

        public static void Draw(ref bool showSpawnSettings, ref string directory, ref string cameraTag, ref string objectsLayer,
            TextureSettings textureSettings, CameraSettings cameraSettings, EditorStyleManager styleManager)
        {
            EditorGUILayout.BeginVertical("box");
            showSpawnSettings = EditorGUILayout.Foldout(showSpawnSettings, "⚙️ Настройки иконки", 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showSpawnSettings)
            {
                EditorGUI.indentLevel++;
                
                DrawDirectoryField(ref directory);
                DrawCameraTagField(ref cameraTag);
                DrawObjectsLayerField(ref objectsLayer);
                DrawSizeDropdown(textureSettings);
                DrawPaddingSlider(cameraSettings);
                DrawRotationField(cameraSettings);
                DrawShadowsToggle(cameraSettings);
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawDirectoryField(ref string directory)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Папка сохранения", "Путь для сохранения созданных иконок"), GUILayout.Width(120));
            directory = EditorGUILayout.TextField(directory);
            if (GUILayout.Button("Обзор", GUILayout.Width(60)))
            {
                string path = EditorUtility.SaveFolderPanel("Выберите папку", "Assets", "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                    directory = "Assets" + path.Substring(Application.dataPath.Length);
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawCameraTagField(ref string cameraTag)
        {
            string[] tags = UnityEditorInternal.InternalEditorUtility.tags;
            int currentIndex = System.Array.IndexOf(tags, cameraTag);
            if (currentIndex == -1) currentIndex = 0;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Тег камеры", "Тег для временной камеры, используется для изоляции"), GUILayout.Width(120));
            int newIndex = EditorGUILayout.Popup(currentIndex, tags);
            if (newIndex >= 0 && newIndex < tags.Length && newIndex != currentIndex)
            {
                cameraTag = tags[newIndex];
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawObjectsLayerField(ref string objectsLayer)
        {
            string[] layers = UnityEditorInternal.InternalEditorUtility.layers;
            int currentIndex = System.Array.IndexOf(layers, objectsLayer);
            if (currentIndex == -1) currentIndex = 0;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Слой объектов", "Слой для объектов на сцене создания иконок"), GUILayout.Width(120));
            int newIndex = EditorGUILayout.Popup(currentIndex, layers);
            if (newIndex >= 0 && newIndex < layers.Length && newIndex != currentIndex)
            {
                objectsLayer = layers[newIndex];
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSizeDropdown(TextureSettings textureSettings)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Размер иконки", "Размер текстуры в пикселях"), GUILayout.Width(120));
            
            int currentSizeIndex = System.Array.IndexOf(SIZE_OPTIONS, textureSettings.Size);
            if (currentSizeIndex == -1) currentSizeIndex = 4;
            
            int newSizeIndex = EditorGUILayout.Popup(currentSizeIndex, SIZE_OPTIONS_STR);
            textureSettings.Size = SIZE_OPTIONS[newSizeIndex];
            
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawPaddingSlider(CameraSettings cameraSettings)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Отступ", "Отступ от краев объекта в кадре"), GUILayout.Width(120));
            cameraSettings.Padding = EditorGUILayout.Slider(cameraSettings.Padding, 0f, 0.5f);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawRotationField(CameraSettings cameraSettings)
        {
            EditorGUILayout.LabelField(new GUIContent("Поворот камеры", "Углы Эйлера для поворота камеры"));
            cameraSettings.Rotation = EditorGUILayout.Vector3Field("", cameraSettings.Rotation);
        }

        private static void DrawShadowsToggle(CameraSettings cameraSettings)
        {
            cameraSettings.RenderShadows = EditorGUILayout.Toggle(new GUIContent("Тени на сцене", "Рендерить тени на сцене создания иконок"), cameraSettings.RenderShadows);
        }
    }
}