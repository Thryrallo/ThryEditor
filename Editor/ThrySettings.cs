using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ThrySettings : EditorWindow
{
    // Add menu named "My Window" to the Window menu
    [MenuItem("Thry/Editor Settings")]
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

        if (isFirstPopop)
            GUILayout.Label(" Please review your thry editor configuration", redInfostyle);
        else if (updatedVersion==-1)
            GUILayout.Label(" Thry editor has been updated", redInfostyle);
        else if (updatedVersion == 1)
            GUILayout.Label(" Warning: Thry editor version has declined", redInfostyle);

        GUILayout.Label("Editor", EditorStyles.boldLabel);

        if (Toggle(config.useBigTextures, "Big Texture Fields") != config.useBigTextures)
        {
            config.useBigTextures = !config.useBigTextures;
            config.save();
            ThryHelper.RepaintAllMaterialEditors();
        }

        GUILayout.BeginHorizontal();
        int newMaterialValuesUpdateRate = EditorGUILayout.IntField("",config.materialValuesUpdateRate,GUILayout.MaxWidth(50));
        GUILayout.Label("Slider Update Rate (in milliseconds)");
        GUILayout.EndHorizontal();
        if (newMaterialValuesUpdateRate != config.materialValuesUpdateRate)
        {
            config.materialValuesUpdateRate = newMaterialValuesUpdateRate;
            config.save();
            ThryEditor.reload();
            ThryHelper.RepaintAllMaterialEditors();
        }

        if (Toggle(config.useRenderQueueSelection, "Use Render Queue Selection") != config.useRenderQueueSelection)
        {
            config.useRenderQueueSelection = !config.useRenderQueueSelection;
            config.save();
            ThryHelper.RepaintAllMaterialEditors();
        }

        GUILayout.Label("Extras", EditorStyles.boldLabel);

        if (Toggle(config.showImportPopup, "Show popup on shader import") != config.showImportPopup)
        {
            config.showImportPopup = !config.showImportPopup;
            config.save();
            ThryHelper.RepaintAllMaterialEditors();
        }

        if (Toggle(config.isVrchatUser, "Use vrchat specific features (Auto Avatar Descriptor)") != config.isVrchatUser)
        {
            config.isVrchatUser = !config.isVrchatUser;
            config.save();
            ThryHelper.RepaintAllMaterialEditors();
        }
    }

    private static bool Toggle(bool val, string text)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(35);
        val = GUILayout.Toggle(val, "", GUILayout.ExpandWidth(false));
        GUILayout.Label(text);
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