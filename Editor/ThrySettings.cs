using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        public new void Show()
        {
            base.Show();
        }

        public static Shader activeShader = null;
        public static Material activeShaderMaterial = null;
        public static PresetHandler presetHandler = null;

        private bool isFirstPopop = false;
        private int updatedVersion = 0;

        private static bool thry_vrc_tools_version_loaded = false;
        private static string thry_vrc_tools_version = "";
        private static string thry_vrc_tools_installed_version = "";
        private static string[] thry_vrc_tools_vrc_sdk_versions;
        private static string thry_vrc_tools_vrc_sdk_versions_string = "";
        private static bool has_vrc_tools = false;
        private bool is_installing_vrc_tools = false;

        public const string DEFINE_SYMBOLE_VRC_SDK_INSTALLED = "VRC_SDK_EXISTS";
        public const string DEFINE_SYMBOLE_API_NET_TWO = "NET_SET_TWO_POINT_ZERO";
        public const string DEFINE_SYMBOLE_MCS_EXISTS = "MCS_EXISTS";
        public const string DEFINE_SYMBOLE_VRC_SDK_VERRSION_SUPPORTED_BY_THRY_TOOLS = "SDK_VERRSION_SUPPORTED_BY_THRY_TOOLS";

        const string THRY_VRC_TOOLS_REPO_URL = "https://raw.githubusercontent.com/Thryrallo/ThryVRCTools/master/";
        const string THRY_VRC_TOOLS_FILE_LIST_URL = "file_list.txt";
        const string THRY_VRC_TOOLS_VERSION_URL = "version.txt";
        const string THRY_MCS_URL = "https://raw.githubusercontent.com/Thryrallo/ThryEditor/master/mcs.rsp";

        const string THRY_VRC_TOOLS_VERSION_PATH = "thry_vrc_tools_version";

        const string MCS_NEEDED_PATH = "Assets/mcs.rsp";

        private static string[][] SETTINGS_CONTENT = new string[][]
        {
        new string[]{ "Big Texture Fields", "Show big texure fields instead of small ones" },
        new string[]{ "Use Render Queue", "enable a render queue selector" },
        new string[]{ "Show popup on shader import", "This popup gives you the option to try to restore materials if they broke on importing" },
        new string[]{ "Render Queue Shaders", "Have the render queue selector work with vrchat by creating seperate shaders for the different queues" },
        new string[]{ "Auto setup avatar descriptor", "Automatically setup the vrc_avatar_descriptor after adding it to a gameobject" },
        new string[]{ " Fallback Default Animation Set", "is applied by auto avatar descriptor if gender of avatar couldn't be determend" },
        new string[]{ "Force Fallback Default Animation Set", "always set default animation set as fallback set" },
        new string[]{ "Gradient Save File Names", "configures the way gradient texture files are named. use <material>, <hash> and <prop> to identify the texture." }
        };
        enum SETTINGS_IDX
        {
            bigTexFields = 0, render_queue = 1, show_popup_on_import = 2, render_queue_shaders = 3, vrc_aad = 4, vrc_fallback_anim = 5,
            vrc_force_fallback_anim = 6, gradient_file_name = 7
        };

        //---------------------Stuff checkers and fixers-------------------

        //checks if slected shaders is using editor
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

        //to check if vrc sdk is being imported
        public class VRChatSdkImportTester : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                CheckVRCSDK(importedAssets);
            }
        }

        public void Awake()
        {
            CheckAPICompatibility(); //check that Net_2.0 is ApiLevel
            CheckMCS(); //check that MCS is imported
            SetupStyle(); //setup styles
            CheckVRCSDK();

            thry_vrc_tools_installed_version = Helper.FindFileAndReadIntoString(THRY_VRC_TOOLS_VERSION_PATH);
            has_vrc_tools = System.Type.GetType("Thry.AutoAvatarDescriptor") != null;
            Helper.getStringFromUrl(THRY_VRC_TOOLS_REPO_URL + THRY_VRC_TOOLS_VERSION_URL, thry_vrc_tools_version_callback);
        }

        private static void CheckVRCSDK()
        {
            Helper.SetDefineSymbol(DEFINE_SYMBOLE_VRC_SDK_INSTALLED, VRCInterface.Get().sdk_is_installed);
        }

        private static void CheckVRCSDK(string[] importedAssets)
        {
            bool vrcImported = false;
            foreach (string s in importedAssets) if (s.Contains("VRCSDK2.dll")) vrcImported = true;

            Helper.SetDefineSymbol(DEFINE_SYMBOLE_VRC_SDK_INSTALLED, VRCInterface.Get().sdk_is_installed | vrcImported);

            if (vrcImported)
            {
                if (thry_vrc_tools_version_loaded)
                    CheckVRCVersionCompatibilityCallback("");
                else
                    Helper.getStringFromUrl(THRY_VRC_TOOLS_REPO_URL + THRY_VRC_TOOLS_FILE_LIST_URL, thry_vrc_tools_version_callback);
            }
        }

        private static void CheckAPICompatibility()
        {
            if (PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Standalone) != ApiCompatibilityLevel.NET_2_0)
            {
                PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_2_0);
            }
            Helper.SetDefineSymbol(DEFINE_SYMBOLE_API_NET_TWO, true);
        }

        private static void CheckMCS()
        {
            int mcs_good = CheckMCSAvailability();
            if (mcs_good == 0)
                MoveMCS();
            else if (mcs_good == -1)
                GenerateMCS();
            mcs_good = CheckMCSAvailability();
            Helper.SetDefineSymbol(DEFINE_SYMBOLE_MCS_EXISTS, mcs_good == 1);
        }

        private static int CheckMCSAvailability()
        {
            bool mcs_wrong_path = false;
            foreach (string id in AssetDatabase.FindAssets("mcs"))
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                if (path.Contains(MCS_NEEDED_PATH))
                    return 1;
                else if (path.Contains("mcs.rsp"))
                    mcs_wrong_path = true;
            }
            if (mcs_wrong_path)
                return 0;
            return -1;
        }

        private static void MoveMCS()
        {
            foreach (string id in AssetDatabase.FindAssets("mcs"))
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                if (path.Contains("mcs.rsp"))
                    AssetDatabase.MoveAsset(path, MCS_NEEDED_PATH);
            }
            AssetDatabase.Refresh();
        }

        private static void GenerateMCS()
        {
            string mcs_data = "-r:System.Drawing.dll";
            Helper.WriteStringToFile(mcs_data, MCS_NEEDED_PATH);
            AssetDatabase.Refresh();
            CheckMCS();
        }

        //------------------Helpers----------------------------

        public static void setActiveShader(Shader shader)
        {
            if (shader != activeShader)
            {
                activeShader = shader;
                presetHandler = new PresetHandler(shader);
                activeShaderMaterial = new Material(shader);
            }
        }

        public static Settings getInstance()
        {
            Settings instance = (Settings)Helper.FindEditorWindow(typeof(Settings));
            if (instance == null) instance = ScriptableObject.CreateInstance<Settings>();
            return instance;
        }

        //---------------------------Callbacks

        public static void MCS_Download_Callback(string data)
        {
            CheckMCS();
        }

        public static void CheckVRCVersionCompatibilityCallback(string s)
        {
            bool supportedVersion = thry_vrc_tools_vrc_sdk_versions.Contains(VRCInterface.Get().installed_sdk_version);
            Debug.Log("Supports version: " + supportedVersion);
            Helper.SetDefineSymbol(DEFINE_SYMBOLE_VRC_SDK_VERRSION_SUPPORTED_BY_THRY_TOOLS, supportedVersion);
        }

        public static void thry_vrc_tools_version_callback(string s)
        {
            string[] data = Regex.Split(s, @"\r?\n");
            if (data.Length > 1)
            {
                thry_vrc_tools_version = data[0];
                thry_vrc_tools_vrc_sdk_versions_string = data[1];
                thry_vrc_tools_vrc_sdk_versions = Regex.Split(data[1], ",");
                thry_vrc_tools_version_loaded = true;
                Helper.RepaintEditorWindow(typeof(Settings));
                CheckVRCVersionCompatibilityCallback("");
            }
        }

        public static void thry_vrc_tools_file_list_callback(string s)
        {
            string[] fileNames = Regex.Split(s, @"\r?\n");
            string path = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("ThrySettings")[0]);
            path = Regex.Replace(path, @"/Editor/ThrySettings.cs", "/ThryTools/");
            foreach (string file in fileNames) Helper.downloadFileToPath(THRY_VRC_TOOLS_REPO_URL + file, path + file);
            Helper.WriteStringToFile(thry_vrc_tools_version, path + THRY_VRC_TOOLS_VERSION_PATH + ".txt");
            has_vrc_tools = true;
            thry_vrc_tools_installed_version = thry_vrc_tools_version;
            Helper.RepaintEditorWindow(typeof(Settings));
        }

        //------------------Main GUI
        void OnGUI()
        {
            if (!style_setup) SetupStyle();
            GUILayout.Label("ThryEditor v" + Config.Get().verion);

            GUINotification();
            drawLine();
            GUIVRC();
            GUIEditor();
            drawLine();
            GUIExtras();
            drawLine();
            GUIVRCTools();
        }

        //--------------------------GUI Helpers-----------------------------

        private static GUIStyle redInfostyle;
        private static GUIStyle redStyle;
        private static GUIStyle yellowStyle;
        private static GUIStyle greenStyle;
        private static bool style_setup = false;

        private static void SetupStyle()
        {
            redInfostyle = new GUIStyle();
            redInfostyle.normal.textColor = Color.red;
            redInfostyle.fontSize = 16;

            redStyle = new GUIStyle();
            redStyle.normal.textColor = Color.red;

            yellowStyle = new GUIStyle();
            yellowStyle.normal.textColor = Color.yellow;

            greenStyle = new GUIStyle();
            greenStyle.normal.textColor = new Color(0, 0.5f, 0);

            style_setup = true;
        }

        private static void drawLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private void GUINotification()
        {
            if (isFirstPopop)
                GUILayout.Label(" Please review your thry editor configuration", redInfostyle);
            else if (updatedVersion == -1)
                GUILayout.Label(" Thry editor has been updated", redInfostyle);
            else if (updatedVersion == 1)
                GUILayout.Label(" Warning: Thry editor version has declined", redInfostyle);
        }

        private static void GUIVRC()
        {
            if (VRCInterface.Get().sdk_is_installed)
            {
                GUILayout.Label("VRC Sdk version: " + VRCInterface.Get().installed_sdk_version);
                if (!VRCInterface.Get().sdk_is_up_to_date)
                {
                    GUILayout.Label("Newest VRC SDK version: " + VRCInterface.Get().newest_sdk_version);
                    if (GUILayout.Button("Update VRC SDK"))
                        VRCInterface.UpdateVRCSDK();
                }
                if (VRCInterface.Get().user_logged_in)
                {
                    GUILayout.Label("VRChat user: " + EditorPrefs.GetString("sdk#username"));
                }
                drawLine();
            }
        }

        private static void GUIEditor()
        {
            GUILayout.Label("Editor", EditorStyles.boldLabel);
            Toggle("useBigTextures", SETTINGS_CONTENT[(int)SETTINGS_IDX.bigTexFields]);
            Toggle("showRenderQueue", SETTINGS_CONTENT[(int)SETTINGS_IDX.render_queue]);
        }

        private static void GUIExtras()
        {
            Config config = Config.Get();
            GUILayout.Label("Extras", EditorStyles.boldLabel);

            Toggle("showImportPopup", SETTINGS_CONTENT[(int)SETTINGS_IDX.show_popup_on_import]);
            if (config.showRenderQueue)
                Toggle("renderQueueShaders", SETTINGS_CONTENT[(int)SETTINGS_IDX.render_queue_shaders]);

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            Text("gradient_name", SETTINGS_CONTENT[(int)SETTINGS_IDX.gradient_file_name], false);
            string gradient_name = config.gradient_name;
            if (gradient_name.Contains("<hash>"))
                GUILayout.Label("Good naming.", greenStyle, GUILayout.ExpandWidth(false));
            else if (gradient_name.Contains("<material>"))
                if (gradient_name.Contains("<prop>"))
                    GUILayout.Label("Good naming.", greenStyle, GUILayout.ExpandWidth(false));
                else
                    GUILayout.Label("Consider adding <hash> or <prop>.", yellowStyle, GUILayout.ExpandWidth(false));
            else if (gradient_name.Contains("<prop>"))
                GUILayout.Label("Consider adding <material>.", yellowStyle, GUILayout.ExpandWidth(false));
            else
                GUILayout.Label("Add <material> <hash> or <prop> to destingish between gradients.", redStyle, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        private void GUIVRCTools()
        {
            if (VRCInterface.Get().sdk_is_installed)
            {
                if (has_vrc_tools)
                    GUIVRCToolsOptions();

                drawLine();

                if (thry_vrc_tools_version_loaded)
                    GUIVRCToolsDownloadOptions(has_vrc_tools);
            }
        }

        private void GUIVRCToolsOptions()
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

        private void GUIVRCToolsDownloadOptions(bool tools_installed)
        {
            GUILayout.Label("Thry's VRC Tools Installer", EditorStyles.boldLabel);


            bool needsUpdate = false;
            if (thry_vrc_tools_version_loaded && has_vrc_tools)
                needsUpdate = Helper.compareVersions(thry_vrc_tools_version, thry_vrc_tools_installed_version) == -1;

            if (tools_installed && !needsUpdate)
                GUILayout.Label("Up to date");
            EditorGUI.BeginDisabledGroup(is_installing_vrc_tools || (tools_installed && !needsUpdate));
            GUILayout.BeginHorizontal();
            string text = "Install now";
            if (tools_installed) text = "Update";
            if (GUILayout.Button(text, GUILayout.ExpandWidth(false)))
            {
                is_installing_vrc_tools = true;
                Helper.getStringFromUrl(THRY_VRC_TOOLS_REPO_URL + THRY_VRC_TOOLS_FILE_LIST_URL, thry_vrc_tools_file_list_callback);
            }
            GUILayout.Label("(v" + thry_vrc_tools_version + ", vrc_sdk_versions: " + thry_vrc_tools_vrc_sdk_versions_string + ")", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            GUILayout.Label("Includes: ");
            GUILayout.Label(" - VRC Content Manager with search function, sorting function and tags for avatars");
            GUILayout.Label(" - VRC Auto Avatar descriptor: automatically fill out your avatar descriptor");
            EditorGUI.EndDisabledGroup();
        }

        private static void Text(string configField, string[] content)
        {
            Text(configField, content, true);
        }

        private static void Text(string configField, string[] content, bool createHorizontal)
        {
            Config config = Config.Get();
            System.Reflection.FieldInfo field = typeof(Config).GetField(configField);
            if (field != null)
            {
                string value = (string)field.GetValue(config);
                if (createHorizontal)
                    GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                GUILayout.Space(57);
                GUILayout.Label(new GUIContent(content[0], content[1]), GUILayout.ExpandWidth(false));
                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.DelayedTextField("", value, GUILayout.ExpandWidth(false));
                if (EditorGUI.EndChangeCheck())
                {
                    field.SetValue(config, value);
                    config.save();
                }
                if (createHorizontal)
                    GUILayout.EndHorizontal();
            }
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
    }
}