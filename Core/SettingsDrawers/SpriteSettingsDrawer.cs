using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class SpriteSettingsDrawer
    {
        public static void Draw(ref bool showSpriteSettings, TextureSettings textureSettings, 
            bool showHelpBoxes, EditorStyleManager styleManager)
        {
            EditorGUILayout.BeginVertical("box");
            showSpriteSettings = EditorGUILayout.Foldout(showSpriteSettings, "🖌️ Настройки спрайта", 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showSpriteSettings)
            {
                EditorGUI.indentLevel++;

                DrawTextureSettings(textureSettings, showHelpBoxes, styleManager);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawTextureSettings(TextureSettings textureSettings, bool showHelpBoxes, EditorStyleManager styleManager)
        {
            textureSettings.Compression = (TextureImporterCompression)EditorGUILayout.EnumPopup(
                new GUIContent("Сжатие", "Настройка сжатия текстуры"), textureSettings.Compression);
            if (showHelpBoxes) 
                DisplaySettingsDrawer.DrawHelpBox("💡 <b>CompressedHQ</b> - высокое качество, <b>Compressed</b> - баланс, <b>Uncompressed</b> - без сжатия", styleManager);

            EditorGUILayout.Space(5f);

            textureSettings.FilterMode = (FilterMode)EditorGUILayout.EnumPopup(
                new GUIContent("Filter Mode", "Метод фильтрации текстуры"), textureSettings.FilterMode);
            if (showHelpBoxes) 
                DisplaySettingsDrawer.DrawHelpBox("💡 <b>Point</b> - пиксельный вид, <b>Bilinear</b> - сглаживание, <b>Trilinear</b> - лучшее сглаживание", styleManager);

            EditorGUILayout.Space(5f);

            textureSettings.AnisoLevel = EditorGUILayout.IntSlider(
                new GUIContent("Aniso Level", "Уровень анизотропной фильтрации"), textureSettings.AnisoLevel, 0, 16);
            if (showHelpBoxes) 
                DisplaySettingsDrawer.DrawHelpBox("💡 <b>Улучшает качество</b> текстур под углом. 0 - отключено", styleManager);
        }
    }
}