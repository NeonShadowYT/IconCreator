using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class LightSettingsDrawer
    {
        private static readonly LightType[] supportedLightTypes = new LightType[] 
        { 
            LightType.Directional, 
            LightType.Point 
        };
        
        private static readonly string[] supportedLightNames = new string[] 
        { 
            "Directional (Направленный)", 
            "Point (Точечный)" 
        };

        public static void Draw(ref bool showLightSettings, LightSettings lightSettings, EditorStyleManager styleManager)
        {
            EditorGUILayout.BeginVertical("box");
            showLightSettings = EditorGUILayout.Foldout(showLightSettings, "💡 Освещение", 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showLightSettings)
            {
                EditorGUI.indentLevel++;
                
                // Находим текущий индекс в поддерживаемом массиве
                int currentIndex = System.Array.IndexOf(supportedLightTypes, lightSettings.Type);
                if (currentIndex < 0) currentIndex = 0; // По умолчанию Directional
                
                int newIndex = EditorGUILayout.Popup(new GUIContent("Тип света", "Тип освещения: направленный или точечный"), 
                    currentIndex, supportedLightNames);
                
                // Применяем выбранный тип
                if (newIndex != currentIndex)
                    lightSettings.Type = supportedLightTypes[newIndex];

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