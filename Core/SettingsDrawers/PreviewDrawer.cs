using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class PreviewDrawer
    {
        private const int PREVIEW_SIZE = 150;
        private const int MAX_PREVIEWS_PER_ROW = 3;

        public static void Draw(Vector2 previewScrollPosition, 
            Texture2D[] cameraPreviews, IconsCreatorData data, bool hasValidTargets, 
            int targetCount, EditorStyleManager styleManager)
        {
            if (!hasValidTargets) return;

            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("👁️ Предпросмотр", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;

            if (cameraPreviews != null && cameraPreviews.Length > 0 && cameraPreviews[0] != null)
            {
                DrawPreviewsGrid(cameraPreviews, data, ref previewScrollPosition);
            }
            else
            {
                EditorGUILayout.HelpBox("Превью будет сгенерировано автоматически", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawPreviewsGrid(Texture2D[] previews, IconsCreatorData data, ref Vector2 previewScrollPosition)
        {
            int previewCount = previews.Length;
            int rows = Mathf.CeilToInt((float)previewCount / MAX_PREVIEWS_PER_ROW);
            float totalHeight = rows * PREVIEW_SIZE + (rows - 1) * 5f + 40f;

            previewScrollPosition = EditorGUILayout.BeginScrollView(previewScrollPosition, 
                GUILayout.Height(Mathf.Min(totalHeight, 400f)));
            
            int previewIndex = 0;
            
            for (int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal();
                
                for (int col = 0; col < MAX_PREVIEWS_PER_ROW; col++)
                {
                    if (previewIndex >= previewCount) break;
                    
                    if (previews[previewIndex] != null)
                    {
                        DrawPreviewItem(previews[previewIndex], data, previewIndex, previewCount);
                    }
                    
                    previewIndex++;
                    
                    if (col < MAX_PREVIEWS_PER_ROW - 1 && previewIndex < previewCount)
                    {
                        GUILayout.Space(5f);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                if (row < rows - 1)
                {
                    GUILayout.Space(5f);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        private static void DrawPreviewItem(Texture2D preview, IconsCreatorData data, int index, int previewCount)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(PREVIEW_SIZE), GUILayout.Height(PREVIEW_SIZE + 25f));
            
            string label = data != null && index < data.Targets.Length 
                ? data.Targets[index].name 
                : $"Объект {index + 1}";
            
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(PREVIEW_SIZE));

            Rect previewRect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE);
            GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
            
            EditorGUILayout.EndVertical();
        }
    }
}