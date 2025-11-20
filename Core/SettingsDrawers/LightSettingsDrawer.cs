using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class LightSettingsDrawer
    {
        public static void Draw(ref bool showLightSettings, LightSettings lightSettings, 
            bool showHelpBoxes, EditorStyleManager styleManager)
        {
            EditorGUILayout.BeginVertical("box");
            showLightSettings = EditorGUILayout.Foldout(showLightSettings, "💡 Настройки освещения", 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showLightSettings)
            {
                EditorGUI.indentLevel++;
                
                lightSettings.Type = (LightType)EditorGUILayout.EnumPopup(
                    new GUIContent("Тип света", "Тип источника освещения"), 
                    lightSettings.Type);

                if (lightSettings.Type == LightType.Directional)
                {
                    DrawDirectionalLightSettings(lightSettings, showHelpBoxes);
                }
                else if (lightSettings.Type == LightType.Point)
                {
                    DrawPointLightSettings(lightSettings, showHelpBoxes);
                }

                DrawLightHelpBox(lightSettings.Type, showHelpBoxes, styleManager);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawDirectionalLightSettings(LightSettings lightSettings, bool showHelpBoxes)
        {
            EditorGUILayout.LabelField("Поворот направленного света");
            lightSettings.DirectionalRotation = EditorGUILayout.Vector3Field("", lightSettings.DirectionalRotation);

            lightSettings.DirectionalColor = EditorGUILayout.ColorField(
                new GUIContent("Цвет света", "Цвет направленного света"), 
                lightSettings.DirectionalColor);

            lightSettings.DirectionalIntensity = EditorGUILayout.Slider(
                new GUIContent("Интенсивность света", "Интенсивность направленного света"), 
                lightSettings.DirectionalIntensity, 0f, 2f);
        }

        private static void DrawPointLightSettings(LightSettings lightSettings, bool showHelpBoxes)
        {
            for (int i = 0; i < lightSettings.PointLights.Length; i++)
            {
                EditorGUILayout.LabelField($"Точечный свет {i + 1}");
                EditorGUI.indentLevel++;
                
                lightSettings.PointLights[i].Position = EditorGUILayout.Vector3Field(
                    new GUIContent("Позиция", "Позиция точечного света"), 
                    lightSettings.PointLights[i].Position);
                
                lightSettings.PointLights[i].Color = EditorGUILayout.ColorField(
                    new GUIContent("Цвет", "Цвет точечного света"), 
                    lightSettings.PointLights[i].Color);
                
                lightSettings.PointLights[i].Intensity = EditorGUILayout.Slider(
                    new GUIContent("Интенсивность", "Интенсивность точечного света"), 
                    lightSettings.PointLights[i].Intensity, 0f, 2f);
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5f);
            }
        }

        private static void DrawLightHelpBox(LightType lightType, bool showHelpBoxes, EditorStyleManager styleManager)
        {
            if (showHelpBoxes)
            {
                string message = lightType == LightType.Directional 
                    ? "💡 <b>Направленный свет</b> освещает все объекты равномерно с одного направления"
                    : "💡 <b>Точечный свет</b> излучает свет во всех направлениях из заданной позиции";
                
                DisplaySettingsDrawer.DrawHelpBox(message, styleManager);
            }
        }
    }
}