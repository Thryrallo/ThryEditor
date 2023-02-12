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
        private static List<PackageInfo> first_party_packages;
        private static List<PackageInfo> third_party_packages;
        private static bool modules_are_being_loaded = false;

        private class PackageCollection
        {
            public List<PackageInfo> first_party = null;
            public List<PackageInfo> third_party = null;
        }

        public static List<PackageInfo> GetFirstPartyModules()
        {
            if (!modules_are_being_loaded)
                LoadPackages();
            return first_party_packages;
        }

        public static void ForceReloadModules()
        {
            LoadPackages();
        }

        public static List<PackageInfo> GetThirdPartyModules()
        {
            if (!modules_are_being_loaded)
                LoadPackages();
            return third_party_packages;
        }

        private static void LoadPackages()
        {
            modules_are_being_loaded = true;
            var installedPackages = Client.List(true);
            WebHelper.DownloadStringASync(URL.MODULE_COLLECTION, (string s) => {
                first_party_packages = new List<PackageInfo>();
                third_party_packages = new List<PackageInfo>();
                PackageCollection module_collection = Parser.Deserialize<PackageCollection>(s);
                while(installedPackages.IsCompleted == false)
                    Thread.Sleep(100);
                module_collection.first_party.ForEach(m => LoadInfoForPackage(m, installedPackages));
                module_collection.third_party.ForEach(m => LoadInfoForPackage(m, installedPackages));
                first_party_packages = module_collection.first_party;
                third_party_packages = module_collection.third_party;
                UnityHelper.RepaintEditorWindow<Settings>();
            });
        }

        static void LoadInfoForPackage(PackageInfo p, ListRequest installedPackages)
        {
            if(p.isUPM)
            {
                var package = installedPackages.Result.FirstOrDefault(pac => pac.name == p.packageId);
                p.IsInstalled = package != null;
                if (p.IsInstalled) p.HasUpdate = package.versions.all.Length > 0 && package.versions.latest != package.version;
            }else
            {
                p.IsInstalled = Helper.ClassWithNamespaceExists(p.classname);
            }
        }

        static List<(Request request, PackageInfo module, bool isInstall)> s_requests = new List<(Request request, PackageInfo module, bool isInstall)>();
        public static void InstallPackage(PackageInfo package)
        {
            if(package.isUPM)
            {
                var request = Client.Add(package.git +".git");
                package.IsInstalled = true;
                package.IsBeingModified = true;
                s_requests.Add((request, package, true));
                UnityHelper.RepaintEditorWindow<Settings>();
                EditorApplication.update += CheckRequests;
            }else
            {
                package.IsBeingModified = true;
                UnityHelper.RepaintEditorWindow<Settings>();
                var parts = package.git.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
                var repo = parts[parts.Length - 2] + "/" + parts[parts.Length - 1];
                InstallUnityPackageFromRelease(package, repo);
            }
            
        }

        static void InstallUnityPackageFromRelease(PackageInfo package, string repo)
        {
            WebHelper.DownloadStringASync($"https://api.github.com/repos/{repo}/releases/latest", (string s) =>
            {
                try
                {
                    Dictionary<object, object> dict = (Dictionary<object, object>)Parser.ParseJson(s);
                    List<object> assets = (List<object>)dict["assets"];
                    foreach(var asset in assets)
                    {
                        Dictionary<object, object> assetDict = (Dictionary<object, object>)asset;
                        if(assetDict.ContainsKey("browser_download_url") && assetDict["browser_download_url"].ToString().EndsWith(".unitypackage"))
                        {
                            var url = assetDict["browser_download_url"].ToString();
                            var filenname = url.Substring(url.LastIndexOf("/") + 1);
                            var path = Path.Combine(Application.temporaryCachePath, filenname);
                            Debug.Log("Downloading " + url);
                            WebHelper.DownloadFileASync(url, path, (string path2) =>
                            {
                                AssetDatabase.ImportPackage(path2, false);
                                package.IsInstalled = true;
                                package.IsBeingModified = false;
                                UnityHelper.RepaintEditorWindow<Settings>();
                            });
                            return;
                        }
                    }
                    
                }catch(Exception e)
                {
                    Debug.LogError("Error while downloading latest release of " + package.git + ".");
                    Debug.LogError(e);
                }
            });
        }

        public static void RemovePackage(PackageInfo module)
        {
            if(module.isUPM)
            {
                var request = Client.Remove(module.packageId);
                module.IsInstalled = false;
                module.IsBeingModified = true;
                s_requests.Add((request, module, false));
                UnityHelper.RepaintEditorWindow<Settings>();
                EditorApplication.update += CheckRequests;
            }else
            {
                EditorUtility.DisplayDialog("Cannot remove module", "This module was installed as a unitypackage. Please remove it manually.", "Ok");
            }
        }

        static void CheckRequests()
        {
            for (int i = 0; i < s_requests.Count; i++)
            {
                var request = s_requests[i];
                if (request.request.IsCompleted)
                {
                    if (request.request.Status == StatusCode.Success)
                    {
                        request.module.IsBeingModified = false;
                        s_requests.RemoveAt(i);
                        i--;
                    }
                    else if (request.request.Status >= StatusCode.Failure)
                    {
                        Debug.LogError(request.request.Error);
                        request.module.IsBeingModified = false;
                        request.module.IsInstalled = !request.isInstall;
                        s_requests.RemoveAt(i);
                        i--;
                    }
                    UnityHelper.RepaintEditorWindow<Settings>();
                }
            }
            if (s_requests.Count == 0)
                EditorApplication.update -= CheckRequests;
        }
    }

}