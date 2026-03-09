using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.Extensions
{
    public static class UnityObjectExtensions
    {
        public static List<GameObject> ExtractAllGameObjects(this List<UnityEngine.Object> objects)
        {
            List<GameObject> result = new List<GameObject>();
            
            if (objects == null || objects.Count == 0)
                return result;

            GameObject[] gameObjects = objects.OfType<GameObject>().ToArray();
            result.AddRange(gameObjects);

            UnityEngine.Object[] folders = objects.Except(gameObjects).Where(o => o.IsFolder()).ToArray();
            foreach (UnityEngine.Object folder in folders)
            {
                if (!folder) continue;
                
                string folderPath = AssetDatabase.GetAssetPath(folder);
                if (folderPath.Length <= 7) continue;
                
                folderPath = folderPath.Substring(7);
                string[] filesPaths = Directory.GetFiles(Application.dataPath + "/" + folderPath, "*", SearchOption.AllDirectories);
                
                foreach (string filePath in filesPaths)
                {
                    if (filePath.Length <= Application.dataPath.Length - 6) continue;
                    
                    string relativeFilePath = filePath.Remove(0, Application.dataPath.Length - 6);
                    GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(relativeFilePath);
                    if (gameObject)
                    {
                        result.Add(gameObject);
                    }
                }
            }

            for (int i = 0; i < result.Count; i++)
            {
                if (result[i] != null)
                {
                    result[i].name = CleanObjectName(result[i].name);
                }
            }

            return result;
        }

        private static bool IsFolder(this UnityEngine.Object obj)
        {
            if (obj == null) return false;
            return AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj));
        }

        public static bool IsFolderContainingGameObjects(this UnityEngine.Object obj)
        {
            if (!obj.IsFolder()) return false;
            
            string folderPath = AssetDatabase.GetAssetPath(obj);
            if (folderPath.Length <= 7) return false;
            
            folderPath = folderPath.Substring(7);
            string[] filesPaths = Directory.GetFiles(Application.dataPath + "/" + folderPath, "*", SearchOption.AllDirectories);
            
            foreach (string filePath in filesPaths)
            {
                if (filePath.Length <= Application.dataPath.Length - 6) continue;
                
                string relativeFilePath = filePath.Remove(0, Application.dataPath.Length - 6);
                GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(relativeFilePath);
                if (gameObject) return true;
            }

            return false;
        }
        
        public static string CleanObjectName(this string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            
            return name.Replace("(Clone)", "")
                      .Replace("(clone)", "")
                      .Replace("(Clone) ", "")
                      .Replace(" (Clone)", "")
                      .Trim();
        }
    }
}