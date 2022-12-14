using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Thry{
    public class Localization : ScriptableObject
    {
        public Shader ValidateWithShader;
        public string DefaultLanguage = "English";
        public string[] Languages = new string[0];
        public int SelectedLanguage = 0;
        public Dictionary<string, string[]> LocalizedStrings = new Dictionary<string, string[]>();
        public Dictionary<string,string> DefaultKeyValues = new Dictionary<string,string>();
        string[] _loadedLanguages;

        // Use
        public static Localization Load(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Localization l = AssetDatabase.LoadAssetAtPath<Localization>(path);
            if(l == null) return ScriptableObject.CreateInstance<Localization>();
            l._loadedLanguages = new string[l.Languages.Length + 1];
            l._loadedLanguages[0] = l.DefaultLanguage;
            Array.Copy(l.Languages, 0, l._loadedLanguages, 1, l.Languages.Length);
            return l;
        }

        public void DrawDropdown()
        {
            SelectedLanguage = EditorGUILayout.Popup(SelectedLanguage, _loadedLanguages);
        }

        public string Get(MaterialProperty prop, string defaultValue)
        {
            if(LocalizedStrings.ContainsKey(prop.name))
            {
                string[] ar = LocalizedStrings[prop.name];
                if(ar.Length > SelectedLanguage)
                {
                    return ar[SelectedLanguage] ?? defaultValue;
                }
            }
            DefaultKeyValues[prop.name] = defaultValue;
            return defaultValue;
        }

        public string Get(MaterialProperty prop, FieldInfo field, string defaultValue)
        {
            string id = prop.name + "." + field.DeclaringType + "." + field.Name;
            if (LocalizedStrings.ContainsKey(id))
            {
                string[] ar = LocalizedStrings[id];
                if (ar.Length > SelectedLanguage)
                {
                    return ar[SelectedLanguage] ?? defaultValue;
                }
            }
            DefaultKeyValues[id] = defaultValue;
            return defaultValue;
        }

        // Managment

        void AddLanguage(string language)
        {
            if (System.Array.IndexOf(Languages, language) == -1)
            {
                System.Array.Resize(ref Languages, Languages.Length + 1);
                Languages[Languages.Length - 1] = language;
                foreach(string key in LocalizedStrings.Keys)
                {
                    string[] ar = LocalizedStrings[key];
                    System.Array.Resize(ref ar, ar.Length + 1);
                    ar[ar.Length - 1] = null;
                    LocalizedStrings[key] = ar;
                }
            }
        }

        void RemoveLanguage(string language)
        {
            int index = System.Array.IndexOf(Languages, language);
            if (index != -1)
            {
                for (int i = index; i < Languages.Length; i++)
                {
                    Languages[i] = Languages[i + 1];
                }
                System.Array.Resize(ref Languages, Languages.Length - 1);
                foreach (string key in LocalizedStrings.Keys)
                {
                    string[] ar = LocalizedStrings[key];
                    for (int i = index; i < ar.Length; i++)
                    {
                        ar[i] = ar[i + 1];
                    }
                    System.Array.Resize(ref ar, ar.Length - 1);
                    LocalizedStrings[key] = ar;
                }
            }
        }

        [MenuItem("Assets/Thry/Shaders/Create Locale File", false)]
        static void CreateLocale()
        {
            Localization locale = ScriptableObject.CreateInstance<Localization>();
            Shader shader = Selection.activeObject as Shader;
            string fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(shader));
            string folderPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(shader));
            locale.ValidateWithShader = shader;
            AssetDatabase.CreateAsset(locale, folderPath + "/" + fileName + "_Locale.asset");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Assets/Thry/Shaders/Create Locale File", true)]
        static bool ValidateCreateLocale()
        {
            return Selection.activeObject is Shader;
        }

        [MenuItem("Assets/Thry/Shaders/Locale Property", false)]
        static void CreateShaderProperty()
        {
            Localization l = Selection.activeObject as Localization;
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(l));
            string outS = $"[HideInInspector] {ShaderEditor.PROPERTY_NAME_LOCALE} (\"{guid}\", Float) = 0";
            EditorGUIUtility.systemCopyBuffer = outS;
        }

        [MenuItem("Assets/Thry/Shaders/Locale Property", true)]
        static bool ValidateCreateShaderProperty()
        {
            return Selection.activeObject is Localization;
        }

        [CustomEditor(typeof(Localization))]
        public class LocaleEditor : Editor
        {
            List<(string key, string defaultValue)> _missingKeys = new List<(string key, string defaultValue)>();
            int _selectedLanguageIndex = -1;
            string _search = "";
            public override void OnInspectorGUI()
            {
                Localization locale = (Localization)target;
                locale.ValidateWithShader = (Shader)EditorGUILayout.ObjectField("Validate With Shader", locale.ValidateWithShader, typeof(Shader), false);
                locale.DefaultLanguage = EditorGUILayout.TextField("Default Language", locale.DefaultLanguage);

                EditorGUILayout.LabelField("Languages");
                for (int i = 0; i < locale.Languages.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    locale.Languages[i] = EditorGUILayout.TextField(locale.Languages[i]);
                    if (GUILayout.Button("Remove"))
                    {
                        locale.RemoveLanguage(locale.Languages[i]);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add"))
                {
                    locale.AddLanguage("New Language");
                }
                EditorGUILayout.EndHorizontal();

                // popup for selecting language
                EditorGUI.BeginChangeCheck();
                _selectedLanguageIndex = EditorGUILayout.Popup("Language to edit", _selectedLanguageIndex, locale.Languages);
                if(EditorGUI.EndChangeCheck())
                {
                    _missingKeys.Clear();
                }

                if(GUILayout.Button("Update"))
                {
                    // add all keys from shader
                    foreach(var kv in locale.DefaultKeyValues)
                    {
                        if (string.IsNullOrEmpty(kv.Value) == false && !locale.LocalizedStrings.ContainsKey(kv.Key))
                        {
                            locale.LocalizedStrings.Add(kv.Key, new string[locale.Languages.Length]);
                        }
                    }
                    // make missing keys a list of all keys that have an empty string in the selected language
                    _missingKeys.Clear();
                    foreach(string key in locale.LocalizedStrings.Keys)
                    {
                        if (string.IsNullOrEmpty(locale.LocalizedStrings[key][_selectedLanguageIndex]))
                        {
                            _missingKeys.Add((key, locale.DefaultKeyValues[key]));
                        }
                    }
                }

                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Missing Entries", EditorStyles.boldLabel);
                int count = 0;
                (string,string) kvToRemove = default;
                foreach((string key, string defaultValue) kv in _missingKeys)
                {
                    if(count > 10)
                    {
                        EditorGUILayout.LabelField("...");
                        break;
                    }
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    string value = EditorGUILayout.DelayedTextField(kv.key, kv.defaultValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        if (!locale.LocalizedStrings.ContainsKey(kv.key))
                        {
                            locale.LocalizedStrings.Add(kv.key, new string[locale.Languages.Length]);
                        }
                        locale.LocalizedStrings[kv.key][_selectedLanguageIndex] = value;
                        kvToRemove = kv;
                    }
                    if(GUILayout.Button("Skip", GUILayout.Width(50)))
                    {
                        kvToRemove = kv;
                    }
                    EditorGUILayout.EndHorizontal();
                    count++;
                }
                if(kvToRemove != default)
                {
                    _missingKeys.Remove(kvToRemove);
                }

                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Existing Entries", EditorStyles.boldLabel);
                _search = EditorGUILayout.TextField("Search", _search);
                count = 0;
                foreach (string key in locale.LocalizedStrings.Keys)
                {
                    if (count > 10)
                    {
                        EditorGUILayout.LabelField("...");
                        break;
                    }
                    if (key.IndexOf(_search, StringComparison.OrdinalIgnoreCase) == -1)
                    {
                        continue;
                    }
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    string value = EditorGUILayout.DelayedTextField(key, locale.LocalizedStrings[key][_selectedLanguageIndex]);
                    if (EditorGUI.EndChangeCheck())
                    {
                        locale.LocalizedStrings[key][_selectedLanguageIndex] = value;
                    }
                    EditorGUILayout.EndHorizontal();
                    count++;
                }

            }
        }
    }
}

