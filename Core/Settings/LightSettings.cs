using UnityEngine;

namespace NeonImperium.IconsCreation
{
    [System.Serializable]
    public class LightSettings
    {
        public LightType Type = LightType.Directional;
        public Vector3 DirectionalRotation = new(50f, -30f, 0f);
        public Color DirectionalColor = Color.white;
        public float DirectionalIntensity = 1f;
        
        [System.Serializable]
        public class PointLightData
        {
            public Vector3 Position = new(1, 0.5f, -0.5f);
            public Color Color = Color.white;
            public float Intensity = 1f;
        }
        
        public PointLightData[] PointLights = new PointLightData[2] 
        { 
            new(), 
            new() 
        };
    }
}