using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using NeonImperium.IconsCreation.Extensions;

namespace NeonImperium.IconsCreation
{
    public class IconCameraService
    {
        private Camera _camera;
        private GameObject _targetObject;
        private Bounds _targetBounds;
        private Scene _cachedScene;

        private TextureSettings _textureSettings;
        private CameraSettings _cameraSettings;
        private ShadowSettings _shadowSettings;
        private string _cameraTag;

        private float _distanceToTarget = 10f;
        private Vector3 CameraOffset => -_camera.transform.forward * _distanceToTarget;
        private bool _cameraWasActive;

        private RenderTexture _cachedRT;
        private int _cachedRTSize;

        public void Initialize(TextureSettings textureSettings, CameraSettings cameraSettings,
            ShadowSettings shadowSettings, string cameraTag)
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

            if (!_camera.gameObject.activeSelf)
                _camera.gameObject.SetActive(true);

            ConfigureCamera();
            AdjustCamera();
            return true;
        }

        public void SetupForTexture(string objectsLayer)
        {
            if (!RetrieveCamera()) return;

            _camera.orthographic = true;
            _camera.orthographicSize = 1f;
            _camera.transform.rotation = Quaternion.identity;
            _camera.transform.position = new Vector3(0f, 0f, -10f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;

            int layerMask = LayerMask.GetMask(objectsLayer);
            if (layerMask == 0) layerMask = LayerMask.GetMask("Default");
            _camera.cullingMask = layerMask;
            _camera.gameObject.SetActive(true);
        }

        private bool RetrieveCamera()
        {
            if (EditorApplication.isPlaying) return false;

            Scene activeScene = EditorSceneManager.GetActiveScene();

            if (_camera != null
                && _camera.gameObject.scene.IsValid()
                && _camera.gameObject.scene == activeScene
                && _camera.CompareTag(_cameraTag))
            {
                _cachedScene = activeScene;
                return true;
            }

            _camera = null;

            foreach (GameObject rootObject in activeScene.GetRootGameObjects())
            {
                if (rootObject == null) continue;

                if (rootObject.TryGetComponent(out Camera cam) && cam.CompareTag(_cameraTag))
                {
                    _camera = cam;
                    break;
                }
            }

            if (_camera == null)
            {
                GameObject cameraGO = new GameObject("IconsCreator_Camera");
                _camera = cameraGO.AddComponent<Camera>();
                _camera.tag = _cameraTag;
                SceneManager.MoveGameObjectToScene(cameraGO, activeScene);
                cameraGO.SetActive(false);
            }

            _cachedScene = activeScene;
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
            _camera.allowMSAA = false;
            _camera.allowHDR = false;
        }

        private void AdjustCamera()
        {
            if (!_targetObject || _camera == null) return;

            SetCameraRotation();
            _targetBounds = _targetObject.GetOrthographicBounds(_camera);
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

            _camera.orthographicSize = Mathf.Max(
                distance2D.BiggestComponentValue() * 0.5f / (1 - Mathf.Clamp01(_cameraSettings.Padding)),
                0.1f);
        }

        // ============================================================
        // Capture
        // ============================================================

        /// <summary>
        /// Захват вида.
        /// isPreview=true  → PreviewSize (клампится 64..1024)
        /// isPreview=false → RenderSize
        /// applyShadow=true → накладывает 2D-силуэт тени, если он включён в ShadowSettings
        /// </summary>
        public Texture2D CaptureView(bool isPreview, bool applyShadow = true)
        {
            if (_camera == null || _textureSettings == null)
                return CreateFallbackTexture();

            int renderSize;

            if (isPreview)
            {
                int p = _textureSettings.PreviewSize;
                if (p <= 0) p = 512;
                renderSize = Mathf.Clamp(p, 64, 1024);
            }
            else
            {
                renderSize = Mathf.Max(64, _textureSettings.RenderSize);
            }

            RenderTexture rt = GetOrCreateRT(renderSize);
            if (rt == null) return CreateFallbackTexture();

            Texture2D image = null;

            try
            {
                _cameraWasActive = _camera.gameObject.activeSelf;
                _camera.gameObject.SetActive(true);

                RenderTexture previous = _camera.targetTexture;
                RenderTexture.active = rt;
                _camera.targetTexture = rt;

                _camera.Render();

                image = new Texture2D(renderSize, renderSize, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, renderSize, renderSize), 0, 0);
                image.Apply();

                bool shadowEnabled = applyShadow && _shadowSettings != null && _shadowSettings.Enabled;
                if (shadowEnabled)
                {
                    Texture2D shadowed = ApplyShadowToTexture(image, _shadowSettings);
                    if (shadowed != null) image = shadowed;
                }

                _camera.targetTexture = previous;
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
            }

            return image;
        }

        // ============================================================
        // Reusable RenderTexture
        // ============================================================

