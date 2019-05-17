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

    public static Shader activeShader = null;
    public static Material activeShaderMaterial = null;
    public static ThryPresetHandler presetHandler = null;

    private bool isFirstPopop = false;

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
        GUILayout.Label("Config", EditorStyles.boldLabel);
        ThryConfig.Config config = ThryConfig.GetConfig();

        if (isFirstPopop)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.red;
            style.fontSize = 16;
            GUILayout.Label(" Please review your thry editor configuration", style);
        }

            if (GUILayout.Toggle(config.useBigTextures, "Big Texture Fields") != config.useBigTextures)
        {
            config.useBigTextures = !config.useBigTextures;
            config.save();
            ThryHelper.RepaintAllMaterialEditors();
        }

        if (GUILayout.Toggle(config.useRenderQueueSelection, "Use Render Queue Selection") != config.useRenderQueueSelection)
        {
            config.useRenderQueueSelection = !config.useRenderQueueSelection;
            config.save();
            ThryHelper.RepaintAllMaterialEditors();
        }
 
        if (GUILayout.Toggle(config.isVrchatUser, "Use vrchat specific features (Auto Avatar Descriptor)") != config.isVrchatUser)
        {
            config.isVrchatUser = !config.isVrchatUser;
            config.save();
            ThryHelper.RepaintAllMaterialEditors();
        }
    }

    public static ThrySettings getInstance()
    {
        ThrySettings instance = (ThrySettings)ThryHelper.FindEditorWindow(typeof(ThrySettings));
        if (instance == null) instance = ScriptableObject.CreateInstance<ThrySettings>();
        return instance;
    }
}