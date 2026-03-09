using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.Helpers
{
    internal static class LayersHelper
    {
        public static void CreateLayer(string newLayerName)
        {
            if (string.IsNullOrEmpty(newLayerName))
                throw new System.ArgumentNullException(nameof(newLayerName), "New layer name string is either null or empty.");

            const int builtInLayersCount = 5;

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets.Length == 0)
            {
                Debug.LogError("TagManager asset not found");
                return;
            }

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layersProperty = tagManager.FindProperty("layers");
            if (layersProperty == null)
            {
                Debug.LogError("Layers property not found in TagManager");
                return;
            }

            int layersCount = layersProperty.arraySize;
            SerializedProperty firstEmptyLayerProperty = null;

            for (int i = 0; i < layersCount; i++)
            {
                SerializedProperty layerProperty = layersProperty.GetArrayElementAtIndex(i);
                if (layerProperty == null) continue;

                string layerName = layerProperty.stringValue;
                if (layerName == newLayerName) return;

                if (i < builtInLayersCount || !string.IsNullOrEmpty(layerName))
                    continue;

                firstEmptyLayerProperty ??= layerProperty;
            }

            if (firstEmptyLayerProperty == null)
            {
                Debug.LogError($"Maximum limit of {layersCount} layers exceeded. Layer \"{newLayerName}\" not created.");
                return;
            }

            firstEmptyLayerProperty.stringValue = newLayerName;
            tagManager.ApplyModifiedProperties();
        }

        public static void RemoveLayer(string existingLayerName)
        {
            if (string.IsNullOrEmpty(existingLayerName))
                throw new System.ArgumentNullException(nameof(existingLayerName), "Layer name string is either null or empty.");

            const int builtInLayersCount = 5;

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets.Length == 0)
            {
                Debug.LogError("TagManager asset not found");
                return;
            }

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layersProperty = tagManager.FindProperty("layers");
            if (layersProperty == null)
            {
                Debug.LogError("Layers property not found in TagManager");
                return;
            }

            int layersCount = layersProperty.arraySize;
            SerializedProperty existingLayerProperty = null;

            for (int i = 0; i < layersCount; i++)
            {
                SerializedProperty layerProperty = layersProperty.GetArrayElementAtIndex(i);
                if (layerProperty == null) continue;

                string layerName = layerProperty.stringValue;
                bool validLayer = i < builtInLayersCount || !string.IsNullOrEmpty(layerName);
                if (!validLayer) continue;
                if (layerName != existingLayerName) continue;

                existingLayerProperty ??= layerProperty;
            }

            if (existingLayerProperty == null)
            {
                Debug.LogError($"Layer named \"{existingLayerName}\" was not found!");
                return;
            }

            existingLayerProperty.stringValue = string.Empty;
            tagManager.ApplyModifiedProperties();
        }
    }
}