using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public static class MaterialToString
    {
        public static string ToYamlString(this ShaderPart shaderPart, bool ignorePropertiesWithDefaultValues, ref int indentLevel)
        {
            string result = string.Empty;
            if(shaderPart is ShaderGroup shaderGroup)
            {
                indentLevel++;
                foreach(var child in shaderGroup.Children)
                    result += child.ToYamlString(ignorePropertiesWithDefaultValues, ref indentLevel);
                indentLevel--;
            }
            
            if(shaderPart.IsHidden || (ignorePropertiesWithDefaultValues && shaderPart.IsPropertyValueDefault))
                return string.Empty;
            
            return $"{shaderPart.Content.text}: {shaderPart.PropertyValue}";
        }

        public static void ToYamlString(this ShaderPart shaderPart, bool ignorePropertiesWithDefaultValues, StringBuilder stringBuilder, ref int indentLevel)
        {
            if(shaderPart is ShaderGroup shaderGroup)
            {
                indentLevel++;
                foreach(var child in shaderGroup.Children)
                    stringBuilder.AppendLine(child.ToYamlString(ignorePropertiesWithDefaultValues, ref indentLevel));
                indentLevel--;
            }
            
            if(shaderPart.IsHidden || (ignorePropertiesWithDefaultValues && shaderPart.IsPropertyValueDefault))
                return;

            stringBuilder.AppendLine($"{shaderPart.MaterialProperty.displayName}: {shaderPart.PropertyValue}");

        }

        public static string ToYamlString(this Material material, bool ignorePropertiesWithDefaultValues)
        {
            StringBuilder sb = new StringBuilder();
            ShaderOptimizer.IsShaderUsingThryOptimizer(material.shader);

            MaterialEditor editor = (MaterialEditor)Editor.CreateEditor(material);
            ShaderEditor thryEditor = editor.customShaderGUI as ShaderEditor;
            thryEditor.SetShader(material.shader);
            thryEditor.FakePartialInitilizationForLocaleGathering(material.shader);
            
            string shaderName = material.shader.name;

            sb.AppendLine($"# Unity {Application.unityVersion}");
            sb.AppendLine($"Shader: {shaderName}");
            sb.AppendLine("Material:");
            int indentLevel = 0;
            
            foreach(var part in thryEditor.ShaderParts)
            {
                if(part.IsHidden || (ignorePropertiesWithDefaultValues && part.IsPropertyValueDefault))
                    continue;
                
                part.ToYamlString(ignorePropertiesWithDefaultValues, sb, ref indentLevel);
            }
            
            return sb.ToString();
        }
    }
}