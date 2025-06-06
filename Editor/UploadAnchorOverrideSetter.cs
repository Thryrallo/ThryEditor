using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Thry.ThryEditor.UploadCallbacks // sry Pumkin for taking away your namespace. Just tring to tidy up a bit
{
    public static class UploadAnchorOverrideSetter // Cool Pumkin Stuff. No Thry stuff
    {
        const string SkipAvatarObjectName = "AutoAnchorDisabled";

        static readonly Type[] RendererTypesToCheck =
        {
            typeof(SkinnedMeshRenderer),
            typeof(MeshRenderer)
        };

        static string DialogTitle => EditorLocale.editor.Get("autoAnchorDialog_Title");
        static string DialogMessage => EditorLocale.editor.Get("autoAnchorDialog_Text");
        static string DialogYes => $"{EditorLocale.editor.Get("yes")} ({EditorLocale.editor.Get("recommended")})";
        static string DialogNo => EditorLocale.editor.Get("no");
        static string ErrorNotHumanoid => EditorLocale.editor.Get("autoAnchorError_NotHumanoid");

        static bool Enabled
        {
            get => Config.Instance.autoSetAnchorOverride;
            set => Config.Instance.autoSetAnchorOverride = value;
        }

        static bool AskedOnce
        {
            get => Config.Instance.autoSetAnchorAskedOnce;
            set => Config.Instance.autoSetAnchorAskedOnce = value;
        }

        static HumanBodyBones HumanBoneAnchor => Config.Instance.humanBoneAnchor;
        static string AnchorName => Config.Instance.anchorOverrideObjectName;

        public static bool ShouldSkipAvatar(GameObject avatar)
        {
            return avatar.GetComponentsInChildren<Transform>(true).Any(t => t.name == SkipAvatarObjectName);
        }

        public static void SetAnchorOverrides(GameObject avatarGameObject)
        {
            Renderer[] renderersWithNoAnchors = null;
            if(!AskedOnce) // If we haven't already asked, only display dialog once a renderer with no anchors is found
            {
                renderersWithNoAnchors = avatarGameObject.GetComponentsInChildren<Renderer>(true)?.Where(ShouldCheckRenderer).ToArray();

                if(renderersWithNoAnchors == null || renderersWithNoAnchors.Length == 0)
                    return;

                Enabled = EditorUtility.DisplayDialog(DialogTitle, DialogMessage, DialogYes, DialogNo);
                AskedOnce = true;
                Config.Instance.Save();
            }

            if(!Enabled)
                return;

            if(renderersWithNoAnchors == null)
                renderersWithNoAnchors = avatarGameObject.GetComponentsInChildren<Renderer>(true)?.Where(ShouldCheckRenderer).ToArray();

            if(renderersWithNoAnchors == null || renderersWithNoAnchors.Length == 0)
                return;

            Transform anchorObject = null;

            string anchorName = AnchorName;
            if(!string.IsNullOrEmpty(anchorName))
            {
                var children = avatarGameObject.GetComponentsInChildren<Transform>().Skip(1);
                anchorObject = children.FirstOrDefault(t => t.name.Equals(anchorName, StringComparison.OrdinalIgnoreCase));
            }

            if(!anchorObject)
            {
                var anim = avatarGameObject.GetComponent<Animator>();
                if(anim && anim.isHuman)
                    anchorObject = anim.GetBoneTransform(HumanBoneAnchor);
                else
                {
                    Debug.LogErrorFormat(ErrorNotHumanoid, avatarGameObject.name);
                }
            }

            anchorName = anchorObject != null ? anchorObject.name : "null";
            foreach(var render in renderersWithNoAnchors)
            {
                if(render.probeAnchor != null)
                    continue;

                render.probeAnchor = anchorObject;
                Debug.Log($"Thry: Setting Anchor Override for {render.name} to {anchorName}");
            }
        }

        static bool ShouldCheckRenderer(Renderer renderer)
        {
            if(renderer == null || !RendererTypesToCheck.Contains(renderer.GetType()))
                return false;
            if(renderer.reflectionProbeUsage == ReflectionProbeUsage.Off && renderer.lightProbeUsage == LightProbeUsage.Off)
                return false;
            return renderer.probeAnchor == null;
        }
    }
}
