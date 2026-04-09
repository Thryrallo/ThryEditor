#if WARUDO_MOD_TOOLS
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UMod.ModTools.Export;
using UMod.Shared;

namespace Thry.ThryEditor.UploadCallbacks
{
    public static class WarudoAutoLock
    {
        [MenuItem("Warudo/Build Mod %#b", priority = 44)]
        internal static void BuildModWithAutoLock()
        {
            ExportSettings settings = ModScriptableAsset<ExportSettings>.Active.Load();
            if (settings == null)
            {
                throw new UMod.ModLoadException("The export settings are missing from this mod tools package");
            }

            List<Material> materials = CollectUnlockedMaterials(settings.ActiveExportProfile.ModAssetsPath);

            if (materials.Count > 0)
            {
                Debug.Log($"[WarudoAutoLock] Auto-locking {materials.Count} Poiyomi material(s) before build...");
                bool success = ShaderOptimizer.LockMaterials(materials, ShaderOptimizer.ProgressBar.Uncancellable);
                if (!success)
                {
                    Debug.LogWarning("[WarudoAutoLock] Some materials failed to lock. Proceeding with build anyway.");
                }
            }

            UMod.BuildEngine.ModToolsUtil.StartBuild(settings);
        }

        private static List<Material> CollectUnlockedMaterials(string modAssetsPath)
        {
            List<Material> materials = new List<Material>();

            // Convert absolute path to project-relative path
            string relativePath = modAssetsPath;
            string dataPath = Application.dataPath.Replace("\\", "/");
            string normalizedModPath = modAssetsPath.Replace("\\", "/");
            if (normalizedModPath.StartsWith(dataPath))
            {
                relativePath = "Assets" + normalizedModPath.Substring(dataPath.Length);
            }

            // Collect materials from Character.prefab renderers and animation clips
            string prefabPath = Path.Combine(relativePath, "Character.prefab").Replace("\\", "/");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null)
            {
                materials.AddRange(prefab.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(r => r.sharedMaterials));

                IEnumerable<AnimationClip> clips = prefab.GetComponentsInChildren<Animator>(true)
                    .Where(a => a != null && a.runtimeAnimatorController != null)
                    .Select(a => a.runtimeAnimatorController)
                    .SelectMany(a => a.animationClips)
                    .Distinct()
                    .Where(c => c != null);

                foreach (AnimationClip clip in clips)
                {
                    IEnumerable<Material> clipMaterials = AnimationUtility.GetObjectReferenceCurveBindings(clip)
                        .Where(b => b.isPPtrCurve && b.type.IsSubclassOf(typeof(Renderer)) && b.propertyName.StartsWith("m_Materials"))
                        .SelectMany(b => AnimationUtility.GetObjectReferenceCurve(clip, b))
                        .Select(r => r.value as Material);
                    materials.AddRange(clipMaterials);
                }
            }
            else
            {
                Debug.LogWarning($"[WarudoAutoLock] Character.prefab not found at '{prefabPath}'. Scanning mod folder for materials.");
            }

            // Also scan the mod folder for any materials not referenced by the prefab
            string[] materialGUIDs = AssetDatabase.FindAssets("t:Material", new[] { relativePath });
            foreach (string guid in materialGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                    materials.Add(mat);
            }

            // Deduplicate, filter to unlocked Poiyomi materials only
            return materials
                .Where(m => m != null)
                .Distinct()
                .Where(m => ShaderOptimizer.IsShaderUsingThryOptimizer(m.shader))
                .Where(m => !ShaderOptimizer.IsMaterialLocked(m))
                .ToList();
        }
    }
}
#endif
