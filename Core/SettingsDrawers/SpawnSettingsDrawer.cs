using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class SpawnSettingsDrawer
    {
        private static readonly int[] SIZE_OPTIONS = { 32, 64, 128, 256, 512, 1024, 2048 };
        private static readonly string[] SIZE_OPTIONS_STR = { "32px", "64px", "128px", "256px", "512px", "1024px", "2048px" };

        public static void Draw(ref bool showSpawnSettings, ref string directory, 
            TextureSettings textureSettings, CameraSettings cameraSettings, 
            bool showHelpBoxes, EditorStyleManager styleManager)
        {
            EditorGUILayout.BeginVertical("box");
            showSpawnSettings = EditorGUILayout.Foldout(showSpawnSettings, "⚙️ Настройки иконки", 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showSpawnSettings)
            {
                EditorGUI.indentLevel++;
                
                DrawDirectoryField(ref directory, showHelpBoxes, styleManager);
                DrawSizeDropdown(textureSettings, showHelpBoxes);
                DrawPaddingSlider(cameraSettings, showHelpBoxes);
                DrawRotationField(cameraSettings, showHelpBoxes);
                DrawShadowsToggle(cameraSettings, showHelpBoxes);
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawDirectoryField(ref string directory, bool showHelpBoxes, EditorStyleManager styleManager)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Папка сохранения", "Папка для сохранения созданных иконок"), GUILayout.Width(120));
            directory = EditorGUILayout.TextField(directory);
            if (GUILayout.Button("Обзор", GUILayout.Width(60)))
            {
                string path = EditorUtility.SaveFolderPanel("Выберите папку для иконок", "Assets", "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                    directory = "Assets" + path.Substring(Application.dataPath.Length);
            }
            EditorGUILayout.EndHorizontal();

            if (showHelpBoxes)
                DisplaySettingsDrawer.DrawHelpBox("💡 <b>Папка должна находиться внутри Assets</b>", styleManager);
        }

        private static void DrawSizeDropdown(TextureSettings textureSettings, bool showHelpBoxes)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Размер иконки", "Размер иконки в пикселях"), GUILayout.Width(120));
            
            int currentSizeIndex = System.Array.IndexOf(SIZE_OPTIONS, textureSettings.Size);
            if (currentSizeIndex == -1) currentSizeIndex = 4; // 512 по умолчанию
            
            int newSizeIndex = EditorGUILayout.Popup(currentSizeIndex, SIZE_OPTIONS_STR);
            textureSettings.Size = SIZE_OPTIONS[newSizeIndex];
            
            EditorGUILayout.EndHorizontal();

            if (showHelpBoxes)
                EditorGUILayout.HelpBox("💡 <b>Рекомендуемые размеры:</b> 512px - стандарт, 256px - для UI, 1024px - HD", MessageType.Info);
        }

        private static void DrawPaddingSlider(CameraSettings cameraSettings, bool showHelpBoxes)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Внутренний отступ", "Отступ от краев объекта"), GUILayout.Width(120));
            cameraSettings.Padding = EditorGUILayout.Slider(cameraSettings.Padding, 0f, 0.5f);
            EditorGUILayout.LabelField($"{cameraSettings.Padding:P0}", GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            if (showHelpBoxes)
                EditorGUILayout.HelpBox("💡 <b>Отступ помогает</b> предотвратить обрезку краев объекта", MessageType.Info);
        }

        private static void DrawRotationField(CameraSettings cameraSettings, bool showHelpBoxes)
        {
            EditorGUILayout.LabelField("Поворот камеры");
            cameraSettings.Rotation = EditorGUILayout.Vector3Field("", cameraSettings.Rotation);

            if (showHelpBoxes)
                EditorGUILayout.HelpBox("💡 <b>Стандартные значения:</b> (45, -45, 0) - изометрический вид", MessageType.Info);
        }

        private static void DrawShadowsToggle(CameraSettings cameraSettings, bool showHelpBoxes)
        {
            cameraSettings.RenderShadows = EditorGUILayout.Toggle(
                new GUIContent("Отображать тени", "Включает отображение теней на иконке"), 
                cameraSettings.RenderShadows);

            if (showHelpBoxes)
                EditorGUILayout.HelpBox("💡 <b>Тени добавляют</b> глубину и реализм, но могут увеличить время рендера", MessageType.Info);
        }
    }
}