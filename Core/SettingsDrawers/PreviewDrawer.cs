using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class PreviewDrawer
    {
        private const int PREVIEW_SIZE = 150;
        private const int MAX_PREVIEWS_PER_ROW = 3;

        public static void Draw(ref bool showPreview, ref Vector2 previewScrollPosition, 
            Texture2D[] cameraPreviews, IconsCreatorData data, bool hasValidTargets, 
            int targetCount, bool showHelpBoxes, System.Action updateIconCreator, 
            EditorStyleManager styleManager)
        {
            if (!hasValidTargets) return;

            EditorGUILayout.BeginVertical("box");
            showPreview = EditorGUILayout.Foldout(showPreview, $"👁️ Предпросмотр всех моделей ({targetCount})", 
                styleManager?.FoldoutStyle ?? EditorStyles.foldout);
            
            if (showPreview)
            {
                EditorGUI.indentLevel++;

                DrawPreviewContent(cameraPreviews, data, previewScrollPosition, showHelpBoxes, updateIconCreator, styleManager);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawPreviewContent(Texture2D[] previews, IconsCreatorData data, 
            Vector2 previewScrollPosition, bool showHelpBoxes, System.Action updateIconCreator, 
            EditorStyleManager styleManager)
        {
            if (previews != null && previews.Length > 0 && previews[0] != null)
            {
                DrawPreviewsGrid(previews, data, previewScrollPosition);
            }
            else
            {
                DrawNoPreviewMessage(updateIconCreator);
            }

            if (showHelpBoxes)
                DisplaySettingsDrawer.DrawHelpBox("💡 <b>Предпросмотр обновляется</b> автоматически при изменении настроек", styleManager);
        }

        private static void DrawPreviewsGrid(Texture2D[] previews, IconsCreatorData data, Vector2 previewScrollPosition)
        {
            int previewCount = previews.Length;
            int rows = Mathf.CeilToInt((float)previewCount / MAX_PREVIEWS_PER_ROW);
            float totalHeight = rows * PREVIEW_SIZE + (rows - 1) * 5f + 40f;

            previewScrollPosition = EditorGUILayout.BeginScrollView(previewScrollPosition, GUILayout.Height(Mathf.Min(totalHeight, 400f)));
            
            for (int i = 0; i < previewCount; i++)
            {
                if (previews[i] == null) continue;
                DrawPreviewItem(previews[i], data, i, previewCount);
            }
            
            EditorGUILayout.EndScrollView();
        }

        private static void DrawPreviewItem(Texture2D preview, IconsCreatorData data, int index, int previewCount)
        {
            int row = index / MAX_PREVIEWS_PER_ROW;
            int col = index % MAX_PREVIEWS_PER_ROW;

            if (col == 0) EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(PREVIEW_SIZE), GUILayout.Height(PREVIEW_SIZE + 25f));
            
            string label = data != null && index < data.Targets.Length 
                ? data.Targets[index].name 
                : $"Объект {index + 1}";
            
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(PREVIEW_SIZE));

            Rect previewRect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE);
            GUI.Box(new Rect(previewRect.x - 1, previewRect.y - 1, previewRect.width + 2, previewRect.height + 2), "");
            GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
            
            EditorGUILayout.EndVertical();

            if (col < MAX_PREVIEWS_PER_ROW - 1 && index < previewCount - 1)
            {
                GUILayout.Space(5f);
            }

            if (col == MAX_PREVIEWS_PER_ROW - 1 || index == previewCount - 1)
            {
                EditorGUILayout.EndHorizontal();
                if (index < previewCount - 1) GUILayout.Space(5f);
            }
        }

        private static void DrawNoPreviewMessage(System.Action updateIconCreator)
        {
            EditorGUILayout.HelpBox("Предпросмотр будет отображен после обновления настроек", MessageType.Info);
            
            if (GUILayout.Button("Обновить предпросмотр"))
            {
                updateIconCreator?.Invoke();
            }
        }
    }
}