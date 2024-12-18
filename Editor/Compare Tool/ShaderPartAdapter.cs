using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Thry.MaterialCompareTool
{
    class ShaderPartAdapter
    {
        public ShaderPart ShaderPart { get; private set; }
        public bool HasChildren => children.Count > 0;

        List<ShaderPartAdapter> children = new List<ShaderPartAdapter>();

        bool IsExpanded
        {
            get => HasChildren && _isExpanded;
            set => _isExpanded = value;
        }

        bool _isExpanded = false;

        private ShaderPartAdapter() {}

        public ShaderPartAdapter(ShaderPart shaderPart)
        {
            ShaderPart = shaderPart;
            if(shaderPart is ShaderGroup group)
            {
                foreach(var child in group.Children)
                    children.Add(new ShaderPartAdapter(child));
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
                    float halfViewWidth = EditorGUIUtility.currentViewWidth * 0.5f;
                    EditorGUILayout.BeginHorizontal();
                    DrawShaderProperty(ShaderPart.MaterialProperty, GUILayout.Width(halfViewWidth));
                    EditorGUILayout.EndHorizontal();
                    return;
                }

                EditorGUILayout.BeginHorizontal();
                var rect = EditorGUILayout.GetControlRect();

                var foldoutRect = new Rect(rect.x, rect.y, rect.width, rect.height);
                var labelRect = new Rect(rect.x + 32f, rect.y, rect.width - 32f, rect.height);
                EditorGUI.LabelField(rect, GUIContent.none, Styles.dropDownHeader);

                IsExpanded = EditorGUI.Foldout(foldoutRect, IsExpanded, string.Empty, true);
                EditorGUI.LabelField(labelRect, ShaderPart.Content);
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

        static void DrawShaderProperty(MaterialProperty prop, GUILayoutOption propertyWidth)
        {
            using(new EditorGUI.DisabledScope(true))
            {
                switch(prop.type)
                {
                    case MaterialProperty.PropType.Color:
                        EditorGUILayout.ColorField(prop.colorValue, propertyWidth);
                        break;
                    case MaterialProperty.PropType.Vector:
                        EditorGUILayout.Vector4Field(GUIContent.none, prop.vectorValue, propertyWidth);
                        break;
#if UNITY_2021_1_OR_NEWER
                    case MaterialProperty.PropType.Int:
                        EditorGUILayout.IntField(prop.intValue, propertyWidth);
                        break;
#endif
                    case MaterialProperty.PropType.Range:
                        EditorGUILayout.Slider(GUIContent.none, prop.floatValue, prop.rangeLimits.x,
                            prop.rangeLimits.y,
                            propertyWidth);
                        break;
                    case MaterialProperty.PropType.Float:
                        EditorGUILayout.FloatField(prop.floatValue, propertyWidth);
                        break;
                    case MaterialProperty.PropType.Texture:
                        EditorGUILayout.ObjectField(prop.textureValue, typeof(Texture), true, propertyWidth);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}