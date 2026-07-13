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

        public static void Draw(ref bool showLightSettings, LightSettings lightSettings, EditorStyleManager styleManager, Color headerColor)
        {
            EditorGUILayout.BeginVertical("box");
            string colorHex = ColorUtility.ToHtmlStringRGB(headerColor);
            string label = $"💡 <color=#{colorHex}>Освещение</color>";
            showLightSettings = EditorGUILayout.Foldout(showLightSettings, label, 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showLightSettings)
            {
                EditorGUI.indentLevel++;
                
                int currentIndex = System.Array.IndexOf(supportedLightTypes, lightSettings.Type);
                if (currentIndex < 0) currentIndex = 0;
                
                int newIndex = EditorGUILayout.Popup(new GUIContent("Тип света", "Тип освещения: направленный или точечный"), 
                    currentIndex, supportedLightNames);
                
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