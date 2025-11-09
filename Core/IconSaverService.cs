using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    public class IconSaverService
    {
        private string _directory;
        private TextureSettings _textureSettings;
        private readonly Dictionary<string, EditorApplication.CallbackFunction> _pendingCallbacks = new Dictionary<string, EditorApplication.CallbackFunction>();

        public void Initialize(string directory, TextureSettings textureSettings)
        {
            _directory = directory;
            _textureSettings = textureSettings;
        }

        public void SaveIcon(Texture2D texture, string name)
        {
            try
            {
                byte[] bytes = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);

                string fullPath = GetSavePath(name);
                EnsureDirectoryExists(fullPath);
                
                File.WriteAllBytes(fullPath, bytes);
                System.Threading.Thread.Sleep(100);
                
                AssetDatabase.Refresh();

                // Создаем уникальный ключ для этого вызова
                string callbackKey = fullPath;
                
                // Создаем callback function вместо System.Action
                EditorApplication.CallbackFunction callback = () => 
                {
                    // Удаляем callback из pending list сразу при вызове
                    _pendingCallbacks.Remove(callbackKey);
                    ConfigureImportedSprite(fullPath);
                };
                
                _pendingCallbacks[callbackKey] = callback;
                #pragma warning disable UDR0005
                EditorApplication.delayCall += callback;
                #pragma warning restore UDR0005
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save icon {name}: {e.Message}");
            }
        }

        public void Dispose()
        {
            // Отменяем все pending callbacks при уничтожении сервиса
            foreach (var callback in _pendingCallbacks.Values)
            {
                EditorApplication.delayCall -= callback;
            }
            _pendingCallbacks.Clear();
        }

        private string GetSavePath(string name)
        {
            string cleanName = CleanFileName(name);
            string directoryPath = Path.Combine(Application.dataPath, _directory.TrimStart('/'));
            return Path.Combine(directoryPath, cleanName + ".png");
        }

        private void EnsureDirectoryExists(string fullPath)
        {
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        private string CleanFileName(string name)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                name = name.Replace(invalidChar.ToString(), "");
            return name.Trim();
        }

        private void ConfigureImportedSprite(string fullPath)
        {
            try
            {
                string relativePath = "Assets" + fullPath.Substring(Application.dataPath.Length);
                
                if (!File.Exists(fullPath))
                {
                    Debug.LogError($"File not found: {fullPath}");
                    return;
                }

                AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
                
                TextureImporter importer = GetTextureImporter(relativePath);
                if (importer == null) return;

                ConfigureTextureImporter(importer);
                importer.SaveAndReimport();
                
                Debug.Log($"Icon created: {relativePath}");
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to configure sprite: {e.Message}");
            }
        }

        private TextureImporter GetTextureImporter(string relativePath)
        {
            TextureImporter importer = null;
            int attempts = 0;
            
            while (importer == null && attempts < 10)
            {
                importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                if (importer == null)
                {
                    attempts++;
                    System.Threading.Thread.Sleep(50);
                    AssetDatabase.Refresh();
                }
            }

            if (importer == null)
                Debug.LogError($"Failed to get TextureImporter for: {relativePath}");

            return importer;
        }

        private void ConfigureTextureImporter(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = _textureSettings.FilterMode;
            importer.anisoLevel = _textureSettings.AnisoLevel;
            importer.textureCompression = _textureSettings.Compression;
            importer.maxTextureSize = _textureSettings.Size;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spritePixelsPerUnit = 1;
            importer.SetTextureSettings(settings);
        }
    }
}