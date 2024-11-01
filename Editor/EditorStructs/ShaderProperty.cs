using JetBrains.Annotations;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Thry.ThryEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.MaterialProperty;

namespace Thry
{
    public class ShaderProperty : ShaderPart
    {
        protected bool _doCustomDrawLogic = false;
        protected bool _doForceIntoOneLine = false;
        protected bool _doDrawTwoFields = false;

        //Done for e.g. Vectors cause they draw in 2 lines for some fucking reasons
        private bool _doCustomHeightOffset { set; get; } = false;
        private float _customHeightOffset { set; get; } = 0;

        public string Keyword { private set; get; }

        protected List<MaterialPropertyDrawer> _customDecorators = new List<MaterialPropertyDrawer>();
        protected Rect[] _customDecoratorRects;
        protected MaterialPropertyDrawer _drawer = null;

        bool _needsDrawerInitlization = true;

        public ShaderProperty(ShaderEditor shaderEditor, string propertyIdentifier, int xOffset, string displayName, string tooltip, int propertyIndex) : base(propertyIdentifier, xOffset, displayName, tooltip, shaderEditor)
        {
            this.ShaderPropertyIndex = propertyIndex;
        }

        public ShaderProperty(ShaderEditor shaderEditor, MaterialProperty materialProperty, string displayName, int xOffset, string optionsRaw, bool forceOneLine, int propertyIndex) : base(shaderEditor, materialProperty, xOffset, displayName, optionsRaw, propertyIndex)
        {
            this._doCustomDrawLogic = false;
            this._doForceIntoOneLine = forceOneLine;
        }

        protected override void InitOptions()
        {
            base.InitOptions();
            this._doDrawTwoFields = Options.reference_property != null;
        }

        public void SetKeyword(string keyword)
        {
            this.Keyword = keyword;
        }

        [PublicAPI]
        public float FloatValue
        {
            get
            {
                return MaterialProperty.floatValue;
            }
            set
            {
                MaterialProperty.SetNumber(value);
                if (Keyword != null) SetKeywordState(ShaderEditor.Active.Materials, MaterialProperty.GetNumber() == 1);
                ExecuteOnValueActions(ShaderEditor.Active.Materials);
                MaterialEditor.ApplyMaterialPropertyDrawers(ShaderEditor.Active.Materials);
            }
        }

        [PublicAPI]
        public Vector4 VectorValue
        {
            get
            {
                return MaterialProperty.vectorValue;
            }
            set
            {
                MaterialProperty.vectorValue = value;
                ExecuteOnValueActions(ShaderEditor.Active.Materials);
                MaterialEditor.ApplyMaterialPropertyDrawers(ShaderEditor.Active.Materials);
            }
        }

        [PublicAPI]
        public Color ColorValue
        {
            get
            {
                return MaterialProperty.colorValue;
            }
            set
            {
                MaterialProperty.colorValue = value;
                ExecuteOnValueActions(ShaderEditor.Active.Materials);
                MaterialEditor.ApplyMaterialPropertyDrawers(ShaderEditor.Active.Materials);
            }
        }

        [PublicAPI]
        public Texture TextureValue
        {
            get
            {
                return MaterialProperty.textureValue;
            }
            set
            {
                MaterialProperty.textureValue = value;
                MaterialEditor.ApplyMaterialPropertyDrawers(ShaderEditor.Active.Materials);
            }
        }

        public override void CopyFrom(Material src, bool applyDrawers = true, bool deepCopy = true, HashSet<PropType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            if(skipPropertyTypes?.Contains(MaterialProperty.type) == true) return;
            if(skipPropertyNames?.Contains(MaterialProperty.name) == true) return;

            MaterialHelper.CopyValue(src, MaterialProperty);
            CopyReferencePropertiesFrom(src, skipPropertyTypes, skipPropertyNames);

            if (Keyword != null) SetKeywordState(ActiveShaderEditor.Materials, src.GetNumber(MaterialProperty) == 1);
            if (IsAnimatable)
            {
                ShaderOptimizer.CopyAnimatedTag(src, MaterialProperty);
                UpdateIsAnimatedFromTag();
            }

            ExecuteOnValueActions(ShaderEditor.Active.Materials);

            if (applyDrawers) ActiveShaderEditor.ApplyDrawers();
        }

