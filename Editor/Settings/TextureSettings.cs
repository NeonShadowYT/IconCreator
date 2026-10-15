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

        [Tooltip("Размер текстуры в пикселях (Max Size при импорте)")]
        public int Size = 512;

        [Tooltip("Разрешение рендеринга готовой иконки")]
        public int RenderSize = 2048;

        [Tooltip("Разрешение рендеринга превью (не влияет на финальную иконку)")]
        public int PreviewSize = 256;
    }
}