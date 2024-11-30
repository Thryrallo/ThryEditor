using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public static class MaterialToDebugString
    {
        [Serializable]
        struct MaterialDebugInfo
        {
            [Serializable]
            public struct MaterialPropertyInfo
            {
                public string propertyName;
                public object propertyValue;
                public bool propertyValueIsDefault;

                public List<MaterialPropertyInfo> childProperties;

                public bool HasChildren => childProperties?.Count > 0;
                public override string ToString()
                {
                    if(!HasChildren)
                        return $"{propertyName}: {propertyValue}";
                    
                    StringBuilder sb = new StringBuilder($"{propertyName}: {propertyValue}");
                    foreach(var child in childProperties)
                        sb.AppendLine($"  {child}");
                    return sb.ToString();
                }
            }

            [Serializable]
            public struct MaterialMetaInfo
            {
                public string unityVersion;
                public string shaderName;
                public string shaderGuid;
                public string materialName;
                
                public override string ToString()
                {
                    return $"Unity: {unityVersion}\nShader: {shaderName}\nShaderGuid: {shaderGuid}\nMaterial: {materialName}";
                }
            }

            public MaterialMetaInfo metaInfo;
            public List<MaterialPropertyInfo> materialProperties;
        }

        public static string ConvertMaterialToDebugString(Material material, bool onlyNonDefaultProperties)
        {
            var editor = Editor.CreateEditor(material) as MaterialEditor;
            var shaderGui = editor.customShaderGUI as ShaderEditor;
            shaderGui.SetShader(material.shader);
            shaderGui.FakePartialInitilizationForLocaleGathering(material.shader);
            
            return ConvertMaterialToDebugString(shaderGui, onlyNonDefaultProperties);
        }

        public static string ConvertMaterialToDebugString(ShaderEditor thryEditor, bool onlyNonDefaultProperties)
        {
            var material = thryEditor.Materials[0];
            var info = new MaterialDebugInfo
            {
                metaInfo =
                {
                    unityVersion = Application.unityVersion,
                    shaderName = material.shader.name,
                    shaderGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material.shader)),
                    materialName = material.name,
                }
            };
            info.materialProperties = thryEditor.ShaderParts
                //.Where(x => IsValidShaderPart(x, onlyNonDefaultProperties))
                .Where(x => x is ShaderGroup)
                .Select(ShaderPartToMaterialPropertyInfo)
                .ToList();
            
            return EditorJsonUtility.ToJson(info, true);
        }

        static bool IsValidShaderPart(ShaderPart shaderPart, bool onlyNonDefaultProperties)
        {
            bool isHidden = shaderPart.IsHidden || (shaderPart.MaterialProperty != null && shaderPart.MaterialProperty.flags.HasFlag(MaterialProperty.PropFlags.HideInInspector));
            if(isHidden)
                return false;

            if(onlyNonDefaultProperties && ShaderPartIsDefault(shaderPart))
                return false;
            return true;
        }

        static bool ShaderPartIsDefault(ShaderPart part)
        {
            if(part is ShaderGroup group)
                return group.IsPropertyValueDefault && group.Children.All(child => child.IsPropertyValueDefault);
            return part.IsPropertyValueDefault;
        }

        static MaterialDebugInfo.MaterialPropertyInfo ShaderPartToMaterialPropertyInfo(ShaderPart shaderPart)
        {
            var partInfo = new MaterialDebugInfo.MaterialPropertyInfo()
            {
                propertyName = shaderPart.MaterialProperty?.name ?? shaderPart.PropertyIdentifier,
                propertyValue = shaderPart.PropertyValue,
            };
            
            if(shaderPart is ShaderGroup group && group.Children != null)
                partInfo.childProperties = group.Children.Select(ShaderPartToMaterialPropertyInfo).ToList();
            
            return partInfo;
        }
    }
}