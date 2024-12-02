using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Thry.MaterialCompareTool
{
    public class MaterialRepresentation
    {
        List<ShaderPartAdapter> shaderParts;

        public MaterialRepresentation(Material material)
        {
            var shaderEditor = new ShaderEditor()
            {
                
            };
            var materialEditor = MaterialEditor.CreateEditor(material);
            
            var mainShaderParts = shaderEditor.ShaderParts.Where(prop => prop is ShaderGroup group && group.Parent == null);
            shaderParts = mainShaderParts.Select(part => new ShaderPartAdapter(part)).ToList();
        }

        public void DrawGUI()
        {
            if(shaderParts == null)
                return;
            
            foreach(var part in shaderParts)
                part.DrawUI();
        }
    }
}
