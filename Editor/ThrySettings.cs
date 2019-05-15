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

    ThryHelper.Config config;
    public static Shader activeShader = null;
    public static Material activeShaderMaterial = null;
    public static ThryPresetHandler presetHandler = null;

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
        activeShader = shader;
        presetHandler = new ThryPresetHandler(shader);
        activeShaderMaterial = new Material(shader);
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
        config = ThryHelper.GetConfig();

        if (GUILayout.Toggle(config.bigTextures, "Big Texture Fields") != config.bigTextures)
        {
            config.bigTextures = !config.bigTextures;
            config.save();
            ThryHelper.RepaintAllMaterialEditors();
        }

        if (GUILayout.Toggle(config.useRenderQueueSelection, "Use Render Queue Selection") != config.useRenderQueueSelection)
        {
            config.useRenderQueueSelection = !config.useRenderQueueSelection;
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