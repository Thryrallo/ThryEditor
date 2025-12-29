using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor
{
    public class RenderQueueProperty : ShaderProperty
    {
        public RenderQueueProperty(ShaderEditor shaderEditor) : base(shaderEditor, "RenderQueue", 0, "", "Change the Queue at which the material is rendered.", -1)
        {
            _doCustomDrawLogic = true;
            IsAnimatable = false;
            CustomStringTagID = "RenderQueue";
        }

        static readonly string[] s_renderQueueNames = { "From Shader", "Geometry", "AlphaTest", "Transparent" };
        static readonly int[] s_renderQueueValues = { -1, 2000, 2450, 3000 };

        protected override void DrawDefault()
        {
            Rect r = RectifiedLayout.GetPaddedRect(18);
            int queue = MyShaderUI.Materials[0].renderQueue;
            
            using (new GUILib.IndentOverrideScope(0))
            {
                // Split rect: label on left, dropdown in middle, int field on right
                float labelWidth = EditorGUIUtility.labelWidth;
                float fieldWidth = 75;
                float dropdownWidth = r.width - labelWidth - fieldWidth - 4;
                
                Rect labelRect = new Rect(r.x, r.y, labelWidth, r.height);
                Rect dropdownRect = new Rect(r.x + labelWidth + 2, r.y, dropdownWidth - 2, r.height);
                Rect intRect = new Rect(dropdownRect.xMax + 4, r.y, fieldWidth, r.height);
                
                EditorGUI.LabelField(labelRect, "Render Queue");
                
                // Dropdown for preset queues
                int selectedIndex = System.Array.FindIndex(s_renderQueueValues, v => v == queue);
                if (selectedIndex < 0) selectedIndex = 0; // Custom value, show "From Shader"
                
                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUI.Popup(dropdownRect, selectedIndex, s_renderQueueNames);
                if (EditorGUI.EndChangeCheck() && newIndex != selectedIndex)
                {
                    queue = s_renderQueueValues[newIndex];
                    foreach (Material m in MyShaderUI.Materials)
                        m.renderQueue = queue;
                }
                
                // Int field for exact value
                EditorGUI.BeginChangeCheck();
                queue = EditorGUI.IntField(intRect, queue);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (Material m in MyShaderUI.Materials)
                        m.renderQueue = queue;
                }
            }
        }

        public override object FetchPropertyValue()
        {
            return MyShaderUI.Materials[0].renderQueue;
        }

        public override object PropertyDefaultValue => ShaderEditor.Active.Shader.renderQueue;
        public override bool IsPropertyValueDefault => MyShaderUI.Materials.All(m => m.renderQueue == ShaderEditor.Active.Shader.renderQueue);

        public override void CopyFrom(Material sourceM, bool applyDrawers = true, bool deepCopy = true, bool copyReferenceProperties = true, HashSet<MaterialProperty.PropType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            foreach (Material m in MyShaderUI.Materials) m.renderQueue = sourceM.renderQueue;
        }
        public override void CopyTo(Material[] targetsM, bool applyDrawers = true, bool deepCopy = true, bool copyReferenceProperties = true, HashSet<MaterialProperty.PropType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            foreach (Material m in targetsM) m.renderQueue = MyShaderUI.Materials[0].renderQueue;
        }
        public override void CopyFrom(ShaderPart srcPart, bool applyDrawers = true, bool deepCopy = true, bool copyReferenceProperties = true, HashSet<MaterialProperty.PropType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            this.CopyFrom(srcPart.MaterialProperty.targets[0] as Material);
        }
        public override void CopyTo(ShaderPart targetPart, bool applyDrawers = true, bool deepCopy = true, bool copyReferenceProperties = true, HashSet<MaterialProperty.PropType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            this.CopyTo(targetPart.MaterialProperty.targets.Cast<Material>().ToArray());
        }
    }
    public class VRCFallbackProperty : ShaderProperty
    {
        static string[] s_fallbackShaderTypes = { "Standard", "Toon", "Unlit", "VertexLit", "Particle", "Sprite", "Matcap", "MobileToon", "toonstandard", "toonstandardoutline" };
        static string[] s_fallbackRenderTypes = { "Opaque", "Cutout", "Transparent", "Fade" };
        static string[] s_fallbackRenderTypesValues = { "", "Cutout", "Transparent", "Fade" };
        static string[] s_fallbackCullTypes = { "OneSided", "DoubleSided" };
        static string[] s_fallbackCullTypesValues = { "", "DoubleSided" };
        static string[] s_fallbackNoTypes = { "None", "Hidden" };
        static string[] s_fallbackNoTypesValues = { "", "Hidden" };
        static string[] s_vRCFallbackOptionsPopup = s_fallbackNoTypes.Union(s_fallbackShaderTypes.SelectMany(s => s_fallbackRenderTypes.SelectMany(r => s_fallbackCullTypes.Select(c => r + "/" + c).Select(rc => s + "/" + rc)))).ToArray();
        static string[] s_vRCFallbackOptionsValues = s_fallbackNoTypes.Union(s_fallbackShaderTypes.SelectMany(s => s_fallbackRenderTypesValues.SelectMany(r => s_fallbackCullTypesValues.Select(c => r + c).Select(rc => s + rc)))).ToArray();

        public VRCFallbackProperty(ShaderEditor shaderEditor) : base(shaderEditor, "VRCFallback", 0, "", "Select the shader VRChat should use when your shaders are being hidden.", -1)
        {
            _doCustomDrawLogic = true;
            IsAnimatable = false;
            CustomStringTagID = "VRCFallback";
            IsExemptFromLockedDisabling = true;
        }

        protected override void DrawDefault()
        {
            Rect r = RectifiedLayout.GetPaddedRect(18);
            string current = MyShaderUI.Materials[0].GetTag("VRCFallback", false, "None");
            EditorGUI.BeginChangeCheck();
            // Reset indent to 0 since our rect already has padding
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            int selected = EditorGUI.Popup(r, "VRChat Fallback Shader", s_vRCFallbackOptionsValues.Select((f, i) => (f, i)).FirstOrDefault(f => f.f == current).i, s_vRCFallbackOptionsPopup);
            EditorGUI.indentLevel = oldIndent;
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material m in MyShaderUI.Materials)
                {
                    m.SetOverrideTag("VRCFallback", s_vRCFallbackOptionsValues[selected]);
                    EditorUtility.SetDirty(m);
                }
            }
        }

        public void SetPropertyValue(string value)
        {
            foreach (Material m in MyShaderUI.Materials)
            {
                m.SetOverrideTag("VRCFallback", value);
                EditorUtility.SetDirty(m);
            }
        }

        public override object FetchPropertyValue()
        {
            return MyShaderUI.Materials[0].GetTag("VRCFallback", false, "None");
        }

        public override object PropertyDefaultValue => "None";
        public override bool IsPropertyValueDefault => MyShaderUI.Materials.All(m => m.GetTag("VRCFallback", false, "None") == "None");

        public override void CopyFrom(Material sourceM, bool applyDrawers = true, bool deepCopy = true, bool copyReferenceProperties = true, HashSet<MaterialProperty.PropType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            string value = sourceM.GetTag("VRCFallback", false, "None");
            foreach (Material m in MyShaderUI.Materials) m.SetOverrideTag("VRCFallback", value);
        }
        public override void CopyTo(Material[] targetsM, bool applyDrawers = true, bool deepCopy = true, bool copyReferenceProperties = true, HashSet<MaterialProperty.PropType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            string value = MyShaderUI.Materials[0].GetTag("VRCFallback", false, "None");
            foreach (Material m in targetsM) m.SetOverrideTag("VRCFallback", value);
        }
        public override void CopyFrom(ShaderPart srcPart, bool applyDrawers = true, bool deepCopy = true, bool copyReferenceProperties = true, HashSet<MaterialProperty.PropType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            this.CopyFrom(srcPart.MaterialProperty.targets[0] as Material, applyDrawers);
        }
        public override void CopyTo(ShaderPart targetPart, bool applyDrawers = true, bool deepCopy = true, bool copyReferenceProperties = true, HashSet<MaterialProperty.PropType> skipPropertyTypes = null, HashSet<string> skipPropertyNames = null)
        {
            this.CopyTo(targetPart.MaterialProperty.targets.Cast<Material>().ToArray(), applyDrawers);
        }
    }
    public class InstancingProperty : ShaderProperty
    {
        public InstancingProperty(ShaderEditor shaderEditor, MaterialProperty materialProperty, string displayName, int xOffset, string optionsRaw, bool forceOneLine, int property_index) : base(shaderEditor, materialProperty, displayName, xOffset, optionsRaw, forceOneLine, property_index)
        {
            _doCustomDrawLogic = true;
            IsAnimatable = false;
        }

        public override object FetchPropertyValue()
        {
            return MyShaderUI.Materials[0].enableInstancing;
        }
        public override object PropertyDefaultValue => false;
        public override bool IsPropertyValueDefault => MyShaderUI.Materials.All(m => m.enableInstancing == false);

        protected override void DrawDefault()
        {
            Rect r = GUILib.GetPropertyRect(XOffset, EditorGUIUtility.singleLineHeight);
            bool mixed = MyShaderUI.Materials.Any(m => m.enableInstancing != MyShaderUI.Materials[0].enableInstancing);
            EditorGUI.showMixedValue = mixed;
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUI.Toggle(r, new GUIContent("Enable GPU Instancing"), MyShaderUI.Materials[0].enableInstancing);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material m in MyShaderUI.Materials)
                    m.enableInstancing = enabled;
            }
            EditorGUI.showMixedValue = false;
        }
    }
    public class GIProperty : ShaderProperty
    {
        public GIProperty(ShaderEditor shaderEditor, MaterialProperty materialProperty, string displayName, int xOffset, string optionsRaw, bool forceOneLine, int property_index) : base(shaderEditor, materialProperty, displayName, xOffset, optionsRaw, forceOneLine, property_index)
        {
            _doCustomDrawLogic = true;
            IsAnimatable = false;
        }

        protected override void DrawInternal(GUIContent content, Rect? rect = null, bool useEditorIndent = false, bool isInHeader = false)
        {
            base.DrawInternal(content, rect, useEditorIndent, isInHeader);
        }

        protected override void DrawDefault()
        {
            LightmapEmissionFlagsProperty(XOffset, false);
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
            MaterialGlobalIlluminationFlags giFlags = MyShaderUI.Materials[0].globalIlluminationFlags & any_em;
            bool isMixed = false;
            for (int i = 1; i < MyShaderUI.Materials.Length; i++)
            {
                if ((MyShaderUI.Materials[i].globalIlluminationFlags & any_em) != giFlags)
                {
                    isMixed = true;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();

            // Show popup with proper positioning
            Rect r = GUILib.GetPropertyRect(indent, EditorGUIUtility.singleLineHeight);
            EditorGUI.showMixedValue = isMixed;
            giFlags = (MaterialGlobalIlluminationFlags)EditorGUI.IntPopup(r, lightmapEmissiveLabel, (int)giFlags, lightmapEmissiveStrings, lightmapEmissiveValues);
            EditorGUI.showMixedValue = false;

            // Apply flags. But only the part that this tool modifies (RealtimeEmissive, BakedEmissive, None)
            bool applyFlags = EditorGUI.EndChangeCheck();
            foreach (Material mat in MyShaderUI.Materials)
            {
                mat.globalIlluminationFlags = applyFlags ? giFlags : mat.globalIlluminationFlags;
                if (!ignoreEmissionColor)
                {
                    FixupEmissiveFlag(mat);
                }
            }
        }

        public override object FetchPropertyValue()
        {
            return MyShaderUI.Materials[0].globalIlluminationFlags;
        }

        public override object PropertyDefaultValue => MaterialGlobalIlluminationFlags.AnyEmissive;
        public override bool IsPropertyValueDefault => MyShaderUI.Materials.All(m => m.globalIlluminationFlags == MaterialGlobalIlluminationFlags.AnyEmissive);
    }
    public class DSGIProperty : ShaderProperty
    {
        public DSGIProperty(ShaderEditor shaderEditor, MaterialProperty materialProperty, string displayName, int xOffset, string optionsRaw, bool forceOneLine, int property_index) : base(shaderEditor, materialProperty, displayName, xOffset, optionsRaw, forceOneLine, property_index)
        {
            _doCustomDrawLogic = true;
            IsAnimatable = false;
        }

        protected override void DrawDefault()
        {
            MyShaderUI.Editor.DoubleSidedGIField();
        }

        public override object FetchPropertyValue()
        {
            return MyShaderUI.Materials[0].doubleSidedGI;
        }
        public override object PropertyDefaultValue => false;
        public override bool IsPropertyValueDefault => MyShaderUI.Materials.All(m => m.doubleSidedGI == false);
    }
    public class LocaleProperty : ShaderProperty
    {
        public LocaleProperty(ShaderEditor shaderEditor, MaterialProperty materialProperty, string displayName, int xOffset, string optionsRaw, bool forceOneLine, int property_index) : base(shaderEditor, materialProperty, displayName, xOffset, optionsRaw, forceOneLine, property_index)
        {
            _doCustomDrawLogic = true;
            IsAnimatable = false;
        }

        protected override void DrawInternal(GUIContent content, Rect? rect = null, bool useEditorIndent = false, bool isInHeader = false)
        {
            ShaderEditor.Active.Locale.DrawDropdown(rect.Value);
        }
    }
}
