using UnityEditor;
using UnityEngine;

namespace NeonImperium.IconsCreation.SettingsDrawers
{
    public static class PreviewDrawer
    {
        private const int PREVIEW_SIZE = 150;
        private const int SCROLL_HEIGHT = 400;
        private const float ITEM_SPACING = 5f;

        public static void Draw(ref Vector2 previewScrollPosition,
            Texture2D[] cameraPreviews, IconsCreatorData data, bool hasValidTargets, IconType iconType)
        {
            if (!hasValidTargets) return;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("👁️ Предпросмотр", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (cameraPreviews != null && cameraPreviews.Length > 0)
            {
                if (iconType == IconType.Mesh)
                    DrawPreviewsGrid(ref previewScrollPosition, cameraPreviews, data);
                else if (cameraPreviews[0] != null)
                    DrawSinglePreview(cameraPreviews[0]);
            }
            else
            {
                EditorGUILayout.HelpBox("Превью будет сгенерировано автоматически", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawPreviewsGrid(ref Vector2 scrollPos, Texture2D[] previews, IconsCreatorData data)
        {
            int count = previews.Length;
            if (count == 0) return;

            // Адаптация: считаем, сколько колонок влезает в текущую ширину окна
            float availableWidth = EditorGUIUtility.currentViewWidth - 60f; // отступы
            int columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (PREVIEW_SIZE + ITEM_SPACING)));
            int rows = Mathf.CeilToInt((float)count / columns);

            float contentHeight = rows * (PREVIEW_SIZE + 25f) + (rows - 1) * ITEM_SPACING;
            float scrollHeight = Mathf.Min(contentHeight, SCROLL_HEIGHT);

            // Если контент влезает — скролл не нужен, рисуем без скроллвью
            bool needScroll = contentHeight > SCROLL_HEIGHT;

            if (needScroll)
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollHeight));

            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int col = 0; col < columns; col++)
                {
                    if (index >= count) break;

                    if (previews[index] != null)
                        DrawPreviewItem(previews[index], data, index);
                    else
                        DrawEmptySlot();

                    index++;

                    if (col < columns - 1 && index < count)
                        GUILayout.Space(ITEM_SPACING);
                }
                EditorGUILayout.EndHorizontal();

                if (row < rows - 1)
                    GUILayout.Space(ITEM_SPACING);
            }

            if (needScroll)
                EditorGUILayout.EndScrollView();
        }

        private static void DrawPreviewItem(Texture2D preview, IconsCreatorData data, int index)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(PREVIEW_SIZE), GUILayout.Height(PREVIEW_SIZE + 25f));

            string label = data != null && data.MeshTargets != null && index < data.MeshTargets.Length
                && data.MeshTargets[index] != null
                ? data.MeshTargets[index].name
                : $"Объект {index + 1}";

            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(PREVIEW_SIZE));

            Rect previewRect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE);
            EditorGUI.DrawRect(previewRect, new Color(0f, 0f, 0f, 0.15f));
            GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);

            EditorGUILayout.EndVertical();
        }

        private static void DrawEmptySlot()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(PREVIEW_SIZE), GUILayout.Height(PREVIEW_SIZE + 25f));
            EditorGUILayout.LabelField("—", EditorStyles.miniLabel, GUILayout.Width(PREVIEW_SIZE));
            Rect r = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE);
            EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0.08f));
            EditorGUILayout.EndVertical();
        }

        private static void DrawSinglePreview(Texture2D preview)
        {
            float size = Mathf.Min(300f, EditorGUIUtility.currentViewWidth - 60f);
            EditorGUILayout.LabelField("Общий результат", EditorStyles.miniLabel);
            Rect previewRect = GUILayoutUtility.GetRect(size, size);
            EditorGUI.DrawRect(previewRect, new Color(0f, 0f, 0f, 0.15f));
            GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
        }
    }
}