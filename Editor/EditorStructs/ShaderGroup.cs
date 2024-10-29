using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Thry.ThryEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Thry
{
    public class ShaderGroup : ShaderPart
    {
        public List<ShaderPart> parts = new List<ShaderPart>();
        protected bool _isExpanded;

        public ShaderGroup(ShaderEditor shaderEditor) : base(null, 0, "", null, shaderEditor)
        {

        }

        public ShaderGroup(ShaderEditor shaderEditor, string optionsRaw) : base(null, 0, "", null, shaderEditor)
        {
            this._optionsRaw = optionsRaw;
        }

        public ShaderGroup(ShaderEditor shaderEditor, MaterialProperty prop, MaterialEditor materialEditor, string displayName, int xOffset, string optionsRaw, int propertyIndex) : base(shaderEditor, prop, xOffset, displayName, optionsRaw, propertyIndex)
        {

        }

        protected override void InitOptions()
        {
            base.InitOptions();
            if (Options.persistent_expand) _isExpanded = this.MaterialProperty.GetNumber() == 1;
            else _isExpanded = Options.default_expand;
        }

        protected bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                if (Options.persistent_expand)
                {
                    if (AnimationMode.InAnimationMode())
                    {


#if UNITY_2020_1_OR_NEWER
                        // So we do this instead
                        _isExpanded = value;
#else
                        // This fails when unselecting the object in hirearchy
                        // Then reselecting it
                        // Don't know why
                        // It seems AnimationMode is not working properly in Unity 2022
                        // It worked fine in Unity 2019
                        
                        AnimationMode.StopAnimationMode();
                        this.MaterialProperty.SetNumber(value ? 1 : 0);
                        AnimationMode.StartAnimationMode();
#endif
                    }
                    else
                    {
                        this.MaterialProperty.SetNumber(value ? 1 : 0);
                    }
                }
                _isExpanded = value;
            }
        }

        protected bool DoDisableChildren
        {
            get
            {
                return Options.condition_enable_children != null && !Options.condition_enable_children.Test();
            }
        }

        public void addPart(ShaderPart part)
        {
            parts.Add(part);
        }

        public override void CopyFrom(Material src, bool isTopCall = false, MaterialProperty.PropType[] skipPropertyTypes = null)
        {
            if (ShouldSkipProperty(MaterialProperty, skipPropertyTypes)) return;
            CopyReferencePropertiesFrom(src, skipPropertyTypes);

            foreach (ShaderPart p in parts)
                p.CopyFrom(src, skipPropertyTypes: skipPropertyTypes);

            if (isTopCall) ActiveShaderEditor.ApplyDrawers();
        }

        public override void CopyFrom(ShaderPart srcPart, bool isTopCall = false, MaterialProperty.PropType[] skipPropertyTypes = null)
        {
            if (ShouldSkipProperty(MaterialProperty, skipPropertyTypes)) return;
            if (srcPart is ShaderGroup == false) return;
            ShaderGroup src = srcPart as ShaderGroup;
            CopyReferencePropertiesFrom(src, skipPropertyTypes);

            for (int i = 0; i < src.parts.Count && i < parts.Count; i++)
            {
                if (!ShouldSkipProperty(parts[i].MaterialProperty, skipPropertyTypes))
                    parts[i].CopyFrom(src.parts[i], skipPropertyTypes: skipPropertyTypes);
            }

            if (isTopCall) ActiveShaderEditor.ApplyDrawers();
        }

        public override void CopyTo(Material target, bool isTopCall = false, MaterialProperty.PropType[] skipPropertyTypes = null)
        {
            if (ShouldSkipProperty(MaterialProperty, skipPropertyTypes)) return;
            CopyReferencePropertiesTo(target, skipPropertyTypes);

            foreach (ShaderPart p in parts)
            {
                if (!ShouldSkipProperty(p.MaterialProperty, skipPropertyTypes))
                    p.CopyTo(target, skipPropertyTypes: skipPropertyTypes);
            }

            if (isTopCall) MaterialEditor.ApplyMaterialPropertyDrawers(target);
        }

        public override void CopyTo(ShaderPart targetPart, bool isTopCall = false, MaterialProperty.PropType[] skipPropertyTypes = null)
        {
            if (ShouldSkipProperty(MaterialProperty, skipPropertyTypes)) return;
            if (targetPart is ShaderGroup == false) return;
            ShaderGroup target = targetPart as ShaderGroup;
            CopyReferencePropertiesTo(target, skipPropertyTypes);

            for(int i = 0; i < parts.Count && i < target.parts.Count; i++)
            {
                if (!ShouldSkipProperty(parts[i].MaterialProperty, skipPropertyTypes))
                    parts[i].CopyTo(target.parts[i], skipPropertyTypes: skipPropertyTypes);
            }

            if (isTopCall) MaterialEditor.ApplyMaterialPropertyDrawers(target.MaterialProperty.targets);
        }

        public override void DrawInternal(GUIContent content, Rect? rect = null, bool useEditorIndent = false, bool isInHeader = false)
        {
            if (Options.margin_top > 0)
            {
                GUILayoutUtility.GetRect(0, Options.margin_top);
            }
            foreach (ShaderPart part in parts)
            {
                part.Draw();
            }
        }

        public override void FindUnusedTextures(List<string> unusedList, bool isEnabled)
        {
            if (isEnabled && Options.condition_enable != null)
            {
                isEnabled &= Options.condition_enable.Test();
            }
            foreach (ShaderPart p in (this as ShaderGroup).parts)
                p.FindUnusedTextures(unusedList, isEnabled);
        }

        protected void HandleLinkedMaterials()
        {
            List<Material> linked_materials = MaterialLinker.GetLinked(MaterialProperty);
            if (linked_materials != null)
                foreach (Material m in linked_materials)
                    this.CopyTo(m);
        }

        protected void FoldoutArrow(Rect rect, Event e)
        {
            if (e.type == EventType.Repaint)
            {
                Rect arrowRect = new RectOffset(4, 0, 0, 0).Remove(rect);
                arrowRect.width = 13;
                EditorStyles.foldout.Draw(arrowRect, false, false, _isExpanded, false);
            }
        }
    }

}