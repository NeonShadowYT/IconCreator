using System.Collections.Generic;
using System.Linq;
using NeonImperium.IconsCreation.Extensions;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class ActionButtonsDrawer
    {
        public static void Draw(List<Object> targets, string directory, 
            bool hasValidTargets, System.Action createIcons, System.Action updatePreview)
        {
            EditorGUILayout.BeginVertical("box");

            if (!hasValidTargets)
            {
                EditorGUILayout.HelpBox("Добавьте хотя бы один объект для создания иконок", MessageType.Warning);
            }
            else
            {
                DrawActionButtons(targets, directory, createIcons, updatePreview);
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawActionButtons(List<Object> targets, string directory, 
            System.Action createIcons, System.Action updatePreview)
        {
            int targetCount = targets.ExtractAllGameObjects().Count(g => g.HasVisibleMesh());
            string buttonText = targetCount > 1 ? $"Создать {targetCount} иконок" : "Создать иконку";

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 35,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            if (GUILayout.Button($"🖼️ {buttonText}", buttonStyle))
                createIcons?.Invoke();

            EditorGUILayout.Space(5f);
            if (GUILayout.Button("🔄 Обновить предпросмотр всех моделей"))
            {
                updatePreview?.Invoke();
            }
        }
    }
}