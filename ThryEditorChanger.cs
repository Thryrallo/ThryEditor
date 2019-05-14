using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class ThryEditorChanger : EditorWindow
{
    // Add menu named "My Window" to the Window menu
    [MenuItem("Thry/Use Thry Editor for other shaders")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        ThryEditorChanger window = (ThryEditorChanger)EditorWindow.GetWindow(typeof(ThryEditorChanger));
        window.Show();
    }

    Vector2 scrollPos;

    bool[] setEditor;
    bool[] wasEditor;

    void OnGUI()
    {
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        string[] shaderGuids = AssetDatabase.FindAssets("t:shader");
        bool init = false;
        if (setEditor == null || setEditor.Length!=shaderGuids.Length)
        {
            setEditor = new bool[shaderGuids.Length];
            wasEditor = new bool[shaderGuids.Length];
            init = true;
        }
        for (int sguid = 0; sguid < shaderGuids.Length; sguid++)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(shaderGuids[sguid]));

            if (init)
            {
                setEditor[sguid] = (new Material(shader)).HasProperty("shader_is_using_thry_editor");
                wasEditor[sguid] = setEditor[sguid];
            }
            setEditor[sguid] = GUILayout.Toggle(setEditor[sguid], shader.name);

        }

        GUILayout.EndScrollView();

        if (GUILayout.Button("Apply"))
        {
            for (int sguid = 0; sguid < shaderGuids.Length; sguid++)
            {
                if (wasEditor[sguid] != setEditor[sguid])
                {
                    string path = AssetDatabase.GUIDToAssetPath(shaderGuids[sguid]);
                    if (setEditor[sguid]) addThryEditor(path);
                    else removeThryEditor(path);
                }

                wasEditor[sguid] = setEditor[sguid];
            }
            AssetDatabase.Refresh();
            ThryHelper.RepaintAllMaterialEditors();
        }
    }

    private void addThryEditor(string path)
    {
        replaceEditorInShader(path, "ThryEditor");
        addProperty(path, "[HideInInspector] shader_is_using_thry_editor(\"\", Float)", "0");
    }

    private void removeThryEditor(string path)
    {
        revertEditor(path);
        removeProperty(path, "[HideInInspector] shader_is_using_thry_editor(\"\", Float)", "0");
    }

    private void addProperty(string path, string property, string value)
    {
        string shaderCode = ThryHelper.readFileIntoString(path);
        string pattern = @"Properties.*\n?\s*{";
        RegexOptions options = RegexOptions.Multiline;
        shaderCode = Regex.Replace(shaderCode, pattern, "Properties \n  {"+" \n      "+ property + "=" + value, options);

        ThryHelper.writeStringToFile(shaderCode, path);
    }

    private void removeProperty(string path, string property, string value)
    {
        string shaderCode = ThryHelper.readFileIntoString(path);
        string pattern = @"\n.*"+Regex.Escape(property)+" ?= ?" + value;
        Debug.Log(pattern);
        RegexOptions options = RegexOptions.Multiline;
        foreach (Match m in Regex.Matches(shaderCode, pattern, options))
        {
            Debug.Log(m.Value+" found at index "+m.Index);
        }

        shaderCode = Regex.Replace(shaderCode, pattern, "", options);

        ThryHelper.writeStringToFile(shaderCode, path);
    }

    private void revertEditor(string path)
    {
        string shaderCode = ThryHelper.readFileIntoString(path);
        string pattern = @"//originalEditor.*\n";
        Match m = Regex.Match(shaderCode, pattern);
        if (m.Success)
        {
            string orignialEditor = m.Value.Replace("//originalEditor","");
            pattern = @"//originalEditor.*\n.*\n";
            shaderCode = Regex.Replace(shaderCode, pattern, orignialEditor);
            ThryHelper.writeStringToFile(shaderCode, path);
        }
    }

    private void replaceEditorInShader(string path, string newEditor)
    {
        string shaderCode = ThryHelper.readFileIntoString(path);
        string pattern = @"CustomEditor ?"".*""";
        Match m = Regex.Match(shaderCode, pattern);
        if (m.Success)
        {
            string oldEditor = "//originalEditor" + m.Value + "\n";
            shaderCode = Regex.Replace(shaderCode, pattern, oldEditor+"CustomEditor \"" + newEditor + "\"");
        }
        else
        {
            pattern = @"SubShader.*{";
            RegexOptions options = RegexOptions.Multiline | RegexOptions.Singleline;
            shaderCode = Regex.Replace(shaderCode, pattern, "CustomEditor \""+ newEditor + "\" \n    SubShader \n  {", options);
        }

        ThryHelper.writeStringToFile(shaderCode, path);
    }

}
