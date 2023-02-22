// Material/Shader Inspector for Unity 2017/2018
// Copyright (C) 2019 Thryrallo

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using System.Threading;
using UnityEditor.PackageManager.Requests;

namespace Thry
{
    public abstract class ModuleSettings
    {
        public const string MODULES_CONFIG = "Thry/modules_config";

        public abstract void Draw();
    }

    public class ModuleHandler
    {
        const string VPM_FILE = "Packages/vpm-manifest.json";
        private static PackageCollection s_packageCollection = new PackageCollection();
        private static bool s_modulesAreBeingLoaded = false;
        private static int s_isVPMAvailable = -1;

        private class PackageCollection
        {
            public List<PackageInfo> first_party = new List<PackageInfo>();
            public List<PackageInfo> third_party_curated = new List<PackageInfo>();
            public List<PackageInfo> third_party_vrcprefabs = new List<PackageInfo>();
        }

        public static void ForceReloadModules()
        {
            LoadPackages();
        }

        public static List<PackageInfo> FirstPartyPackages
        {
            get
            {
                if (!s_modulesAreBeingLoaded)
                    LoadPackages();
                return s_packageCollection.first_party;
            }
        }

        public static List<PackageInfo> CuratedPackages
        {
            get
            {
                if (!s_modulesAreBeingLoaded)
                    LoadPackages();
                return s_packageCollection.third_party_curated;
            }
        }

        public static List<PackageInfo> VRCPrefabsPackages
        {
            get
            {
                if (!s_modulesAreBeingLoaded)
                    LoadPackages();
                return s_packageCollection.third_party_vrcprefabs;
            }
        }

        public static bool IsVPMAvailable
        {
            get
            {
                if (s_isVPMAvailable == -1)
                {
                    s_isVPMAvailable = File.Exists(VPM_FILE) ? 1 : 0;
                }
                return s_isVPMAvailable == 1;
            }
        }

        private static void LoadPackages()
        {
            s_modulesAreBeingLoaded = true;
            var installedPackages = Client.List(true);
            WebHelper.DownloadStringASync(URL.MODULE_COLLECTION, (string s) => {
                s_packageCollection = Parser.Deserialize<PackageCollection>(s);
                while(installedPackages.IsCompleted == false)
                    Thread.Sleep(100);
                s_packageCollection.first_party.ForEach(m => LoadInfoForPackage(m, installedPackages));
                s_packageCollection.third_party_curated.ForEach(m => LoadInfoForPackage(m, installedPackages));
                s_packageCollection.third_party_vrcprefabs.ForEach(m => LoadInfoForPackage(m, installedPackages));
                UnityHelper.RepaintEditorWindow<Settings>();
            });
        }

        static void LoadInfoForPackage(PackageInfo p, ListRequest installedPackages)
        {
            if(p.type != PackageType.UNITYPACKAGE)
            {
                var package = installedPackages.Result.FirstOrDefault(pac => pac.name == p.packageId);
                p.UnityPackageInfo = package;
                p.IsInstalled = package != null;
                if (p.IsInstalled) p.HasUpdate = package.versions.all.Length > 0 && package.versions.latest != package.version;
            }else
            {
                string path = AssetDatabase.GUIDToAssetPath(p.guid);
                p.IsInstalled = string.IsNullOrWhiteSpace(path) == false && (File.Exists(path) || Directory.Exists(path));
            }
        }

        enum RequestType
        {
            INSTALL,
            UNINSTALL,
            EMBED
        }

        struct UPMRequest
        {
            public RequestType Type;
            public Request Request;
            public PackageInfo Package;
        }

        static List<UPMRequest> s_requests = new List<UPMRequest>();
        static List<PackageInfo> s_packagesToEmbed = new List<PackageInfo>();
        public static void InstallPackage(PackageInfo package)
        {
            if(package.type != PackageType.UNITYPACKAGE && !package.upmInstallFromUnitypackage) InstallPackageInternal(package, package.git + ".git");
            else GetPackageUrlFromReleases(package, (string url) => InstallPackageInternal(package, url));
        }

        static void InstallPackageInternal(PackageInfo package, string url)
        {
            if(package.type != PackageType.UNITYPACKAGE && !package.upmInstallFromUnitypackage)
            {
                Debug.Log("[UPM] Downloading & Installing " + url);
                var request = Client.Add(url);
                package.IsInstalled = true;
                package.IsBeingModified = true;
                UPMRequest upmRequest = new UPMRequest();
                upmRequest.Type = RequestType.INSTALL;
                upmRequest.Request = request;
                upmRequest.Package = package;
                s_requests.Add(upmRequest);
                PlayerPrefs.SetString("ThryUPMEmbed", package.packageId);
                UnityHelper.RepaintEditorWindow<Settings>();
                EditorApplication.update += CheckRequests;
            }else
            {
                Debug.Log("[Unitypackage] Downloading & Installing " + url);
                package.IsBeingModified = true;
                UnityHelper.RepaintEditorWindow<Settings>();
                var filenname = url.Substring(url.LastIndexOf("/") + 1);
                var path = Path.Combine(Application.temporaryCachePath, filenname);
                WebHelper.DownloadFileASync(url, path, (string path2) =>
                {
                    AssetDatabase.ImportPackage(path2, false);
                    package.IsInstalled = true;
                    package.IsBeingModified = false;
                    UnityHelper.RepaintEditorWindow<Settings>();
                });
            }
        }