        public override void CopyFrom(ShaderPart srcPart, bool applyDrawers = true, bool deepCopy = true, HashSet<PropType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            if(skipPropertyTypes?.Contains(MaterialProperty.type) == true) return;
            if(skipPropertyNames?.Contains(MaterialProperty.name) == true) return;
            if (srcPart is ShaderProperty == false) return;
            ShaderProperty src = srcPart as ShaderProperty;

            MaterialHelper.CopyValue(src.MaterialProperty, MaterialProperty);
            CopyReferencePropertiesFrom(src, skipPropertyTypes, skipPropertyNames);

            if (Keyword != null) SetKeywordState(ActiveShaderEditor.Materials, (src.MaterialProperty.targets[0] as Material).GetNumber(MaterialProperty) == 1);
            if (IsAnimatable)
            {
                ShaderOptimizer.CopyAnimatedTag(src.MaterialProperty, MaterialProperty);
                UpdateIsAnimatedFromTag();
            }

            ExecuteOnValueActions(ShaderEditor.Active.Materials);

            if (applyDrawers) ActiveShaderEditor.ApplyDrawers();
        }

        public override void CopyTo(Material[] targets, bool applyDrawers = true, bool deepCopy = true, HashSet<PropType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            if(skipPropertyTypes?.Contains(MaterialProperty.type) == true) return;
            if(skipPropertyNames?.Contains(MaterialProperty.name) == true) return;

            MaterialHelper.CopyValue(MaterialProperty, targets);
            CopyReferencePropertiesTo(targets, skipPropertyTypes, skipPropertyNames);

            if (Keyword != null) SetKeywordState(targets, MaterialProperty.GetNumber() == 1);
            if (IsAnimatable)
            {
                ShaderOptimizer.CopyAnimatedTag(MaterialProperty, targets);
            }

            ExecuteOnValueActions(targets);

            if (applyDrawers) MaterialEditor.ApplyMaterialPropertyDrawers(targets);
        }

        public override void CopyTo(ShaderPart targetPart, bool applyDrawers = true, bool deepCopy = true, HashSet<PropType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            if(skipPropertyTypes?.Contains(MaterialProperty.type) == true) return;
            if(skipPropertyNames?.Contains(MaterialProperty.name) == true) return;
            if (targetPart is ShaderProperty == false) return;
            ShaderProperty target = targetPart as ShaderProperty;

            MaterialHelper.CopyValue(MaterialProperty, target.MaterialProperty);
            CopyReferencePropertiesTo(target, skipPropertyTypes, skipPropertyNames);

            if (Keyword != null) SetKeywordState(target.MaterialProperty.targets as Material[], MaterialProperty.GetNumber() == 1);
            if (IsAnimatable)
            {
                ShaderOptimizer.CopyAnimatedTag(MaterialProperty, target.MaterialProperty.targets as Material[]);
            }

            target.ExecuteOnValueActions(target.MaterialProperty.targets as Material[]);

            if (applyDrawers) MaterialEditor.ApplyMaterialPropertyDrawers(target.MaterialProperty.targets as Material[]);
        }

        private void SetKeywordState(Material[] materials, bool enabled)
        {
            if (enabled) foreach (Material m in materials) m.EnableKeyword(Keyword);
            else foreach (Material m in materials) m.DisableKeyword(Keyword);
        }

        private void SetKeywordState(Material m, bool enabled)
        {
            if (enabled) m.EnableKeyword(Keyword);
            else m.DisableKeyword(Keyword);
        }

        public void UpdateKeywordFromValue()
        {
            if (Keyword != null) SetKeywordState(ActiveShaderEditor.Materials, MaterialProperty.GetNumber() == 1);
        }

        private static ShaderProperty _activeProperty;
        public static void RegisterDrawer(MaterialPropertyDrawer drawer)
        {
            if(_activeProperty == null) return;
            _activeProperty._drawer = drawer;
        }
        public static void RegisterDecorator(MaterialPropertyDrawer drawer)
        {
            if(_activeProperty._customDecorators.Contains(drawer) == false)
            {
                _activeProperty._customDecorators.Add(drawer);
                _activeProperty._customDecoratorRects = new Rect[_activeProperty._customDecorators.Count];
            }
        }
        public static void DisallowAnimation()
        {
            _activeProperty.IsAnimatable = false;
        }

        void InitializeDrawers()
        {
            // Makes Drawers and Decorators Register themself
            _activeProperty = this;
            ShaderEditor.Active.Editor.GetPropertyHeight(MaterialProperty, MaterialProperty.displayName);

            if (MaterialProperty.type == MaterialProperty.PropType.Vector && _doForceIntoOneLine == false)
            {
                this._doCustomHeightOffset = _drawer == null;
                this._customHeightOffset = -EditorGUIUtility.singleLineHeight;
            }
            UpdateIsAnimatedFromTag();
        }

