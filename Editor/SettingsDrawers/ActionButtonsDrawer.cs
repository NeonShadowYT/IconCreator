using System.Collections.Generic;
using System.Linq;
using NeonImperium.IconsCreation.Extensions;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class ActionButtonsDrawer
    {
        public static void Draw(List<Object> targets, bool hasValidTargets, bool isGenerating, System.Action createIcons)
        {
            EditorGUILayout.BeginVertical("box");

            if (!hasValidTargets)
            {
                EditorGUILayout.HelpBox("Добавьте объекты для создания иконок", MessageType.Warning);
            }
            else
            {
                DrawActionButtons(targets, isGenerating, createIcons);
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawActionButtons(List<Object> targets, bool isGenerating, System.Action createIcons)
        {
            int targetCount = targets.ExtractAllGameObjects().Count(g => g.HasVisibleMesh());
            string buttonText = targetCount > 1 ? $"Создать {targetCount} иконок" : "Создать иконку";

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 40,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.enabled = !isGenerating && targetCount > 0;
            if (GUILayout.Button($"🖼️ {buttonText}", buttonStyle))
                createIcons?.Invoke();
            GUI.enabled = true;
            
            if (isGenerating)
            {
                EditorGUILayout.HelpBox("Создание иконок...", MessageType.Info);
            }
        }
    }
}