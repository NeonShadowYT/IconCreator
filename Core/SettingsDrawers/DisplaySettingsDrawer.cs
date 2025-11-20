using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class DisplaySettingsDrawer
    {
        public static void Draw(ref bool showHelpBoxes, EditorStyleManager styleManager)
        {
            EditorGUILayout.BeginVertical("box");
            showHelpBoxes = GUILayout.Toggle(showHelpBoxes, 
                new GUIContent(" 📚 Показать подсказки", "Включает/выключает подробные подсказки"), 
                EditorStyles.miniButton, GUILayout.Height(22));
            
            if (showHelpBoxes)
                DrawHelpBox("💡 <b>Режим подсказок активен</b>. Наводите курсор на названия настроек для получения информации.", styleManager);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        public static void DrawHelpBox(string message, EditorStyleManager styleManager)
        {
            var helpBoxStyle = styleManager?.HelpBoxStyle ?? EditorStyles.helpBox;
            var miniLabelStyle = styleManager?.MiniLabelStyle ?? EditorStyles.miniLabel;

            EditorGUILayout.BeginVertical(helpBoxStyle);
            EditorGUILayout.LabelField(message, miniLabelStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3f);
        }
    }
}