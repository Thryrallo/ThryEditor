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

        drawLine();

        GUILayout.Label("Editor", EditorStyles.boldLabel);

        if (Toggle(config.useBigTextures, "Big Texture Fields", "Show big texure fields instead of small ones") != config.useBigTextures)
        {
            config.useBigTextures = !config.useBigTextures;
            config.save();
            ThryEditor.repaint();
        }

        GUILayout.BeginHorizontal();
        int newMaterialValuesUpdateRate = EditorGUILayout.IntField("",config.materialValuesUpdateRate,GUILayout.MaxWidth(50));
        GUILayout.Label(new GUIContent("Slider Update Rate (in milliseconds)", "change the update rate of float sliders to get a smoother editor experience"));
        GUILayout.EndHorizontal();
        if (newMaterialValuesUpdateRate != config.materialValuesUpdateRate)
        {
            config.materialValuesUpdateRate = newMaterialValuesUpdateRate;
            config.save();
            ThryEditor.reload();
            ThryEditor.repaint();
        }

        if (Toggle(config.useRenderQueueSelection, "Use Render Queue Selection" ,"enable a render queue selector that works with vrchat by creating seperate shaders for the different queues") != config.useRenderQueueSelection)
        {
            config.useRenderQueueSelection = !config.useRenderQueueSelection;
            config.save();
            ThryEditor.repaint();
        }

        drawLine();

        GUILayout.Label("Extras", EditorStyles.boldLabel);

        if (Toggle(config.showImportPopup, "Show popup on shader import", "This popup gives you the option to try to restore materials if they broke on importing") != config.showImportPopup)
        {
            config.showImportPopup = !config.showImportPopup;
            config.save();
            ThryEditor.repaint();
        }

        if (Toggle(config.isVrchatUser, "Use vrchat specific features", "Automatically setup the vrc_avatar_descriptor after adding it to a gameobject") != config.isVrchatUser)
        {
            config.isVrchatUser = !config.isVrchatUser;
            config.save();
            ThryEditor.repaint();
        }

        drawLine();
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