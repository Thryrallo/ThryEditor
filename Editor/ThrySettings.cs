using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ThrySettings : EditorWindow
{
    // Add menu named "My Window" to the Window menu
    [MenuItem("Thry/Settings")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        ThrySettings window = (ThrySettings)EditorWindow.GetWindow(typeof(ThrySettings));
        window.Show();
    }

    public static void firstTimePopup()
    {
        ThrySettings window = (ThrySettings)EditorWindow.GetWindow(typeof(ThrySettings));
        window.isFirstPopop = true;
        window.Show();
    }

    public static void updatedPopup(int compare)
    {
        ThrySettings window = (ThrySettings)EditorWindow.GetWindow(typeof(ThrySettings));
        window.updatedVersion = compare;
        window.Show();
    }

    public static Shader activeShader = null;
    public static Material activeShaderMaterial = null;
    public static ThryPresetHandler presetHandler = null;

    private bool isFirstPopop = false;
    private int updatedVersion = 0;

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
    enum SETTINGS_IDX { bigTexFields = 0, render_queue=1,show_popup_on_import=2,render_queue_shaders=3,vrc_aad=4, vrc_fallback_anim=5,
        vrc_force_fallback_anim=6 };

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
            presetHandler = new ThryPresetHandler(shader);
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
        GUILayout.Label("ThryEditor v" + ThryConfig.VERSION);
        ThryConfig.Config config = ThryConfig.GetConfig();

        GUIStyle redInfostyle = new GUIStyle();
        redInfostyle.normal.textColor = Color.red;
        redInfostyle.fontSize = 16;

        GUIStyle normal = new GUIStyle();

        if (isFirstPopop)
            GUILayout.Label(" Please review your thry editor configuration", redInfostyle);
        else if (updatedVersion==-1)
            GUILayout.Label(" Thry editor has been updated", redInfostyle);
        else if (updatedVersion == 1)
            GUILayout.Label(" Warning: Thry editor version has declined", redInfostyle);

        drawLine();

        bool hasVRCSdk = System.Type.GetType("VRC.AccountEditorWindow") != null;
        bool vrcIsLoggedIn = EditorPrefs.HasKey("sdk#username");

        if (hasVRCSdk)
        {
            //VRC.AccountEditorWindow window = (VRC.AccountEditorWindow)EditorWindow.GetWindow(typeof(VRC.AccountEditorWindow));
            //window.Show();

            EditorGUILayout.BeginHorizontal();

            if (vrcIsLoggedIn)
            {
                GUILayout.Label("VRChat user: " + EditorPrefs.GetString("sdk#username"));
            }
            EditorGUILayout.EndHorizontal();

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
            GUILayout.Label(new GUIContent(SETTINGS_CONTENT[(int)SETTINGS_IDX.vrc_fallback_anim][0], SETTINGS_CONTENT[(int)SETTINGS_IDX.vrc_fallback_anim][1]), normal);
            GUILayout.EndHorizontal();

            Toggle("vrchatForceFallbackAnimationSet", SETTINGS_CONTENT[(int)SETTINGS_IDX.vrc_force_fallback_anim]);

            drawLine();
        }
    }

    private static void Toggle(string configField, string[] content)
    {
        Toggle(configField, content[0], content[1]);
    }

    private static void Toggle(string configField,string label, string hover)
    {
        ThryConfig.Config config = ThryConfig.GetConfig();
        System.Reflection.FieldInfo field = typeof(ThryConfig.Config).GetField(configField);
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
        val = GUILayout.Toggle(val, new GUIContent("",tooltip), GUILayout.ExpandWidth(false));
        GUILayout.Label(new GUIContent(text,tooltip));
        GUILayout.EndHorizontal();
        return val;
    }

    public static ThrySettings getInstance()
    {
        ThrySettings instance = (ThrySettings)ThryHelper.FindEditorWindow(typeof(ThrySettings));
        if (instance == null) instance = ScriptableObject.CreateInstance<ThrySettings>();
        return instance;
    }
}