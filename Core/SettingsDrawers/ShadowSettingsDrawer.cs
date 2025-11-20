using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class ShadowSettingsDrawer
    {
        public static void Draw(ref bool showShadowSettings, ShadowSettings shadowSettings, 
            bool showHelpBoxes, EditorStyleManager styleManager)
        {
            EditorGUILayout.BeginVertical("box");
            showShadowSettings = EditorGUILayout.Foldout(showShadowSettings, "👥 Настройки тени", 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showShadowSettings)
            {
                EditorGUI.indentLevel++;
                
                shadowSettings.Enabled = EditorGUILayout.Toggle(
                    new GUIContent("Включить тень", "Добавляет тень к иконке"), 
                    shadowSettings.Enabled);

                if (shadowSettings.Enabled)
                {
                    DrawShadowContent(shadowSettings, showHelpBoxes, styleManager);
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawShadowContent(ShadowSettings shadowSettings, bool showHelpBoxes, EditorStyleManager styleManager)
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

            if (showHelpBoxes)
                DisplaySettingsDrawer.DrawHelpBox("💡 <b>Тень добавляется</b> к текстуре иконки и не зависит от освещения сцены", styleManager);
        }
    }
}