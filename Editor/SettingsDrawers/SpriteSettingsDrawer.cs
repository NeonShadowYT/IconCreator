using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class SpriteSettingsDrawer
    {
        public static void Draw(ref bool showSpriteSettings, TextureSettings textureSettings, EditorStyleManager styleManager, Color headerColor)
        {
            EditorGUILayout.BeginVertical("box");
            string colorHex = ColorUtility.ToHtmlStringRGB(headerColor);
            string label = $"🖌️ <color=#{colorHex}>Текстура</color>";
            showSpriteSettings = EditorGUILayout.Foldout(showSpriteSettings, label, 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showSpriteSettings)
            {
                EditorGUI.indentLevel++;

                textureSettings.Compression = (TextureImporterCompression)EditorGUILayout.EnumPopup(new GUIContent("Сжатие", "Качество сжатия текстуры"), textureSettings.Compression);
                textureSettings.FilterMode = (FilterMode)EditorGUILayout.EnumPopup(new GUIContent("Фильтр", "Метод фильтрации текстуры"), textureSettings.FilterMode);
                textureSettings.AnisoLevel = EditorGUILayout.IntSlider(new GUIContent("Качество", "Уровень анизотропной фильтрации"), textureSettings.AnisoLevel, 0, 16);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }
    }
}