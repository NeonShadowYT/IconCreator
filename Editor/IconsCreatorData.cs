using System.Collections.Generic;
using System.Linq;
using NeonImperium.IconsCreation.Extensions;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    public enum IconType
    {
        Mesh,
        Texture
    }

    [System.Serializable]
    public class IconsCreatorData
    {
        public TextureSettings Texture { get; }
        public CameraSettings Camera { get; }
        public LightSettings Light { get; }
        public ShadowSettings Shadow { get; }
        public string Directory { get; }
        public IconType Type { get; }
        public GameObject[] MeshTargets { get; }
        public TextureObjectData[] TextureTargets { get; }
        public string CameraTag { get; }
        public string ObjectsLayer { get; }

        public IconsCreatorData(TextureSettings texture, CameraSettings camera, LightSettings light,
                              ShadowSettings shadow, string directory, List<Object> targets,
                              string cameraTag, string objectsLayer)
            : this(texture, camera, light, shadow, directory, IconType.Mesh, null, targets, cameraTag, objectsLayer)
        {
        }

        public IconsCreatorData(TextureSettings texture, CameraSettings camera, LightSettings light,
                              ShadowSettings shadow, string directory, List<TextureObjectData> textureTargets,
                              string cameraTag, string objectsLayer)
            : this(texture, camera, light, shadow, directory, IconType.Texture, textureTargets, null, cameraTag, objectsLayer)
        {
        }

        private IconsCreatorData(TextureSettings texture, CameraSettings camera, LightSettings light,
                               ShadowSettings shadow, string directory, IconType type,
                               List<TextureObjectData> textureTargets, List<Object> meshTargets,
                               string cameraTag, string objectsLayer)
        {
            Texture = texture;
            Camera = camera;
            Light = light;
            Shadow = shadow;
            Directory = directory;
            Type = type;
            CameraTag = cameraTag;
            ObjectsLayer = objectsLayer;

            if (type == IconType.Mesh)
            {
                MeshTargets = meshTargets.ExtractAllGameObjects().Where(g => g.HasVisibleMesh()).ToArray();
                TextureTargets = null;
            }
            else
            {
                TextureTargets = textureTargets?.Where(t => t.texture != null).ToArray() ?? new TextureObjectData[0];
                MeshTargets = null;
            }
        }
    }
}