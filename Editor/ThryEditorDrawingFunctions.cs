using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class DrawingData
    {
        public static ThryEditor.TextureProperty currentTexProperty;
        public static Rect lastGuiObjectRect;
        public static bool lastPropertyUsedCustomDrawer;
    }

    public class TextureDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            ThryEditorGuiHelper.drawConfigTextureProperty(position, prop, label, editor, true);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.lastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public class TextureNoSODrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            ThryEditorGuiHelper.drawConfigTextureProperty(position, prop, label, editor, false);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.lastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public class SmallTextureDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            ThryEditorGuiHelper.drawSmallTextureProperty(position, prop, label, editor, true);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.lastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public class SmallTextureNoSODrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            ThryEditorGuiHelper.drawSmallTextureProperty(position, prop, label, editor, false);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.lastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public class BigTextureDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            ThryEditorGuiHelper.drawBigTextureProperty(position, prop, label, editor, true);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.lastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public class BigTextureNoSODrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            ThryEditorGuiHelper.drawBigTextureProperty(position, prop, label, editor, false);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.lastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    class GradientObject : ScriptableObject
    {
        public Gradient gradient = new Gradient();
    }

    public class GradientDrawer : MaterialPropertyDrawer
    {
        private GradientObject gradientObj;
        private SerializedProperty colorGradient;
        private SerializedObject serializedGradient;

        private Texture2D texture;

        private bool saved;

        private EditorWindow gradientWindow;

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (gradientObj == null)
            {
                gradientObj = GradientObject.CreateInstance<GradientObject>();
                serializedGradient = new SerializedObject(gradientObj);
                colorGradient = serializedGradient.FindProperty("gradient");
                texture = new Texture2D(128, 16);
                GradientToTexture();
            }
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(colorGradient, label);
            string windowName = EditorWindow.focusedWindow.titleContent.text;
            bool isGradientEditor = windowName == "Gradient Editor";
            if (isGradientEditor)
            {
                gradientWindow = EditorWindow.focusedWindow;
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedGradient.ApplyModifiedProperties();
                GradientToTexture();
                prop.textureValue = texture;
                saved = false;
            }
            if (gradientWindow == null && !saved)
            {
                byte[] encoding = texture.EncodeToPNG();
                Debug.Log("Gradient saved.");
                string path = "Assets/Textures/Gradients/" + GradientToString() + ".png";
                Helper.writeBytesToFile(encoding, path);
                AssetDatabase.ImportAsset(path);
                prop.textureValue = (Texture)EditorGUIUtility.Load(path);
                saved = true;
            }
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.lastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }

        private string GradientToString()
        {
            string ret = "";
            foreach (GradientColorKey key in gradientObj.gradient.colorKeys)
                ret += key.color.ToString() + key.time.ToString();
            foreach (GradientAlphaKey key in gradientObj.gradient.alphaKeys)
                ret += key.alpha.ToString() + key.time.ToString();
            ret += gradientObj.gradient.mode.ToString();
            return ret;
        }

        private void GradientToTexture()
        {
            for (int x = 0; x < texture.width; x++)
            {
                Color col = gradientObj.gradient.Evaluate((float)x / texture.width);
                for (int y = 0; y < texture.height; y++) texture.SetPixel(x, y, col);
            }
            texture.Apply();
        }
    }

    public class MyToggleDrawer : MaterialPropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            // Setup
            bool value = (prop.floatValue != 0.0f);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;

            // Show the toggle control
            value = EditorGUI.Toggle(position, label, value);

            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                // Set the new value if it has changed
                prop.floatValue = value ? 1.0f : 0.0f;
            }
        }
    }

    //-------------------------------------------------------------

    public class ThryEditorGuiHelper
    {

        public static void drawConfigTextureProperty(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor, bool scaleOffset)
        {
            if (Config.Get().useBigTextures) drawBigTextureProperty(position, prop, label, editor, scaleOffset);
            else drawSmallTextureProperty(position, prop, label, editor, scaleOffset);
        }

        public static void drawSmallTextureProperty(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor, bool scaleOffset)
        {
            editor.TexturePropertyMiniThumbnail(position, prop, label.text, "Click here for scale / offset" + (label.tooltip != "" ? " | " : "") + label.tooltip);
            if (scaleOffset && DrawingData.currentTexProperty != null)
            {
                if (DrawingData.currentTexProperty.showScaleOffset) ThryEditor.currentlyDrawing.editor.TextureScaleOffsetProperty(prop);
                if (ThryEditor.isMouseClick && position.Contains(Event.current.mousePosition))
                    DrawingData.currentTexProperty.showScaleOffset = !DrawingData.currentTexProperty.showScaleOffset;
            }

            DrawingData.lastGuiObjectRect = position;
        }

        public static void drawBigTextureProperty(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor, bool scaleOffset)
        {
            GUILayoutUtility.GetRect(label, bigTextureStyle);
            editor.TextureProperty(position, prop, label.text, label.tooltip, scaleOffset);
            DrawingData.lastGuiObjectRect = position;
        }

        public static GUIStyle m_sectionStyle;
        public static GUIStyle bigTextureStyle;
        public static GUIStyle vectorPropertyStyle;

        //----------Idk what this does-------------
        public static void SetupStyle()
        {
            m_sectionStyle = new GUIStyle(EditorStyles.boldLabel);
            m_sectionStyle.alignment = TextAnchor.MiddleCenter;

            bigTextureStyle = new GUIStyle();
            bigTextureStyle.fixedHeight = 48;

            vectorPropertyStyle = new GUIStyle();
            vectorPropertyStyle.padding = new RectOffset(0, 0, 2, 2);
        }

        //draw the render queue selector
        public static int drawRenderQueueSelector(Shader defaultShader, int customQueueFieldInput)
        {
            EditorGUILayout.BeginHorizontal();
            if (customQueueFieldInput == -1) customQueueFieldInput = ThryEditor.currentlyDrawing.materials[0].renderQueue;
            int[] queueOptionsQueues = new int[] { defaultShader.renderQueue, 2000, 2450, 3000, customQueueFieldInput };
            string[] queueOptions = new string[] { "From Shader", "Geometry", "Alpha Test", "Transparency" };
            int queueSelection = 4;
            if (defaultShader.renderQueue == customQueueFieldInput) queueSelection = 0;
            else
            {
                string customOption = null;
                int q = customQueueFieldInput;
                if (q < 2000) customOption = queueOptions[1] + "-" + (2000 - q);
                else if (q < 2450) { if (q > 2000) customOption = queueOptions[1] + "+" + (q - 2000); else queueSelection = 1; }
                else if (q < 3000) { if (q > 2450) customOption = queueOptions[2] + "+" + (q - 2450); else queueSelection = 2; }
                else if (q < 5001) { if (q > 3000) customOption = queueOptions[3] + "+" + (q - 3000); else queueSelection = 3; }
                if (customOption != null) queueOptions = new string[] { "From Shader", "Geometry", "Alpha Test", "Transparency", customOption };
            }
            EditorGUILayout.LabelField("Render Queue", GUILayout.ExpandWidth(true));
            int newQueueSelection = EditorGUILayout.Popup(queueSelection, queueOptions, GUILayout.MaxWidth(100));
            int newQueue = queueOptionsQueues[newQueueSelection];
            if (queueSelection != newQueueSelection) customQueueFieldInput = newQueue;
            int newCustomQueueFieldInput = EditorGUILayout.IntField(customQueueFieldInput, GUILayout.MaxWidth(65));
            bool isInput = customQueueFieldInput != newCustomQueueFieldInput || queueSelection != newQueueSelection;
            customQueueFieldInput = newCustomQueueFieldInput;
            foreach (Material m in ThryEditor.currentlyDrawing.materials)
                if (customQueueFieldInput != m.renderQueue && isInput) m.renderQueue = customQueueFieldInput;
            if (customQueueFieldInput != ThryEditor.currentlyDrawing.materials[0].renderQueue && !isInput) customQueueFieldInput = ThryEditor.currentlyDrawing.materials[0].renderQueue;
            EditorGUILayout.EndHorizontal();
            return customQueueFieldInput;
        }

        //draw all collected footers
        public static void drawFooters(List<string> footers)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Space(2);
            foreach (string footNote in footers)
            {
                drawFooter(footNote);
                GUILayout.Space(2);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        //draw single footer
        private static void drawFooter(string data)
        {
            string[] splitNote = data.TrimEnd(')').Split("(".ToCharArray(), 2);
            string value = splitNote[1];
            string type = splitNote[0];
            if (type == "linkButton")
            {
                string[] values = value.Split(",".ToCharArray());
                drawLinkButton(70, 20, values[0], values[1]);
            }
        }

        //draw a button with a link
        private static void drawLinkButton(int Width, int Height, string title, string link)
        {
            if (GUILayout.Button(title, GUILayout.Width(Width), GUILayout.Height(Height)))
            {
                Application.OpenURL(link);
            }
        }

        public static void DrawHeader(ref bool enabled, ref bool options, GUIContent name)
        {
            var r = EditorGUILayout.BeginHorizontal("box");
            enabled = EditorGUILayout.Toggle(enabled, EditorStyles.radioButton, GUILayout.MaxWidth(15.0f));
            options = GUI.Toggle(r, options, GUIContent.none, new GUIStyle());
            EditorGUILayout.LabelField(name, m_sectionStyle);
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawMasterLabel(string shaderName)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            style.alignment = TextAnchor.MiddleCenter;

            EditorGUILayout.LabelField("<size=16>" + shaderName + "</size>", style, GUILayout.MinHeight(18));
        }
    }

    //-----------------------------------------------------------------

    public class ThryEditorHeader
    {
        private List<MaterialProperty> propertyes;
        private bool currentState;

        public ThryEditorHeader(MaterialEditor materialEditor, string propertyName)
        {
            this.propertyes = new List<MaterialProperty>();
            foreach (Material materialEditorTarget in materialEditor.targets)
            {
                Object[] asArray = new Object[] { materialEditorTarget };
                propertyes.Add(MaterialEditor.GetMaterialProperty(asArray, propertyName));
            }

            this.currentState = fetchState();
        }

        public bool fetchState()
        {
            foreach (MaterialProperty materialProperty in propertyes)
            {
                if (materialProperty.floatValue == 1)
                    return true;
            }



            return false;
        }

        public bool getState()
        {
            return this.currentState;
        }

        public void Toggle()
        {

            if (getState())
            {
                foreach (MaterialProperty materialProperty in propertyes)
                {
                    materialProperty.floatValue = 0;
                }
            }
            else
            {
                foreach (MaterialProperty materialProperty in propertyes)
                {
                    materialProperty.floatValue = 1;
                }
            }

            this.currentState = !this.currentState;
        }

        public void Foldout(int xOffset, string name, ThryEditor gui)
        {
            var style = new GUIStyle("ShurikenModuleTitle");
            style.font = new GUIStyle(EditorStyles.label).font;
            style.border = new RectOffset(15, 7, 4, 4);
            style.fixedHeight = 22;
            style.contentOffset = new Vector2(20f, -2f);
            style.margin.left = 30 * xOffset;

            var rect = GUILayoutUtility.GetRect(16f + 20f, 22f, style);
            GUI.Box(rect, name, style);

            var e = Event.current;

            var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, getState(), false);
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                this.Toggle();
                e.Use();
            }
        }
    }
}