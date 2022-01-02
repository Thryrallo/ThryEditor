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

        static Dictionary<Material, (Material, Material)> appliedPresets = new Dictionary<Material, (Material, Material)>();

        static string[] p_presetNames;
        static Material[] p_presetMaterials;
        static string[] presetNames { get
            {
                if (p_presetNames == null)
                {
                    p_presetMaterials = AssetDatabase.FindAssets("t:material")
                        .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                        .Where(m => IsPreset(m)).ToArray();
                    p_presetNames = p_presetMaterials.Select(m => m.GetTag(TAG_PRESET_NAME,false,m.name)).Prepend("").ToArray();
                }
                return p_presetNames;
            }
        }

        public static void PresetGUI(Rect r, ShaderEditor shaderEditor)
        {
            int i = EditorGUI.Popup(r, 0, presetNames, Styles.icon_style_presets);
            if(i > 0)
            {
                Debug.Log("Apply Preset: " + presetNames[i]);
                Apply(p_presetMaterials[i - 1], shaderEditor);
            }
        }

        public static void PresetEditorGUI(ShaderEditor shaderEditor)
        {
            if (shaderEditor._isPresetEditor)
            {
                EditorGUILayout.LabelField(Locale.editor.Get("preset_material_notify"), Styles.greenStyle);
                string name = shaderEditor.materials[0].GetTag(TAG_PRESET_NAME, false, "");
                EditorGUI.BeginChangeCheck();
                name = EditorGUILayout.TextField(Locale.editor.Get("preset_name"), name);
                if (EditorGUI.EndChangeCheck())
                {
                    shaderEditor.materials[0].SetOverrideTag(TAG_PRESET_NAME, name);
                    p_presetNames = null;
                }
            }
            if (appliedPresets.ContainsKey(shaderEditor.materials[0]))
            {
                if(GUILayout.Button(Locale.editor.Get("preset_revert")+appliedPresets[shaderEditor.materials[0]].Item1.name))
                {
                    Revert(shaderEditor);
                }
            }
        }

        static void Apply(Material preset, ShaderEditor shaderEditor)
        {
            appliedPresets[shaderEditor.materials[0]] = (preset, new Material(shaderEditor.materials[0]));
            foreach (ShaderPart prop in shaderEditor.shaderParts)
            {
                if (IsPreset(preset, prop.materialProperty))
                {
                    prop.CopyFromMaterial(preset);
                }
            }
        }

        static void Revert(ShaderEditor shaderEditor)
        {
            Material key = shaderEditor.materials[0];
            Material preset = appliedPresets[key].Item1;
            Material prePreset = appliedPresets[key].Item2;
            foreach (ShaderPart prop in shaderEditor.shaderParts)
            {
                if (IsPreset(preset, prop.materialProperty))
                {
                    prop.CopyFromMaterial(prePreset);
                }
            }
            appliedPresets.Remove(key);
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