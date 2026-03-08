using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Thry.ThryEditor.Helpers;

namespace Thry.ThryEditor
{
    [CustomEditor(typeof(Blueprint))]
    public class BlueprintEditor : Editor
    {
        SerializedProperty _targetShaderProp;
        SerializedProperty _renderingPresetValueProp;
        SerializedProperty _presetNamesProp;
        SerializedProperty _sectionPresetEntriesProp;

        ReorderableList _presetList;
        ReorderableList _sectionPresetList;
        string[] _shaderNames;
        int _selectedShaderIndex = -1;
        string[] _availablePresetNames;
        string[] _sectionPresetDisplayLabels;
        string[] _sectionPresetStoredValues;

        Dictionary<string, string> _collectionKeyToDisplayName = new Dictionary<string, string>();

        // Rendering Preset mode names for display
        // These are the standard Poiyomi rendering modes.
        // If the shader defines different options via ThryWideEnum, the user
        // sees the Float value and can consult the shader for the mapping.
        static readonly string[] s_renderingModeLabels = new string[]
        {
            "Skip (Don't Set)",
            "Opaque",
            "Cutout",
            "Fade",
            "Transparent",
            "Additive",
            "Soft Additive",
            "Multiplicative",
            "2x Multiplicative",
            "TransClipping",
            "Custom (enter value)"
        };

        static readonly float[] s_dropdownIndexToModeValue = new float[]
        {
            -1, 0, 1, 2, 3, 4, 5, 6, 7, 9, -99
        };

        bool _useCustomRenderingValue = false;

        void OnEnable()
        {
            _targetShaderProp = serializedObject.FindProperty(nameof(Blueprint.TargetShader));
            _renderingPresetValueProp = serializedObject.FindProperty(nameof(Blueprint.RenderingPresetValue));
            _presetNamesProp = serializedObject.FindProperty(nameof(Blueprint.PresetNames));
            _sectionPresetEntriesProp = serializedObject.FindProperty(nameof(Blueprint.SectionPresetEntries));

            // Build shader name list
            _shaderNames = ShaderUtil.GetAllShaderInfo().Select(s => s.name).Where(s => !s.StartsWith("Hidden/")).Where(s => { Shader sh = Shader.Find(s); return sh != null && ShaderHelper.IsShaderUsingThryEditor(sh); }).OrderBy(s => s).ToArray();

            // Determine current shader selection index
            Shader currentShader = _targetShaderProp.objectReferenceValue as Shader;
            if (currentShader != null) _selectedShaderIndex = System.Array.IndexOf(_shaderNames, currentShader.name);

            // Determine rendering mode selection state
            float currentVal = _renderingPresetValueProp.floatValue;
            _useCustomRenderingValue = currentVal >= 0 && RenderingValueToDropdownIndex(currentVal) == s_renderingModeLabels.Length - 1;

            RefreshAvailablePresets();

            // Setup the Reorderable list for presets
            _presetList = new ReorderableList(serializedObject, _presetNamesProp, true, true, true, true);
            _presetList.drawHeaderCallback = DrawPresetListHeader;
            _presetList.drawElementCallback = DrawPresetListElement;
            _presetList.elementHeight = EditorGUIUtility.singleLineHeight + 4;
            _presetList.onAddCallback = OnPresetAdded;

            // Setup the Reorderable list for section presets
            _sectionPresetList = new ReorderableList(serializedObject, _sectionPresetEntriesProp, true, true, true, true);
            _sectionPresetList.drawHeaderCallback = DrawSectionPresetListHeader;
            _sectionPresetList.drawElementCallback = DrawSectionPresetListElement;
            _sectionPresetList.elementHeight = EditorGUIUtility.singleLineHeight + 4;
            _sectionPresetList.onAddCallback = OnSectionPresetAdded;
        }

        void RefreshAvailablePresets()
        {
            _availablePresetNames = Presets.GetFullPresetNames().ToArray();
            RefreshSectionPresetData();
        }

