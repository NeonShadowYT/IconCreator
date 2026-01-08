using UnityEngine;

namespace NeonImperium.IconsCreation
{
    [System.Serializable]
    public class LightSettings
    {
        [Tooltip("Тип освещения: направленный или точечный")]
        public LightType Type = LightType.Directional;
        
        [Tooltip("Углы Эйлера для направления света")]
        public Vector3 DirectionalRotation = new(50f, -30f, 0f);
        
        [Tooltip("Цвет направленного света")]
        public Color DirectionalColor = Color.white;
        
        [Tooltip("Яркость направленного света")]
        public float DirectionalIntensity = 1f;
        
        [System.Serializable]
        public class PointLightData
        {
            [Tooltip("Позиция в локальном пространстве сцены")]
            public Vector3 Position = new(1, 0.5f, -0.5f);
            
            [Tooltip("Цвет точечного света")]
            public Color Color = Color.white;
            
            [Tooltip("Яркость точечного света")]
            public float Intensity = 1f;
        }
        
        [Tooltip("Настройки точечных источников света")]
        public PointLightData[] PointLights = new PointLightData[2] 
        { 
            new(), 
            new() 
        };
    }
}