using System.Collections.Generic;
using System.Linq;
using NeonImperium.IconsCreation.Extensions;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    [System.Serializable]
    public class IconsCreatorData
    {
        public TextureSettings Texture { get; }
        public CameraSettings Camera { get; }
        public LightSettings Light { get; }
        public ShadowSettings Shadow { get; }
        public string Directory { get; }
        public GameObject[] Targets { get; }

        public IconsCreatorData(TextureSettings texture, CameraSettings camera, LightSettings light, ShadowSettings shadow, string directory, List<Object> targets)
        {
            Texture = texture;
            Camera = camera;
            Light = light;
            Shadow = shadow;
            Directory = directory;
            Targets = targets.ExtractAllGameObjects().Where(g => g.HasVisibleMesh()).ToArray();
        }
    }
}