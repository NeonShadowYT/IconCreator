using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation
{
    public class IconSaverService
    {
        private string _directory;
        private TextureSettings _textureSettings;
        private readonly List<string> _savedPaths = new List<string>();
        private bool _batchActive = false;

        private readonly Dictionary<string, TextureImporterSettings> _importerSettingsCache
            = new Dictionary<string, TextureImporterSettings>();

        public void Initialize(string directory, TextureSettings textureSettings)
        {
            _directory = ValidateDirectoryPath(directory);
            _textureSettings = textureSettings;
        }

        /// <summary>
        /// Начать батч. Оборачивает все сохранения в StartAssetEditing/StopAssetEditing.
        /// </summary>
        public void BeginBatch()
        {
            if (_batchActive) return;
            _batchActive = true;
            _savedPaths.Clear();
            AssetDatabase.StartAssetEditing();
        }

        /// <summary>
        /// Завершить батч: один Refresh, один проход импортеров.
        /// </summary>
        public void EndBatch()
        {
            if (!_batchActive) return;

            try
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                // Настраиваем импортеры пакетно, без повторных реимпортов
                ConfigureAllSavedSprites();
            }
            finally
            {
                _batchActive = false;
                _savedPaths.Clear();
            }
        }

        public void SaveIcon(Texture2D texture, string name)
        {
            if (texture == null) return;
            if (string.IsNullOrEmpty(_directory)) return;

            try
            {
                byte[] bytes = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);

                string fullPath = GetSavePath(name);
                EnsureDirectoryExists(fullPath);

                File.WriteAllBytes(fullPath, bytes);

                string relativePath = "Assets" + fullPath.Substring(Application.dataPath.Length)
                    .Replace('\\', '/');

                _savedPaths.Add(relativePath);

                // Если работаем вне батча — сохраняем старое поведение (одиночная иконка)
                if (!_batchActive)
                {
                    AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
                    ConfigureImportedSprite(relativePath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save icon {name}: {e.Message}");
            }
        }

        private void ConfigureAllSavedSprites()
        {
            foreach (string relativePath in _savedPaths)
            {
                try
                {
                    TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                    if (importer == null)
                    {
                        AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
                        importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                    }
                    if (importer == null) continue;

                    // Если настройки уже такие же, как были в прошлый раз — не дёргаем реимпорт
                    TextureImporterSettings current = new TextureImporterSettings();
                    importer.ReadTextureSettings(current);

                    if (_importerSettingsCache.TryGetValue(relativePath, out var cached)
                        && CachedSettingsEqual(cached, current, importer))
                    {
                        continue;
                    }

                    ConfigureTextureImporter(importer);
                    importer.SaveAndReimport();

                    _importerSettingsCache[relativePath] = current;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to configure sprite {relativePath}: {e.Message}");
                }
            }
        }

        private bool CachedSettingsEqual(TextureImporterSettings a, TextureImporterSettings b, TextureImporter importer)
        {
            return a.spritePixelsPerUnit == b.spritePixelsPerUnit
                && a.spriteMode == b.spriteMode
                && importer.textureType == TextureImporterType.Sprite
                && importer.mipmapEnabled == false
                && importer.filterMode == _textureSettings.FilterMode
                && importer.anisoLevel == _textureSettings.AnisoLevel
                    && importer.textureCompression == _textureSettings.Compression
                    && importer.maxTextureSize == _textureSettings.Size;
        }

        public void Dispose()
        {
            if (_batchActive)
            {
                try { AssetDatabase.StopAssetEditing(); } catch { }
                _batchActive = false;
            }
            _savedPaths.Clear();
            _importerSettingsCache.Clear();
        }

        private string ValidateDirectoryPath(string directory)
        {
            if (string.IsNullOrEmpty(directory)) return "Assets/Icons/";
            directory = directory.Replace('\\', '/');
            if (!directory.StartsWith("Assets/")) directory = "Assets/" + directory.TrimStart('/');
            if (!directory.EndsWith("/")) directory += "/";
            return directory;
        }

        private string GetSavePath(string name)
        {
            string cleanName = CleanFileName(name);
            string relativePath = _directory.TrimStart('/');
            if (relativePath.StartsWith("Assets/")) relativePath = relativePath.Substring(7);
            string directoryPath = Path.Combine(Application.dataPath, relativePath);
            return Path.Combine(directoryPath, cleanName + ".png");
        }

        private void EnsureDirectoryExists(string fullPath)
        {
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        private string CleanFileName(string name)
        {
            name = name.Replace("(Clone)", "")
                       .Replace("(clone)", "")
                       .Replace("(Clone) ", "")
                       .Replace(" (Clone)", "")
                       .Trim();
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                name = name.Replace(invalidChar.ToString(), "");
            return name;
        }

        private void ConfigureImportedSprite(string relativePath)
        {
            try
            {
                if (!File.Exists(Application.dataPath + relativePath.Substring(6)))
                {
                    Debug.LogError($"File not found: {relativePath}");
                    return;
                }

                TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                if (importer == null)
                {
                    AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
                    importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                }
                if (importer == null) return;

                ConfigureTextureImporter(importer);
                importer.SaveAndReimport();

                Texture2D importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
                if (importedTexture != null)
                    EditorGUIUtility.PingObject(importedTexture);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to configure sprite: {e.Message}");
            }
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