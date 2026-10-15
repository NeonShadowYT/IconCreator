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
        public IconType iconType;
        public TextureObjectData[] textureObjects;

        public PresetData()
        {
            textureSettings = new TextureSettings();
            cameraSettings = new CameraSettings();
            lightSettings = new LightSettings();
            shadowSettings = new ShadowSettings();
            iconType = IconType.Mesh;
            textureObjects = new TextureObjectData[0];
        }
    }
}