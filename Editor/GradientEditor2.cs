using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class GradientEditor2 : EditorWindow
    {
        private Gradient _gradient;
        private Vector2Int _textureSize;
        private bool _makeTextureVertical;
        private Action<Gradient, Texture2D> _onGradientChanged;
        private object _gradientEditor;


        public static void Open(Gradient gradient, Vector2Int textureSize, bool textureVertical, Action<Gradient, Texture2D> onGradientChanged)
        {
            var window = GetWindow<GradientEditor2>();
            window._gradient = gradient;
            window._textureSize = textureSize;
            window._makeTextureVertical = textureVertical;
            window._onGradientChanged = onGradientChanged;
            window.titleContent = new GUIContent("Gradient Editor");
            // show in center of screen
            float width = 500;
            float height = 300;
            window.position = new Rect(Screen.width / 2 - width / 2, Screen.height / 2 - height / 2, width, height);
            window.Show();
        }

        static MethodInfo s_gradientEditorGUIMethodInfo = null;
        public static MethodInfo GradientEditorGUI {
            get
            {
                if(s_gradientEditorGUIMethodInfo == null)
                {
                    Type gradient_editor_type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GradientEditor");
                    s_gradientEditorGUIMethodInfo = gradient_editor_type.GetMethod("OnGUI");
                }
                return s_gradientEditorGUIMethodInfo;
            }
        }

        public static object GetGradientEditor(Gradient gradient)
        {
            Type gradientEditorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GradientEditor");
            var gradientEditor = Activator.CreateInstance(gradientEditorType);
            var gradientEditorInit = gradientEditorType.GetMethod("Init");

#if UNITY_2020_1_OR_NEWER
            gradientEditorInit.Invoke(gradientEditor, new object[] { gradient, 0, true, ColorSpace.Linear });
#else
            gradientEditorInit.Invoke(gradientEditor, new object[] { gradient, 0, true });
#endif

            return gradientEditor;
        }

        private void OnGUI()
        {
            if(_gradientEditor == null)
            {
                _gradientEditor = GetGradientEditor(_gradient);
            }
            GradientEditorGUI.Invoke(_gradientEditor, new object[] { new Rect(20, 20, position.width - 40, position.height - 90) });
            Rect buttonRect = new Rect(40, position.height - 50, position.width - 80, 40);
            if (GUI.Button(buttonRect, "Apply"))
            {
                Apply();
            }
        }

        private void OnDestroy()
        {
            Apply();
        }

        void Apply()
        {
            Texture2D gradientTexture = Converter.GradientToTexture(_gradient, _textureSize.x, _textureSize.y, _makeTextureVertical);
            _onGradientChanged?.Invoke(_gradient, gradientTexture);
        }
    }
}