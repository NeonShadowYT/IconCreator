using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class LightSettingsDrawer
    {
        public static void Draw(ref bool showLightSettings, LightSettings lightSettings, EditorStyleManager styleManager)
        {
            EditorGUILayout.BeginVertical("box");
            showLightSettings = EditorGUILayout.Foldout(showLightSettings, "💡 Освещение", 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showLightSettings)
            {
                EditorGUI.indentLevel++;
                
                lightSettings.Type = (LightType)EditorGUILayout.EnumPopup(new GUIContent("Тип света", "Тип освещения: направленный или точечный"), lightSettings.Type);

                if (lightSettings.Type == LightType.Directional)
                {
                    DrawDirectionalLightSettings(lightSettings);
                }
                else if (lightSettings.Type == LightType.Point)
                {
                    DrawPointLightSettings(lightSettings);
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawDirectionalLightSettings(LightSettings lightSettings)
        {
            EditorGUILayout.LabelField(new GUIContent("Поворот", "Углы Эйлера для направления света"));
            lightSettings.DirectionalRotation = EditorGUILayout.Vector3Field("", lightSettings.DirectionalRotation);

            lightSettings.DirectionalColor = EditorGUILayout.ColorField(new GUIContent("Цвет", "Цвет направленного света"), lightSettings.DirectionalColor);

            lightSettings.DirectionalIntensity = EditorGUILayout.Slider(new GUIContent("Интенсивность", "Яркость направленного света"), lightSettings.DirectionalIntensity, 0f, 2f);
        }

        private static void DrawPointLightSettings(LightSettings lightSettings)
        {
            for (int i = 0; i < lightSettings.PointLights.Length; i++)
            {
                EditorGUILayout.LabelField(new GUIContent($"Точечный свет {i + 1}", $"Параметры {i+1}-го точечного источника"));
                EditorGUI.indentLevel++;
                
                lightSettings.PointLights[i].Position = EditorGUILayout.Vector3Field(new GUIContent("Позиция", "Позиция в локальном пространстве сцены"), lightSettings.PointLights[i].Position);
                lightSettings.PointLights[i].Color = EditorGUILayout.ColorField(new GUIContent("Цвет", "Цвет точечного света"), lightSettings.PointLights[i].Color);
                lightSettings.PointLights[i].Intensity = EditorGUILayout.Slider(new GUIContent("Интенсивность", "Яркость точечного света"), lightSettings.PointLights[i].Intensity, 0f, 2f);
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5f);
            }
        }
    }
}