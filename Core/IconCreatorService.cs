using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    public class IconCreatorService
    {
        private IconsCreatorData _data;
        private readonly IconSceneService _sceneService;
        private readonly IconCameraService _cameraService;
        private readonly IconSaverService _saverService;

        public Texture2D[] CameraPreviews { get; private set; }

        public IconCreatorService()
        {
            _sceneService = new IconSceneService();
            _cameraService = new IconCameraService();
            _saverService = new IconSaverService();
        }

        public void InitializeEnvironment()
        {
            if (EditorApplication.isPlaying) return;

            try
            {
                EnsureCameraTagExists();
                _sceneService.EnsureSceneExists();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to initialize environment: {e.Message}");
            }
        }

        private void EnsureCameraTagExists()
        {
            if (EditorApplication.isPlaying) return;

            try
            {
                if (!InternalEditorUtility.tags.Contains("IconsCreationCamera"))
                    InternalEditorUtility.AddTag("IconsCreationCamera");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to ensure camera tag: {e.Message}");
            }
        }

        public void SetData(IconsCreatorData data)
        {
            if (EditorApplication.isPlaying) return;

            _data = data;
            
            try
            {
                _cameraService.Initialize(data.Texture, data.Camera, data.Shadow);
                _saverService.Initialize(data.Directory, data.Texture);
                
                UpdatePreview();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set data: {e.Message}");
            }
        }

        private void UpdatePreview()
        {
            if (!HasValidTargets) return;

            try
            {
                // Создаем превью для всех объектов
                CameraPreviews = new Texture2D[_data.Targets.Length];
                
                for (int i = 0; i < _data.Targets.Length; i++)
                {
                    if (_data.Targets[i] == null) continue;

                    _sceneService.ExecuteWithTarget(_data.Targets[i], _data.Light, _data.Camera.RenderShadows, target =>
                    {
                        if (target != null)
                        {
                            _cameraService.SetupForTarget(target);
                            CameraPreviews[i] = _cameraService.CaptureView();
                        }
                    });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to update preview: {e.Message}");
            }
        }

        public void CreateIcons()
        {
            if (!HasValidTargets) return;

            try
            {
                foreach (GameObject target in _data.Targets)
                {
                    if (target == null) continue;

                    _sceneService.ExecuteWithTarget(target, _data.Light, _data.Camera.RenderShadows, t =>
                    {
                        if (t != null)
                        {
                            _cameraService.SetupForTarget(t);
                            Texture2D icon = _cameraService.CaptureView();
                            if (icon != null)
                            {
                                _saverService.SaveIcon(icon, target.name);
                            }
                        }
                    });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create icons: {e.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                // Очищаем превью
                if (CameraPreviews != null)
                {
                    foreach (var preview in CameraPreviews)
                    {
                        if (preview != null)
                            UnityEngine.Object.DestroyImmediate(preview);
                    }
                    CameraPreviews = null;
                }

                _saverService.Dispose();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during disposal: {e.Message}");
            }
        }

        private bool HasValidTargets => _data?.Targets?.Any(t => t) ?? false;
    }
}