using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    [System.Serializable]
    public class TextureSettings
    {
        [Tooltip("Качество сжатия текстуры")]
        public TextureImporterCompression Compression = TextureImporterCompression.CompressedHQ;
        
        [Tooltip("Метод фильтрации текстуры")]
        public FilterMode FilterMode = FilterMode.Point;
        
        [Tooltip("Уровень анизотропной фильтрации")]
        public int AnisoLevel = 0;
        
        [Tooltip("Размер текстуры в пикселях")]
        public int Size = 512;
    }
}