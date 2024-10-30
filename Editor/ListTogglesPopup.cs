using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class ListTogglesPopup : EditorWindow
    {
        class ShaderPartUIAdapter
        {
            public ShaderPart ShaderPart { get; private set; }
            public bool HasChildren => children.Count > 0;
            public bool IsEnabled { get; set; } = true;

            List<ShaderPartUIAdapter> children = new List<ShaderPartUIAdapter>();

            void SetChildrenEnabledRecursive(bool enabled)
            {
                if(!HasChildren)
                    return;
                foreach(var child in children)
                {
                    child.IsEnabled = enabled;
                    child.SetChildrenEnabledRecursive(enabled);
                }
            }

            bool IsExpanded
            {
                get => HasChildren && _isExpanded;
                set => _isExpanded = value;
            }

            bool _isExpanded = false;
            bool _isEnabled = true;

            private ShaderPartUIAdapter() {}

            public ShaderPartUIAdapter(ShaderPart shaderPart)
            {
                ShaderPart = shaderPart;
                if(shaderPart is ShaderGroup group)
                {
                    foreach(var child in group.parts)
                        children.Add(new ShaderPartUIAdapter(child));
                }
            }

            public void DrawUI()
            {
                if(ShaderPart == null)
                    return;

                using(new EditorGUILayout.VerticalScope(Styles.padding2pxHorizontal1pxVertical))
                {
                    if(!HasChildren)
                    {
                        EditorGUILayout.BeginHorizontal();
                        IsEnabled = EditorGUILayout.ToggleLeft(ShaderPart.Content, IsEnabled);
                        DrawMaterialPropertyValue(ShaderPart.MaterialProperty);
                        EditorGUILayout.EndHorizontal();
                        return;
                    }

                    EditorGUILayout.BeginHorizontal();
                    var rect = EditorGUILayout.GetControlRect();

                    var foldoutRect = new Rect(rect.x, rect.y, rect.width, rect.height);
                    var toggleRect = new Rect(rect.x + 16f, rect.y, 14f, rect.height);
                    var labelRect = new Rect(rect.x + 32f, rect.y, rect.width - 32f, rect.height);
                    EditorGUI.LabelField(rect, GUIContent.none, Styles.dropDownHeader);
                    
                    IsEnabled = EditorGUI.Toggle(toggleRect, GUIContent.none, IsEnabled);
                    IsExpanded = EditorGUI.Foldout(foldoutRect, IsExpanded, string.Empty, true);
                    EditorGUI.LabelField(labelRect, ShaderPart.Content);
                    if(GUILayout.Button("None", GUILayout.MaxWidth(40f)))
                        SetChildrenEnabledRecursive(false);
                    if(GUILayout.Button("All", GUILayout.MaxWidth(40f)))
                        SetChildrenEnabledRecursive(true);
                    EditorGUILayout.EndHorizontal();
                    if(IsExpanded)
                    {
                        EditorGUI.indentLevel++;
                        foreach(var child in children)
                            child.DrawUI();
                        EditorGUI.indentLevel--;
                    }
                }
            }

            public void AddEnabledShaderPartsToListRecursive(ref List<ShaderPart> enabledParts)
            {
                if(!IsEnabled)
                    return;
                
                enabledParts.Add(ShaderPart);
                
                if(HasChildren)
                    foreach(var child in children)
                        child.AddEnabledShaderPartsToListRecursive(ref enabledParts);
            }
        }
        
        Vector2 scrollPosition = Vector2.zero;
        ShaderPartUIAdapter partAdapter;
        
        /// <summary>
        /// OnPasteClicked, comes with a list of shader parts the user left enabled
        /// </summary>
        public event Action<List<ShaderPart>> OnPasteClicked;
        
        public void Init(ShaderPart shaderPart)
        {
            partAdapter = new ShaderPartUIAdapter(shaderPart);
        }

        void OnGUI()
        {
            if(partAdapter?.ShaderPart == null)
                Close();
            
            using(var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                partAdapter.DrawUI();
                scrollPosition = scroll.scrollPosition;
            }

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Cancel", GUILayout.Height(30))) 
            {
                Close();
            }

            if(GUILayout.Button("Paste Selected", GUILayout.Height(30)))
            {
                List<ShaderPart> enabledParts = new List<ShaderPart>();
                partAdapter.AddEnabledShaderPartsToListRecursive(ref enabledParts);
                OnPasteClicked?.Invoke(enabledParts);

                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        static void DrawMaterialPropertyValue(MaterialProperty prop)
        {
            EditorGUI.BeginDisabledGroup(true);
            switch(prop.type)
            {
                case MaterialProperty.PropType.Color: 
                    EditorGUILayout.ColorField(prop.colorValue); 
                    break;
                case MaterialProperty.PropType.Vector:
                    EditorGUILayout.Vector4Field(GUIContent.none, prop.vectorValue);
                    break;
#if UNITY_2021_1_OR_NEWER
                case MaterialProperty.PropType.Int:
                    EditorGUILayout.IntField(prop.intValue);
                    break;
#else
                    EditorGUILayout.FloatField(prop.floatValue);
                    break;
#endif
                case MaterialProperty.PropType.Range:
                    EditorGUILayout.Slider(GUIContent.none, prop.floatValue, prop.rangeLimits.x, prop.rangeLimits.y);
                    break;
                case MaterialProperty.PropType.Float:
                    EditorGUILayout.FloatField(prop.floatValue);
                    break;
                case MaterialProperty.PropType.Texture:
                    EditorGUILayout.ObjectField(prop.textureValue, typeof(Texture), true);
                    break;
                default:
                    break;
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}