using System.Collections.Generic;
using System.IO;
using System.Linq;
using Thry.ThryEditor.Helpers;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.Rendering;

/// Material Blueprint Generator
/// Allows the creation of Materials with a predetermined set of instructions and presets pre-applied upon Material creation.

namespace Thry.ThryEditor
{
    public class Blueprint : ScriptableObject
    {
        const string TAG_POSTFIX_IS_PROPERTY_PRESET = "_isPreset";

        [Tooltip("The shader to assign to the new material.")]
        public Shader TargetShader;

        [Tooltip("The rendering preset value to apply (maps to the _Mode property). Set to -1 to skip.")]
        public float RenderingPresetValue = -1;

        [Tooltip("Ordered list of preset names to apply. Applied from top to bottom.")]
        public List<string> PresetNames = new List<string>();

        [Tooltip("Ordered list of section preset entries to apply. Each entry is stored as 'collectionKey/presetName'. Applied from top to bottom, after full presets.")]
        public List<string> SectionPresetEntries = new List<string>();

        public static bool TryParseSectionEntry(string entry, out string collectionKey, out string presetName)
        {
            collectionKey = null;
            presetName = null;
            if (string.IsNullOrEmpty(entry)) return false;
            int sep = entry.IndexOf('/');
            if (sep < 0 || sep == entry.Length - 1) return false;
            collectionKey = entry.Substring(0, sep);
            presetName = entry.Substring(sep + 1);
            return true;
        }

        public string CreateMaterial(string savePath)
        {
            if (TargetShader == null)
            {
                EditorUtility.DisplayDialog("Blueprint Error", "No Target Shader assigned to the Blueprint.", "OK");
                return null;
            }

            // Resolve Preset Names to Materials
            List<(string name, Material mat)> resolved = new List<(string, Material)>();
            foreach (string presetName in PresetNames)
            {
                if (string.IsNullOrEmpty(presetName)) continue;

                string guid = Presets.GetFullPresetGuid(presetName);
                if (guid == null)
                {
                    EditorUtility.DisplayDialog("Blueprint Error", $"Preset '{presetName}' was not found in the preset cache.\n\n" + "It may have been renamed or deleted. Please update the Blueprint.", "OK");
                    return null;
                }

                Material presetMat = Presets.GetPresetMaterial(guid);
                if (presetMat == null)
                {
                    EditorUtility.DisplayDialog("Blueprint Error", $"Could not load the material for preset '{presetName}'.", "OK");
                    return null;
                }

                resolved.Add((presetName, presetMat));
            }

            // Create the Material
            Material newMaterial = new Material(TargetShader);
            newMaterial.name = Path.GetFileNameWithoutExtension(savePath);

            // Apply Rendering Preset _Mode if specified
            // The on_value_actions are parsed from the shader's property attributes
            // and only execute through ThryEditor's ShaderProperty.FloatValue setter.
            // ShaderEditor.ApplyRenderingPresetToMaterial temporarily spins up a
            // ShaderEditor context so that the full side-effect chain runs.
            if (RenderingPresetValue >= 0) ShaderEditor.ApplyRenderingPresetToMaterial(newMaterial, RenderingPresetValue);

            // Apply each Preset in order
            foreach (var (name, presetMat) in resolved)
            {
                ApplyPresetToMaterial(presetMat, newMaterial);
            }

            // Resolve and apply Section Presets in order
            foreach (string entry in SectionPresetEntries)
            {
                if (!TryParseSectionEntry(entry, out string collectionKey, out string presetName)) continue;

                string guid = Presets.GetSectionPresetGuid(collectionKey, presetName);
                if (guid == null)
                {
                    EditorUtility.DisplayDialog("Blueprint Error", $"Section Preset '{presetName}' was not found.\n\n" + "It may have been renamed or deleted. Please update the Blueprint.", "OK");
                    return null;
                }

                Material sectionPresetMat = Presets.GetPresetMaterial(guid);
                if (sectionPresetMat == null)
                {
                    EditorUtility.DisplayDialog("Blueprint Error", $"Could not load the material for section preset '{presetName}'.", "OK");
                    return null;
                }

                ApplySectionPresetToMaterial(sectionPresetMat, newMaterial, collectionKey);
            }

            // Fix Keywords after all Presets are applied
            ShaderEditor.FixKeywords(new Material[] { newMaterial });

            // Final Pass of ApplyMaterialPropertyDrawers to ensure all drawers are consistent
            MaterialEditor.ApplyMaterialPropertyDrawers(newMaterial);

            // Save the Asset
            AssetDatabase.CreateAsset(newMaterial, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            return savePath;
        }

        static void ApplyPresetToMaterial(Material preset, Material target)
        {
            if (preset == null || target == null) return;

            // Temporarily swap the Preset's shader to the target's shader
            // so that Property Names align (same approach as Presets.ApplyPresetInternal)
            Shader prevPresetShader = preset.shader;
            preset.shader = target.shader;

            Shader shader = target.shader;
            int propCount = shader.GetPropertyCount();

            for (int i = 0; i < propCount; i++)
            {
                string propName = shader.GetPropertyName(i);

                // Check if this property is tagged as a preset property
                bool isPresetProp = preset.GetTag(propName + TAG_POSTFIX_IS_PROPERTY_PRESET, false, "") == "true";
                if (!isPresetProp) continue;

                // Copy the values from the preset to target based on property type
                ShaderPropertyType propType = shader.GetPropertyType(i);
                CopyProperty(preset, target, propName, propType);
                CopyAnimatedTag(preset, target, propName);
            }

            // Also check for header/section properties
            string[] serializedFloatProps = MaterialHelper.GetFloatPropertiesFromSerializedObject(preset);
            foreach (string propName in serializedFloatProps)
            {
                // Skip properties we already handled above
                if (shader.FindPropertyIndex(propName) >= 0) continue;

                bool isPresetProp = preset.GetTag(propName + TAG_POSTFIX_IS_PROPERTY_PRESET, false, "") == "true";
                if (!isPresetProp) continue;

                // Float and Header properties
                if (target.HasProperty(propName)) target.SetFloat(propName, preset.GetFloat(propName));

                // Copy the animation tags for Header/Section properties too, if supported.
                CopyAnimatedTag(preset, target, propName);
            }

            // Restore Preset's original shader
            preset.shader = prevPresetShader;
        }

        static void ApplySectionPresetToMaterial(Material preset, Material target, string collectionKey)
        {
            ApplyPresetToMaterial(preset, target);
        }

        static void CopyProperty(Material source, Material target, string propName, ShaderPropertyType propType)
        {
            if (!source.HasProperty(propName) || !target.HasProperty(propName)) return;

            switch (propType)
            {
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    target.SetFloat(propName, source.GetFloat(propName));
                    break;
                #if UNITY_2022_3_OR_NEWER
                case ShaderPropertyType.Int:
                    target.SetInt(propName, source.GetInt(propName));
                    break;
                #endif
                case ShaderPropertyType.Color:
                    target.SetColor(propName, source.GetColor(propName));
                    break;
                case ShaderPropertyType.Vector:
                    target.SetVector(propName, source.GetVector(propName));
                    break;
                case ShaderPropertyType.Texture:
                    target.SetTexture(propName, source.GetTexture(propName));
                    target.SetTextureOffset(propName, source.GetTextureOffset(propName));
                    target.SetTextureScale(propName, source.GetTextureScale(propName));
                    break;
            }
        }
        static void CopyAnimatedTag(Material source, Material target, string propName)
        {
            string animatedTag = source.GetTag(propName + ShaderOptimizer.AnimatedTagSuffix, false, "");
            if (!string.IsNullOrEmpty(animatedTag)) target.SetOverrideTag(propName + ShaderOptimizer.AnimatedTagSuffix, animatedTag);
        }

        #region Asset Creation Menu

        [MenuItem("Assets/Thry/Shaders/New Material Blueprint", priority = 381)]
        static void CreateNewBlueprint()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<DoCreateNewBlueprint>(), "New Material Blueprint.asset", EditorGUIUtility.IconContent("ScriptableObject Icon").image as Texture2D, null);
        }

