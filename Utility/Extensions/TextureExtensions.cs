using UnityEngine;

namespace NeonImperium.IconsCreation.Extensions
{
    public static class TextureExtensions
    {
        public static Texture2D Resize(this Texture2D texture, int targetSize)
        {
            if (texture == null || targetSize <= 0 || targetSize > 8192) return null;

            FilterMode filterMode = texture.filterMode;

            RenderTexture temporaryRenderTexture = RenderTexture.GetTemporary(targetSize, targetSize);
            if (temporaryRenderTexture == null) return null;

            RenderTexture.active = temporaryRenderTexture;
            
            Graphics.Blit(texture, temporaryRenderTexture);

            Texture2D resizedTexture = new Texture2D(targetSize, targetSize)
            {
                filterMode = filterMode
            };

            resizedTexture.ReadPixels(new Rect(0, 0, targetSize, targetSize), 0, 0);
            resizedTexture.Apply();

            RenderTexture.ReleaseTemporary(temporaryRenderTexture);
            RenderTexture.active = null;

            return resizedTexture;
        }
    }
}