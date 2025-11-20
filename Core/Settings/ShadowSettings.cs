using UnityEngine;

namespace NeonImperium.IconsCreation
{
    [System.Serializable]
    public class ShadowSettings
    {
        public bool Enabled = false;
        public Color Color = new(0f, 0f, 0f, 0.5f);
        public Vector2 Offset = new(0.05f, -0.05f);
        public float Scale = 0.95f;
    }
}