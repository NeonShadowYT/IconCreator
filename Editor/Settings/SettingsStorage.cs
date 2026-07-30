using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    public static class SettingsStorage
    {
        public static void LoadAll(ref string directory, ref string scenePath, ref string presetsFolder,
            ref TextureSettings textureSettings, ref CameraSettings cameraSettings,
            ref LightSettings lightSettings, ref ShadowSettings shadowSettings,
            ref string cameraTag, ref string objectsLayer)
        {
            directory = EditorPrefs.GetString("directory", "Assets/Icons/");
            scenePath = EditorPrefs.GetString("scenePath", "Assets/Plugins/NeonImperium/IconCreator/Scenes/Icons_Creation.unity");
            presetsFolder = EditorPrefs.GetString("presetsFolder", "Assets/Plugins/NeonImperium/IconCreator/Presets");
            
            textureSettings.Size = EditorPrefs.GetInt("textureSize", 512);
            textureSettings.RenderSize = EditorPrefs.GetInt("renderSize", 2048);
            textureSettings.Compression = (TextureImporterCompression)EditorPrefs.GetInt("compression", (int)TextureImporterCompression.CompressedHQ);
            textureSettings.FilterMode = (FilterMode)EditorPrefs.GetInt("filterMode", (int)FilterMode.Point);
            textureSettings.AnisoLevel = EditorPrefs.GetInt("anisoLevel", 0);

            cameraSettings.Rotation = LoadVector3("cameraRotation", new Vector3(45f, -45f, 0f));
            cameraSettings.Padding = EditorPrefs.GetFloat("padding", 0.1f);
            cameraSettings.RenderShadows = EditorPrefs.GetBool("renderShadows", false);

            lightSettings.Type = (LightType)EditorPrefs.GetInt("lightType", (int)LightType.Directional);
            lightSettings.DirectionalRotation = LoadVector3("directionalRotation", new Vector3(50f, -30f, 0f));
            lightSettings.DirectionalColor = LoadColor("directionalColor", Color.white);
            lightSettings.DirectionalIntensity = EditorPrefs.GetFloat("directionalIntensity", 1f);

            for (int i = 0; i < lightSettings.PointLights.Length; i++)
            {
                lightSettings.PointLights[i].Position = LoadVector3($"pointLight{i}Position", new Vector3(1, 0.5f, -0.5f));
                lightSettings.PointLights[i].Color = LoadColor($"pointLight{i}Color", Color.white);
                lightSettings.PointLights[i].Intensity = EditorPrefs.GetFloat($"pointLight{i}Intensity", 1f);
            }

            shadowSettings.Enabled = EditorPrefs.GetBool("shadowEnabled", false);
            shadowSettings.Color = LoadColor("shadowColor", new Color(0f, 0f, 0f, 0.5f));
            shadowSettings.Offset = LoadVector2("shadowOffset", new Vector2(0.05f, -0.05f));
            shadowSettings.Scale = EditorPrefs.GetFloat("shadowScale", 0.95f);

            cameraTag = EditorPrefs.GetString("cameraTag", "EditorOnly");
            objectsLayer = EditorPrefs.GetString("objectsLayer", "TransparentFX");
        }

        public static void SaveAll(string directory, string scenePath, string presetsFolder,
            TextureSettings textureSettings, CameraSettings cameraSettings,
            LightSettings lightSettings, ShadowSettings shadowSettings,
            string cameraTag, string objectsLayer)
        {
            EditorPrefs.SetString("directory", directory);
            EditorPrefs.SetString("scenePath", scenePath);
            EditorPrefs.SetString("presetsFolder", presetsFolder);
            EditorPrefs.SetInt("textureSize", textureSettings.Size);
            EditorPrefs.SetInt("renderSize", textureSettings.RenderSize);
            EditorPrefs.SetFloat("padding", cameraSettings.Padding);
            EditorPrefs.SetInt("compression", (int)textureSettings.Compression);
            EditorPrefs.SetInt("filterMode", (int)textureSettings.FilterMode);
            EditorPrefs.SetInt("anisoLevel", textureSettings.AnisoLevel);
            EditorPrefs.SetBool("renderShadows", cameraSettings.RenderShadows);

            SaveVector3("cameraRotation", cameraSettings.Rotation);

            EditorPrefs.SetInt("lightType", (int)lightSettings.Type);
            SaveVector3("directionalRotation", lightSettings.DirectionalRotation);
            SaveColor("directionalColor", lightSettings.DirectionalColor);
            EditorPrefs.SetFloat("directionalIntensity", lightSettings.DirectionalIntensity);

            for (int i = 0; i < lightSettings.PointLights.Length; i++)
            {
                SaveVector3($"pointLight{i}Position", lightSettings.PointLights[i].Position);
                SaveColor($"pointLight{i}Color", lightSettings.PointLights[i].Color);
                EditorPrefs.SetFloat($"pointLight{i}Intensity", lightSettings.PointLights[i].Intensity);
            }

            EditorPrefs.SetBool("shadowEnabled", shadowSettings.Enabled);
            SaveColor("shadowColor", shadowSettings.Color);
            SaveVector2("shadowOffset", shadowSettings.Offset);
            EditorPrefs.SetFloat("shadowScale", shadowSettings.Scale);

            EditorPrefs.SetString("cameraTag", cameraTag);
            EditorPrefs.SetString("objectsLayer", objectsLayer);
        }

        private static Vector3 LoadVector3(string key, Vector3 defaultValue)
        {
            return new Vector3(
                EditorPrefs.GetFloat($"{key}_x", defaultValue.x),
                EditorPrefs.GetFloat($"{key}_y", defaultValue.y),
                EditorPrefs.GetFloat($"{key}_z", defaultValue.z)
            );
        }

        private static void SaveVector3(string key, Vector3 vector)
        {
            EditorPrefs.SetFloat($"{key}_x", vector.x);
            EditorPrefs.SetFloat($"{key}_y", vector.y);
            EditorPrefs.SetFloat($"{key}_z", vector.z);
        }

        private static Vector2 LoadVector2(string key, Vector2 defaultValue)
        {
            return new Vector2(
                EditorPrefs.GetFloat($"{key}_x", defaultValue.x),
                EditorPrefs.GetFloat($"{key}_y", defaultValue.y)
            );
        }

        private static void SaveVector2(string key, Vector2 vector)
        {
            EditorPrefs.SetFloat($"{key}_x", vector.x);
            EditorPrefs.SetFloat($"{key}_y", vector.y);
        }

        private static Color LoadColor(string key, Color defaultValue)
        {
            return new Color(
                EditorPrefs.GetFloat($"{key}_r", defaultValue.r),
                EditorPrefs.GetFloat($"{key}_g", defaultValue.g),
                EditorPrefs.GetFloat($"{key}_b", defaultValue.b),
                EditorPrefs.GetFloat($"{key}_a", defaultValue.a)
            );
        }

        private static void SaveColor(string key, Color color)
        {
            EditorPrefs.SetFloat($"{key}_r", color.r);
            EditorPrefs.SetFloat($"{key}_g", color.g);
            EditorPrefs.SetFloat($"{key}_b", color.b);
            EditorPrefs.SetFloat($"{key}_a", color.a);
        }
    }
}