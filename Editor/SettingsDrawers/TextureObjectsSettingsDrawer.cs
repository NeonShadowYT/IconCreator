using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class TextureObjectsSettingsDrawer
    {
        public static void Draw(ref bool showSettings, SerializedProperty textureObjectsProperty, EditorStyleManager styleManager, Color headerColor)
        {
            EditorGUILayout.BeginVertical("box");
            string colorHex = ColorUtility.ToHtmlStringRGB(headerColor);
            string label = $"🖼️ <color=#{colorHex}>Текстурные объекты</color>";
            showSettings = EditorGUILayout.Foldout(showSettings, label,
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);

            if (showSettings)
            {
                EditorGUI.indentLevel++;

                if (textureObjectsProperty != null)
                {
                    EditorGUILayout.PropertyField(textureObjectsProperty, new GUIContent("Список текстур", "Добавьте текстуры для композиции"), true);
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }
    }
}