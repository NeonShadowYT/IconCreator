using System.Linq;
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
            EnsureCameraTagExists();
            _sceneService.EnsureSceneExists();
        }

        private void EnsureCameraTagExists()
        {
            if (!InternalEditorUtility.tags.Contains("IconsCreationCamera"))
                InternalEditorUtility.AddTag("IconsCreationCamera");
        }

        public void SetData(IconsCreatorData data)
        {
            _data = data;
            
            _cameraService.Initialize(data.Texture, data.Camera, data.Shadow);
            _saverService.Initialize(data.Directory, data.Texture);
            
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (!HasValidTargets) return;

            // Создаем превью для всех объектов
            CameraPreviews = new Texture2D[_data.Targets.Length];
            
            for (int i = 0; i < _data.Targets.Length; i++)
            {
                _sceneService.ExecuteWithTarget(_data.Targets[i], _data.Camera.RenderShadows, target =>
                {
                    if (target != null)
                    {
                        _cameraService.SetupForTarget(target);
                        CameraPreviews[i] = _cameraService.CaptureView();
                    }
                });
            }
        }

        public void CreateIcons()
        {
            if (!HasValidTargets) return;

            foreach (GameObject target in _data.Targets)
            {
                _sceneService.ExecuteWithTarget(target, _data.Camera.RenderShadows, t =>
                {
                    _cameraService.SetupForTarget(t);
                    Texture2D icon = _cameraService.CaptureView();
                    _saverService.SaveIcon(icon, t.name);
                });
            }
        }

        public void Dispose()
        {
            _saverService.Dispose();
        }

        private bool HasValidTargets => _data?.Targets?.Any(t => t) ?? false;
    }
}