using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor
{
    public class Presets
    {
        const string TAG_IS_PRESET = "isPreset";
        const string TAG_POSTFIX_IS_PRESET = "_isPreset";
        const string TAG_PRESET_NAME = "presetName";

        static string[] p_presetNames;
        static Material[] p_presetMaterials;
        static string[] presetNames { get
            {
                if (p_presetNames == null)
                {
                    p_presetMaterials = AssetDatabase.FindAssets("t:material")
                        .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                        .Where(m => IsPreset(m)).ToArray();
                    p_presetNames = p_presetMaterials.Select(m => m.GetTag(TAG_PRESET_NAME,false,m.name)).Prepend("Presets").ToArray();
                }
                return p_presetNames;
            }
        }

        public static void PresetGUI(ShaderEditor shaderEditor)
        {
            int i = EditorGUILayout.Popup(0, presetNames);
            if(i > 0)
            {
                Debug.Log("Apply Preset: " + presetNames[i]);
                Apply(p_presetMaterials[i - 1], shaderEditor);
            }
            if (shaderEditor._isPresetEditor)
            {
                string name = shaderEditor.materials[0].GetTag(TAG_PRESET_NAME, false, "");
                EditorGUI.BeginChangeCheck();
                name = EditorGUILayout.TextField("Preset Name", name);
                if(EditorGUI.EndChangeCheck())
                {
                    shaderEditor.materials[0].SetOverrideTag(TAG_PRESET_NAME, name);
                    p_presetNames = null;
                }
            }
        }

        static void Apply(Material preset, ShaderEditor shaderEditor)
        {
            foreach(ShaderPart prop in shaderEditor.shaderParts)
            {
                if (IsPreset(preset, prop.materialProperty))
                {
                    prop.CopyFromMaterial(preset);
                }
            }
        }

        public static void SetProperty(Material m, MaterialProperty prop, bool value)
        {
            m.SetOverrideTag(prop.name + TAG_POSTFIX_IS_PRESET, value?"true":"");
        }

        public static bool IsPreset(Material m, MaterialProperty prop)
        {
            if (prop == null) return false;
            return m.GetTag(prop.name + TAG_POSTFIX_IS_PRESET, false, "") == "true";
        }

        public static bool ArePreset(Material[] mats)
        {
            return mats.All(m => IsPreset(m));
        }

        public static bool IsPreset(Material m)
        {
            return m.GetTag(TAG_IS_PRESET, false, "false") == "true";
        }

        [MenuItem("Assets/Thry/Mark as preset")]
        static void MarkAsPreset()
        {
            IEnumerable<Material> mats = Selection.assetGUIDs.Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)));
            foreach (Material m in mats)
            {
                m.SetOverrideTag(TAG_IS_PRESET, "true");
                if (m.GetTag("presetName", false, "") == "") m.SetOverrideTag("presetName", m.name);
            }
            p_presetNames = null;
        }

        [MenuItem("Assets/Thry/Mark as preset", true)]
        static bool MarkAsPresetValid()
        {
            IEnumerable<Material> mats = Selection.assetGUIDs.Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)));
            return mats.Count() > 0 && mats.All(m => m.GetTag(TAG_IS_PRESET, false, "false") != "true");
        }

        [MenuItem("Assets/Thry/Remove as preset")]
        static void RemoveAsPreset()
        {
            IEnumerable<Material> mats = Selection.assetGUIDs.Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)));
            foreach (Material m in mats)
            {
                m.SetOverrideTag(TAG_IS_PRESET, "");
            }
            p_presetNames = null;
        }

        [MenuItem("Assets/Thry/Remove as preset", true)]
        static bool RemoveAsPresetValid()
        {
            IEnumerable<Material> mats = Selection.assetGUIDs.Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)));
            return mats.Count() > 0 && mats.All(m => m.GetTag(TAG_IS_PRESET, false, "false") == "true");
        }
    }
}