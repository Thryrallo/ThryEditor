using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ThryPresetEditor : EditorWindow
{
    // Add menu named "My Window" to the Window menu
    [MenuItem("Thry/Preset Editor")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        ThryPresetEditor window = (ThryPresetEditor)EditorWindow.GetWindow(typeof(ThryPresetEditor));
        window.Show();
    }

    public static void open()
    {
        ThryPresetEditor.Init();
    }

    private string[] shaders;
    private int selectedShaderIndex = 0;

    private bool newPreset = false;
    private string newPresetName;

    private void loadShaders()
    {
        string[] sguids = AssetDatabase.FindAssets("t:shader");
        List<Shader> shaders = new List<Shader>();
        foreach(string g in sguids)
        {
            Shader s = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(g));
            if (new Material(s).HasProperty("shader_presets")) shaders.Add(s);
            if (s == ThrySettings.activeShader) selectedShaderIndex = shaders.Count - 1;
        }
        this.shaders = new string[shaders.Count];
        Shader[] ar = shaders.ToArray();
        for (int i = 0; i < shaders.Count; i++) this.shaders[i] = ar[i].name;
    }

    private int selectedPreset = 0;
    Vector2 scrollPos;
    private List<string[]> properties = new List<string[]>();
    bool reloadProperties = true;

    void OnGUI()
    {
        if (shaders == null) loadShaders();
        Shader activeShader = ThrySettings.activeShader;
        int newIndex = EditorGUILayout.Popup(selectedShaderIndex, shaders, GUILayout.MaxWidth(500));
        if (newIndex != selectedShaderIndex)
        {
            selectedShaderIndex = newIndex;
            selectedPreset = 0;
            ThrySettings.setActiveShader(Shader.Find(shaders[selectedShaderIndex]));
            activeShader = ThrySettings.activeShader;
            reloadProperties = true;
        }
        if (activeShader != null)
        {
            ThryPresetHandler presetHandler = ThrySettings.presetHandler;
            if (presetHandler.shaderHasPresetPath())
            {
                Dictionary<string, List<string[]>> presets = presetHandler.getPresets();
                string[] presetStrings = new string[presets.Count+1];
                int i = 0;
                foreach (KeyValuePair<string, List<string[]>> entry in presets) presetStrings[i++] = entry.Key;
                presetStrings[presets.Count] = "<New Preset>";
                GUILayout.BeginHorizontal();
                int newSelectedPreset = EditorGUILayout.Popup(selectedPreset, presetStrings, GUILayout.MaxWidth(500));
                if (GUILayout.Button("Delete", GUILayout.MaxWidth(80)))
                {
                    presetHandler.removePreset(presetStrings[selectedPreset]);
                }
                GUILayout.EndHorizontal();
                if (newSelectedPreset != selectedPreset || reloadProperties)
                {
                    this.selectedPreset = newSelectedPreset;
                    if (newSelectedPreset == presetStrings.Length - 1)
                    {
                        newPreset = true;
                        newPresetName = "<name>";
                        this.properties.Clear();
                    }
                    else
                    {
                        this.properties = presetHandler.getPropertiesOfPreset(presetStrings[selectedPreset]);
                        this.properties.Add(new string[] { "new property", "new value" });
                        reloadProperties = false;
                        newPreset = false;
                    }
                }
                if (newPreset)
                {
                    GUILayout.BeginHorizontal();
                    newPresetName = GUILayout.TextField(newPresetName, GUILayout.MaxWidth(200));
                    if(GUILayout.Button("Add Preset",GUILayout.MaxWidth(80))){
                        presetHandler.addNewPreset(newPresetName);
                        reloadProperties = true;
                        Repaint();
                        selectedPreset = presetStrings.Length-1;
                    }
                    GUILayout.EndHorizontal();
                }
                scrollPos = GUILayout.BeginScrollView(scrollPos);
                for(i=0;i<properties.Count;i++)
                {
                    GUILayout.BeginHorizontal();
                    properties[i][0]=GUILayout.TextField(properties[i][0], GUILayout.MaxWidth(200));
                    properties[i][1]=GUILayout.TextField(properties[i][1], GUILayout.MaxWidth(200));
                    if (i < properties.Count - 1)
                    {
                        if (GUILayout.Button("Delete", GUILayout.MaxWidth(80)))
                        {
                            properties.RemoveAt(i);
                            saveProperties(presetHandler, presetStrings);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                Event e = Event.current;
                if (e.isKey) {
                    if (Event.current.keyCode == (KeyCode.Return))
                    {
                        saveProperties(presetHandler, presetStrings);
                    }
                }
            }
        }
    }

    private void saveProperties(ThryPresetHandler presetHandler, string[] presetStrings)
    {
        if (properties[properties.Count - 1][0] == "new property") properties.RemoveAt(properties.Count - 1);
        Debug.Log("Preset saved");
        presetHandler.setPreset(presetStrings[selectedPreset], properties);
        this.properties.Add(new string[] { "new property", "new value" });
        Repaint();
    }
}
