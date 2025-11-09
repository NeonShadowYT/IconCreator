using System.Collections.Generic;
using System.Linq;
using NeonImperium.IconsCreation.Extensions;
using UnityEngine;
using UnityEditor;

namespace NeonImperium.IconsCreation
{
    [System.Serializable]
    public class ShadowSettings
    {
        public bool Enabled = false;
        public Color Color = new Color(0f, 0f, 0f, 0.5f);
        public Vector2 Offset = new Vector2(0.05f, -0.05f);
        public float Scale = 0.95f;
    }

    [System.Serializable]
    public class CameraSettings
    {
        public Vector3 Rotation = new(45f, -45f, 0f);
        public float Padding = 0.1f;
        public bool RenderShadows = false;
    }

    [System.Serializable]
    public class TextureSettings
    {
        public TextureImporterCompression Compression = TextureImporterCompression.CompressedHQ;
        public FilterMode FilterMode = FilterMode.Point;
        public int AnisoLevel = 0;
        public int Size = 512;
    }

    [System.Serializable]
    public class IconsCreatorData
    {
        public TextureSettings Texture { get; }
        public CameraSettings Camera { get; }
        public ShadowSettings Shadow { get; }
        public string Directory { get; }
        public GameObject[] Targets { get; }

        public IconsCreatorData(TextureSettings texture, CameraSettings camera, ShadowSettings shadow, string directory, List<Object> targets)
        {
            Texture = texture;
            Camera = camera;
            Shadow = shadow;
            Directory = directory;
            Targets = targets.ExtractAllGameObjects().Where(g => g.HasVisibleMesh()).ToArray();
        }
    }
}