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
        public Color HeaderColor { get; private set; }

        private bool _isInitialized = false;

        public void InitializeStyles()
        {
            if (_isInitialized) return;

            try
            {
                HeaderStyle = CreateSafeStyle(() => new GUIStyle(EditorStyles.boldLabel));
                HeaderStyle.fontStyle = FontStyle.Bold;
                HeaderStyle.fontSize = 18;
                HeaderStyle.alignment = TextAnchor.MiddleCenter;
                HeaderStyle.margin = new RectOffset(0, 0, 10, 10);
                HeaderStyle.richText = true;

                CenteredLabelStyle = CreateSafeStyle(() => new GUIStyle(EditorStyles.label));
                CenteredLabelStyle.alignment = TextAnchor.MiddleCenter;
                CenteredLabelStyle.fontSize = 12;
                CenteredLabelStyle.fontStyle = FontStyle.Italic;
                CenteredLabelStyle.wordWrap = true;
                CenteredLabelStyle.richText = true;

                FoldoutStyle = CreateSafeStyle(() => new GUIStyle(EditorStyles.foldout));
                FoldoutStyle.fontStyle = FontStyle.Bold;
                FoldoutStyle.fontSize = 13;
                FoldoutStyle.richText = true;

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
            catch
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
            catch
            {
                SolidColorTexture = null;
            }
        }

        private void CreateFallbackStyles()
        {
            HeaderStyle = new GUIStyle
            {
                fontStyle = FontStyle.Bold,
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            CenteredLabelStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                richText = true
            };

            FoldoutStyle = new GUIStyle
            {
                fontStyle = FontStyle.Bold,
                richText = true
            };

            HelpBoxStyle = new GUIStyle
            {
                wordWrap = true,
                padding = new RectOffset(8, 8, 6, 6),
                richText = true
            };

            MiniLabelStyle = new GUIStyle
            {
                wordWrap = true,
                fontSize = 11,
                richText = true
            };
        }

        public void UpdateStyles(Color headerColor)
        {
            if (!_isInitialized)
            {
                InitializeStyles();
                return;
            }

            HeaderColor = headerColor;

            try
            {
                if (SolidColorTexture != null)
                {
                    Color bgColor = new Color(headerColor.r, headerColor.g, headerColor.b, 0.1f);
                    SolidColorTexture.SetPixel(0, 0, bgColor);
                    SolidColorTexture.Apply();
                }
            }
            catch { }
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