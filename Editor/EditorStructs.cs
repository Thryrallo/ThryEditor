using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Thry.ThryEditor;
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
        int _button;
        bool _MouseClick;
        bool _MouseLeftClickIgnoreLocked;
        bool _MouseRightClickIgnoreLocked;
        bool _MouseLeftClick;
        bool _MouseRightClick;

        public bool is_alt_down;

        public bool is_drag_drop_event;
        public bool is_drop_event;

        public Vector2 mouse_position;
        public Vector2 screen_mouse_position;

        public void Update(bool isLockedMaterial)
        {
            Event e = Event.current;
            _button = e.button;
            _MouseClick = e.type == EventType.MouseDown && !isLockedMaterial;
            _MouseLeftClick = _MouseClick && _button == 0;
            _MouseRightClick = _MouseClick && _button == 1;
            _MouseLeftClickIgnoreLocked = e.type == EventType.MouseDown && _button == 0;
            _MouseRightClickIgnoreLocked = e.type == EventType.MouseDown && _button == 1;
            if (_MouseClick) HadMouseDown = _MouseClick;
            if (HadMouseDown && e.type == EventType.Repaint)
            {
                HadMouseDownRepaint = true;
                HadMouseDown = false;
            }
            is_alt_down = e.alt;
            mouse_position = e.mousePosition;
            screen_mouse_position = GUIUtility.GUIToScreenPoint(e.mousePosition);
            is_drop_event = e.type == EventType.DragPerform;
            is_drag_drop_event = is_drop_event || e.type == EventType.DragUpdated;
        }

        public void Use()
        {
            _MouseClick = false;
            _MouseLeftClick = false;
            _MouseRightClick = false;
            Event.current.Use();
        }

        public bool LeftClick_IgnoreUnityUses
        {
            get { return _MouseLeftClick;  }
        }

        public bool RightClick_IgnoreUnityUses
        {
            get { return _MouseRightClick; }
        }

        public bool LeftClick_IgnoreLocked
        {
            get { return _MouseLeftClickIgnoreLocked && Event.current.type != EventType.Used; }
        }

        public bool RightClick_IgnoreLocked
        {
            get { return _MouseRightClickIgnoreLocked && Event.current.type != EventType.Used; }
        }

        public bool LeftClick_IgnoreLockedAndUnityUses
        {
            get { return _MouseLeftClickIgnoreLocked; }
        }

        public bool RightClick_IgnoreLockedAndUnityUses
        {
            get { return _MouseRightClickIgnoreLocked; }
        }

        public bool Click
        {
            get { return _MouseClick && Event.current.type != EventType.Used; }
        }

        public bool RightClick
        {
            get { return _MouseRightClick && Event.current.type != EventType.Used; }
        }

        public bool LeftClick
        {
            get { return _MouseLeftClick && Event.current.type != EventType.Used; }
        }
    }

    public abstract class ShaderPart
    {
        public ShaderEditor shaderEditor;

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
        public bool is_preset = false;
        public bool exempt_from_locked_disabling = false;

        public BetterTooltips.Tooltip tooltip;

        public bool has_not_searchedFor = false; //used for property search

        public ShaderPart(ShaderEditor shaderEditor, MaterialProperty prop, int xOffset, string displayName, PropertyOptions options)
        {
            this.shaderEditor = shaderEditor;
            this.materialProperty = prop;
            this.xOffset = xOffset;
            this.options = options;
            this.content = new GUIContent(displayName);
            this.tooltip = new BetterTooltips.Tooltip(options.tooltip);
            this.reference_properties_exist = options.reference_properties != null && options.reference_properties.Length > 0;
            this.reference_property_exists = options.reference_property != null;
            this.is_preset = shaderEditor.IsPresetEditor && Presets.IsPreset(shaderEditor.Materials[0], prop);

            if (prop == null)
                return;
            bool propHasDuplicate = shaderEditor.GetMaterialProperty(prop.name + "_" + shaderEditor.AnimPropertySuffix) != null;
            string tag = null;
            //If prop is og, but is duplicated (locked) dont have it animateable
            if (propHasDuplicate)
            {
                this.is_animatable = false;
            }
            else
            {
                //if prop is a duplicated or renamed get og property to check for animted status
                if (prop.name.Contains(shaderEditor.AnimPropertySuffix))
                {
                    string ogName = prop.name.Substring(0, prop.name.Length - shaderEditor.AnimPropertySuffix.Length - 1);
                    tag = ShaderOptimizer.GetAnimatedTag(materialProperty.targets[0] as Material, ogName);
                }
                else
                {
                    tag = ShaderOptimizer.GetAnimatedTag(materialProperty);
                }
                this.is_animatable = true;
            }
            
            
            this.is_animated = is_animatable && tag != "";
            this.is_renaming = is_animatable && tag == "2";
        }

        public void SetReferenceProperty(string s)
        {
            options.reference_property = s;
            this.reference_property_exists = options.reference_property != null;
        }

        public void SetReferenceProperties(string[] properties)
        {
            options.reference_properties = properties;
            this.reference_properties_exist = options.reference_properties != null && options.reference_properties.Length > 0;
        }

        public void SetTooltip(string tooltip)
        {
            this.tooltip.SetText(tooltip);
        }

        public abstract void DrawInternal(GUIContent content, CRect rect = null, bool useEditorIndent = false, bool isInHeader = false);
        public abstract void CopyFromMaterial(Material m, bool isTopCall = false);
        public abstract void CopyToMaterial(Material m, bool isTopCall = false);

        public abstract void TransferFromMaterialAndGroup(Material m, ShaderPart g, bool isTopCall = false);

        bool hasAddedDisabledGroup = false;
        public void Draw(CRect rect = null, GUIContent content = null, bool useEditorIndent = false, bool isInHeader = false)
        {
            if (has_not_searchedFor)
                return;
            if (DrawingData.IsEnabled && options.condition_enable != null)
            {
                hasAddedDisabledGroup = options.condition_enable.Test();
                if(hasAddedDisabledGroup == false)
                {
                    DrawingData.IsEnabled = hasAddedDisabledGroup;
                    EditorGUI.BeginDisabledGroup(false);
                }
            }
            if (options.condition_show.Test())
            {
                PerformDraw(content, rect, useEditorIndent, isInHeader);
            }
            if (hasAddedDisabledGroup)
            {
                hasAddedDisabledGroup = false;
                DrawingData.IsEnabled = true;
                EditorGUI.EndDisabledGroup();
            }
        }

        public virtual void HandleRightClickToggles(bool isInHeader)
        {
            if (ShaderEditor.Input.RightClick_IgnoreUnityUses && DrawingData.TooltipCheckRect.Contains(Event.current.mousePosition))
            {
                //Preset toggle
                if (Event.current.shift)
                {
                    if (shaderEditor.IsPresetEditor && isInHeader == false)
                    {
                        is_preset = !is_preset;
                        Presets.SetProperty(shaderEditor.Materials[0], materialProperty, is_preset);
                        ShaderEditor.RepaintActive();
                        ShaderEditor.Input.Use();
                    }
                }
                //Animated toggle
                else
                {
                    if (is_animatable && isInHeader == false && this is ShaderHeader == false)
                    {
                        if (Event.current.control && Config.Singleton.renameAnimatedProps)
                        {
                            if (!is_animated)
                            {
                                is_animated = true;
                                is_renaming = true;
                            }
                            else if (!is_renaming)
                            {
                                is_renaming = true;
                            }
                            else
                            {
                                is_animated = false;
                                is_renaming = false;
                            }
                        }
                        else
                        {
                            is_animated = !is_animated;
                            is_renaming = false;
                        }
                        ShaderOptimizer.SetAnimatedTag(materialProperty, is_animated ? (is_renaming ? "2" : "1") : "");
                        ShaderEditor.RepaintActive();
                        ShaderEditor.Input.Use();
                    }
                }
            }
            
        }

        private void PerformDraw(GUIContent content, CRect rect, bool useEditorIndent, bool isInHeader = false)
        {
            if (content == null)
                content = this.content;
            if(options.on_value_actions != null)
                EditorGUI.BeginChangeCheck();
            DrawInternal(content, rect, useEditorIndent, isInHeader);

            if(this is TextureProperty == false) DrawingData.TooltipCheckRect = DrawingData.LastGuiObjectRect;
            DrawingData.TooltipCheckRect.width = EditorGUIUtility.labelWidth;

            HandleRightClickToggles(isInHeader);
            if (is_animated) DrawLockedAnimated();
            if (is_preset) DrawPresetProperty();

            tooltip.ConditionalDraw(DrawingData.TooltipCheckRect);

            if (options.on_value_actions != null)
            {
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (PropertyValueAction action in options.on_value_actions)
                    {
                        action.Execute(materialProperty);
                    }
                }
            }
            //Alt click testing
            if (options.altClick != null && ShaderEditor.Input.HadMouseDownRepaint && ShaderEditor.Input.is_alt_down && DrawingData.LastGuiObjectRect.Contains(ShaderEditor.Input.mouse_position))
            {
                options.altClick.Perform();
            }
        }

        private void DrawLockedAnimated()
        {
            Rect r = new Rect(14, DrawingData.TooltipCheckRect.y + 2, 16, 16);
            //GUI.DrawTexture(r, is_renaming ? Styles.texture_animated_renamed : Styles.texture_animated, ScaleMode.StretchToFill, true);
            if (is_renaming) GUI.Label(r, "RA", Styles.animatedIndicatorStyle);
            else GUI.Label(r, "A", Styles.animatedIndicatorStyle);
        }

        private void DrawPresetProperty()
        {
            Rect r = new Rect(2, DrawingData.TooltipCheckRect.y + 2, 8, 16);
            //GUI.DrawTexture(r, Styles.texture_preset, ScaleMode.StretchToFill, true);
            GUI.Label(r, "P", Styles.cyanStyle);
        }
    }

    public class ShaderGroup : ShaderPart
    {
        public List<ShaderPart> parts = new List<ShaderPart>();

        public ShaderGroup(ShaderEditor shaderEditor) : base(shaderEditor, null, 0, "", new PropertyOptions())
        {

        }

        public ShaderGroup(ShaderEditor shaderEditor, PropertyOptions options) : base(shaderEditor, null, 0, "", new PropertyOptions())
        {
            this.options = options;
        }

        public ShaderGroup(ShaderEditor shaderEditor, MaterialProperty prop, MaterialEditor materialEditor, string displayName, int xOffset, PropertyOptions options) : base(shaderEditor, prop, xOffset, displayName, options)
        {

        }

        public void addPart(ShaderPart part)
        {
            parts.Add(part);
        }

        public override void CopyFromMaterial(Material m, bool isTopCall = false)
        {
            if (options.reference_property != null)
                shaderEditor.PropertyDictionary[options.reference_property].CopyFromMaterial(m);
            foreach (ShaderPart p in parts)
                p.CopyFromMaterial(m);
            if (isTopCall) shaderEditor.ApplyDrawers();
        }

        public override void CopyToMaterial(Material m, bool isTopCall = false)
        {
            if (options.reference_property != null)
                shaderEditor.PropertyDictionary[options.reference_property].CopyToMaterial(m);
            foreach (ShaderPart p in parts)
                p.CopyToMaterial(m);
            if (isTopCall) MaterialEditor.ApplyMaterialPropertyDrawers(m);
        }

        public override void DrawInternal(GUIContent content, CRect rect = null, bool useEditorIndent = false, bool isInHeader = false)
        {
            foreach (ShaderPart part in parts)
            {
                part.Draw();
            }
        }

        public override void TransferFromMaterialAndGroup(Material m, ShaderPart p, bool isTopCall = false)
        {
            if (p is ShaderGroup == false) return;
            ShaderGroup group = p as ShaderGroup;
            if (options.reference_property != null && group.options.reference_property != null)
                shaderEditor.PropertyDictionary[options.reference_property].TransferFromMaterialAndGroup(m, group.shaderEditor.PropertyDictionary[group.options.reference_property]);
            for(int i=0;i<group.parts.Count && i < parts.Count; i++)
            {
                parts[i].TransferFromMaterialAndGroup(m, group.parts[i]);
            }
            if (isTopCall) shaderEditor.ApplyDrawers();
        }
    }

    public class ShaderHeader : ShaderGroup
    {
        private ThryHeaderDrawer headerDrawer;
        private bool isLegacy;

        public ShaderHeader(ShaderEditor shaderEditor) : base(shaderEditor)
        {
            this.headerDrawer = new ThryHeaderDrawer();
        }

        public ShaderHeader(ShaderEditor shaderEditor, MaterialProperty prop, MaterialEditor materialEditor, string displayName, int xOffset, PropertyOptions options) : base(shaderEditor, prop, materialEditor, displayName, xOffset, options)
        {
            if(DrawingData.LastPropertyDrawerType == DrawerType.Header)
            {
                //new header setup with drawer
                this.headerDrawer = DrawingData.LastPropertyDrawer as ThryHeaderDrawer;
            }
            else
            {
                //legacy setup with HideInInspector
                this.headerDrawer = new ThryHeaderDrawer();
                isLegacy = true;
            }
            this.headerDrawer.xOffset = xOffset;
        }

        public string GetEndProperty()
        {
            return headerDrawer.GetEndProperty();
        }

        public override void DrawInternal(GUIContent content, CRect rect = null, bool useEditorIndent = false, bool isInHeader = false)
        {
            shaderEditor.CurrentProperty = this;
            EditorGUI.BeginChangeCheck();
            Rect position = GUILayoutUtility.GetRect(content, Styles.dropDownHeader);
            if (isLegacy) headerDrawer.OnGUI(position, this.materialProperty, content, shaderEditor.Editor);
            else shaderEditor.Editor.ShaderProperty(position, this.materialProperty, content);
            Rect headerRect = DrawingData.LastGuiObjectHeaderRect;
            if (this.headerDrawer.is_expanded)
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
            DrawingData.LastGuiObjectHeaderRect = headerRect;
            DrawingData.LastGuiObjectRect = headerRect;
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
        public float setFloat;
        public bool updateFloat;

        public bool doCustomDrawLogic = false;
        public bool doForceIntoOneLine = false;
        public bool doDrawTwoFields = false;

        //Done for e.g. Vectors cause they draw in 2 lines for some fucking reasons
        public bool doCustomHeightOffset = false;
        public float customHeightOffset = 0;

        private int property_index = 0;

        public string keyword;

        public ShaderProperty(ShaderEditor shaderEditor, MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine, int property_index) : base(shaderEditor, materialProperty, xOffset, displayName, options)
        {
            this.doCustomDrawLogic = false;
            this.doForceIntoOneLine = forceOneLine;

            if (materialProperty.type == MaterialProperty.PropType.Vector && forceOneLine == false)
            {
                this.doCustomHeightOffset = !DrawingData.LastPropertyUsedCustomDrawer;
                this.customHeightOffset = -EditorGUIUtility.singleLineHeight;
            }

            this.doDrawTwoFields = options.reference_property != null;

            this.property_index = property_index;
        }

        public override void CopyFromMaterial(Material m, bool isTopCall = false)
        {
            MaterialHelper.CopyPropertyValueFromMaterial(materialProperty, m);
            if (keyword != null) SetKeyword(shaderEditor.Materials, m.GetFloat(materialProperty.name)==1);
            if (is_animatable)
            {
                ShaderOptimizer.CopyAnimatedTagFromMaterial(m, materialProperty);
            }
            this.is_animated = is_animatable && ShaderOptimizer.GetAnimatedTag(materialProperty) != "";
            this.is_renaming = is_animatable && ShaderOptimizer.GetAnimatedTag(materialProperty) == "2";

            if (isTopCall) shaderEditor.ApplyDrawers();
        }

        public override void CopyToMaterial(Material m, bool isTopCall = false)
        {
            MaterialHelper.CopyPropertyValueToMaterial(materialProperty, m);
            if (keyword != null) SetKeyword(m, materialProperty.floatValue == 1);
            if (is_animatable)
                ShaderOptimizer.CopyAnimatedTagToMaterials(new Material[] { m }, materialProperty);

            if (isTopCall) MaterialEditor.ApplyMaterialPropertyDrawers(m);
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
            shaderEditor.CurrentProperty = this;
            this.materialProperty = shaderEditor.Properties[property_index];
            if (shaderEditor.IsLockedMaterial)
                EditorGUI.BeginDisabledGroup(!(is_animatable && (is_animated || is_renaming)) && !exempt_from_locked_disabling);
            int oldIndentLevel = EditorGUI.indentLevel;
            if (!useEditorIndent)
                EditorGUI.indentLevel = xOffset + 1;

            if (doCustomDrawLogic)
            {
                DrawDefault();
            }
            else if (doDrawTwoFields)
            {
                Rect r = GUILayoutUtility.GetRect(content, Styles.vectorPropertyStyle);
                float labelWidth = (r.width - EditorGUIUtility.labelWidth) / 2; ;
                r.width -= labelWidth;
                shaderEditor.Editor.ShaderProperty(r, this.materialProperty, content);

                r.x += r.width;
                r.width = labelWidth;
                float prevLabelW = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 0;
                shaderEditor.PropertyDictionary[options.reference_property].Draw(new CRect(r), new GUIContent());
                EditorGUIUtility.labelWidth = prevLabelW;
            }
            else if (doForceIntoOneLine)
            {
                shaderEditor.Editor.ShaderProperty(GUILayoutUtility.GetRect(content, Styles.vectorPropertyStyle), this.materialProperty, content);
            }else if (doCustomHeightOffset)
            {
                shaderEditor.Editor.ShaderProperty(
                    GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, shaderEditor.Editor.GetPropertyHeight(this.materialProperty, content.text) + customHeightOffset)
                    , this.materialProperty, content);
            }
            else if (rect != null)
            {
                shaderEditor.Editor.ShaderProperty(rect.r, this.materialProperty, content);
            }
            else
            {
                shaderEditor.Editor.ShaderProperty(this.materialProperty, content);
            }

            EditorGUI.indentLevel = oldIndentLevel;
            if (rect == null) DrawingData.LastGuiObjectRect = GUILayoutUtility.GetLastRect();
            else DrawingData.LastGuiObjectRect = rect.r;
            if (shaderEditor.IsLockedMaterial)
                EditorGUI.EndDisabledGroup();
        }

        public virtual void PreDraw() { }

        public virtual void DrawDefault() { }

        public override void TransferFromMaterialAndGroup(Material m, ShaderPart p, bool isTopCall = false)
        {
            if (materialProperty.type != p.materialProperty.type) return;
            MaterialHelper.CopyMaterialValueFromProperty(materialProperty, p.materialProperty);
            if (keyword != null) SetKeyword(shaderEditor.Materials, m.GetFloat(p.materialProperty.name) == 1);
            if (is_animatable && p.is_animatable)
                ShaderOptimizer.CopyAnimatedTagFromProperty(p.materialProperty, materialProperty);
            this.is_animated = is_animatable && ShaderOptimizer.GetAnimatedTag(materialProperty) != "";
            this.is_renaming = is_animatable && ShaderOptimizer.GetAnimatedTag(materialProperty) == "2";

            if (isTopCall) shaderEditor.ApplyDrawers();
        }
    }

    public class TextureProperty : ShaderProperty
    {
        public bool showFoldoutProperties = false;
        public bool hasFoldoutProperties = false;
        public bool hasScaleOffset = false;

        public TextureProperty(ShaderEditor shaderEditor, MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool hasScaleOffset, bool forceThryUI, int property_index) : base(shaderEditor, materialProperty, displayName, xOffset, options, false, property_index)
        {
            doCustomDrawLogic = forceThryUI;
            this.hasScaleOffset = hasScaleOffset;
            this.hasFoldoutProperties = hasScaleOffset || reference_properties_exist;
        }

        public override void PreDraw()
        {
            DrawingData.CurrentTextureProperty = this;
        }

        public override void DrawDefault()
        {
            Rect pos = GUILayoutUtility.GetRect(content, Styles.vectorPropertyStyle);
            GuiHelper.ConfigTextureProperty(pos, materialProperty, content, shaderEditor.Editor, hasFoldoutProperties);
            DrawingData.LastGuiObjectRect = pos;
        }

        public override void CopyFromMaterial(Material m, bool isTopCall = false)
        {
            MaterialHelper.CopyPropertyValueFromMaterial(materialProperty, m);
            CopyReferencePropertiesFromMaterial(m);

            if (isTopCall) shaderEditor.ApplyDrawers();
        }

        public override void CopyToMaterial(Material m, bool isTopCall = false)
        {
            MaterialHelper.CopyPropertyValueToMaterial(materialProperty, m);
            CopyReferencePropertiesToMaterial(m);

            if (isTopCall) MaterialEditor.ApplyMaterialPropertyDrawers(m);
        }

        public override void TransferFromMaterialAndGroup(Material m, ShaderPart p, bool isTopCall = false)
        {
            if (materialProperty.type != p.materialProperty.type) return;
            MaterialHelper.CopyMaterialValueFromProperty(materialProperty, p.materialProperty);
            TransferReferencePropertiesToMaterial(m, p);
        }
        private void TransferReferencePropertiesToMaterial(Material target, ShaderPart p)
        {
            if (p.options.reference_properties == null || this.options.reference_properties == null) return;
            for (int i = 0; i < p.options.reference_properties.Length && i < options.reference_properties.Length; i++)
            {
                if (shaderEditor.PropertyDictionary.ContainsKey(this.options.reference_properties[i]) == false) continue;

                ShaderProperty targetP = shaderEditor.PropertyDictionary[this.options.reference_properties[i]];
                ShaderProperty sourceP = p.shaderEditor.PropertyDictionary[p.options.reference_properties[i]];
                MaterialHelper.CopyMaterialValueFromProperty(targetP.materialProperty, sourceP.materialProperty);
            }
        }

        private void CopyReferencePropertiesToMaterial(Material target)
        {
            if (options.reference_properties != null)
                foreach (string r_property in options.reference_properties)
                {
                    ShaderProperty property = shaderEditor.PropertyDictionary[r_property];
                    MaterialHelper.CopyPropertyValueToMaterial(property.materialProperty, target);
                }
        }

        private void CopyReferencePropertiesFromMaterial(Material source)
        {
            if (options.reference_properties != null)
                foreach (string r_property in options.reference_properties)
                {
                    ShaderProperty property = shaderEditor.PropertyDictionary[r_property];
                    MaterialHelper.CopyPropertyValueFromMaterial(property.materialProperty, source);
                }
        }
    }

    public class ShaderHeaderProperty : ShaderPart
    {
        public ShaderHeaderProperty(ShaderEditor shaderEditor, MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(shaderEditor, materialProperty, xOffset, displayName, options)
        {
        }

        public override void HandleRightClickToggles(bool isInHeader)
        {

        }

        public override void DrawInternal(GUIContent content, CRect rect = null, bool useEditorIndent = false, bool isInHeader = false)
        {
            if (rect == null)
            {
                if (options.texture != null && options.texture.name != null)
                {
                    //is texutre draw
                    content = new GUIContent(options.texture.loaded_texture, content.tooltip);
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
                DrawingData.LastGuiObjectRect = headerrect;
            }
        }

        public override void CopyFromMaterial(Material m, bool isTopCall = false)
        {
            throw new System.NotImplementedException();
        }

        public override void CopyToMaterial(Material m, bool isTopCall = false)
        {
            throw new System.NotImplementedException();
        }

        public override void TransferFromMaterialAndGroup(Material m, ShaderPart p, bool isTopCall = false)
        {
            throw new System.NotImplementedException();
        }
    }

    public class InstancingProperty : ShaderProperty
    {
        public InstancingProperty(ShaderEditor shaderEditor, MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(shaderEditor, materialProperty, displayName, xOffset, options, forceOneLine, 0)
        {
            doCustomDrawLogic = true;
        }

        public override void DrawDefault()
        {
            shaderEditor.Editor.EnableInstancingField();
        }
    }
    public class GIProperty : ShaderProperty
    {
        public GIProperty(ShaderEditor shaderEditor, MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(shaderEditor, materialProperty, displayName, xOffset, options, forceOneLine, 0)
        {
            doCustomDrawLogic = true;
        }

        public override void DrawDefault()
        {
            LightmapEmissionFlagsProperty(xOffset, false);
        }

        public static readonly GUIContent lightmapEmissiveLabel = EditorGUIUtility.TrTextContent("Global Illumination", "Controls if the emission is baked or realtime.\n\nBaked only has effect in scenes where baked global illumination is enabled.\n\nRealtime uses realtime global illumination if enabled in the scene. Otherwise the emission won't light up other objects.");
        public static GUIContent[] lightmapEmissiveStrings = { EditorGUIUtility.TrTextContent("Realtime"), EditorGUIUtility.TrTextContent("Baked"), EditorGUIUtility.TrTextContent("None") };
        public static int[] lightmapEmissiveValues = { (int)MaterialGlobalIlluminationFlags.RealtimeEmissive, (int)MaterialGlobalIlluminationFlags.BakedEmissive, (int)MaterialGlobalIlluminationFlags.None };

        public static void FixupEmissiveFlag(Material mat)
        {
            if (mat == null)
                throw new System.ArgumentNullException("mat");

            mat.globalIlluminationFlags = FixupEmissiveFlag(mat.GetColor("_EmissionColor"), mat.globalIlluminationFlags);
        }

        public static MaterialGlobalIlluminationFlags FixupEmissiveFlag(Color col, MaterialGlobalIlluminationFlags flags)
        {
            if ((flags & MaterialGlobalIlluminationFlags.BakedEmissive) != 0 && col.maxColorComponent == 0.0f) // flag black baked
                flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            else if (flags != MaterialGlobalIlluminationFlags.EmissiveIsBlack) // clear baked flag on everything else, unless it's explicity disabled
                flags &= MaterialGlobalIlluminationFlags.AnyEmissive;
            return flags;
        }

        public void LightmapEmissionFlagsProperty(int indent, bool enabled)
        {
            LightmapEmissionFlagsProperty(indent, enabled, false);
        }

        public void LightmapEmissionFlagsProperty(int indent, bool enabled, bool ignoreEmissionColor)
        {
            // Calculate isMixed
            MaterialGlobalIlluminationFlags any_em = MaterialGlobalIlluminationFlags.AnyEmissive;
            MaterialGlobalIlluminationFlags giFlags = shaderEditor.Materials[0].globalIlluminationFlags & any_em;
            bool isMixed = false;
            for (int i = 1; i < shaderEditor.Materials.Length; i++)
            {
                if((shaderEditor.Materials[i].globalIlluminationFlags & any_em) != giFlags)
                {
                    isMixed = true;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();

            // Show popup
            EditorGUI.showMixedValue = isMixed;
            giFlags = (MaterialGlobalIlluminationFlags)EditorGUILayout.IntPopup(lightmapEmissiveLabel, (int)giFlags, lightmapEmissiveStrings, lightmapEmissiveValues);
            EditorGUI.showMixedValue = false;

            // Apply flags. But only the part that this tool modifies (RealtimeEmissive, BakedEmissive, None)
            bool applyFlags = EditorGUI.EndChangeCheck();
            foreach (Material mat in shaderEditor.Materials)
            {
                mat.globalIlluminationFlags = applyFlags ? giFlags : mat.globalIlluminationFlags;
                if (!ignoreEmissionColor)
                {
                    FixupEmissiveFlag(mat);
                }
            }
        }
    }
    public class DSGIProperty : ShaderProperty
    {
        public DSGIProperty(ShaderEditor shaderEditor, MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(shaderEditor, materialProperty, displayName, xOffset, options, forceOneLine, 0)
        {
            doCustomDrawLogic = true;
        }

        public override void DrawDefault()
        {
            shaderEditor.Editor.DoubleSidedGIField();
        }
    }
    public class LocaleProperty : ShaderProperty
    {
        public LocaleProperty(ShaderEditor shaderEditor, MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(shaderEditor, materialProperty, displayName, xOffset, options, forceOneLine, 0)
        {
            doCustomDrawLogic = true;
        }

        public override void DrawDefault()
        {
            GuiHelper.DrawLocaleSelection(this.content, shaderEditor.Locale.available_locales, shaderEditor.Locale.selected_locale_index);
        }
    }
}