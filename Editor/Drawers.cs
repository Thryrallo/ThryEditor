// Material/Shader Inspector for Unity 2017/2018
// Copyright (C) 2019 Thryrallo

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    #region Texture Drawers
    public class ThryTextureDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            GuiHelper.ConfigTextureProperty(position, prop, label, editor, ((TextureProperty)ShaderEditor.Active.CurrentProperty).hasScaleOffset);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public class SmallTextureDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            GuiHelper.SmallTextureProperty(position, prop, label, editor, ((TextureProperty)ShaderEditor.Active.CurrentProperty).hasScaleOffset);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public class BigTextureDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            GuiHelper.BigTextureProperty(position, prop, label, editor, ((TextureProperty)ShaderEditor.Active.CurrentProperty).hasScaleOffset);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public class StylizedBigTextureDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            GuiHelper.StylizedBigTextureProperty(position, prop, label, editor, ((TextureProperty)ShaderEditor.Active.CurrentProperty).hasScaleOffset);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }
    #endregion

    #region Special Texture Drawers
    public class CurveDrawer : MaterialPropertyDrawer
    {
        public AnimationCurve curve;
        public EditorWindow window;
        public Texture2D texture;
        public bool saved = true;
        public TextureData imageData;

        public CurveDrawer()
        {
            curve = new AnimationCurve();
        }

        private void Init()
        {
            if (imageData == null)
            {
                if (ShaderEditor.Active.CurrentProperty.options.texture == null)
                    imageData = new TextureData();
                else
                    imageData = ShaderEditor.Active.CurrentProperty.options.texture;
            }
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            Init();
            Rect border_position = new Rect(position.x + EditorGUIUtility.labelWidth - 15, position.y, position.width - EditorGUIUtility.labelWidth - position.x + 15, position.height);

            EditorGUI.BeginChangeCheck();
            curve = EditorGUI.CurveField(border_position, curve);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateCurveTexture(prop);
            }

            GuiHelper.SmallTextureProperty(position, prop, label, editor, DrawingData.CurrentTextureProperty.hasFoldoutProperties);

            CheckWindowForCurveEditor();

            if (window == null && !saved)
                Save(prop);
        }

        private void UpdateCurveTexture(MaterialProperty prop)
        {
            texture = Converter.CurveToTexture(curve, imageData);
            prop.textureValue = texture;
            saved = false;
        }

        private void CheckWindowForCurveEditor()
        {
            string windowName = "";
            if (EditorWindow.focusedWindow != null)
                windowName = EditorWindow.focusedWindow.titleContent.text;
            bool isCurveEditor = windowName == "Curve";
            if (isCurveEditor)
                window = EditorWindow.focusedWindow;
        }

        private void Save(MaterialProperty prop)
        {
            Debug.Log(prop.textureValue.ToString());
            Texture saved_texture = TextureHelper.SaveTextureAsPNG(texture, PATH.TEXTURES_DIR + "curves/" + curve.GetHashCode() + ".png", null);
            prop.textureValue = saved_texture;
            saved = true;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }
    public class ThryExternalTextureToolDrawer : MaterialPropertyDrawer
    {
        string _toolTypeName;
        string _toolHeader;

        Type t_ExternalToolType;
        MethodInfo _onGui;
        object _externalTool;
        MaterialProperty _prop;

        bool _isTypeLoaded;
        bool _doesExternalTypeExist;
        bool _isInit;
        bool _showTool;

        public ThryExternalTextureToolDrawer(string toolHeader, string toolTypeName)
        {
            this._toolTypeName = toolTypeName;
            this._toolHeader = toolHeader;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            LoadType();
            if (_doesExternalTypeExist)
            {
                _prop = prop;
                GuiHelper.SmallTextureProperty(position, prop, label, editor, DrawingData.CurrentTextureProperty.hasFoldoutProperties, ExternalGUI);
            }
            else
            {
                GuiHelper.SmallTextureProperty(position, prop, label, editor, DrawingData.CurrentTextureProperty.hasFoldoutProperties);
            }
        }

        void ExternalGUI()
        {
            if (GUI.Button(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect()), _toolHeader)) _showTool = !_showTool;
            if (_showTool)
            {
                Init();

                int indent = EditorGUI.indentLevel;
                GuiHelper.BeginCustomIndentLevel(0);
                GUILayout.BeginHorizontal();
                GUILayout.Space(indent * 15);
                GUILayout.BeginVertical();
                _onGui.Invoke(_externalTool, new object[0]);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GuiHelper.EndCustomIndentLevel();
            }
        }

        public void LoadType()
        {
            if (_isTypeLoaded) return;
            t_ExternalToolType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(_toolTypeName)).Where(t => t != null).FirstOrDefault();
            _doesExternalTypeExist = t_ExternalToolType != null;
            _isTypeLoaded = true;
        }

        public void Init()
        {
            if (_isInit) return;
            if (_isTypeLoaded && _doesExternalTypeExist)
            {
                _onGui = t_ExternalToolType.GetMethod("OnGUI", BindingFlags.NonPublic | BindingFlags.Instance);
                _externalTool = ScriptableObject.CreateInstance(t_ExternalToolType);
                EventInfo eventTextureGenerated = t_ExternalToolType.GetEvent("TextureGenerated");
                if (eventTextureGenerated != null)
                    eventTextureGenerated.AddEventHandler(_externalTool, new EventHandler(TextureGenerated));
            }
            _isInit = true;
        }

        void TextureGenerated(object sender, EventArgs args)
        {
            if (args != null && args.GetType().GetField("generated_texture") != null)
            {
                Texture2D generated = args.GetType().GetField("generated_texture").GetValue(args) as Texture2D;
                _prop.textureValue = generated;
            }
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public class RGBAAtlasDrawer : MaterialPropertyDrawer
    {
        (Texture2D, bool, float, TextureChannel) _input_r;
        (Texture2D, bool, float, TextureChannel) _input_g;
        (Texture2D, bool, float, TextureChannel) _input_b;
        (Texture2D, bool, float, TextureChannel) _input_a;

        bool _isInit;
        MaterialProperty _prop;

        string _label1;
        string _label2;
        string _label3;
        string _label4;

        bool _makeSRGB = true;

        public RGBAAtlasDrawer(string label1, string label2, string label3, string label4)
        {
            _label1 = label1;
            _label2 = label2;
            _label3 = label3;
            _label4 = label4;
        }

        public RGBAAtlasDrawer(string label1, string label2, string label3, string label4, float sRGB) : this(label1,label2,label3,label4)
        {
            _makeSRGB = sRGB == 1;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            _prop = prop;
            GuiHelper.SmallTextureProperty(position, prop, label, editor, true, TexturePackerGUI);
        }

        void TexturePackerGUI()
        {
            Init();
            _input_r = TexturePackerSlotGUI(_input_r, _label1);
            _input_g = TexturePackerSlotGUI(_input_g, _label2);
            _input_b = TexturePackerSlotGUI(_input_b, _label3);
            _input_a = TexturePackerSlotGUI(_input_a, _label4);
            if (GUI.Button(EditorGUI.IndentedRect(EditorGUILayout.GetControlRect()), "Pack"))
            {
                Save();
                Pack();
            }
        }

        (Texture2D, bool, float, TextureChannel) TexturePackerSlotGUI((Texture2D, bool, float, TextureChannel) input, string label)
        {
            Rect totalRect = EditorGUILayout.GetControlRect(false);
            totalRect = EditorGUI.IndentedRect(totalRect);
            Rect r = totalRect;

            int ind = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (input.Item1 == null)
            {
                r.width = 60;
                r.x = totalRect.x + totalRect.width - r.width;
                input.Item3 = EditorGUI.FloatField(r, input.Item3);

                r.width = 60;
                r.x -= r.width;
                EditorGUI.LabelField(r, "Fallback:");
            }
            else
            {
                r.width = 40;
                r.x = totalRect.x + totalRect.width - r.width;
                input.Item4 = (TextureChannel)EditorGUI.EnumPopup(r, input.Item4);

                r.width = 20;
                r.x -= r.width;
                input.Item2 = EditorGUI.Toggle(r, input.Item2);

                r.width = 60;
                r.x -= r.width;
                EditorGUI.LabelField(r, "Inverted:");
            }

            float texWidth = Math.Max(50, r.x - totalRect.x) - 5;
            r.x = totalRect.x;
            r.width = 30;
            input.Item1 = EditorGUI.ObjectField(r, input.Item1, typeof(Texture2D), false) as Texture2D;

            r.x += r.width + 5;
            r.width = texWidth - r.width - 5;
            EditorGUI.LabelField(r, label);


            EditorGUI.indentLevel = ind;

            return input;
        }

        void Init()
        {
            if (_isInit) return;
            _input_r = LoadForChannel(ShaderEditor.Active.Materials[0], _prop.name, "r");
            _input_g = LoadForChannel(ShaderEditor.Active.Materials[0], _prop.name, "g");
            _input_b = LoadForChannel(ShaderEditor.Active.Materials[0], _prop.name, "b");
            _input_a = LoadForChannel(ShaderEditor.Active.Materials[0], _prop.name, "a");
            _isInit = true;
        }

        void Save()
        {
            SaveForChannel(_input_r, _prop.name, "r");
            SaveForChannel(_input_g, _prop.name, "g");
            SaveForChannel(_input_b, _prop.name, "b");
            SaveForChannel(_input_a, _prop.name, "a");
        }

        void SaveForChannel((Texture2D, bool, float, TextureChannel) input, string id, string channel)
        {
            foreach (Material m in ShaderEditor.Active.Materials)
            {
                if (input.Item1 != null) m.SetOverrideTag(id + "_texPack_" + channel + "_guid", AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(input.Item1)));
                else m.SetOverrideTag(id + "_texPack_" + channel + "_guid", "");
                m.SetOverrideTag(id + "_texPack_" + channel + "_fallback", input.Item3.ToString());
                m.SetOverrideTag(id + "_texPack_" + channel + "_inverted", input.Item2.ToString());
                m.SetOverrideTag(id + "_texPack_" + channel + "_channel", ((int)input.Item4).ToString());
            }
        }

        (Texture2D, bool, float, TextureChannel) LoadForChannel(Material m, string id, string channel)
        {
            Texture2D tex = null;
            string guid = m.GetTag(id + "_texPack_" + channel + "_guid", false, "");
            float fallback = float.Parse(m.GetTag(id + "_texPack_" + channel + "_fallback", false, "1"));
            bool inverted = bool.Parse(m.GetTag(id + "_texPack_" + channel + "_inverted", false, "false"));
            TextureChannel inputChannel = (TextureChannel)int.Parse(m.GetTag(id + "_texPack_" + channel + "_channel", false, "0"));
            if (string.IsNullOrEmpty(guid) == false)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p != null)
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            }
            return (tex, inverted, fallback, inputChannel);
        }

        void Pack()
        {
            int width = 16;
            int height = 16;
            Texture2D[] textures = new Texture2D[] { _input_r.Item1, _input_g.Item1, _input_b.Item1, _input_a.Item1 };
            bool[] doFallback = textures.Select(t => t == null).ToArray();
            int[] inputChannels = new int[] { (int)_input_r.Item4, (int)_input_g.Item4, (int)_input_b.Item4, (int)_input_a.Item4 };
            //Find max size
            foreach (Texture2D t in textures)
            {
                if (t == null) continue;
                width = Mathf.Max(width, t.width);
                height = Mathf.Max(height, t.height);
            }
            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i] == null) continue;
                textures[i] = TextureHelper.Resize(TextureHelper.GetReadableTexture(textures[i]), width, height);
            }
            Texture2D atlas = new Texture2D(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color c = new Color();
                    c.r = doFallback[0] ? _input_r.Item3 : Mathf.Abs((_input_r.Item2 ? 1 : 0) - textures[0].GetPixel(x, y)[inputChannels[0]]);
                    c.g = doFallback[1] ? _input_g.Item3 : Mathf.Abs((_input_g.Item2 ? 1 : 0) - textures[1].GetPixel(x, y)[inputChannels[1]]);
                    c.b = doFallback[2] ? _input_b.Item3 : Mathf.Abs((_input_b.Item2 ? 1 : 0) - textures[2].GetPixel(x, y)[inputChannels[2]]);
                    c.a = doFallback[3] ? _input_a.Item3 : Mathf.Abs((_input_a.Item2 ? 1 : 0) - textures[3].GetPixel(x, y)[inputChannels[3]]);
                    atlas.SetPixel(x, y, c);
                }
            }
            atlas.Apply();
            string path = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(ShaderEditor.Active.Materials[0]));
            path = path + "/" + ShaderEditor.Active.Materials[0].name + _prop.name + ".png";
            _prop.textureValue = TextureHelper.SaveTextureAsPNG(atlas, path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.streamingMipmaps = true;
            importer.crunchedCompression = true;
            importer.sRGBTexture = _makeSRGB;
            importer.SaveAndReimport();
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }

        enum TextureChannel { R, G, B, A }
    }

    public class GradientDrawer : MaterialPropertyDrawer
    {
        GradientData data;
        bool is_init = false;

        Rect border_position;
        Rect gradient_position;

        private void Init(MaterialProperty prop)
        {
            data = new GradientData();
            data.PreviewTexture = prop.textureValue;
            is_init = true;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (!is_init)
                Init(prop);

            EditorGUI.BeginChangeCheck();
            if (EditorGUI.EndChangeCheck())
                Init(prop);

            UpdateRects(position);
            if (ShaderEditor.Input.Click && border_position.Contains(Event.current.mousePosition))
            {
                ShaderEditor.Input.Use();
                PropertyOptions options = ShaderEditor.Active.CurrentProperty.options;
                GradientEditor.Open(data, prop, options.texture, options.force_texture_options, !options.force_texture_options);
            }

            GuiHelper.SmallTextureProperty(position, prop, label, editor, DrawingData.CurrentTextureProperty.hasFoldoutProperties);

            GradientField();
        }

        private void UpdateRects(Rect position)
        {
            border_position = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth - position.x, position.height);
            gradient_position = new Rect(border_position.x + 1, border_position.y + 1, border_position.width - 2, border_position.height - 2);
        }

        private void GradientField()
        {
            DrawBackgroundTexture();
            if (data.PreviewTexture != null)
                DrawGradientTexture();
            else
                GUI.DrawTexture(border_position, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.grey, 1, 1);
        }

        private void DrawBackgroundTexture()
        {
            Texture2D backgroundTexture = TextureHelper.GetBackgroundTexture();
            Rect texCoordsRect = new Rect(0, 0, gradient_position.width / backgroundTexture.width, gradient_position.height / backgroundTexture.height);
            GUI.DrawTextureWithTexCoords(gradient_position, backgroundTexture, texCoordsRect, false);
        }

        private void DrawGradientTexture()
        {
            TextureWrapMode wrap_mode = data.PreviewTexture.wrapMode;
            data.PreviewTexture.wrapMode = TextureWrapMode.Clamp;
            bool vertical = data.PreviewTexture.height > data.PreviewTexture.width;
            Vector2 pivot = new Vector2();
            if (vertical)
            {
                pivot = new Vector2(gradient_position.x, gradient_position.y + gradient_position.height);
                GUIUtility.RotateAroundPivot(-90, pivot);
                gradient_position.y += gradient_position.height;
                float h = gradient_position.width;
                gradient_position.width = gradient_position.height;
                gradient_position.y += h;
                gradient_position.height = -h;
            }
            GUI.DrawTexture(gradient_position, data.PreviewTexture, ScaleMode.StretchToFill, true);
            if (vertical)
            {
                GUIUtility.RotateAroundPivot(90, pivot);
            }
            GUI.DrawTexture(border_position, data.PreviewTexture, ScaleMode.StretchToFill, false, 0, Color.grey, 1, 1);
            data.PreviewTexture.wrapMode = wrap_mode;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public class TextureArrayDrawer : MaterialPropertyDrawer
    {
        private string framesProperty;

        public TextureArrayDrawer(){}

        public TextureArrayDrawer(string framesProperty)
        {
            this.framesProperty = framesProperty;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            ShaderProperty shaderProperty = (ShaderProperty)ShaderEditor.Active.CurrentProperty;
            GuiHelper.ConfigTextureProperty(position, prop, label, editor, true, true);

            if ((ShaderEditor.Input.is_drag_drop_event) && position.Contains(ShaderEditor.Input.mouse_position))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (ShaderEditor.Input.is_drop_event)
                {
                    DragAndDrop.AcceptDrag();
                    HanldeDropEvent(prop, shaderProperty);
                }
            }
            if (ShaderEditor.Active.IsFirstCall)
                ShaderEditor.Active.TextureArrayProperties.Add(shaderProperty);
        }

        public void HanldeDropEvent(MaterialProperty prop, ShaderProperty shaderProperty)
        {
            string[] paths = DragAndDrop.paths;
            Texture2DArray tex;
            if (AssetDatabase.GetMainAssetTypeAtPath(paths[0]) != typeof(Texture2DArray))
                tex = Converter.PathsToTexture2DArray(paths);
            else
                tex = AssetDatabase.LoadAssetAtPath<Texture2DArray>(paths[0]);
            prop.textureValue = tex;
            UpdateFramesProperty(prop, shaderProperty, tex);
            EditorGUIUtility.ExitGUI();
        }

        private void UpdateFramesProperty(MaterialProperty prop, ShaderProperty shaderProperty, Texture2DArray tex)
        {
            if (framesProperty == null)
                framesProperty = shaderProperty.options.reference_property;

            if (framesProperty != null)
            {
                if (ShaderEditor.Active.PropertyDictionary.ContainsKey(framesProperty))
                    ShaderEditor.Active.PropertyDictionary[framesProperty].materialProperty.floatValue = tex.depth;
            }
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }
    #endregion

    #region Decorators
    public class ThryHeaderLabelDecorator : MaterialPropertyDrawer
    {
        readonly string text;
        readonly int size;
        GUIStyle style;

        public ThryHeaderLabelDecorator(string text) : this(text, EditorStyles.standardFont.fontSize)
        {
        }
        public ThryHeaderLabelDecorator(string text, float size)
        {
            this.text = text;
            this.size = (int)size;
            style = new GUIStyle(EditorStyles.boldLabel);
            style.fontSize = this.size;
        }


        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return size + 6;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            float offst = position.height;
            position = EditorGUI.IndentedRect(position);
            GUI.Label(position, text, style);
        }
    }

    public class ThryHeaderLabel2Drawer : MaterialPropertyDrawer
    {
        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return 16f;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            position = EditorGUI.IndentedRect(position);
            GUI.Label(position, label, EditorStyles.boldLabel);
        }
    }
    #endregion

    public class ThryToggleDrawer : MaterialPropertyDrawer
    {
        public string keyword;
        private bool isFirstGUICall = true;
        public bool left = false;
        private bool hasKeyword = false;

        public ThryToggleDrawer()
        {
        }

        //the reason for weird string thing here is that you cant have bools as params for drawers
        public ThryToggleDrawer(string keywordLeft)
        {
            if (keywordLeft == "true") left = true;
            else if (keywordLeft == "false") left = false;
            else keyword = keywordLeft;
            hasKeyword = keyword != null;
        }

        public ThryToggleDrawer(string keyword, string left)
        {
            this.keyword = keyword;
            this.left = left == "true";
            hasKeyword = keyword != null;
        }

        protected void SetKeyword(MaterialProperty prop, bool on)
        {
            SetKeywordInternal(prop, on, "_ON");
        }

        protected void CheckKeyword(MaterialProperty prop)
        {
            if (prop.hasMixedValue)
            {
                foreach (Material m in prop.targets)
                {
                    if (m.GetFloat(prop.name) == 1)
                        m.EnableKeyword(keyword);
                    else
                        m.DisableKeyword(keyword);
                }
            }
            else
            {
                foreach (Material m in prop.targets)
                {
                    if (prop.floatValue == 1)
                        m.EnableKeyword(keyword);
                    else
                        m.DisableKeyword(keyword);
                }
            }
        }

        static bool IsPropertyTypeSuitable(MaterialProperty prop)
        {
            return prop.type == MaterialProperty.PropType.Float || prop.type == MaterialProperty.PropType.Range;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            if (!IsPropertyTypeSuitable(prop))
            {
                return EditorGUIUtility.singleLineHeight * 2.5f;
            }
            if (hasKeyword) CheckKeyword(prop);
            return base.GetPropertyHeight(prop, label, editor);
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (!IsPropertyTypeSuitable(prop))
            {
                return;
            }
            if (isFirstGUICall && !ShaderEditor.Active.IsLockedMaterial)
            {
                if(hasKeyword) CheckKeyword(prop);
                isFirstGUICall = false;
            }
            //why is this not inFirstGUICall ? cause it seems drawers are kept between different openings of the shader editor, so this needs to be set again every time the shader editor is reopened for that material
            (ShaderEditor.Active.PropertyDictionary[prop.name] as ShaderProperty).keyword = keyword;

            EditorGUI.BeginChangeCheck();
            
            bool value = (Math.Abs(prop.floatValue) > 0.001f);
            EditorGUI.showMixedValue = prop.hasMixedValue;
            if(left) value = EditorGUI.ToggleLeft(position, label, value, Styles.style_toggle_left_richtext);
            else     value = EditorGUI.Toggle(position, label, value);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = value ? 1.0f : 0.0f;
                if(hasKeyword) SetKeyword(prop, value);
            }
        }

        public override void Apply(MaterialProperty prop)
        {
            base.Apply(prop);
            if (!IsPropertyTypeSuitable(prop))
                return;

            if (prop.hasMixedValue)
                return;

            if(hasKeyword) SetKeyword(prop, (Math.Abs(prop.floatValue) > 0.001f));
        }

        protected void SetKeywordInternal(MaterialProperty prop, bool on, string defaultKeywordSuffix)
        {
            // if no keyword is provided, use <uppercase property name> + defaultKeywordSuffix
            string kw = string.IsNullOrEmpty(keyword) ? prop.name.ToUpperInvariant() + defaultKeywordSuffix : keyword;
            // set or clear the keyword
            foreach (Material material in prop.targets)
            {
                if (on)
                    material.EnableKeyword(kw);
                else
                    material.DisableKeyword(kw);
            }
        }
    }

    //This class only exists for backward compatibility
    public class ThryToggleUIDrawer: ThryToggleDrawer
    {
        public ThryToggleUIDrawer()
        {
        }

        //the reason for weird string thing here is that you cant have bools as params for drawers
        public ThryToggleUIDrawer(string keywordLeft)
        {
            if (keywordLeft == "true") left = true;
            else if (keywordLeft == "false") left = false;
            else keyword = keywordLeft;
        }

        public ThryToggleUIDrawer(string keyword, string left)
        {
            this.keyword = keyword;
            this.left = left == "true";
        }
    }

    public class MultiSliderDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            GuiHelper.MinMaxSlider(position, label, prop);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public class Vector4TogglesDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            EditorGUI.LabelField(position, label);
            position.x += EditorGUIUtility.labelWidth;
            position.width = (position.width - EditorGUIUtility.labelWidth) / 4;
            bool b1 = GUI.Toggle(position, prop.vectorValue.x == 1, "");
            position.x += position.width;
            bool b2 = GUI.Toggle(position, prop.vectorValue.y == 1, "");
            position.x += position.width;
            bool b3 = GUI.Toggle(position, prop.vectorValue.z == 1, "");
            position.x += position.width;
            bool b4 = GUI.Toggle(position, prop.vectorValue.w == 1, "");
            if (EditorGUI.EndChangeCheck())
            {
                prop.vectorValue = new Vector4(b1 ? 1 : 0, b2 ? 1 : 0, b3 ? 1 : 0, b4 ? 1 : 0);
            }
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public class Vector3Drawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            Vector4 vec = EditorGUI.Vector3Field(position, label, prop.vectorValue);
            if (EditorGUI.EndChangeCheck())
            {
                prop.vectorValue = new Vector4(vec.x, vec.y, vec.z, prop.vectorValue.w);
            }
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public class Vector2Drawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            Vector4 vec = EditorGUI.Vector2Field(position, label, prop.vectorValue);
            if (EditorGUI.EndChangeCheck())
            {
                prop.vectorValue = new Vector4(vec.x, vec.y, prop.vectorValue.z, prop.vectorValue.w);
            }
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public class HelpboxDrawer : MaterialPropertyDrawer
    {
        readonly MessageType type;

        public HelpboxDrawer()
        {
            type = MessageType.Info;
        }

        public HelpboxDrawer(float f)
        {
            type = (MessageType)(int)f;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            EditorGUILayout.HelpBox(label.text, type);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return 0;
        }
    }

    public class sRGBWarningDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            GuiHelper.ConfigTextureProperty(position, prop, label, editor, ((TextureProperty)ShaderEditor.Active.CurrentProperty).hasScaleOffset);
            GuiHelper.sRGBWarning(prop);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

    public enum ColorMask
    {
        None,
        Alpha,
        Blue,
        BA,
        Green,
        GA,
        GB,
        GBA,
        Red,
        RA,
        RB,
        RBA,
        RG,
        RGA,
        RGB,
        RGBA
    }

    // DX11 only blend operations
    public enum BlendOp
    {
        Add,
        Subtract,
        ReverseSubtract,
        Min,
        Max,
        LogicalClear,
        LogicalSet,
        LogicalCopy,
        LogicalCopyInverted,
        LogicalNoop,
        LogicalInvert,
        LogicalAnd,
        LogicalNand,
        LogicalOr,
        LogicalNor,
        LogicalXor,
        LogicalEquivalence,
        LogicalAndReverse,
        LogicalAndInverted,
        LogicalOrReverse,
        LogicalOrInverted
    }

    //Original Code from https://github.com/DarthShader/Kaj-Unity-Shaders
    /**MIT License

    Copyright (c) 2020 DarthShader

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.**/
    public class ThryShaderOptimizerLockButtonDrawer : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty shaderOptimizer, string label, MaterialEditor materialEditor)
        {
            bool isLocked = (shaderOptimizer.targets[0] as Material).shader.name.StartsWith("Hidden/") && (shaderOptimizer.targets[0] as Material).GetTag("OriginalShader",false,"") != "";
            //this will make sure the button is unlocked if you manually swap to an unlocked shader
            //shaders that have the ability to be locked shouldnt really be hidden themself. at least it wouldnt make too much sense
            if (shaderOptimizer.hasMixedValue == false && shaderOptimizer.floatValue == 1 && isLocked == false)
            {
                shaderOptimizer.floatValue = 0;
            }else if(shaderOptimizer.hasMixedValue == false && shaderOptimizer.floatValue == 0 && isLocked)
            {
                shaderOptimizer.floatValue = 1;
            }

            // Theoretically this shouldn't ever happen since locked in materials have different shaders.
            // But in a case where the material property says its locked in but the material really isn't, this
            // will display and allow users to fix the property/lock in
            ShaderEditor.Active.IsLockedMaterial = shaderOptimizer.floatValue == 1;
            if (shaderOptimizer.hasMixedValue)
            {
                EditorGUI.BeginChangeCheck();
                GUILayout.Button(Locale.editor.Get("lockin_button_multi").ReplaceVariables(materialEditor.targets.Length));
                if (EditorGUI.EndChangeCheck())
                {
                    SaveChangeStack();
                    Material[] materials = new Material[shaderOptimizer.targets.Length];
                    for (int i = 0; i < materials.Length; i++) materials[i] = shaderOptimizer.targets[i] as Material;
                    ShaderOptimizer.SetLockedForAllMaterials(materials, shaderOptimizer.floatValue == 1 ? 0 : 1, true, false, false, shaderOptimizer);
                    RestoreChangeStack();
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                if (shaderOptimizer.floatValue == 0)
                {
                    if (materialEditor.targets.Length == 1)
                        GUILayout.Button(Locale.editor.Get("lockin_button_single"));
                    else GUILayout.Button(Locale.editor.Get("lockin_button_multi").ReplaceVariables(materialEditor.targets.Length));
                }
                else GUILayout.Button(Locale.editor.Get("unlock_button"));
                if (EditorGUI.EndChangeCheck())
                {
                    SaveChangeStack();
                    Material[] materials = new Material[shaderOptimizer.targets.Length];
                    for (int i = 0; i < materials.Length; i++) materials[i] = shaderOptimizer.targets[i] as Material;
                    ShaderOptimizer.SetLockedForAllMaterials(materials, shaderOptimizer.floatValue == 1 ? 0 : 1, true, false, false, shaderOptimizer);
                    RestoreChangeStack();
                }
            }
        }

        //This code purly exists cause Unity 2019 is a piece of shit that looses it's internal change stack on locking CAUSE FUCK IF I KNOW
        static System.Reflection.FieldInfo changeStack = typeof(EditorGUI).GetField("s_ChangedStack", BindingFlags.Static | BindingFlags.NonPublic);
        static int preLockStackSize = 0;
        private static void SaveChangeStack()
        {
            if (changeStack != null)
            {
                Stack<bool> stack = (Stack<bool>)changeStack.GetValue(null);
                if(stack != null)
                {
                    preLockStackSize = stack.Count();
                }
            }
        }

        private static void RestoreChangeStack()
        {
            if (changeStack != null)
            {
                Stack<bool> stack = (Stack<bool>)changeStack.GetValue(null);
                if (stack != null)
                {
                    int postLockStackSize = stack.Count();
                    //Restore change stack from before lock / unlocking
                    for(int i=postLockStackSize; i < preLockStackSize; i++)
                    {
                        EditorGUI.BeginChangeCheck();
                    }
                }
            }
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            ShaderEditor.Active.DoUseShaderOptimizer = true;
            return -2;
        }
    }

    // Enum with normal editor width, rather than MaterialEditor Default GUI widths
    // Would be nice if Decorators could access Drawers too so this wouldn't be necessary for something to trivial
    // Adapted from Unity interal MaterialEnumDrawer https://github.com/Unity-Technologies/UnityCsReference/
    public class ThryWideEnumDrawer : MaterialPropertyDrawer
    {
        private readonly GUIContent[] names;
        private readonly float[] values;

        // internal Unity AssemblyHelper can't be accessed
        private Type[] TypesFromAssembly(Assembly a)
        {
            if (a == null)
                return new Type[0];
            try
            {
                return a.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                return new Type[0];
            }
        }
        public ThryWideEnumDrawer(string enumName,int j)
        {
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(
                x => TypesFromAssembly(x)).ToArray();
            try
            {
                var enumType = types.FirstOrDefault(
                    x => x.IsEnum && (x.Name == enumName || x.FullName == enumName)
                );
                var enumNames = Enum.GetNames(enumType);
                names = new GUIContent[enumNames.Length];
                for (int i = 0; i < enumNames.Length; ++i)
                    names[i] = new GUIContent(enumNames[i]);

                var enumVals = Enum.GetValues(enumType);
                values = new float[enumVals.Length];
                for (int i = 0; i < enumVals.Length; ++i)
                    values[i] = (int)enumVals.GetValue(i);
            }
            catch (Exception)
            {
                Debug.LogWarningFormat("Failed to create  WideEnum, enum {0} not found", enumName);
                throw;
            }

        }

        public ThryWideEnumDrawer(string n1, float v1) : this(new[] { n1 }, new[] { v1 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2) : this(new[] { n1, n2 }, new[] { v1, v2 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3) : this(new[] { n1, n2, n3 }, new[] { v1, v2, v3 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4) : this(new[] { n1, n2, n3, n4 }, new[] { v1, v2, v3, v4 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5) : this(new[] { n1, n2, n3, n4, n5 }, new[] { v1, v2, v3, v4, v5 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6) : this(new[] { n1, n2, n3, n4, n5, n6 }, new[] { v1, v2, v3, v4, v5, v6 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7) : this(new[] { n1, n2, n3, n4, n5, n6, n7 }, new[] { v1, v2, v3, v4, v5, v6, v7 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7, string n8, float v8) : this(new[] { n1, n2, n3, n4, n5, n6, n7, n8}, new[] { v1, v2, v3, v4, v5, v6, v7, v8}) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7, string n8, float v8, string n9, float v9) : this(new[] { n1, n2, n3, n4, n5, n6, n7, n8, n9}, new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9}) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7, string n8, float v8, string n9, float v9, string n10, float v10) : this(new[] { n1, n2, n3, n4, n5, n6, n7, n8, n9, n10 }, new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9, v10 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7, string n8, float v8, string n9, float v9, string n10, float v10, string n11, float v11) : this(new[] { n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11 }, new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7, string n8, float v8, string n9, float v9, string n10, float v10, string n11, float v11, string n12, float v12) : this(new[] { n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12 }, new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7, string n8, float v8, string n9, float v9, string n10, float v10, string n11, float v11, string n12, float v12, string n13, float v13) : this(new[] { n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13 }, new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7, string n8, float v8, string n9, float v9, string n10, float v10, string n11, float v11, string n12, float v12, string n13, float v13, string n14, float v14) : this(new[] { n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13, n14 }, new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7, string n8, float v8, string n9, float v9, string n10, float v10, string n11, float v11, string n12, float v12, string n13, float v13, string n14, float v14, string n15, float v15) : this(new[] { n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13, n14, n15 }, new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7, string n8, float v8, string n9, float v9, string n10, float v10, string n11, float v11, string n12, float v12, string n13, float v13, string n14, float v14, string n15, float v15, string n16, float v16) : this(new[] { n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13, n14, n15, n16 }, new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7, string n8, float v8, string n9, float v9, string n10, float v10, string n11, float v11, string n12, float v12, string n13, float v13, string n14, float v14, string n15, float v15, string n16, float v16, string n17, float v17) : this(new[] { n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13, n14, n15, n16, n17 }, new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7, string n8, float v8, string n9, float v9, string n10, float v10, string n11, float v11, string n12, float v12, string n13, float v13, string n14, float v14, string n15, float v15, string n16, float v16, string n17, float v17, string n18, float v18) : this(new[] { n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13, n14, n15, n16, n17, n18 }, new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7, string n8, float v8, string n9, float v9, string n10, float v10, string n11, float v11, string n12, float v12, string n13, float v13, string n14, float v14, string n15, float v15, string n16, float v16, string n17, float v17, string n18, float v18, string n19, float v19) : this(new[] { n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13, n14, n15, n16, n17, n18, n19 }, new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19 }) { }
        public ThryWideEnumDrawer(string n1, float v1, string n2, float v2, string n3, float v3, string n4, float v4, string n5, float v5, string n6, float v6, string n7, float v7, string n8, float v8, string n9, float v9, string n10, float v10, string n11, float v11, string n12, float v12, string n13, float v13, string n14, float v14, string n15, float v15, string n16, float v16, string n17, float v17, string n18, float v18, string n19, float v19, string n20, float v20) : this(new[] { n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13, n14, n15, n16, n17, n18, n19, n20 }, new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19, v20 }) { }
        public ThryWideEnumDrawer(string[] enumNames, float[] vals)
        {
            names = new GUIContent[enumNames.Length];
            for (int i = 0; i < enumNames.Length; ++i)
                names[i] = new GUIContent(enumNames[i]);

            values = new float[vals.Length];
            for (int i = 0; i < vals.Length; ++i)
                values[i] = vals[i];
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            EditorGUI.showMixedValue = prop.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            var value = prop.floatValue;
            int selectedIndex = -1;
            for (int i = 0; i < values.Length; i++)
                if (values[i] == value)
                {
                    selectedIndex = i;
                    break;
                }

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0f;
            var selIndex = EditorGUI.Popup(position, label, selectedIndex, names);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.floatValue = values[selIndex];
            EditorGUIUtility.labelWidth = labelWidth;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            DrawingData.LastPropertyUsedCustomDrawer = true;
            return base.GetPropertyHeight(prop, label, editor);
        }
    }
}