using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Thry.ThryEditor.ShaderTranslations
{
    [CustomEditor(typeof(ShaderTranslator))]
    public class ShaderTranslatorEditorUI : Editor
    {
        List<string> shaderNames;
        public List<string> sourceShaderPropertyNames;
        public List<string> targetShaderPropertyNames;

        static UnityEngine.Object[] _material;
        ListView propertyList;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            var treeAsset = Resources.Load<VisualTreeAsset>("Thry/TranslatorEditor");
            treeAsset.CloneTree(root);

            shaderNames = AssetDatabase.FindAssets("t:shader")
               .Select(g => AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(g)).name)
               .Where(s => s.StartsWith("Hidden") == false).ToList();

            var translationsProp = serializedObject.FindProperty(nameof(ShaderTranslator.PropertyTranslations));
            propertyList = root.Q<ListView>("propertyList");
            propertyList.makeItem = () => new TranslatorListItem(this);

            SetupShaderSelectionUI(root.Q<VisualElement>("originShaderContainer"), true);
            SetupShaderSelectionUI(root.Q<VisualElement>("targetShaderContainer"), false);

            return root;
        }

        void SetupShaderSelectionUI(VisualElement container, bool isSourceShader)
        {
            var shaderText = container.Q<TextField>("shaderText");
            var shaderDropdown = container.Q<DropdownField>("shaderDropdown");

            shaderDropdown.choices = shaderNames;
            shaderDropdown.RegisterValueChangedCallback(evt => shaderText.value = evt.newValue);

            shaderText.RegisterValueChangedCallback(evt => UpdateShaderProperties(evt.newValue, ref isSourceShader ? ref sourceShaderPropertyNames : ref targetShaderPropertyNames));

            var shaderRegexText = container.Q<TextField>("shaderRegexText");
            var regexToggle = container.Q<Toggle>("shaderRegexToggle");

            regexToggle.RegisterValueChangedCallback(evt => HandleRegexEnabled(shaderText, shaderRegexText, shaderDropdown, evt.newValue));

            EditorApplication.delayCall += () =>
            {
                HandleRegexEnabled(shaderText, shaderRegexText, shaderDropdown, regexToggle.value);
            };
        }

        void HandleRegexEnabled(TextField shaderText, TextField regexText, DropdownField shaderDropdown, bool isRegexEnabled)
        {
            UIElementsHelpers.SetTextFieldReadonly(shaderText, isRegexEnabled);
            UIElementsHelpers.SetTextFieldReadonly(regexText, !isRegexEnabled);

            shaderDropdown.SetEnabled(!isRegexEnabled);
        }

        void UpdateShaderProperties(string shaderName, ref List<string> properties)
        {
            var shader = Shader.Find(shaderName);
            if(!shader)
                return;

            if(_material == null)
                _material = new UnityEngine.Object[] { new Material(shader) };

            (_material[0] as Material).shader = shader;

            properties = MaterialEditor.GetMaterialPropertyNames(_material).ToList();

            propertyList.Rebuild();
        }

        #region Old inspector
        public void OnInspectorGUI_old() // override
        {
            serializedObject.Update();
            ShaderTranslator translator = serializedObject.targetObject as ShaderTranslator;

            translator.Name = EditorGUILayout.TextField("Translation File Name: ", translator.Name);

            GUILayout.Space(10);

            string[] shaders = AssetDatabase.FindAssets("t:shader").Select(g => AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(g)).name).
                Where(s => s.StartsWith("Hidden") == false).ToArray();

            EditorGUI.BeginChangeCheck();
            int originIndex = EditorGUILayout.Popup("From Shader", Array.IndexOf(shaders, translator.OriginShader), shaders);
            if(EditorGUI.EndChangeCheck()) translator.OriginShader = shaders[originIndex];

            EditorGUI.BeginChangeCheck();
            int targetIndex = EditorGUILayout.Popup("To Shader", Array.IndexOf(shaders, translator.TargetShader), shaders);
            if(EditorGUI.EndChangeCheck()) translator.TargetShader = shaders[targetIndex];

            translator.MatchOriginShaderBasedOnRegex = EditorGUILayout.ToggleLeft(new GUIContent("Match Origin Shader Using Regex",
                "Match the origin shader for suggestions based on a regex definition."), translator.MatchOriginShaderBasedOnRegex);
            if(translator.MatchOriginShaderBasedOnRegex)
                translator.OriginShaderRegex = EditorGUILayout.TextField("Origin Shader Regex", translator.OriginShaderRegex);
            translator.MatchTargetShaderBasedOnRegex = EditorGUILayout.ToggleLeft(new GUIContent("Match Target Shader Using Regex",
                "Match the target shader for suggestions based on a regex definition."), translator.MatchTargetShaderBasedOnRegex);
            if(translator.MatchTargetShaderBasedOnRegex)
                translator.TargetShaderRegex = EditorGUILayout.TextField("Target Shader Regex", translator.TargetShaderRegex);

            if(originIndex < 0 || targetIndex < 0)
            {
                EditorGUILayout.HelpBox("Could not find origin or target shader.", MessageType.Error);
                return;
            }

            Shader origin = Shader.Find(shaders[originIndex]);
            Shader target = Shader.Find(shaders[targetIndex]);

            GUILayout.Space(10);

            using(new GUILayout.VerticalScope("box"))
            {
                GUILayout.Label("Property Translation", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                GUILayout.Label("From");
                GUILayout.Label("To");
                GUILayout.Label("Math");
                GUILayout.EndHorizontal();
                List<PropertyTranslation> remove = new List<PropertyTranslation>();
                foreach(PropertyTranslation trans in translator.GetPropertyTranslations())
                {
                    Rect fullWidth = EditorGUILayout.GetControlRect();
                    Rect r = fullWidth;
                    r.width = (r.width - 20) / 3;
                    if(GUI.Button(r, trans.Origin)) GuiHelper.SearchableEnumPopup.CreateSearchableEnumPopup(
                         MaterialEditor.GetMaterialProperties(new UnityEngine.Object[] { new Material(origin) }).Select(p => p.name).ToArray(), trans.Origin,
                         (newValue) => trans.Origin = newValue);
                    r.x += r.width;
                    if(GUI.Button(r, trans.Target)) GuiHelper.SearchableEnumPopup.CreateSearchableEnumPopup(
                         MaterialEditor.GetMaterialProperties(new UnityEngine.Object[] { new Material(target) }).Select(p => p.name).ToArray(), trans.Target,
                         (newValue) => trans.Target = newValue);
                    r.x += r.width;
                    trans.Math = EditorGUI.TextField(r, trans.Math);
                    r.x += r.width;
                    r.width = 20;
                    if(GUI.Button(r, GUIContent.none, Styles.icon_style_remove)) remove.Add(trans);
                }

                foreach(PropertyTranslation r in remove)
                    translator.GetPropertyTranslations().Remove(r);

                Rect buttonRect = EditorGUILayout.GetControlRect();
                buttonRect.x = buttonRect.width - 20;
                buttonRect.width = 20;
                if(GUI.Button(buttonRect, GUIContent.none, Styles.icon_style_add)) translator.GetPropertyTranslations().Add(new PropertyTranslation());
            }

            serializedObject.Update();
            EditorUtility.SetDirty(serializedObject.targetObject);
        }
        #endregion
    }
}