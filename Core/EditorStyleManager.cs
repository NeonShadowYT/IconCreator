using UnityEngine;
using UnityEditor;

namespace NeonImperium.IconsCreation
{
    public class EditorStyleManager
    {
        public GUIStyle HeaderStyle { get; private set; }
        public GUIStyle FoldoutStyle { get; private set; }
        public GUIStyle CenteredLabelStyle { get; private set; }
        public GUIStyle HelpBoxStyle { get; private set; }
        public GUIStyle MiniLabelStyle { get; private set; }
        public Texture2D SolidColorTexture { get; private set; }

        private bool _isInitialized = false;

        public void InitializeStyles()
        {
            if (_isInitialized) return;

            try
            {
                // Создаем стили с безопасными значениями по умолчанию
                HeaderStyle = CreateSafeStyle(() => new GUIStyle(EditorStyles.boldLabel));
                HeaderStyle.fontStyle = FontStyle.Bold;
                HeaderStyle.fontSize = 18;
                HeaderStyle.alignment = TextAnchor.MiddleCenter;
                HeaderStyle.margin = new RectOffset(0, 0, 10, 10);

                CenteredLabelStyle = CreateSafeStyle(() => new GUIStyle(EditorStyles.label));
                CenteredLabelStyle.alignment = TextAnchor.MiddleCenter;
                CenteredLabelStyle.fontSize = 12;
                CenteredLabelStyle.fontStyle = FontStyle.Italic;
                CenteredLabelStyle.wordWrap = true;

                FoldoutStyle = CreateSafeStyle(() => new GUIStyle(EditorStyles.foldout));
                FoldoutStyle.fontStyle = FontStyle.Bold;
                FoldoutStyle.fontSize = 13;

                HelpBoxStyle = CreateSafeStyle(() => new GUIStyle(EditorStyles.helpBox));
                HelpBoxStyle.fontSize = 11;
                HelpBoxStyle.wordWrap = true;
                HelpBoxStyle.padding = new RectOffset(10, 10, 8, 8);
                HelpBoxStyle.margin = new RectOffset(0, 0, 4, 4);
                HelpBoxStyle.richText = true;

                MiniLabelStyle = CreateSafeStyle(() => new GUIStyle(EditorStyles.label));
                MiniLabelStyle.fontSize = 11;
                MiniLabelStyle.wordWrap = true;
                MiniLabelStyle.richText = true;
                MiniLabelStyle.padding = new RectOffset(2, 2, 0, 0);

                // Безопасное создание текстуры
                CreateSafeTexture();

                _isInitialized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to initialize EditorStyleManager: {e.Message}");
                CreateFallbackStyles();
            }
        }

        private GUIStyle CreateSafeStyle(System.Func<GUIStyle> styleCreator)
        {
            try
            {
                return styleCreator();
            }
            catch (System.Exception)
            {
                return new GUIStyle();
            }
        }

        private void CreateSafeTexture()
        {
            try
            {
                SolidColorTexture = new Texture2D(1, 1);
                if (SolidColorTexture != null)
                {
                    SolidColorTexture.hideFlags = HideFlags.DontSave;
                    SolidColorTexture.SetPixel(0, 0, new Color(0.2f, 0.6f, 1f, 0.1f));
                    SolidColorTexture.Apply();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to create texture: {e.Message}");
                SolidColorTexture = null;
            }
        }

        private void CreateFallbackStyles()
        {
            HeaderStyle = new GUIStyle();
            HeaderStyle.fontStyle = FontStyle.Bold;
            HeaderStyle.fontSize = 18;
            HeaderStyle.alignment = TextAnchor.MiddleCenter;

            CenteredLabelStyle = new GUIStyle();
            CenteredLabelStyle.alignment = TextAnchor.MiddleCenter;
            CenteredLabelStyle.wordWrap = true;

            FoldoutStyle = new GUIStyle();
            FoldoutStyle.fontStyle = FontStyle.Bold;

            HelpBoxStyle = new GUIStyle();
            HelpBoxStyle.wordWrap = true;
            HelpBoxStyle.padding = new RectOffset(8, 8, 6, 6);

            MiniLabelStyle = new GUIStyle();
            MiniLabelStyle.wordWrap = true;
            MiniLabelStyle.fontSize = 11;
        }

        public void UpdateStyles(Color headerColor)
        {
            if (!_isInitialized)
            {
                InitializeStyles();
                return;
            }

            try
            {
                if (HeaderStyle != null)
                    HeaderStyle.normal.textColor = headerColor;

                if (FoldoutStyle != null)
                    FoldoutStyle.normal.textColor = Color.Lerp(headerColor, Color.white, 0.1f);

                if (SolidColorTexture != null)
                {
                    Color bgColor = new Color(headerColor.r, headerColor.g, headerColor.b, 0.1f);
                    SolidColorTexture.SetPixel(0, 0, bgColor);
                    SolidColorTexture.Apply();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to update styles: {e.Message}");
            }
        }

        public void Dispose()
        {
            if (SolidColorTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(SolidColorTexture);
                SolidColorTexture = null;
            }
            _isInitialized = false;
        }
    }
}