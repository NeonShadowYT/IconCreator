using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class ObjectsSettingsDrawer
    {
        public static void Draw(ref bool showObjectsSettings, List<Object> targets, 
            EditorStyleManager styleManager, SerializedObject serializedObject, Color headerColor)
        {
            EditorGUILayout.BeginVertical("box");
            string colorHex = ColorUtility.ToHtmlStringRGB(headerColor);
            string label = $"🎯 <color=#{colorHex}>Объекты</color>";
            showObjectsSettings = EditorGUILayout.Foldout(showObjectsSettings, label, 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showObjectsSettings)
            {
                EditorGUI.indentLevel++;

                SerializedProperty targetsProperty = serializedObject.FindProperty("targets");
                if (targetsProperty != null)
                {
                    EditorGUILayout.PropertyField(targetsProperty, new GUIContent("Список объектов", "Добавьте GameObject или папку с префабами"), true);
                    serializedObject.ApplyModifiedProperties();
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }
    }
}