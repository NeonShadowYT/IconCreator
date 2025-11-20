using UnityEngine;

namespace NeonImperium.IconsCreation
{
    [System.Serializable]
    public class CameraSettings
    {
        public Vector3 Rotation = new Vector3(45f, -45f, 0f);
        public float Padding = 0.1f;
        public bool RenderShadows = false;
    }
}