        private RenderTexture GetOrCreateRT(int size)
        {
            if (_cachedRT != null && _cachedRTSize == size && _cachedRT.IsCreated())
                return _cachedRT;

            ReleaseRT();

            _cachedRT = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32)
            {
                name = $"IconCreator_RT_{size}"
            };
            _cachedRT.Create();
            _cachedRTSize = size;
            return _cachedRT;
        }

        private void ReleaseRT()
        {
            if (_cachedRT == null) return;

            if (_cachedRT.IsCreated()) _cachedRT.Release();
            UnityEngine.Object.DestroyImmediate(_cachedRT);
            _cachedRT = null;
            _cachedRTSize = 0;
        }

        // ============================================================
        // Helpers
        // ============================================================

        private Texture2D CreateFallbackTexture()
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color[] colors = new Color[64 * 64];
            Color fallbackColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

            for (int i = 0; i < colors.Length; i++)
                colors[i] = fallbackColor;

            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Накладывает 2D-силуэт тени за объектом.
        /// Логика: тень — это сжатый силуэт, смещённый на Offset.
        /// Порядок пикселя: тень (снизу) → оригинал (сверху).
        /// Прозрачные пиксели оригинала показывают тень.
        /// </summary>
        private Texture2D ApplyShadowToTexture(Texture2D originalTexture, ShadowSettings shadow)
        {
            if (originalTexture == null || shadow == null) return originalTexture;

            int width = originalTexture.width;
            int height = originalTexture.height;
            int total = width * height;

            Color[] originalPixels = originalTexture.GetPixels();
            Color[] resultPixels = new Color[total];

            // Параметры тени
            float scale = Mathf.Clamp(shadow.Scale, 0.1f, 2f);
            int shadowWidth = Mathf.Max(1, Mathf.RoundToInt(width * scale));
            int shadowHeight = Mathf.Max(1, Mathf.RoundToInt(height * scale));

            int offsetX = Mathf.RoundToInt(shadow.Offset.x * width);
            int offsetY = Mathf.RoundToInt(shadow.Offset.y * height);

            // Центрируем + смещаем
            int shadowStartX = (width - shadowWidth) / 2 + offsetX;
            int shadowStartY = (height - shadowHeight) / 2 + offsetY;

            // Фаза 1: строим силуэт тени
            // Для каждого пикселя тени берём пиксель оригинала через маппинг и, если он непрозрачный,
            // красим выходной пиксель цветом тени.
            for (int sy = 0; sy < shadowHeight; sy++)
            {
                int dstY = sy + shadowStartY;
                if (dstY < 0 || dstY >= height) continue;

                // Обратный маппинг: какому пикселю оригинала соответствует
                int srcY = Mathf.Clamp(
                    Mathf.FloorToInt((float)sy / shadowHeight * height),
                    0, height - 1);

                for (int sx = 0; sx < shadowWidth; sx++)
                {
                    int dstX = sx + shadowStartX;
                    if (dstX < 0 || dstX >= width) continue;

                    int srcX = Mathf.Clamp(
                        Mathf.FloorToInt((float)sx / shadowWidth * width),
                        0, width - 1);

                    float srcAlpha = originalPixels[srcY * width + srcX].a;
                    if (srcAlpha < 0.01f) continue;

                    // Альфа тени = альфа силуэта * альфа цвета тени (для мягких краёв)
                    Color shadowColor = shadow.Color;
                    shadowColor.a *= srcAlpha;

                    resultPixels[dstY * width + dstX] = shadowColor;
                }
            }

            // Фаза 2: накладываем оригинал поверх тени (straight alpha blending)
            for (int i = 0; i < total; i++)
            {
                Color orig = originalPixels[i];
                Color sh = resultPixels[i];

                if (orig.a >= 0.999f)
                {
                    resultPixels[i] = orig;
                }
                else if (orig.a <= 0.001f)
                {
                    resultPixels[i] = sh;  // уже может быть 0
                }
                else
                {
                    // Standard "over": orig over sh
                    float outA = orig.a + sh.a * (1f - orig.a);
                    if (outA < 0.001f)
                    {
                        resultPixels[i] = Color.clear;
                    }
                    else
                    {
                        float r = (orig.r * orig.a + sh.r * sh.a * (1f - orig.a)) / outA;
                        float g = (orig.g * orig.a + sh.g * sh.a * (1f - orig.a)) / outA;
                        float b = (orig.b * orig.a + sh.b * sh.a * (1f - orig.a)) / outA;
                        resultPixels[i] = new Color(r, g, b, outA);
                    }
                }
            }

            Texture2D resultTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            resultTexture.SetPixels(resultPixels);
            resultTexture.Apply();

            UnityEngine.Object.DestroyImmediate(originalTexture);
            return resultTexture;
        }

        public void Cleanup()
        {
            if (_camera != null && _cameraWasActive)
                _camera.gameObject.SetActive(false);

            _camera = null;
            _targetObject = null;
            _cachedScene = default;
            _cameraWasActive = false;

            ReleaseRT();
        }
    }
}