        void RefreshSectionPresetData()
        {
            _collectionKeyToDisplayName.Clear();
            Shader targetShader = _targetShaderProp.objectReferenceValue as Shader;
            if (targetShader != null)
            {
                int propCount = targetShader.GetPropertyCount();
                for (int i = 0; i < propCount; i++)
                {
                    string propName = targetShader.GetPropertyName(i);
                    // Section collections use header property names (m_start_xxx, m_xxx, etc.)
                    string displayName = targetShader.GetPropertyDescription(i);
                    // Strip ThryEditor options from display name (everything after "--")
                    int optIdx = displayName.IndexOf("--", System.StringComparison.Ordinal);
                    if (optIdx >= 0) displayName = displayName.Substring(0, optIdx).Trim();
                    if (!string.IsNullOrEmpty(displayName)) _collectionKeyToDisplayName[propName] = displayName;
                }
            }

            List<string> labels = new List<string>();
            List<string> values = new List<string>();

            List<string> collectionKeys = Presets.GetSectionCollectionKeys();
            foreach (string key in collectionKeys.OrderBy(k => GetSectionDisplayName(k)))
            {
                string sectionDisplay = GetSectionDisplayName(key);
                List<string> presetNames = Presets.GetSectionPresetNames(key);
                foreach (string presetName in presetNames)
                {
                    labels.Add($"{sectionDisplay}/{presetName}");
                    values.Add($"{key}/{presetName}");
                }
            }

            _sectionPresetDisplayLabels = labels.ToArray();
            _sectionPresetStoredValues = values.ToArray();
        }

        string GetSectionDisplayName(string collectionKey)
        {
            if (_collectionKeyToDisplayName.TryGetValue(collectionKey, out string displayName)) return displayName;
            return collectionKey;
        }

        string GetSectionEntryDisplayLabel(string storedValue)
        {
            if (Blueprint.TryParseSectionEntry(storedValue, out string key, out string name)) return $"{GetSectionDisplayName(key)}/{name}";
            return storedValue;
        }

