using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using NeonImperium.IconsCreation.Extensions;
using UnityEditor;

namespace NeonImperium.IconsCreation
{
    public class IconCameraService
    {
        private Camera _camera;
        private GameObject _targetObject;
        private Bounds _targetBounds;
        
        private TextureSettings _textureSettings;
        private CameraSettings _cameraSettings;
        private ShadowSettings _shadowSettings;
        private string _cameraTag;
        
        private float _distanceToTarget = 10f;
        private Vector3 CameraOffset => -_camera.transform.forward * _distanceToTarget;
        private bool _cameraWasActive;

        public void Initialize(TextureSettings textureSettings, CameraSettings cameraSettings, ShadowSettings shadowSettings, string cameraTag)
        {
            _textureSettings = textureSettings;
            _cameraSettings = cameraSettings;
            _shadowSettings = shadowSettings;
            _cameraTag = cameraTag;
        }

        public bool SetupForTarget(GameObject target)
        {
            if (target == null) return false;
            
            _targetObject = target;
            
            if (!RetrieveCamera())
                return false;
                
            ConfigureCamera();
            AdjustCamera();
            return true;
        }

        private bool RetrieveCamera()
        {
            if (EditorApplication.isPlaying) return false;

            Scene activeScene = EditorSceneManager.GetActiveScene();

            if (_camera && _camera.scene == activeScene && _camera.gameObject.CompareTag(_cameraTag))
                return true;

            _camera = null;
            foreach (GameObject rootObject in activeScene.GetRootGameObjects())
            {
                Camera camera = rootObject.GetComponentInChildren<Camera>();
                if (camera && camera.CompareTag(_cameraTag))
                {
                    _camera = camera;
                    break;
                }
            }

            if (_camera == null)
            {
                GameObject cameraGO = new GameObject("IconsCreator_Camera");
                _camera = cameraGO.AddComponent<Camera>();
                _camera.tag = _cameraTag;
                cameraGO.SetActive(false);
            }
            
            return _camera != null;
        }

        private void ConfigureCamera()
        {
            if (_camera == null) return;

            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;
            _camera.nearClipPlane = 0.01f;
            _camera.farClipPlane = 1000f;
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
            _distanceToTarget = Mathf.Max(_targetBounds.size.z / 2 + 10, 5f);
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

            _camera.orthographicSize = Mathf.Max(distance2D.BiggestComponentValue() * 0.5f / (1 - Mathf.Clamp01(_cameraSettings.Padding)), 0.1f);
        }

        public Texture2D CaptureView()
        {
            if (_camera == null || _textureSettings.Size < 1)
            {
                return CreateFallbackTexture();
            }

            RenderTexture tempRT = null;
            Texture2D image = null;

            try
            {
                _cameraWasActive = _camera.gameObject.activeSelf;
                _camera.gameObject.SetActive(true);
                
                tempRT = RenderTexture.GetTemporary(_textureSettings.Size, _textureSettings.Size, 24, RenderTextureFormat.ARGB32);
                if (tempRT == null) return CreateFallbackTexture();
                
                RenderTexture previousRT = _camera.targetTexture;
                RenderTexture.active = tempRT;
                _camera.targetTexture = tempRT;

                _camera.Render();

                image = new Texture2D(_textureSettings.Size, _textureSettings.Size, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, _textureSettings.Size, _textureSettings.Size), 0, 0);
                image.Apply();
                
                if (_shadowSettings.Enabled && image != null)
                {
                    Texture2D shadowedImage = ApplyShadowToTexture(image);
                    if (shadowedImage != null)
                    {
                        UnityEngine.Object.DestroyImmediate(image);
                        image = shadowedImage;
                    }
                }
                
                _camera.targetTexture = previousRT;
                _camera.gameObject.SetActive(_cameraWasActive);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to capture view: {e.Message}");
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                image = CreateFallbackTexture();
            }
            finally
            {
                RenderTexture.active = null;
                if (tempRT != null)
                    RenderTexture.ReleaseTemporary(tempRT);
            }

            return image;
        }

        private Texture2D CreateFallbackTexture()
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color[] colors = new Color[64 * 64];
            Color fallbackColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = fallbackColor;
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
                
                int offsetX = (int)(_shadowSettings.Offset.x * width);
                int offsetY = (int)(_shadowSettings.Offset.y * height);
                
                int shadowWidth = Mathf.Clamp((int)(width * _shadowSettings.Scale), 1, width);
                int shadowHeight = Mathf.Clamp((int)(height * _shadowSettings.Scale), 1, height);
                int shadowOffsetX = (width - shadowWidth) / 2;
                int shadowOffsetY = (height - shadowHeight) / 2;

                Color[] originalPixels = originalTexture.GetPixels();
                Color[] resultPixels = new Color[width * height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        
                        int shadowX = x - shadowOffsetX - offsetX;
                        int shadowY = y - shadowOffsetY - offsetY;
                        
                        if (shadowX >= 0 && shadowX < shadowWidth && shadowY >= 0 && shadowY < shadowHeight)
                        {
                            int sourceX = (int)((float)shadowX / shadowWidth * width);
                            int sourceY = (int)((float)shadowY / shadowHeight * height);
                            sourceX = Mathf.Clamp(sourceX, 0, width - 1);
                            sourceY = Mathf.Clamp(sourceY, 0, height - 1);
                            
                            int sourceIndex = sourceY * width + sourceX;
                            Color sourceColor = originalPixels[sourceIndex];
                            
                            if (sourceColor.a > 0.01f)
                            {
                                resultPixels[index] = _shadowSettings.Color;
                            }
                        }
                    }
                }

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        Color originalColor = originalPixels[index];
                        Color shadowColor = resultPixels[index];
                        
                        if (originalColor.a > 0.01f)
                        {
                            resultPixels[index] = Color.Lerp(shadowColor, originalColor, originalColor.a);
                        }
                        else if (shadowColor.a > 0.01f)
                        {
                            resultPixels[index] = shadowColor;
                        }
                        else
                        {
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
                return originalTexture;
            }
        }
        
        public void Cleanup()
        {
            if (_camera != null && _cameraWasActive)
            {
                _camera.gameObject.SetActive(false);
            }
            _camera = null;
            _targetObject = null;
        }
    }
}