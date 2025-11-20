using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    [System.Serializable]
    public class TextureSettings
    {
        public TextureImporterCompression Compression = TextureImporterCompression.CompressedHQ;
        public FilterMode FilterMode = FilterMode.Point;
        public int AnisoLevel = 0;
        public int Size = 512;
    }
}