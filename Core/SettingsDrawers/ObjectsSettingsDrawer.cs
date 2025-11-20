using System.Collections.Generic;
using System.Linq;
using NeonImperium.IconsCreation.Extensions;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class ObjectsSettingsDrawer
    {
        public static void Draw(ref bool showObjectsSettings, List<Object> targets, 
            bool showHelpBoxes, EditorStyleManager styleManager, SerializedObject serializedObject)
        {
            EditorGUILayout.BeginVertical("box");
            showObjectsSettings = EditorGUILayout.Foldout(showObjectsSettings, "🎯 Объекты для иконок", 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showObjectsSettings)
            {
                EditorGUI.indentLevel++;

                DrawTargetsList(targets, showHelpBoxes, styleManager, serializedObject);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawTargetsList(List<Object> targets, bool showHelpBoxes, EditorStyleManager styleManager, SerializedObject serializedObject)
        {
            if (serializedObject != null)
            {
                SerializedProperty targetsProperty = serializedObject.FindProperty("targets");
                if (targetsProperty != null)
                {
                    EditorGUILayout.PropertyField(targetsProperty, new GUIContent("Список объектов", "Добавьте объекты для создания иконок"), true);
                    serializedObject.ApplyModifiedProperties();
                }
            }

            foreach (var target in targets.Where(t => t != null).OfType<GameObject>())
            {
                if (!target.HasVisibleMesh())
                    EditorGUILayout.HelpBox($"Объект '{target.name}' не имеет видимых мешей!", MessageType.Warning);
            }

            if (showHelpBoxes)
                DisplaySettingsDrawer.DrawHelpBox("💡 <b>Можно добавлять:</b> префабы, объекты на сцене, папки с префабами", styleManager);
        }
    }
}