        private void UpdateIsAnimatedFromTag()
        {
            // Animatable Stuff
            bool propHasDuplicate = ShaderEditor.Active.GetMaterialProperty(MaterialProperty.name + "_" + ShaderEditor.Active.RenamedPropertySuffix) != null;
            string tag = null;
            //If prop is og, but is duplicated (locked) dont have it animateable
            if (propHasDuplicate)
            {
                this.IsAnimatable = false;
            }
            else
            {
                //if prop is a duplicated or renamed get og property to check for animted status
                if (MaterialProperty.name.Contains(ShaderEditor.Active.RenamedPropertySuffix))
                {
                    string ogName = MaterialProperty.name.Substring(0, MaterialProperty.name.Length - ShaderEditor.Active.RenamedPropertySuffix.Length - 1);
                    tag = ShaderOptimizer.GetAnimatedTag(MaterialProperty.targets[0] as Material, ogName);
                }
                else
                {
                    tag = ShaderOptimizer.GetAnimatedTag(MaterialProperty);
                }
            }

            this.IsAnimated = IsAnimatable && tag != "";
            this.IsRenaming = IsAnimatable && tag == "2";
        }

        protected override void DrawInternal(GUIContent content, Rect? rect = null, bool useEditorIndent = false, bool isInHeader = false)
        {
            ActiveShaderEditor.CurrentProperty = this;
            UpdatedMaterialPropertyReference();

            if (_needsDrawerInitlization)
            {
                InitializeDrawers();
                _needsDrawerInitlization = false;
            }

            PreDraw();
            if (ActiveShaderEditor.IsLockedMaterial)
                EditorGUI.BeginDisabledGroup(!(IsAnimatable && (IsAnimated || IsRenaming)) && !IsExemptFromLockedDisabling);

            int oldIndentLevel = EditorGUI.indentLevel;
            if (!useEditorIndent)
                EditorGUI.indentLevel = XOffset + 1;

            if (_customDecoratorRects != null && _doCustomDrawLogic)
            {
                for (int i = 0; i < _customDecoratorRects.Length; i++)
                {
                    _customDecoratorRects[i] = EditorGUILayout.GetControlRect(false, GUILayout.Height(_customDecorators[i].GetPropertyHeight(MaterialProperty, content.text, ActiveShaderEditor.Editor)));
                }
            }

            if (_doCustomDrawLogic)
            {
                DrawDefault();
            }
            else if (_doDrawTwoFields)
            {
                Rect r = GUILayoutUtility.GetRect(content, Styles.vectorPropertyStyle);
                float labelWidth = (r.width - EditorGUIUtility.labelWidth) / 2; ;
                r.width -= labelWidth;
                ActiveShaderEditor.Editor.ShaderProperty(r, this.MaterialProperty, content);

                r.x += r.width;
                r.width = labelWidth;
                float prevLabelW = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 0;
                ActiveShaderEditor.PropertyDictionary[Options.reference_property].Draw(r, new GUIContent());
                EditorGUIUtility.labelWidth = prevLabelW;
            }
            else if (_doForceIntoOneLine)
            {
                ActiveShaderEditor.Editor.ShaderProperty(GUILayoutUtility.GetRect(content, Styles.vectorPropertyStyle), this.MaterialProperty, content);
            }
            else if (_doCustomHeightOffset)
            {
                ActiveShaderEditor.Editor.ShaderProperty(
                    GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, ActiveShaderEditor.Editor.GetPropertyHeight(this.MaterialProperty, content.text) + _customHeightOffset)
                    , this.MaterialProperty, content);
            }
            else if (rect != null)
            {
                // Custom Drawing for Range, because it doesnt draw correctly if inside the big texture property
                if (_drawer == null && MaterialProperty.type == MaterialProperty.PropType.Range)
                {
                    MaterialProperty.floatValue = EditorGUI.Slider(rect.Value, content, MaterialProperty.floatValue, 0, MaterialProperty.rangeLimits.y);
                }
                else
                {
                    ActiveShaderEditor.Editor.ShaderProperty(rect.Value, this.MaterialProperty, content);
                }
            }
            else
            {
                ActiveShaderEditor.Editor.ShaderProperty(this.MaterialProperty, content);
            }

            if (_customDecorators != null && _doCustomDrawLogic)
            {
                for (int i = 0; i < _customDecorators.Count; i++)
                {
                    _customDecorators[i].OnGUI(_customDecoratorRects[i], MaterialProperty, content, ShaderEditor.Active.Editor);
                }
            }

            EditorGUI.indentLevel = oldIndentLevel;
            if (rect == null) DrawingData.LastGuiObjectRect = GUILayoutUtility.GetLastRect();
            else DrawingData.LastGuiObjectRect = rect.Value;
            if (ActiveShaderEditor.IsLockedMaterial)
                EditorGUI.EndDisabledGroup();
        }

        protected virtual void PreDraw() { }

        protected virtual void DrawDefault() { }

        public override void FindUnusedTextures(List<string> unusedList, bool isEnabled)
        {
            if (isEnabled && Options.condition_enable != null)
            {
                isEnabled &= Options.condition_enable.Test();
            }
            if (!isEnabled && MaterialProperty != null && MaterialProperty.type == MaterialProperty.PropType.Texture && MaterialProperty.textureValue != null)
            {
                unusedList.Add(MaterialProperty.name);
            }
        }
    }

}