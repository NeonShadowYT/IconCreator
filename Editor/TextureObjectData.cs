using UnityEngine;

namespace NeonImperium.IconsCreation
{
    [System.Serializable]
    public class TextureObjectData
    {
        public Texture2D texture;
        public Vector3 position;
        public float rotation; // degrees around Z
        public Vector2 scale = Vector2.one;
        public Color color = Color.white;
    }
}