using UnityEngine;

namespace NeonImperium.IconsCreation
{
    [System.Serializable]
    public class ShadowSettings
    {
        [Tooltip("Включить отрисовку искусственной тени")]
        public bool Enabled = false;
        
        [Tooltip("Цвет и прозрачность тени")]
        public Color Color = new(0f, 0f, 0f, 0.5f);
        
        [Tooltip("Смещение тени относительно объекта")]
        public Vector2 Offset = new(0.05f, -0.05f);
        
        [Tooltip("Масштаб тени относительно объекта")]
        public float Scale = 0.95f;
    }
}