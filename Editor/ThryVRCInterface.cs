using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class VRCInterface
    {
        private const string TEMP_VRC_SDK_PACKAGE_PATH = "./vrc_sdk_package.unitypackage";

        private static VRCInterface instance;
        public static VRCInterface Get()
        {
            if (instance == null) instance = new VRCInterface();
            return instance;
        }
        public static void Update()
        {
            instance = new VRCInterface();
        }

        public bool sdk_is_installed;
        public bool sdk_is_up_to_date;
        public string installed_sdk_version;
        public string newest_sdk_version;

        public bool user_logged_in;

        public VRCInterface()
        {
            sdk_is_installed = IsVRCSDKInstalled();
            InitSDKVersionVariables();
            InitUserVariables();
        }

        private void InitSDKVersionVariables()
        {
            if (!sdk_is_installed)
                return;
            installed_sdk_version = GetInstalledSDKVersion();
            VRC.Core.RemoteConfig.Init(delegate ()
            {
                newest_sdk_version = GetNewestSDKVersion();
                sdk_is_up_to_date = SDKIsUpToDate();
                Debug.Log("new: " + newest_sdk_version);

                Debug.Log(sdk_is_up_to_date);
            });
        }

        private void InitUserVariables()
        {
            user_logged_in = EditorPrefs.HasKey("sdk#username");
        }
        
        private static string GetInstalledSDKVersion()
        {
            string[] guids = AssetDatabase.FindAssets("version");
            string path = null;
            foreach (string guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.Contains("VRCSDK/version"))
                    path = p;
            }
            if (path == null)
                return "";
            return Helper.ReadFileIntoString(path);
        }

        private static string GetNewestSDKVersion()
        {
#if VRC_SDK_EXISTS
            string version = VRC.Core.RemoteConfig.GetString("devSdkVersion");
            if(version!=null && version!="")
                return Regex.Match(version, @"[\d\.]+").Value;
#endif
            return "0";
        }

        private bool SDKIsUpToDate()
        {
            return Helper.compareVersions(installed_sdk_version, newest_sdk_version) != -1;
        }

        private static bool IsVRCSDKInstalled()
        {
            return System.Type.GetType("VRC.AccountEditorWindow") != null || System.Type.GetType("SDKUpdater") != null;
        }

        public static void UpdateVRCSDK()
        {
            string url = "https://vrchat.net/download/sdk";

            if (File.Exists(TEMP_VRC_SDK_PACKAGE_PATH))
            {
                Debug.Log(TEMP_VRC_SDK_PACKAGE_PATH + " exists");
                AssetDatabase.ImportPackage(TEMP_VRC_SDK_PACKAGE_PATH, false);
            }
            else
            {
                Helper.DownloadBytesToPath(url, TEMP_VRC_SDK_PACKAGE_PATH, VRCSDKUpdateCallback);
            }
        }

        public static void VRCSDKUpdateCallback(string data)
        {
            AssetDatabase.ImportPackage(TEMP_VRC_SDK_PACKAGE_PATH, false);
            File.Delete(TEMP_VRC_SDK_PACKAGE_PATH);
            Update();
        }
    }
}
