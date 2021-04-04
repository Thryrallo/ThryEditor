﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Pumkin.Benchmark;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class CRect
    {
        public Rect r;
        public CRect(Rect r)
        {
            this.r = r;
        }
    }

    public class InputEvent
    {
        public bool HadMouseDownRepaint;
        public bool HadMouseDown;
        public bool MouseClick;
        public bool MouseLeftClick;

        public bool is_alt_down;

        public bool is_drag_drop_event;
        public bool is_drop_event;

        public Vector2 mouse_position;

        public void Use()
        {
            HadMouseDownRepaint = false;
            HadMouseDown = false;
            MouseClick = false;
            MouseLeftClick = false;
        }
    }

    public abstract class ShaderPart
    {
        public int xOffset = 0;
        public GUIContent content;
        public MaterialProperty materialProperty;
        public System.Object property_data = null;
        public PropertyOptions options;
        public bool reference_properties_exist = false;
        public bool reference_property_exists = false;
        public bool is_hidden = false;
        public bool is_animated = false;
        public bool is_animatable = false;
        public bool is_renaming = false;
        public MaterialProperty kaj_isAnimatedProperty;

        public ShaderPart(MaterialProperty prop, int xOffset, string displayName, PropertyOptions options)
        {
            this.materialProperty = prop;
            this.xOffset = xOffset;
            this.options = options;
            this.content = new GUIContent(displayName, options.tooltip);
            this.reference_properties_exist = options.reference_properties != null && options.reference_properties.Length > 0;
            this.reference_property_exists = options.reference_property != null;

            if (prop == null)
                return;
            bool propHasDuplicate = ShaderEditor.active.GetMaterialProperty(prop.name + "_" + ShaderEditor.currentlyDrawing.animPropertySuffix) != null;
            if (propHasDuplicate)
            {
                this.kaj_isAnimatedProperty = null;
            }
            else
            {
                this.kaj_isAnimatedProperty = ShaderEditor.active.GetMaterialProperty(prop.name + "Animated");
                if (prop.name.Contains(ShaderEditor.currentlyDrawing.animPropertySuffix))
                {
                    string ogNameAnimated = prop.name.Substring(0, prop.name.Length - ShaderEditor.currentlyDrawing.animPropertySuffix.Length - 1) + "Animated";
                    MaterialProperty p = ShaderEditor.active.GetMaterialProperty(ogNameAnimated);
                    if (p != null) this.kaj_isAnimatedProperty = p;
                }
            }
            this.is_animatable = this.kaj_isAnimatedProperty != null;
            this.is_animated = is_animatable && kaj_isAnimatedProperty.floatValue > 0;
            this.is_renaming = is_animatable && kaj_isAnimatedProperty.floatValue == 2;
        }

        public abstract void DrawInternal(GUIContent content, CRect rect = null, bool useEditorIndent = false, bool isInHeader = false);
        public abstract void CopyFromMaterial(Material m);
        public abstract void CopyToMaterial(Material m);

        public void Draw(CRect rect = null, GUIContent content = null, bool useEditorIndent = false, bool isInHeader = false)
        {
            if (HeaderHider.IsHeaderHidden(this))
                return;
            bool addDisableGroup = options.condition_enable != null && DrawingData.is_enabled;
            if (addDisableGroup)
            {
                DrawingData.is_enabled = options.condition_enable.Test();
                EditorGUI.BeginDisabledGroup(!DrawingData.is_enabled);
            }

            if (options.condition_show.Test())
            {
                PerformDraw(content, rect, useEditorIndent, isInHeader);
            }
            if (addDisableGroup)
            {
                DrawingData.is_enabled = true;
                EditorGUI.EndDisabledGroup();
            }
        }

        public void HandleKajAnimatable()
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();
            if (ShaderEditor.currentlyDrawing.isLockedMaterial == false && Event.current.isMouse && Event.current.button == 1 && lastRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.control && Config.Get().renameAnimatedProps)
                {
                    if (!is_animated)
                    {
                        is_animated = true;
                    }

                    if (is_animated)
                    {
                        is_renaming = !is_renaming;
                    }
                }
                else
                {
                    is_animated = !is_animated;
                }

                kaj_isAnimatedProperty.floatValue = is_animated ? (is_renaming ? 2 : 1) : 0;
                GUIUtility.ExitGUI();
            }
            if (is_animated)
            {
                Rect r = new Rect(8, lastRect.y + 2, 16, 16);
                GUI.DrawTexture(r, is_renaming ? Styles.texture_animated_renamed : Styles.texture_animated, ScaleMode.StretchToFill, true);
            }
        }

        private void PerformDraw(GUIContent content, CRect rect, bool useEditorIndent, bool isInHeader = false)
        {
            if (content == null)
                content = this.content;
            EditorGUI.BeginChangeCheck();
            DrawInternal(content, rect, useEditorIndent, isInHeader);
            if (EditorGUI.EndChangeCheck())
            {
                if (options.on_value_actions != null)
                {
                    foreach (PropertyValueAction action in options.on_value_actions)
                    {
                        action.Execute(materialProperty);
                    }
                }
            }
            Helper.testAltClick(DrawingData.lastGuiObjectHeaderRect, this);
        }
    }

    public class ShaderGroup : ShaderPart
    {
        public List<ShaderPart> parts = new List<ShaderPart>();

        public ShaderGroup() : base(null, 0, "", new PropertyOptions())
        {

        }

        public ShaderGroup(PropertyOptions options) : base(null, 0, "", new PropertyOptions())
        {
            this.options = options;
        }

        public ShaderGroup(MaterialProperty prop, MaterialEditor materialEditor, string displayName, int xOffset, PropertyOptions options) : base(prop, xOffset, displayName, options)
        {

        }

        public void addPart(ShaderPart part)
        {
            parts.Add(part);
        }

        public override void CopyFromMaterial(Material m)
        {
            if (options.reference_property != null)
                ShaderEditor.currentlyDrawing.propertyDictionary[options.reference_property].CopyFromMaterial(m);
            foreach (ShaderPart p in parts)
                p.CopyFromMaterial(m);
        }

        public override void CopyToMaterial(Material m)
        {
            if (options.reference_property != null)
                ShaderEditor.currentlyDrawing.propertyDictionary[options.reference_property].CopyToMaterial(m);
            foreach (ShaderPart p in parts)
                p.CopyToMaterial(m);
        }

        public override void DrawInternal(GUIContent content, CRect rect = null, bool useEditorIndent = false, bool isInHeader = false)
        {
            foreach (ShaderPart part in parts)
            {
                part.Draw();
            }
        }
    }

    public class ShaderHeader : ShaderGroup
    {
        public ShaderEditorHeader guiElement;

        public ShaderHeader() : base()
        {

        }

        public ShaderHeader(MaterialProperty prop, MaterialEditor materialEditor, string displayName, int xOffset, PropertyOptions options) : base(prop, materialEditor, displayName, xOffset, options)
        {
            this.guiElement = new ShaderEditorHeader(prop);
        }

        public override void DrawInternal(GUIContent content, CRect rect = null, bool useEditorIndent = false, bool isInHeader = false)
        {
            ShaderEditor.currentlyDrawing.currentProperty = this;
            EditorGUI.BeginChangeCheck();
            guiElement.Foldout(xOffset, content, ShaderEditor.currentlyDrawing.gui);
            Rect headerRect = DrawingData.lastGuiObjectHeaderRect;
            if (guiElement.is_expanded)
            {
                EditorGUILayout.Space();
                foreach (ShaderPart part in parts)
                {
                    part.Draw();
                }
                EditorGUILayout.Space();
            }
            if (EditorGUI.EndChangeCheck())
                HandleLinkedMaterials();
            DrawingData.lastGuiObjectHeaderRect = headerRect;
        }

        private void HandleLinkedMaterials()
        {
            List<Material> linked_materials = MaterialLinker.GetLinked(materialProperty);
            if (linked_materials != null)
                foreach (Material m in linked_materials)
                    this.CopyToMaterial(m);
        }
    }

    public class ShaderProperty : ShaderPart
    {
        public bool drawDefault;

        public float setFloat;
        public bool updateFloat;

        public bool forceOneLine = false;

        private int property_index = 0;

        public string keyword;

        public ShaderProperty(MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(materialProperty, xOffset, displayName, options)
        {
            drawDefault = false;
            this.forceOneLine = forceOneLine;

            property_index = System.Array.IndexOf(ShaderEditor.currentlyDrawing.properties, materialProperty);
        }

        public override void CopyFromMaterial(Material m)
        {
            MaterialHelper.CopyPropertyValueFromMaterial(materialProperty, m);
            if (keyword != null) SetKeyword(ShaderEditor.currentlyDrawing.materials, m.GetFloat(materialProperty.name)==1);
            if (is_animatable)
                MaterialHelper.CopyPropertyValueFromMaterial(kaj_isAnimatedProperty, m);
            this.is_animated = is_animatable && kaj_isAnimatedProperty.floatValue > 0;
            this.is_renaming = is_animatable && kaj_isAnimatedProperty.floatValue == 2;
        }

        public override void CopyToMaterial(Material m)
        {
            MaterialHelper.CopyPropertyValueToMaterial(materialProperty, m);
            if (keyword != null) SetKeyword(m, materialProperty.floatValue == 1);
            if (is_animatable)
                MaterialHelper.CopyPropertyValueToMaterial(kaj_isAnimatedProperty, m);
        }

        private void SetKeyword(Material[] materials, bool enabled)
        {
            if (enabled) foreach (Material m in materials) m.EnableKeyword(keyword);
            else foreach (Material m in materials) m.DisableKeyword(keyword);
        }

        private void SetKeyword(Material m, bool enabled)
        {
            if (enabled) m.EnableKeyword(keyword);
            else m.DisableKeyword(keyword);
        }

        public override void DrawInternal(GUIContent content, CRect rect = null, bool useEditorIndent = false, bool isInHeader = false)
        {
            PreDraw();
            ShaderEditor.currentlyDrawing.currentProperty = this;
            this.materialProperty = ShaderEditor.currentlyDrawing.properties[property_index];
            if (ShaderEditor.currentlyDrawing.isLockedMaterial)
                EditorGUI.BeginDisabledGroup(!(is_animatable && (is_animated || is_renaming)));
            if (rect != null)
                DrawingData.lastGuiObjectHeaderRect = rect.r;
            else
                DrawingData.lastGuiObjectHeaderRect = new Rect(-1, -1, -1, -1);
            int oldIndentLevel = EditorGUI.indentLevel;
            if (!useEditorIndent)
                EditorGUI.indentLevel = xOffset + 1;

            if (drawDefault)
                DrawDefault();
            else
            {
                ShaderEditor.currentlyDrawing.gui.BeginAnimatedCheck(materialProperty);
                if (forceOneLine)
                    ShaderEditor.currentlyDrawing.editor.ShaderProperty(GUILayoutUtility.GetRect(content, Styles.vectorPropertyStyle), this.materialProperty, content);
                else if (rect != null)
                    ShaderEditor.currentlyDrawing.editor.ShaderProperty(rect.r, this.materialProperty, content);
                else
                    ShaderEditor.currentlyDrawing.editor.ShaderProperty(this.materialProperty, content);
                ShaderEditor.currentlyDrawing.gui.EndAnimatedCheck();
            }

            EditorGUI.indentLevel = oldIndentLevel;
            if (DrawingData.lastGuiObjectHeaderRect.x == -1) DrawingData.lastGuiObjectHeaderRect = GUILayoutUtility.GetLastRect();
            if (this is TextureProperty == false && is_animatable && isInHeader == false)
                HandleKajAnimatable();
            if (ShaderEditor.currentlyDrawing.isLockedMaterial)
                EditorGUI.EndDisabledGroup();
        }

        public virtual void PreDraw() { }

        public virtual void DrawDefault() { }
    }

    public class TextureProperty : ShaderProperty
    {
        public bool showFoldoutProperties = false;
        public bool hasFoldoutProperties = false;
        public bool hasScaleOffset = false;

        public TextureProperty(MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool hasScaleOffset, bool forceThryUI) : base(materialProperty, displayName, xOffset, options, false)
        {
            drawDefault = forceThryUI;
            this.hasScaleOffset = hasScaleOffset;
            this.hasFoldoutProperties = hasScaleOffset || reference_properties_exist;
        }

        public override void PreDraw()
        {
            DrawingData.currentTexProperty = this;
        }

        public override void DrawDefault()
        {
            Rect pos = GUILayoutUtility.GetRect(content, Styles.vectorPropertyStyle);
            GuiHelper.drawConfigTextureProperty(pos, materialProperty, content, ShaderEditor.currentlyDrawing.editor, hasFoldoutProperties);
            DrawingData.lastGuiObjectHeaderRect = pos;
        }

        public override void CopyFromMaterial(Material m)
        {
            MaterialHelper.CopyPropertyValueFromMaterial(materialProperty, m);
            CopyReferencePropertiesFromMaterial(m);
        }

        public override void CopyToMaterial(Material m)
        {
            MaterialHelper.CopyPropertyValueToMaterial(materialProperty, m);
            CopyReferencePropertiesToMaterial(m);
        }

        private void CopyReferencePropertiesToMaterial(Material target)
        {
            if (options.reference_properties != null)
                foreach (string r_property in options.reference_properties)
                {
                    ShaderProperty property = ShaderEditor.currentlyDrawing.propertyDictionary[r_property];
                    MaterialHelper.CopyPropertyValueToMaterial(property.materialProperty, target);
                }
        }

        private void CopyReferencePropertiesFromMaterial(Material source)
        {
            if (options.reference_properties != null)
                foreach (string r_property in options.reference_properties)
                {
                    ShaderProperty property = ShaderEditor.currentlyDrawing.propertyDictionary[r_property];
                    MaterialHelper.CopyPropertyValueFromMaterial(property.materialProperty, source);
                }
        }
    }

    public class ShaderHeaderProperty : ShaderPart
    {
        public ShaderHeaderProperty(MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(materialProperty, xOffset, displayName, options)
        {
        }

        public override void DrawInternal(GUIContent content, CRect rect = null, bool useEditorIndent = false, bool isInHeader = false)
        {
            if (rect == null)
            {
                if (options.texture != null && options.texture.name != null)
                {
                    //is texutre draw
                    content = new GUIContent(options.texture.GetTextureFromName(), content.tooltip);
                    int height = options.texture.height;
                    int width = (int)((float)options.texture.loaded_texture.width / options.texture.loaded_texture.height * height);
                    Rect control = EditorGUILayout.GetControlRect(false, height-18);
                    Rect r = new Rect((control.width-width)/2,control.y,width, height);
                    GUI.DrawTexture(r, options.texture.loaded_texture);
                }
            }
            else
            {
                //is text draw
                Rect headerrect = new Rect(0, rect.r.y, rect.r.width, 18);
                EditorGUI.LabelField(headerrect, "<size=16>" + this.content.text + "</size>", Styles.masterLabel);
                DrawingData.lastGuiObjectHeaderRect = headerrect;
            }
        }

        public override void CopyFromMaterial(Material m)
        {
            throw new System.NotImplementedException();
        }

        public override void CopyToMaterial(Material m)
        {
            throw new System.NotImplementedException();
        }
    }

    public class InstancingProperty : ShaderProperty
    {
        public InstancingProperty(MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(materialProperty, displayName, xOffset, options, forceOneLine)
        {
            drawDefault = true;
        }

        public override void DrawDefault()
        {
            ShaderEditor.currentlyDrawing.editor.EnableInstancingField();
        }
    }
    public class GIProperty : ShaderProperty
    {
        public GIProperty(MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(materialProperty, displayName, xOffset, options, forceOneLine)
        {
            drawDefault = true;
        }

        public override void DrawDefault()
        {
            ShaderEditor.currentlyDrawing.editor.LightmapEmissionFlagsProperty(xOffset, true);
        }
    }
    public class DSGIProperty : ShaderProperty
    {
        public DSGIProperty(MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(materialProperty, displayName, xOffset, options, forceOneLine)
        {
            drawDefault = true;
        }

        public override void DrawDefault()
        {
            ShaderEditor.currentlyDrawing.editor.DoubleSidedGIField();
        }
    }
    public class LocaleProperty : ShaderProperty
    {
        public LocaleProperty(MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(materialProperty, displayName, xOffset, options, forceOneLine)
        {
            drawDefault = true;
        }

        public override void DrawDefault()
        {
            GuiHelper.DrawLocaleSelection(this.content, ShaderEditor.currentlyDrawing.gui.locale.available_locales, ShaderEditor.currentlyDrawing.gui.locale.selected_locale_index);
        }
    }
}