        void DrawPresetListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "List of Presets to Apply");
        }

        void DrawPresetListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = _presetNamesProp.GetArrayElementAtIndex(index);
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;

            float labelWidth = 30f;
            float buttonWidth = rect.width - labelWidth - 4f;

            // Index label
            Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            EditorGUI.LabelField(labelRect, $"#{index + 1}");

            // Preset dropdown button
            Rect buttonRect = new Rect(rect.x + labelWidth + 4f, rect.y, buttonWidth, rect.height);
            string currentName = element.stringValue;

            // Determine display text and style
            string displayText;
            bool isValid;
            if (string.IsNullOrEmpty(currentName))
            {
                displayText = "(Select a Preset)";
                isValid = false;
            }
            else if (System.Array.IndexOf(_availablePresetNames, currentName) < 0)
            {
                displayText = currentName + " [MISSING]";
                isValid = false;
            }
            else
            {
                displayText = currentName;
                isValid = true;
            }

            // Draw as a popup-style button that opens EditorUtility.DisplayCustomMenu
            // This matches the style used by the existing Presets system
            GUIStyle popupStyle = new GUIStyle(EditorStyles.popup);
            if (!isValid && !string.IsNullOrEmpty(currentName)) popupStyle.normal.textColor = new Color(1f, 0.4f, 0.2f);

            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(displayText), FocusType.Keyboard, popupStyle))
            {
                GUIContent[] menuItems = _availablePresetNames.Select(n => new GUIContent(n)).ToArray();

                int currentSelected = System.Array.IndexOf(_availablePresetNames, currentName);

                EditorUtility.DisplayCustomMenu(buttonRect, menuItems, currentSelected, (userData, options, selected) =>
                {
                    SerializedProperty prop = userData as SerializedProperty;
                    prop.serializedObject.Update();
                    prop.stringValue = options[selected];
                    prop.serializedObject.ApplyModifiedProperties();
                },
                element);
            }
        }

        void OnPresetAdded(ReorderableList list)
        {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.InsertArrayElementAtIndex(index);
            SerializedProperty newElement = list.serializedProperty.GetArrayElementAtIndex(index);
            newElement.stringValue = "";
        }

        void DrawSectionPresetListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "List of Section Presets to Apply");
        }

        void DrawSectionPresetListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = _sectionPresetEntriesProp.GetArrayElementAtIndex(index);
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;

            float labelWidth = 30f;
            float buttonWidth = rect.width - labelWidth - 4f;

            Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            EditorGUI.LabelField(labelRect, $"#{index + 1}");

            Rect buttonRect = new Rect(rect.x + labelWidth + 4f, rect.y, buttonWidth, rect.height);
            string storedValue = element.stringValue;

            string displayText;
            bool isValid;
            if (string.IsNullOrEmpty(storedValue))
            {
                displayText = "(Select a Section Preset)";
                isValid = false;
            }
            else
            {
                int idx = System.Array.IndexOf(_sectionPresetStoredValues, storedValue);
                if (idx < 0)
                {
                    displayText = GetSectionEntryDisplayLabel(storedValue) + " [MISSING]";
                    isValid = false;
                }
                else
                {
                    displayText = _sectionPresetDisplayLabels[idx];
                    isValid = true;
                }
            }

            GUIStyle popupStyle = new GUIStyle(EditorStyles.popup);
            if (!isValid && !string.IsNullOrEmpty(storedValue)) popupStyle.normal.textColor = new Color(1f, 0.4f, 0.2f);

            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(displayText), FocusType.Keyboard, popupStyle))
            {
                GUIContent[] menuItems = _sectionPresetDisplayLabels.Select(n => new GUIContent(n)).ToArray();

                int currentSelected = System.Array.IndexOf(_sectionPresetStoredValues, storedValue);

                EditorUtility.DisplayCustomMenu(buttonRect, menuItems, currentSelected, (userData, options, selected) =>
                {
                    SerializedProperty prop = userData as SerializedProperty;
                    prop.serializedObject.Update();
                    // Store the internal value (collectionKey/presetName), not the display label
                    prop.stringValue = _sectionPresetStoredValues[selected];
                    prop.serializedObject.ApplyModifiedProperties();
                },
                element);
            }
        }

        void OnSectionPresetAdded(ReorderableList list)
        {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.InsertArrayElementAtIndex(index);
            SerializedProperty newElement = list.serializedProperty.GetArrayElementAtIndex(index);
            newElement.stringValue = "";
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Blueprint blueprint = target as Blueprint;

            // === Header ===
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Material Blueprint", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Scriptable Object Blueprint that generates a fresh Material with a predetermined configuration automatically applied in advance.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(4);

            // === Target Shader ===
            EditorGUILayout.LabelField("Shader", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _selectedShaderIndex = EditorGUILayout.Popup(new GUIContent("Target Shader", "Due to the feature set used by this script, ONLY shaders that use ThryEditor (such as Poiyomi Shaders) are eligible to be selected."), _selectedShaderIndex, _shaderNames);
            if (EditorGUI.EndChangeCheck() && _selectedShaderIndex >= 0 && _selectedShaderIndex < _shaderNames.Length)
            {
                Shader found = Shader.Find(_shaderNames[_selectedShaderIndex]);
                _targetShaderProp.objectReferenceValue = found;
                RefreshAvailablePresets();
            }

            EditorGUILayout.Space(10);

            // === Rendering Preset ===
            EditorGUILayout.LabelField("Rendering Preset", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Select which Rendering Preset you wish to be applied when the Material is generated from this Blueprint. To skip this step, set it to 'Skip (Don't Set)'.", MessageType.Info);

            float currentValue = _renderingPresetValueProp.floatValue;
            int dropdownIndex = RenderingValueToDropdownIndex(currentValue);
            
            EditorGUI.BeginChangeCheck();
            int newDropdownIndex = EditorGUILayout.Popup("Rendering Mode", dropdownIndex, s_renderingModeLabels);
            if (EditorGUI.EndChangeCheck())
            {
                if (newDropdownIndex == s_renderingModeLabels.Length - 1)
                {
                    _useCustomRenderingValue = true;
                }
                else
                {
                    _renderingPresetValueProp.floatValue = s_dropdownIndexToModeValue[newDropdownIndex];
                    _useCustomRenderingValue = false;
                }
            }

            if (_useCustomRenderingValue) EditorGUILayout.PropertyField(_renderingPresetValueProp, new GUIContent("Custom _Mode Value"));

            if (_targetShaderProp.objectReferenceValue != null)
            {
                Shader s = _targetShaderProp.objectReferenceValue as Shader;
                if (s != null && s.FindPropertyIndex(ShaderEditor.PROPERTY_NAME_IN_SHADER_PRESETS) < 0) EditorGUILayout.HelpBox($"The selected shader does not have a '{ShaderEditor.PROPERTY_NAME_IN_SHADER_PRESETS}' property. " + "The Rendering Preset value will be ignored.", MessageType.Warning);
            }

            EditorGUILayout.Space(20);

            // === Full Preset List ===
            EditorGUILayout.LabelField("Full Presets", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("When a Material is generated from this Blueprint, Presets will be applied in descending order (Top to Bottom).", MessageType.Info);

            // Refresh Button + Rebuild Cache Button + Count
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{_availablePresetNames.Length} full presets available", EditorStyles.miniLabel);
            if (GUILayout.Button("Refresh List", EditorStyles.miniButton, GUILayout.Width(85))) RefreshAvailablePresets();
            if (GUILayout.Button("Rebuild Cache", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                Presets.RebuildCache();
                RefreshAvailablePresets();
            }
            EditorGUILayout.EndHorizontal();

            _presetList.DoLayoutList();

            // Warn about missing entries
            bool hasMissing = false;
            for (int i = 0; i < _presetNamesProp.arraySize; i++)
            {
                string name = _presetNamesProp.GetArrayElementAtIndex(i).stringValue;
                if (!string.IsNullOrEmpty(name) && System.Array.IndexOf(_availablePresetNames, name) < 0)
                {
                    hasMissing = true;
                    break;
                }
            }
            if (hasMissing) EditorGUILayout.HelpBox("One or more presets in this list are marked [MISSING]. " + "They may have been renamed or deleted. Try clicking 'Refresh List' or update the entries.", MessageType.Warning);

            EditorGUILayout.Space(10);

            // === Section Preset List ===
            EditorGUILayout.LabelField("Sectioned Presets", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Section Presets apply to specific shader features (e.g. a single module like Emission or Outlines). They are applied after Full Presets, in descending order.", MessageType.Info);

            if (_targetShaderProp.objectReferenceValue == null) EditorGUILayout.HelpBox("Select a Target Shader to see section presets with friendly display names.", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{_sectionPresetDisplayLabels.Length} section presets available", EditorStyles.miniLabel);
            if (GUILayout.Button("Refresh List", EditorStyles.miniButton, GUILayout.Width(85))) RefreshAvailablePresets();
            if (GUILayout.Button("Rebuild Cache", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                Presets.RebuildCache();
                RefreshAvailablePresets();
            }
            EditorGUILayout.EndHorizontal();

            _sectionPresetList.DoLayoutList();

            // Warn about missing section entries
            bool hasMissingSections = false;
            for (int i = 0; i < _sectionPresetEntriesProp.arraySize; i++)
            {
                string val = _sectionPresetEntriesProp.GetArrayElementAtIndex(i).stringValue;
                if (!string.IsNullOrEmpty(val) && System.Array.IndexOf(_sectionPresetStoredValues, val) < 0)
                {
                    hasMissingSections = true;
                    break;
                }
            }

            if (hasMissingSections) EditorGUILayout.HelpBox("One or more section presets in this list are marked [MISSING]. " + "They may have been renamed or deleted. Try clicking 'Refresh List' or update the entries.", MessageType.Warning);

            EditorGUILayout.Space(12);

            // === Create Material Button ===
            bool canCreate = _targetShaderProp.objectReferenceValue != null;
            EditorGUI.BeginDisabledGroup(!canCreate);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.fixedHeight = 32;

            if (GUILayout.Button("Create Material from Blueprint", buttonStyle))
            {
                serializedObject.ApplyModifiedProperties();
                Blueprint.CreateMaterialFromBlueprintAsset(blueprint);
                GUIUtility.ExitGUI();
            }

            EditorGUI.EndDisabledGroup();

            if (!canCreate) EditorGUILayout.HelpBox("Assign a Target Shader to enable material creation.", MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }

        int RenderingValueToDropdownIndex(float value)
        {
            // Find which dropdown index corresponds to this _Mode value
            if (value < 0) return 0; // Skip

            int intVal = Mathf.RoundToInt(value);
            for (int i = 1; i < s_dropdownIndexToModeValue.Length - 1; i++)
            {
                if (Mathf.RoundToInt(s_dropdownIndexToModeValue[i]) == intVal) return i;
            }

            // Value doesn't match any known mode -> Custom
            _useCustomRenderingValue = true;
            return s_renderingModeLabels.Length - 1;
        }
    }
}