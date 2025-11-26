#if CVR_CCK_EXISTS
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using Thry.ThryEditor.Helpers;
using UnityEngine.Events;

namespace Thry.ThryEditor.UploadCallbacks
{
    public class AbiAutoLock
    {
        [InitializeOnLoad]
        public class CVR_UploadLocking
        {
            private static readonly Type CVRAvatarType;
            private static FieldInfo _cvrAvatarOverridesField;

            static CVR_UploadLocking()
            {

                try
                {

                    // CCK_BuildUtility.PreAvatarBundleEvent.AddListener(OnPreBundleEvent);
                    // CCK_BuildUtility.PrePropBundleEvent.AddListener(OnPreBundleEvent);
                    Assembly assemblyEditor = Assembly.Load("Assembly-CSharp-Editor");
                    Type type = assemblyEditor.GetType("ABI.CCK.Scripts.Editor.CCK_BuildUtility");
                    FieldInfo preAvatarBundleEvent = type.GetField("PreAvatarBundleEvent", BindingFlags.Static | BindingFlags.Public);
                    FieldInfo prePropBundleEvent = type.GetField("PrePropBundleEvent", BindingFlags.Static | BindingFlags.Public);
                    MethodInfo method = typeof(UnityEvent<GameObject>).GetMethod("AddListener");
                    MethodInfo methodOnBuild = typeof(CVR_UploadLocking).GetMethod(nameof(OnPreBundleEvent), BindingFlags.Static | BindingFlags.NonPublic);
                    UnityAction<GameObject> m = (UnityAction<GameObject>)Delegate.CreateDelegate(typeof(UnityAction<GameObject>), null, methodOnBuild!);
                    method!.Invoke(preAvatarBundleEvent!.GetValue(null), new object[]{m});
                    method.Invoke(prePropBundleEvent!.GetValue(null), new object[]{m});

                    Assembly assembly = Assembly.Load("Assembly-CSharp");
                    CVRAvatarType = assembly.GetType("ABI.CCK.Components.CVRAvatar");
                    _cvrAvatarOverridesField = CVRAvatarType.GetField("overrides", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }
                catch(Exception e)
                {
                    Debug.LogError("Failed to hook to ChilloutVR pre-build events, the material auto-lock won't work for ChilloutVR.");
                    Debug.LogException(e);
                }
            }

            private static void OnPreBundleEvent(GameObject uploadedObject)
            {
                List<Material> materials = new List<Material>();
                // CVRAvatar descriptor = uploadedObject.GetComponent<CVRAvatar>();
                Component descriptor = uploadedObject.GetComponent(CVRAvatarType);

                // All renderers
                materials.AddRange(uploadedObject.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials));

                // Find animation clips
                IEnumerable<AnimationClip> clips = uploadedObject.GetComponentsInChildren<Animator>(true).Where(a => a != null && a.runtimeAnimatorController != null).
                    Select(a => a.runtimeAnimatorController).SelectMany(a => a.animationClips);

                // if (descriptor != null && descriptor.overrides != null)
                //     clips = clips.Concat(descriptor.overrides.animationClips);
                if (descriptor != null)
                {
                    var overrideController = _cvrAvatarOverridesField.GetValue(descriptor) as AnimatorOverrideController;
                    if (overrideController != null)
                    {
                        clips = clips.Concat(overrideController.animationClips);
                    }
                }

                // Hanlde clips
                clips = clips.Distinct().Where(c => c != null);
                foreach (AnimationClip clip in clips)
                {
                    IEnumerable<Material> clipMaterials = AnimationUtility.GetObjectReferenceCurveBindings(clip).Where(b => b.isPPtrCurve && b.type.IsSubclassOf(typeof(Renderer)) && b.propertyName.StartsWith("m_Materials"))
                        .SelectMany(b => AnimationUtility.GetObjectReferenceCurve(clip, b)).Select(r => r.value as Material);
                    materials.AddRange(clipMaterials);
                }

                materials = materials.Distinct().ToList();
                ShaderOptimizer.LockMaterials(materials);
            }
        }
    }
}
#endif
