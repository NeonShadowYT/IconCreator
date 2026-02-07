using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class ShadowSettingsDrawer
    {
        public static void Draw(ref bool showShadowSettings, ShadowSettings shadowSettings, EditorStyleManager styleManager)
        {
            EditorGUILayout.BeginVertical("box");
            showShadowSettings = EditorGUILayout.Foldout(showShadowSettings, "👥 Тень", 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showShadowSettings)
            {
                EditorGUI.indentLevel++;
                
                shadowSettings.Enabled = EditorGUILayout.Toggle(new GUIContent("Включить", "Включить отрисовку искусственной тени"), shadowSettings.Enabled);

                if (shadowSettings.Enabled)
                {
                    DrawShadowContent(shadowSettings);
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawShadowContent(ShadowSettings shadowSettings)
        {
            shadowSettings.Color = EditorGUILayout.ColorField(new GUIContent("Цвет", "Цвет и прозрачность тени"), shadowSettings.Color);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Смещение", "Смещение тени относительно объекта"), GUILayout.Width(120));
            shadowSettings.Offset = EditorGUILayout.Vector2Field("", shadowSettings.Offset);
            EditorGUILayout.EndHorizontal();

            shadowSettings.Scale = EditorGUILayout.Slider(new GUIContent("Масштаб", "Масштаб тени относительно объекта"), shadowSettings.Scale, 0.5f, 1.2f);
        }
    }
}