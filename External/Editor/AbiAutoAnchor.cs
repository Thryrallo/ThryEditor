#if CVR_CCK_EXISTS
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

namespace Thry.ThryEditor.UploadCallbacks // sry Pumkin for taking away your namespace. Just tring to tidy up a bit
{
    [InitializeOnLoad]
    public class AbiAutoAnchor
    {
        static AbiAutoAnchor()
        {
            try
            {
                // CCK_BuildUtility.PreAvatarBundleEvent.AddListener(OnPreBundleEvent);
                Type type = Assembly.Load("Assembly-CSharp-Editor").GetType("ABI.CCK.Scripts.Editor.CCK_BuildUtility");
                FieldInfo preAvatarBundleEvent = type.GetField("PreAvatarBundleEvent", BindingFlags.Static | BindingFlags.Public);
                MethodInfo method = typeof(UnityEvent<GameObject>).GetMethod("AddListener");
                MethodInfo methodOnBuild = typeof(AbiAutoAnchor).GetMethod(nameof(OnPreBundleEvent), BindingFlags.Static | BindingFlags.NonPublic);
                UnityAction<GameObject> m = (UnityAction<GameObject>)Delegate.CreateDelegate(typeof(UnityAction<GameObject>), null, methodOnBuild!);
                method!.Invoke(preAvatarBundleEvent!.GetValue(null), new object[]{m});
            }
            catch(Exception e)
            {
                Debug.LogError("Failed to hook to ChilloutVR pre-build events, the auto-anchor won't work for ChilloutVR.");
                Debug.LogException(e);
            }
        }

        private static void OnPreBundleEvent(GameObject uploadedObject)
        {
            try
            {
                if(!UploadAnchorOverrideSetter.ShouldSkipAvatar(uploadedObject))
                    UploadAnchorOverrideSetter.SetAnchorOverrides(uploadedObject);
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
#endif