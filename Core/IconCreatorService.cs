using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    public class IconCreatorService
    {
        private IconsCreatorData _data;
        private readonly IconSceneService _sceneService;
        private readonly IconCameraService _cameraService;
        private readonly IconSaverService _saverService;
        private EditorCoroutine _generationCoroutine;
        private EditorCoroutine _previewCoroutine;
        private bool _previewDirty = true;

        public Texture2D[] CameraPreviews { get; private set; }
        public bool IsGenerating { get; private set; }
        public float GenerationProgress { get; private set; }
        public string GenerationStatus { get; private set; }

        public IconCreatorService()
        {
            _sceneService = new IconSceneService();
            _cameraService = new IconCameraService();
            _saverService = new IconSaverService();
        }

        public void InitializeEnvironment()
        {
            if (EditorApplication.isPlaying) return;
            _sceneService.EnsureSceneExists();
        }

        public void SetData(IconsCreatorData data)
        {
            if (EditorApplication.isPlaying) return;
            
            if (_data == null || !DataEquals(_data, data))
            {
                _data = data;
                _previewDirty = true;
                
                _cameraService.Initialize(data.Texture, data.Camera, data.Shadow, data.CameraTag);
                _saverService.Initialize(data.Directory, data.Texture);
            }
        }

        private bool DataEquals(IconsCreatorData a, IconsCreatorData b)
        {
            if (a == null || b == null) return false;
            if (a.Targets == null || b.Targets == null) return false;
            
            if (a.Targets.Length != b.Targets.Length) return false;
            
            for (int i = 0; i < a.Targets.Length; i++)
            {
                if (a.Targets[i] != b.Targets[i]) return false;
            }
            
            return a.CameraTag == b.CameraTag &&
                   a.ObjectsLayer == b.ObjectsLayer &&
                   a.Texture.Size == b.Texture.Size &&
                   a.Texture.Compression == b.Texture.Compression &&
                   a.Texture.FilterMode == b.Texture.FilterMode &&
                   a.Texture.AnisoLevel == b.Texture.AnisoLevel &&
                   a.Camera.Padding == b.Camera.Padding &&
                   a.Camera.Rotation == b.Camera.Rotation &&
                   a.Camera.RenderShadows == b.Camera.RenderShadows &&
                   a.Light.Type == b.Light.Type &&
                   a.Shadow.Enabled == b.Shadow.Enabled &&
                   a.Shadow.Color == b.Shadow.Color &&
                   a.Shadow.Offset == b.Shadow.Offset &&
                   a.Shadow.Scale == b.Shadow.Scale;
        }

        public void RequestPreviewUpdate(bool forceUpdate = false)
        {
            if (!HasValidTargets || IsGenerating) return;
            
            if (_previewDirty || forceUpdate)
            {
                _previewDirty = false;
                
                if (_previewCoroutine != null)
                {
                    EditorCoroutine.Stop(_previewCoroutine);
                }
                
                _previewCoroutine = EditorCoroutine.Start(UpdatePreviewCoroutine());
            }
        }

        private IEnumerator UpdatePreviewCoroutine()
        {
            yield return null;
            
            ClearPreviews();
            
            if (_data == null || _data.Targets == null)
                yield break;
                
            GameObject[] validTargets = _data.Targets.Where(t => t != null).ToArray();
            CameraPreviews = new Texture2D[validTargets.Length];
            
            for (int i = 0; i < validTargets.Length; i++)
            {
                GameObject target = validTargets[i];
                if (target == null) continue;
                
                string targetName = CleanObjectName(target.name);
                bool completed = false;
                
                _sceneService.ExecuteWithTarget(target, targetName, _data.Light, _data.Camera.RenderShadows, 
                    _data.CameraTag, _data.ObjectsLayer, targetInstance =>
                {
                    if (targetInstance != null)
                    {
                        _cameraService.SetupForTarget(targetInstance);
                        CameraPreviews[i] = _cameraService.CaptureView();
                    }
                    completed = true;
                });
                
                while (!completed)
                    yield return null;
                    
                if (i % 2 == 0)
                    yield return null;
            }
        }

        public void CreateIconsAsync()
        {
            if (!HasValidTargets || IsGenerating) return;
            
            StopGeneration();
            _generationCoroutine = EditorCoroutine.Start(CreateIconsCoroutine());
        }

        private IEnumerator CreateIconsCoroutine()
        {
            IsGenerating = true;
            GenerationProgress = 0f;
            GenerationStatus = "Подготовка...";
            
            GameObject[] targets = _data.Targets.Where(t => t != null).ToArray();
            int totalTargets = targets.Length;
            
            if (totalTargets == 0)
            {
                StopGeneration();
                yield break;
            }
            
            for (int i = 0; i < totalTargets; i++)
            {
                if (targets[i] == null) continue;
                
                GenerationProgress = (float)i / totalTargets;
                GenerationStatus = $"Обработка {i + 1} из {totalTargets}";
                
                string targetName = CleanObjectName(targets[i].name);
                bool completed = false;
                
                _sceneService.ExecuteWithTarget(targets[i], targetName, _data.Light, _data.Camera.RenderShadows, 
                    _data.CameraTag, _data.ObjectsLayer, targetInstance =>
                {
                    if (targetInstance != null)
                    {
                        _cameraService.SetupForTarget(targetInstance);
                        Texture2D icon = _cameraService.CaptureView();
                        if (icon != null)
                        {
                            _saverService.SaveIcon(icon, targetName);
                        }
                    }
                    completed = true;
                });
                
                while (!completed)
                    yield return null;
                
                yield return null;
            }
            
            GenerationStatus = "Сохранение...";
            AssetDatabase.Refresh();
            
            yield return null;
            
            EditorUtility.DisplayDialog("Готово", $"Создано {totalTargets} иконок", "OK");
            
            StopGeneration();
        }

        public void StopGeneration()
        {
            if (_generationCoroutine != null)
            {
                EditorCoroutine.Stop(_generationCoroutine);
                _generationCoroutine = null;
            }
            
            if (_previewCoroutine != null)
            {
                EditorCoroutine.Stop(_previewCoroutine);
                _previewCoroutine = null;
            }
            
            IsGenerating = false;
            GenerationProgress = 0f;
            GenerationStatus = "";
        }

        public void Dispose()
        {
            StopGeneration();
            ClearPreviews();
            _cameraService.Cleanup();
            _saverService.Dispose();
        }

        private void ClearPreviews()
        {
            if (CameraPreviews != null)
            {
                foreach (Texture2D preview in CameraPreviews)
                {
                    if (preview != null)
                        UnityEngine.Object.DestroyImmediate(preview);
                }
                CameraPreviews = null;
            }
        }

        private bool HasValidTargets => _data?.Targets?.Any(t => t != null) ?? false;
        
        private string CleanObjectName(string name)
        {
            return name.Replace("(Clone)", "").Replace("(clone)", "").Trim();
        }
        
        public void MarkPreviewDirty()
        {
            _previewDirty = true;
        }
    }

    public class EditorCoroutine
    {
        private readonly IEnumerator _routine;
        
        public static EditorCoroutine Start(IEnumerator routine)
        {
            EditorCoroutine coroutine = new(routine);
            coroutine.Start();
            return coroutine;
        }
        
        private EditorCoroutine(IEnumerator routine)
        {
            _routine = routine;
        }
        
        private void Start()
        {
            #pragma warning disable UDR0005 // Domain Reload Analyzer
            EditorApplication.update += Update;
            #pragma warning restore UDR0005 // Domain Reload Analyzer
        }
        
        private void Update()
        {
            if (!_routine.MoveNext())
            {
                Stop();
            }
        }
        
        public static void Stop(EditorCoroutine coroutine)
        {
            if (coroutine != null)
            {
                coroutine.Stop();
            }
        }
        
        private void Stop()
        {
            EditorApplication.update -= Update;
        }
    }
}