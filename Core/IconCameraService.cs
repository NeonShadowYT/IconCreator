using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using NeonImperium.IconsCreation.Extensions;
using UnityEditor;

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
            if (_camera == null)
            {
                Debug.LogError("Camera not found for icon creation");
                return;
            }
            ConfigureCamera();
            AdjustCamera();
        }

        private void RetrieveCamera()
        {
            if (EditorApplication.isPlaying) return;

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
            if (_camera == null) return;

            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.orthographic = true;
        }

        private void AdjustCamera()
        {
            if (!_targetObject || _camera == null) return;

            _targetBounds = _targetObject.GetOrthographicBounds(_camera);
            
            SetCameraRotation();
            SetCameraPosition();
            SetCameraSize();
        }

        private void SetCameraRotation()
        {
            if (_camera == null) return;
            _camera.transform.rotation = Quaternion.Euler(_cameraSettings.Rotation);
        }

        private void SetCameraPosition()
        {
            if (_camera == null) return;
            _distanceToTarget = _targetBounds.size.z / 2 + 10;
            Vector3 targetCenter = _targetBounds.center;
            _camera.transform.position = targetCenter + CameraOffset;
        }

        private void SetCameraSize()
        {
            if (_camera == null) return;
            Vector3 min = _camera.transform.InverseTransformPoint(_targetBounds.min);
            Vector3 max = _camera.transform.InverseTransformPoint(_targetBounds.max);
            
            Vector2 min2D = new Vector2(min.x, min.y);
            Vector2 max2D = new Vector2(max.x, max.y);
            Vector2 distance2D = (max2D - min2D).Abs();

            _camera.orthographicSize = distance2D.BiggestComponentValue() * 0.5f / (1 - _cameraSettings.Padding);
        }

        public Texture2D CaptureView()
        {
            if (_camera == null)
            {
                Debug.LogError("Cannot capture view: camera is null");
                return CreateFallbackTexture();
            }

            if (_textureSettings.Size < 1)
            {
                Debug.LogError($"Invalid texture size: {_textureSettings.Size}");
                return CreateFallbackTexture();
            }

            RenderTexture tempRT = null;
            Texture2D image = null;

            try
            {
                tempRT = RenderTexture.GetTemporary(_textureSettings.Size, _textureSettings.Size, 24);
                _camera.targetTexture = tempRT;
                RenderTexture.active = tempRT;

                // Безопасный рендеринг с обработкой исключений
                SafeCameraRender();

                image = new Texture2D(_textureSettings.Size, _textureSettings.Size, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, _textureSettings.Size, _textureSettings.Size), 0, 0);
                image.Apply();
                
                // Apply shadow if enabled
                if (_shadowSettings.Enabled && image != null)
                {
                    var shadowedImage = ApplyShadowToTexture(image);
                    if (shadowedImage != null)
                    {
                        UnityEngine.Object.DestroyImmediate(image);
                        image = shadowedImage;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to capture camera view: {e.Message}");
                if (image != null)
                {
                    UnityEngine.Object.DestroyImmediate(image);
                    image = null;
                }
                image = CreateFallbackTexture();
            }
            finally
            {
                // Всегда очищаем ресурсы
                if (_camera != null)
                    _camera.targetTexture = null;
                RenderTexture.active = null;
                if (tempRT != null)
                    RenderTexture.ReleaseTemporary(tempRT);
            }

            return image;
        }

        private void SafeCameraRender()
        {
            if (_camera == null) return;

            try
            {
                // Пытаемся рендерить стандартным способом
                _camera.Render();
            }
            catch (System.NullReferenceException)
            {
                // Игнорируем NullReferenceException от внутренних компонентов Unity
                Debug.LogWarning("Camera render caused NullReferenceException (ignored)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Camera render failed: {e.Message}");
                throw;
            }
        }

        private Texture2D CreateFallbackTexture()
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var colors = new Color[64 * 64];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            }
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        private Texture2D ApplyShadowToTexture(Texture2D originalTexture)
        {
            if (originalTexture == null) return null;

            try
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
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to apply shadow: {e.Message}");
                return originalTexture; // Возвращаем оригинальную текстуру в случае ошибки
            }
        }
    }
}