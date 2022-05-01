#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using System;
using System.Linq;
using Thry;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Pumkin.VRCCallbacks
{
    class UploadAnchorOverrideSetter : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => 0;
        
        string DialogTitle => Locale.editor.Get("autoAnchorDialog_Title");
        string DialogMessage => Locale.editor.Get("autoAnchorDialog_Text");
        string DialogYes => Locale.editor.Get("yes");
        string DialogNo => Locale.editor.Get("no");
        string ErrorNotHumanoid => Locale.editor.Get("autoAnchorError_NotHumanoid");
        
        bool Enabled
        {
            get => Config.Singleton.autoSetAnchorOverride;
            set => Config.Singleton.autoSetAnchorOverride = value;
        }

        bool AskedOnce
        {
            get => Config.Singleton.autoSetAnchorAskedOnce;
            set => Config.Singleton.autoSetAnchorAskedOnce = value;
        } 
        
        HumanBodyBones HumanBoneAnchor => Config.Singleton.humanBoneAnchor;
        string AnchorName => Config.Singleton.anchorOverrideObjectName;
        

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            try
            {
                if(!AskedOnce)
                {
                    Enabled = EditorUtility.DisplayDialog(DialogTitle, DialogMessage, DialogYes, DialogNo);
                    AskedOnce = true;
                    Config.Singleton.Save();
                }

                if(!Enabled)
                    return true;

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
                        return true;
                    }
                }

                var renderers = avatarGameObject.GetComponentsInChildren<Renderer>();
                foreach(var render in renderers)
                {
                    if(render.probeAnchor != null)
                        continue;

                    render.probeAnchor = anchorObject;
                }
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
            return true;
        }
    }
}
#endif