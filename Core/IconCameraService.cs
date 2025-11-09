using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using NeonImperium.IconsCreation.Extensions;

namespace NeonImperium.IconsCreation
{
    public class IconCameraService
    {
        private const string CAMERA_TAG = "IconsCreationCamera";
        
        private Camera _camera;
        private GameObject _targetObject;
        private Bounds _targetBounds;
        
        private TextureSettings _textureSettings;
        private CameraSettings _cameraSettings;
        private ShadowSettings _shadowSettings;
        
        private float _distanceToTarget = 10f;
        private Vector3 CameraOffset => -_camera.transform.forward * _distanceToTarget;

        public void Initialize(TextureSettings textureSettings, CameraSettings cameraSettings, ShadowSettings shadowSettings)
        {
            _textureSettings = textureSettings;
            _cameraSettings = cameraSettings;
            _shadowSettings = shadowSettings;
        }

        public void SetupForTarget(GameObject target)
        {
            _targetObject = target;
            RetrieveCamera();
            ConfigureCamera();
            AdjustCamera();
        }

        private void RetrieveCamera()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();

            if (_camera && _camera.scene == activeScene && _camera.gameObject.CompareTag(CAMERA_TAG))
                return;

            foreach (GameObject rootObject in activeScene.GetRootGameObjects())
            {
                Camera camera = rootObject.GetComponentInChildren<Camera>();
                if (camera && camera.CompareTag(CAMERA_TAG))
                {
                    _camera = camera;
                    break;
                }
            }

            if (!_camera)
                Debug.LogWarning($"No camera tagged '{CAMERA_TAG}' found!");
        }

        private void ConfigureCamera()
        {
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.orthographic = true;
        }

        private void AdjustCamera()
        {
            if (!_targetObject) return;

            _targetBounds = _targetObject.GetOrthographicBounds(_camera);
            
            SetCameraRotation();
            SetCameraPosition();
            SetCameraSize();
        }

        private void SetCameraRotation()
        {
            _camera.transform.rotation = Quaternion.Euler(_cameraSettings.Rotation);
        }

        private void SetCameraPosition()
        {
            _distanceToTarget = _targetBounds.size.z / 2 + 10;
            Vector3 targetCenter = _targetBounds.center;
            _camera.transform.position = targetCenter + CameraOffset;
        }

        private void SetCameraSize()
        {
            Vector3 min = _camera.transform.InverseTransformPoint(_targetBounds.min);
            Vector3 max = _camera.transform.InverseTransformPoint(_targetBounds.max);
            
            // Используем только X и Y для 2D расчетов
            Vector2 min2D = new Vector2(min.x, min.y);
            Vector2 max2D = new Vector2(max.x, max.y);
            Vector2 distance2D = (max2D - min2D).Abs();

            _camera.orthographicSize = distance2D.BiggestComponentValue() * 0.5f / (1 - _cameraSettings.Padding);
        }

        public Texture2D CaptureView()
        {
            if (_textureSettings.Size < 1)
                throw new System.ArgumentOutOfRangeException(nameof(_textureSettings.Size));

            RenderTexture tempRT = RenderTexture.GetTemporary(_textureSettings.Size, _textureSettings.Size, 24);
            _camera.targetTexture = tempRT;
            RenderTexture.active = tempRT;

            _camera.Render();

            Texture2D image = new Texture2D(_textureSettings.Size, _textureSettings.Size, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, _textureSettings.Size, _textureSettings.Size), 0, 0);
            image.Apply();
            
            _camera.targetTexture = null;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(tempRT);

            // Apply shadow if enabled
            if (_shadowSettings.Enabled)
            {
                image = ApplyShadowToTexture(image);
            }

            return image;
        }

        private Texture2D ApplyShadowToTexture(Texture2D originalTexture)
        {
            int width = originalTexture.width;
            int height = originalTexture.height;
            
            Texture2D resultTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            
            // Calculate shadow offset in pixels
            int offsetX = (int)(_shadowSettings.Offset.x * width);
            int offsetY = (int)(_shadowSettings.Offset.y * height);
            
            // Calculate scaled dimensions for shadow
            int shadowWidth = (int)(width * _shadowSettings.Scale);
            int shadowHeight = (int)(height * _shadowSettings.Scale);
            int shadowOffsetX = (width - shadowWidth) / 2;
            int shadowOffsetY = (height - shadowHeight) / 2;

            Color[] originalPixels = originalTexture.GetPixels();
            Color[] resultPixels = new Color[width * height];

            // First pass: draw shadow
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    
                    // Calculate position in scaled shadow
                    int shadowX = x - shadowOffsetX - offsetX;
                    int shadowY = y - shadowOffsetY - offsetY;
                    
                    if (shadowX >= 0 && shadowX < shadowWidth && shadowY >= 0 && shadowY < shadowHeight)
                    {
                        // Map to original texture coordinates for sampling
                        int sourceX = (int)((float)shadowX / shadowWidth * width);
                        int sourceY = (int)((float)shadowY / shadowHeight * height);
                        sourceX = Mathf.Clamp(sourceX, 0, width - 1);
                        sourceY = Mathf.Clamp(sourceY, 0, height - 1);
                        
                        int sourceIndex = sourceY * width + sourceX;
                        Color sourceColor = originalPixels[sourceIndex];
                        
                        // Use alpha from original texture for shadow shape
                        if (sourceColor.a > 0)
                        {
                            resultPixels[index] = _shadowSettings.Color;
                        }
                    }
                }
            }

            // Second pass: draw original image over shadow
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Color originalColor = originalPixels[index];
                    Color shadowColor = resultPixels[index];
                    
                    if (originalColor.a > 0)
                    {
                        // Blend original over shadow
                        resultPixels[index] = Color.Lerp(shadowColor, originalColor, originalColor.a);
                    }
                    else if (shadowColor.a > 0)
                    {
                        // Keep shadow where there's no original content
                        resultPixels[index] = shadowColor;
                    }
                    else
                    {
                        // Transparent background
                        resultPixels[index] = Color.clear;
                    }
                }
            }

            resultTexture.SetPixels(resultPixels);
            resultTexture.Apply();
            
            UnityEngine.Object.DestroyImmediate(originalTexture);
            
            return resultTexture;
        }
    }
}