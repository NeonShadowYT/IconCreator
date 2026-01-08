using UnityEngine;

namespace NeonImperium.IconsCreation
{
    [System.Serializable]
    public class PresetData
    {
        public string presetName;
        public TextureSettings textureSettings;
        public CameraSettings cameraSettings;
        public LightSettings lightSettings;
        public ShadowSettings shadowSettings;
        public string directory;
        public string cameraTag;
        public string objectsLayer;

        public PresetData()
        {
            textureSettings = new TextureSettings();
            cameraSettings = new CameraSettings();
            lightSettings = new LightSettings();
            shadowSettings = new ShadowSettings();
            directory = "Assets/Icons/";
            cameraTag = "EditorOnly";
            objectsLayer = "TransparentFX";
        }
    }
}