        class DoCreateNewBlueprint : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var blueprint = CreateInstance<Blueprint>();
                blueprint.name = Path.GetFileNameWithoutExtension(pathName);
                AssetDatabase.CreateAsset(blueprint, pathName);
                Selection.activeObject = blueprint;
            }
        }

        [MenuItem("Assets/Thry/Shaders/Create Material from Blueprint", priority = 382)]
        static void CreateMaterialFromBlueprintMenu()
        {
            Blueprint blueprint = Selection.activeObject as Blueprint;
            if (blueprint == null)
            {
                EditorUtility.DisplayDialog("Blueprint Error", "Please select a Material Blueprint asset.", "OK");
                return;
            }
            CreateMaterialFromBlueprintAsset(blueprint);
        }

        [MenuItem("Assets/Thry/Shaders/Create Material from Blueprint", true)]
        static bool CreateMaterialFromBlueprintMenuValidation()
        {
            return Selection.activeObject is Blueprint;
        }

        public static Material CreateMaterialFromBlueprintAsset(Blueprint blueprint, string savePath = null)
        {
            if (blueprint == null) return null;

            if (string.IsNullOrEmpty(savePath))
            {
                string blueprintPath = AssetDatabase.GetAssetPath(blueprint);
                string defaultFolder = string.IsNullOrEmpty(blueprintPath) ? "Assets" : Path.GetDirectoryName(blueprintPath);
                string defaultName = blueprint.name + " Material";

                savePath = EditorUtility.SaveFilePanelInProject("Save new Material", defaultName, "mat", "Choose where to save the new material.", defaultFolder);

                if (string.IsNullOrEmpty(savePath)) return null;
            }

            string createdPath = blueprint.CreateMaterial(savePath);
            if (createdPath != null)
            {
                Material created = AssetDatabase.LoadAssetAtPath<Material>(createdPath);
                if (created != null)
                {
                    Selection.activeObject = created;
                    EditorGUIUtility.PingObject(created);
                }
                return created;
            }
            return null;
        }

        #endregion
    }
}