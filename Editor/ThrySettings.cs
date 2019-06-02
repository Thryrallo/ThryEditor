using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class Settings : EditorWindow
    {
        // Add menu named "My Window" to the Window menu
        [MenuItem("Thry/Settings")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            Settings window = (Settings)EditorWindow.GetWindow(typeof(Settings));
            window.Show();
        }

        public static void firstTimePopup()
        {
            Settings window = (Settings)EditorWindow.GetWindow(typeof(Settings));
            window.isFirstPopop = true;
            window.Show();
        }

        public static void updatedPopup(int compare)
        {
            Settings window = (Settings)EditorWindow.GetWindow(typeof(Settings));
            window.updatedVersion = compare;
            window.Show();
        }

        public static Shader activeShader = null;
        public static Material activeShaderMaterial = null;
        public static PresetHandler presetHandler = null;

        private bool isFirstPopop = false;
        private int updatedVersion = 0;

        private static bool firstLoad = true;
        private static bool thry_vrc_tools_version_loaded = false;
        private static string thry_vrc_tools_version = "";
        private static string thry_vrc_tools_vrc_sdk_version = "";
        private static string thry_vrc_tools_installed_version = "";
        private static bool has_vrc_tools = false;
        private bool is_installing_vrc_tools = false;

        const string THRY_VRC_TOOLS_REPO_URL = "https://raw.githubusercontent.com/Thryrallo/ThryVRCTools/master/";
        const string THRY_VRC_TOOLS_FILE_LIST_URL = "file_list.txt";
        const string THRY_VRC_TOOLS_VERSION_URL = "version.txt";

        const string THRY_VRC_TOOLS_VERSION_PATH = "thry_vrc_tools_version";

        private static string[][] SETTINGS_CONTENT = new string[][]
        {
        new string[]{ "Big Texture Fields", "Show big texure fields instead of small ones" },
        new string[]{ "Use Render Queue", "enable a render queue selector" },
        new string[]{ "Show popup on shader import", "This popup gives you the option to try to restore materials if they broke on importing" },
        new string[]{ "Render Queue Shaders", "Have the render queue selector work with vrchat by creating seperate shaders for the different queues" },
        new string[]{ "Auto setup avatar descriptor", "Automatically setup the vrc_avatar_descriptor after adding it to a gameobject" },
        new string[]{ " Fallback Default Animation Set", "is applied by auto avatar descriptor if gender of avatar couldn't be determend" },
        new string[]{ "Force Fallback Default Animation Set", "always set default animation set as fallback set" }
        };
        enum SETTINGS_IDX
        {
            bigTexFields = 0, render_queue = 1, show_popup_on_import = 2, render_queue_shaders = 3, vrc_aad = 4, vrc_fallback_anim = 5,
            vrc_force_fallback_anim = 6
        };

        private void OnSelectionChange()
        {
            string[] selectedAssets = Selection.assetGUIDs;
            if (selectedAssets.Length == 1)
            {
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(selectedAssets[0]));
                if (obj.GetType() == typeof(Shader))
                {
                    Shader shader = (Shader)obj;
                    Material m = new Material(shader);
                    if (m.HasProperty(Shader.PropertyToID("shader_is_using_thry_editor")))
                    {
                        setActiveShader(shader);
                    }
                }
            }
            this.Repaint();
        }

        public class VRChatSdkImportTester : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                bool vrcImported = false;
                foreach (string s in importedAssets) if (s.Contains("VRCSDK2.dll")) vrcImported = true;

                bool hasVRCSdk = System.Type.GetType("VRC.AccountEditorWindow") != null;

                string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                    BuildTargetGroup.Standalone);
                if ((vrcImported | hasVRCSdk) && !symbols.Contains("VRC_SDK_EXISTS")) PlayerSettings.SetScriptingDefineSymbolsForGroup(
                              BuildTargetGroup.Standalone, symbols + ";VRC_SDK_EXISTS");
                else if (!hasVRCSdk && symbols.Contains("VRC_SDK_EXISTS")) PlayerSettings.SetScriptingDefineSymbolsForGroup(
                     BuildTargetGroup.Standalone, symbols.Replace(";VRC_SDK_EXISTS", ""));
            }
        }

        public static void setActiveShader(Shader shader)
        {
            if (shader != activeShader)
            {
                activeShader = shader;
                presetHandler = new PresetHandler(shader);
                activeShaderMaterial = new Material(shader);
            }
        }

        private void drawLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        void OnGUI()
        {
            Config config = Config.Get();
            GUILayout.Label("ThryEditor v" + config.verion);

            GUIStyle redInfostyle = new GUIStyle();
            redInfostyle.normal.textColor = Color.red;
            redInfostyle.fontSize = 16;

            if (isFirstPopop)
                GUILayout.Label(" Please review your thry editor configuration", redInfostyle);
            else if (updatedVersion == -1)
                GUILayout.Label(" Thry editor has been updated", redInfostyle);
            else if (updatedVersion == 1)
                GUILayout.Label(" Warning: Thry editor version has declined", redInfostyle);

            drawLine();

            bool hasVRCSdk = System.Type.GetType("VRC.AccountEditorWindow") != null;
            bool vrcIsLoggedIn = EditorPrefs.HasKey("sdk#username");

            if (hasVRCSdk)
            {
                GUILayout.Label("VRC Sdk version: " + Helper.GetCurrentVRCSDKVersion());

                if (vrcIsLoggedIn)
                {
                    GUILayout.Label("VRChat user: " + EditorPrefs.GetString("sdk#username"));
                }

                drawLine();
            }

            GUILayout.Label("Editor", EditorStyles.boldLabel);

            Toggle("useBigTextures", SETTINGS_CONTENT[(int)SETTINGS_IDX.bigTexFields]);
            Toggle("showRenderQueue", SETTINGS_CONTENT[(int)SETTINGS_IDX.render_queue]);

            drawLine();
            GUILayout.Label("Extras", EditorStyles.boldLabel);

            Toggle("showImportPopup", SETTINGS_CONTENT[(int)SETTINGS_IDX.show_popup_on_import]);
            if (config.showRenderQueue)
                Toggle("renderQueueShaders", SETTINGS_CONTENT[(int)SETTINGS_IDX.render_queue_shaders]);

            drawLine();

            if (hasVRCSdk)
            {
                if (firstLoad)
                {
                    thry_vrc_tools_installed_version = Helper.findFileAndReadIntoString(THRY_VRC_TOOLS_VERSION_PATH);
                    has_vrc_tools = System.Type.GetType("Thry.AutoAvatarDescriptor") != null;
                }
                if (has_vrc_tools)
                    DrawVRCToolsOptions();

                drawLine();

                if (thry_vrc_tools_version_loaded)
                    DrawVRCToolsDownloadOptions(has_vrc_tools);
            }

            if (firstLoad)
            {
                Helper.getStringFromUrl(THRY_VRC_TOOLS_REPO_URL+THRY_VRC_TOOLS_VERSION_URL, thry_vrc_tools_version_callback);
                firstLoad = false;
            }
        }

        public static void DrawVRCToolsOptions()
        {
            Config config = Config.Get();
            GUILayout.Label("VRChat features", EditorStyles.boldLabel);

            Toggle("vrchatAutoFillAvatarDescriptor", SETTINGS_CONTENT[(int)SETTINGS_IDX.vrc_aad]);

            string[] options = new string[] { "Male", "Female", "None" };
            GUILayout.BeginHorizontal();
            int newVRCFallbackAnimationSet = EditorGUILayout.Popup(config.vrchatDefaultAnimationSetFallback, options, GUILayout.MaxWidth(45));
            if (newVRCFallbackAnimationSet != config.vrchatDefaultAnimationSetFallback)
            {
                config.vrchatDefaultAnimationSetFallback = newVRCFallbackAnimationSet;
                config.save();
            }
            GUILayout.Label(new GUIContent(SETTINGS_CONTENT[(int)SETTINGS_IDX.vrc_fallback_anim][0], SETTINGS_CONTENT[(int)SETTINGS_IDX.vrc_fallback_anim][1]));
            GUILayout.EndHorizontal();

            Toggle("vrchatForceFallbackAnimationSet", SETTINGS_CONTENT[(int)SETTINGS_IDX.vrc_force_fallback_anim]);
        }

        public void DrawVRCToolsDownloadOptions(bool tools_installed)
        {
            GUILayout.Label("Thry's VRC Tools Installer", EditorStyles.boldLabel);

            bool needsUpdate = false;
            if (thry_vrc_tools_version_loaded && has_vrc_tools)
                needsUpdate = Helper.compareVersions(thry_vrc_tools_version, thry_vrc_tools_installed_version) == -1;
            
            if(tools_installed && !needsUpdate)
                GUILayout.Label("Up to date");
            EditorGUI.BeginDisabledGroup(is_installing_vrc_tools || (tools_installed&&!needsUpdate));
            GUILayout.BeginHorizontal();
            string text = "Install now";
            if (tools_installed) text = "Update";
            if (GUILayout.Button(text, GUILayout.ExpandWidth(false)))
            {
                is_installing_vrc_tools = true;
                Helper.getStringFromUrl(THRY_VRC_TOOLS_REPO_URL + THRY_VRC_TOOLS_FILE_LIST_URL, thry_vrc_tools_file_list_callback);
            }
            GUILayout.Label("(v" + thry_vrc_tools_version + ", vrc_sdk_version: "+ thry_vrc_tools_vrc_sdk_version+")", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            GUILayout.Label("Includes: ");
            GUILayout.Label(" - VRC Content Manager with search function, sorting function and tags for avatars");
            GUILayout.Label(" - VRC Auto Avatar descriptor: automatically fill out your avatar descriptor");
            EditorGUI.EndDisabledGroup();
        }

        public static void thry_vrc_tools_version_callback(string s)
        {
            string[] data = Regex.Split(s, @"\r?\n");
            thry_vrc_tools_version = data[0];
            thry_vrc_tools_vrc_sdk_version = data[1];
            thry_vrc_tools_version_loaded = true;
            Helper.RepaintEditorWindow(typeof(Settings));
        }

        public static void thry_vrc_tools_file_list_callback(string s)
        {
            string[] fileNames = Regex.Split(s, @"\r?\n");
            string path = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("ThrySettings")[0]);
            path = Regex.Replace(path, @"/Editor/ThrySettings.cs", "/ThryTools/");
            foreach (string file in fileNames) Helper.downloadFileToPath(THRY_VRC_TOOLS_REPO_URL + file, path+file);
            Helper.writeStringToFile(thry_vrc_tools_version, path + THRY_VRC_TOOLS_VERSION_PATH + ".txt");
            has_vrc_tools = true;
            thry_vrc_tools_installed_version = thry_vrc_tools_version;
            Helper.RepaintEditorWindow(typeof(Settings));
        }

        private static void Toggle(string configField, string[] content)
        {
            Toggle(configField, content[0], content[1]);
        }

        private static void Toggle(string configField, string label, string hover)
        {
            Config config = Config.Get();
            System.Reflection.FieldInfo field = typeof(Config).GetField(configField);
            if (field != null)
            {
                bool value = (bool)field.GetValue(config);
                if (Toggle(value, label, hover) != value)
                {
                    field.SetValue(config, !value);
                    config.save();
                    ThryEditor.repaint();
                }
            }
        }

        private static bool Toggle(bool val, string text)
        {
            return Toggle(val, text, "");
        }

        private static bool Toggle(bool val, string text, string tooltip)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(35);
            val = GUILayout.Toggle(val, new GUIContent("", tooltip), GUILayout.ExpandWidth(false));
            GUILayout.Label(new GUIContent(text, tooltip));
            GUILayout.EndHorizontal();
            return val;
        }

        public static Settings getInstance()
        {
            Settings instance = (Settings)Helper.FindEditorWindow(typeof(Settings));
            if (instance == null) instance = ScriptableObject.CreateInstance<Settings>();
            return instance;
        }
    }
}