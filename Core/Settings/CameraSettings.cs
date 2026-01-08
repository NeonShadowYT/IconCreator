using UnityEngine;

namespace NeonImperium.IconsCreation
{
    [System.Serializable]
    public class CameraSettings
    {
        [Tooltip("Углы Эйлера для поворота камеры")]
        public Vector3 Rotation = new Vector3(45f, -45f, 0f);
        
        [Tooltip("Отступ от краев объекта в кадре")]
        public float Padding = 0.1f;
        
        [Tooltip("Рендерить тени на сцене создания иконок")]
        public bool RenderShadows = false;
    }
}