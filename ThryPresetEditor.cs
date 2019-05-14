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

                    bool typeFound = false;
                    ShaderUtil.ShaderPropertyType propertyType = ShaderUtil.ShaderPropertyType.Float;
                    for (int p = 0; p < ShaderUtil.GetPropertyCount(activeShader); p++)
                        if (ShaderUtil.GetPropertyName(activeShader, p) == properties[i][0])
                        {
                            propertyType = ShaderUtil.GetPropertyType(activeShader, p);
                            typeFound = true;
                            break;
                        }
                    if (typeFound)
                    {
                        switch (propertyType)
                        {
                            case ShaderUtil.ShaderPropertyType.Color:
                                float[] rgba = new float[4];
                                string[] rgbaString = properties[i][1].Split(',');
                                float.TryParse(rgbaString[0], out rgba[0]);
                                float.TryParse(rgbaString[1], out rgba[1]);
                                float.TryParse(rgbaString[2], out rgba[2]);
                                if (rgbaString.Length > 3) float.TryParse(rgbaString[3], out rgba[3]); else rgba[3] = 1;
                                Color p = EditorGUI.ColorField(EditorGUILayout.GetControlRect(GUILayout.MaxWidth(204)), new Color(rgba[0], rgba[1], rgba[2], rgba[3]));
                                properties[i][1] = "" + p.r + "," + p.g + "," + p.b + "," + p.a;
                                break;
                            case ShaderUtil.ShaderPropertyType.TexEnv:
                                string[] guids = AssetDatabase.FindAssets(properties[i][1]);
                                Texture texture = null;
                                if (guids.Length > 0)
                                texture = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guids[0]));
                                texture = (Texture)EditorGUI.ObjectField(EditorGUILayout.GetControlRect(GUILayout.MaxWidth(100)), texture, typeof(Texture));
                                if(texture!=null) properties[i][1] = texture.name;
                                GUILayout.Label("("+properties[i][1]+")",GUILayout.MaxWidth(100));
                                break;
                            case ShaderUtil.ShaderPropertyType.Vector:
                                string[] xyzw = properties[i][1].Split(",".ToCharArray());
                                Vector4 vector = new Vector4(float.Parse(xyzw[0]), float.Parse(xyzw[1]), float.Parse(xyzw[2]), float.Parse(xyzw[3]));
                                vector = EditorGUI.Vector4Field(EditorGUILayout.GetControlRect(GUILayout.MaxWidth(204)), "", vector);
                                properties[i][1] = "" + vector.x + "," + vector.y + "," + vector.z + "," + vector.w;
                                break;
                            default:
                                properties[i][1] = GUILayout.TextField(properties[i][1], GUILayout.MaxWidth(204));
                                break;
                        }
                    }else{
                        properties[i][1] = GUILayout.TextField(properties[i][1], GUILayout.MaxWidth(204));
                    }
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
                if(GUILayout.Button("Save",GUILayout.MinWidth(50))) saveProperties(presetHandler, presetStrings);
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
