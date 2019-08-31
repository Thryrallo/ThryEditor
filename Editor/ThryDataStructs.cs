using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public struct EditorData
    {
        public MaterialEditor editor;
        public MaterialProperty[] properties;
        public ThryEditor gui;
        public Material[] materials;
        public Shader shader;
        public Shader defaultShader;
        public ThryEditor.ShaderProperty currentProperty;
        public Dictionary<string, ThryEditor.ShaderProperty> propertyDictionary;
        public List<MaterialProperty> textureArrayProperties;
        public bool firstCall;
    }

    public class DrawingData
    {
        public static ThryEditor.TextureProperty currentTexProperty;
        public static Rect lastGuiObjectRect;
        public static bool lastPropertyUsedCustomDrawer;
    }

    public class GradientObject : ScriptableObject
    {
        public Gradient gradient = new Gradient();
    }

    public class GradientData
    {
        public GradientObject gradientObj;
        public SerializedProperty colorGradient;
        public SerializedObject serializedGradient;

        public Texture2D texture;
        public bool saved;
        public EditorWindow gradientWindow;
    }
}