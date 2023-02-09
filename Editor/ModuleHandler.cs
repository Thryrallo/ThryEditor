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
        private static List<Module> first_party_modules;
        private static List<Module> third_party_modules;
        private static bool modules_are_being_loaded = false;

        private class ModuleCollection
        {
            public List<Module> first_party = null;
            public List<Module> third_party = null;
        }

        public static List<Module> GetFirstPartyModules()
        {
            if (!modules_are_being_loaded)
                LoadModules();
            return first_party_modules;
        }

        public static void ForceReloadModules()
        {
            LoadModules();
        }

        public static List<Module> GetThirdPartyModules()
        {
            if (!modules_are_being_loaded)
                LoadModules();
            return third_party_modules;
        }

        private static void LoadModules()
        {
            modules_are_being_loaded = true;
            var installedPackages = Client.List(true);
            WebHelper.DownloadStringASync(URL.MODULE_COLLECTION, (string s) => {
                first_party_modules = new List<Module>();
                third_party_modules = new List<Module>();
                ModuleCollection module_collection = Parser.Deserialize<ModuleCollection>(s);
                while(installedPackages.IsCompleted == false)
                    Thread.Sleep(100);
                foreach(Module m in module_collection.first_party)
                {
                    var package = installedPackages.Result.FirstOrDefault(p => p.name == m.packageId);
                    m.IsInstalled = package != null;
                    if(m.IsInstalled) m.HasUpdate = package.versions.all.Length > 0 && package.versions.latest != package.version;
                    Debug.Log(package.version);
                }
                foreach (Module m in module_collection.third_party)
                {
                    var package = installedPackages.Result.FirstOrDefault(p => p.name == m.packageId);
                    m.IsInstalled = package != null;
                    if(m.IsInstalled) m.HasUpdate = package.versions.all.Length > 0 && package.versions.latest != package.version;
                }
                first_party_modules = module_collection.first_party;
                third_party_modules = module_collection.third_party;
                UnityHelper.RepaintEditorWindow<Settings>();
            });
        }

        static List<(Request request, Module module, bool isInstall)> s_requests = new List<(Request request, Module module, bool isInstall)>();
        public static void InstallModule(Module module)
        {
            var request = Client.Add(module.git);
            module.IsInstalled = true;
            module.IsBeingModified = true;
            s_requests.Add((request, module, true));
            UnityHelper.RepaintEditorWindow<Settings>();
            EditorApplication.update += CheckRequests;
        }

        public static void RemoveModule(Module module)
        {
            var request = Client.Remove(module.packageId);
            module.IsInstalled = false;
            module.IsBeingModified = true;
            s_requests.Add((request, module, false));
            UnityHelper.RepaintEditorWindow<Settings>();
            EditorApplication.update += CheckRequests;
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