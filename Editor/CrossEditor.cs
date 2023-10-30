using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using Thry;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class CrossEditor : EditorWindow
    {
        [MenuItem("Thry/Cross Shader Editor", priority = -20)]
        public static void ShowWindow()
        {
            CrossEditor window = EditorWindow.GetWindow(typeof(CrossEditor)) as CrossEditor;
            window.name = "Cross Shader Editor";
        }

        [MenuItem("Assets/Thry/Materials/Open in Cross Shader Editor")]
        public static void OpenInCrossShaderEditor()
        {
            CrossEditor window = EditorWindow.GetWindow(typeof(CrossEditor)) as CrossEditor;
            window.name = "Cross Shader Editor";
            window._targets = Selection.objects.Where(o => o is Material).Cast<Material>().ToList();
            window._shaderEditor = null;
        }

        [MenuItem("Assets/Thry/Materials/Open in Cross Shader Editor", true)]
        public static bool OpenInCrossShaderEditorValidation()
        {
            return Selection.objects.All(o => o is Material);
        }

        List<Material> _targets = new List<Material>();
        Dictionary<Material,Shader> _targetShaders = new Dictionary<Material, Shader>();
        ShaderEditor _shaderEditor = null;
        MaterialEditor _materialEditor = null;
        MaterialProperty[] _materialProperties = null;
        Vector2 _scrollPosition = Vector2.zero;
        bool _showMaterials = true;

        private void OnGUI()
        {
            // List of materials, remove button next to each
            // Add button at bottom

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            _showMaterials = EditorGUILayout.Foldout(_showMaterials, "Materials");
            EditorGUI.BeginChangeCheck();
            if(_showMaterials)
            {
                EditorGUILayout.BeginVertical();
                for (int i = 0; i < _targets.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    _targets[i] = (Material)EditorGUILayout.ObjectField(_targets[i], typeof(Material), false);
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        _targets.RemoveAt(i);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(30);
                if (GUILayout.Button("Add", GUILayout.Width(70)))
                {
                    _targets.Add(null);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            // Check if targets have changed
            bool didShadersChange = false;
            foreach(Material m in _targets)
            {
                if(!_targetShaders.ContainsKey(m) || _targetShaders[m] != m.shader)
                {
                    didShadersChange = true;
                    _targetShaders[m] = m.shader;
                }
            }
            if (EditorGUI.EndChangeCheck() || didShadersChange)
            {
                _shaderEditor = null;
            }

            // Draw shader editor
            if (_targets.Count > 0)
            {
                // Create shader editor
                if (_shaderEditor == null)
                {
                    _shaderEditor = new ShaderEditor();
                    _materialEditor = MaterialEditor.CreateEditor(_targets.ToArray()) as MaterialEditor;

                    // group targets by shader, take one material per shader
                    IEnumerable<Material> materialsToSearchProperties = _targets.GroupBy(t => t.shader).Select(g => g.First());
                    // get all properties of each material
                    IEnumerable<string[]> properties = materialsToSearchProperties.Select(m => MaterialEditor.GetMaterialProperties(new UnityEngine.Object[] { m }).Select(p => p.name).ToArray());
                    // get intersection of all properties
                    string[] intersection = properties.Aggregate((a, b) => a.Intersect(b).ToArray());

                    // Get MaterialProperties of all materials
                    _materialProperties = intersection.Select(p => MaterialEditor.GetMaterialProperty(_targets.ToArray(), p)).ToArray();
                }

                // Seperator
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                // Cursed but makes it render similar to the inspector
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(15);
                EditorGUILayout.BeginVertical();
                _shaderEditor.OnGUI(_materialEditor, _materialProperties);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                
            }

            EditorGUILayout.EndScrollView();
        }
    }
}