        static void GetPackageUrlFromReleases(PackageInfo package, Action<string> callback)
        {
            var parts = package.git.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
            var repo = parts[parts.Length - 2] + "/" + parts[parts.Length - 1];
            WebHelper.DownloadStringASync($"https://api.github.com/repos/{repo}/releases/latest", (string s) =>
            {
                try
                {
                    Dictionary<object, object> dict = (Dictionary<object, object>)Parser.ParseJson(s);
                    List<object> assets = (List<object>)dict["assets"];
                    bool useRegex = string.IsNullOrWhiteSpace(package.unitypackageRegex) == false;
                    foreach(var asset in assets)
                    {
                        Dictionary<object, object> assetDict = (Dictionary<object, object>)asset;
                        if(assetDict.ContainsKey("browser_download_url") && assetDict["browser_download_url"].ToString().EndsWith(".unitypackage") &&
                            (useRegex == false || Regex.Match(assetDict["browser_download_url"].ToString(), package.unitypackageRegex).Success))
                        {
                            var url = assetDict["browser_download_url"].ToString();
                            callback(url);
                        }
                    }
                    
                }catch(Exception e)
                {
                    Debug.LogError("Error while downloading latest release of " + package.git + ".");
                    Debug.LogError(e);
                }
            });
        }

        public static void RemovePackage(PackageInfo package)
        {
            if(package.type != PackageType.UNITYPACKAGE)
            {
                var request = Client.Remove(package.packageId);
                package.IsInstalled = false;
                package.IsBeingModified = true;
                UPMRequest upmRequest = new UPMRequest();
                upmRequest.Type = RequestType.UNINSTALL;
                upmRequest.Request = request;
                upmRequest.Package = package;
                UnityHelper.RepaintEditorWindow<Settings>();
                EditorApplication.update += CheckRequests;
                // Deleting Manually because Client.Remove does not work on embedded packages
                if(package.UnityPackageInfo.assetPath.StartsWith("Packages/"))
                {
                    DeleteUPMManually(package);
                    package.IsInstalled = false;
                    package.IsBeingModified = false;
                }
            }else
            {
                string path = AssetDatabase.GUIDToAssetPath(package.guid);
                if(EditorUtility.DisplayDialog("Remove module", "Do you want to delete the folder " + path + "?", "Yes", "No"))
                {
                    AssetDatabase.DeleteAsset(path);
                    package.IsInstalled = false;
                    package.IsBeingModified = false;
                    AssetDatabase.Refresh();
                    UnityHelper.RepaintEditorWindow<Settings>();
                }
            }
        }

        [InitializeOnLoadMethod]
        static void TryEmbeddingUPM()
        {
            string id = PlayerPrefs.GetString("ThryUPMEmbed", "");
            if(string.IsNullOrWhiteSpace(id) == false)
            {
                Client.Embed(id);
                PlayerPrefs.SetString("ThryUPMEmbed", "");
            }
        }

        static bool DeleteUPMManually(PackageInfo package)
        {
            if(package.UnityPackageInfo == null) return false;
            string path = package.UnityPackageInfo.assetPath;
            Debug.Log($"[Package] Deleting the package manually from {path}.");
            AssetDatabase.DeleteAsset(path);
            return true;
        }

        static void CheckRequests()
        {
            for (int i = 0; i < s_requests.Count; i++)
            {
                UPMRequest request = s_requests[i];
                if (request.Request.IsCompleted)
                {
                    if (request.Request.Status == StatusCode.Success)
                    {
                        request.Package.IsBeingModified = false;
                        s_requests.RemoveAt(i);
                        i--;
                        if(request.Type == RequestType.INSTALL)
                        {
                            Debug.Log("[Package] Installed '" + request.Package.packageId);
                        }
                        else if(request.Type == RequestType.UNINSTALL)
                        {
                            Debug.Log("[Package] Uninstalled '" + request.Package.packageId);
                        }
                        else if(request.Type == RequestType.EMBED)
                        {
                            Debug.Log("[Package] Embeded '" + request.Package.packageId);
                        }
                    }
                    else if (request.Request.Status >= StatusCode.Failure)
                    {
                        s_requests.RemoveAt(i);
                        i--;
                        // Try manually deleting the package
                        if(request.Type == RequestType.UNINSTALL && request.Package.UnityPackageInfo != null)
                        {
                            string path = request.Package.UnityPackageInfo.assetPath;
                            Debug.LogWarning($"[Package] UPM Removing failed, trying to delete the package manually from {path}.");
                            AssetDatabase.DeleteAsset(path);
                        }else if(request.Type == RequestType.INSTALL || request.Type == RequestType.UNINSTALL)
                        {
                            Debug.LogError(request.Request.Error);
                            request.Package.IsBeingModified = false;
                            request.Package.IsInstalled = request.Type != RequestType.INSTALL;
                        }
                    }
                    UnityHelper.RepaintEditorWindow<Settings>();
                }
            }
            if (s_requests.Count == 0)
                EditorApplication.update -= CheckRequests;
        }
    }

}