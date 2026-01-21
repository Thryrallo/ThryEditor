using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Thry.ThryEditor.Helpers;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Thry.ThryEditor
{
    public class Localization : ScriptableObject
    {
        [SerializeField] Shader[] ValidateWithShaders;
        [SerializeField] string DefaultLanguage = "English";
        [SerializeField] string[] Languages = new string[0];
        [SerializeField] int SelectedLanguage = -1;
        [SerializeField] string[] _keys = new string[0];
        [SerializeField] string[] _values = new string[0];
        [SerializeField] string SpreadsheetCsvUrl;

        Dictionary<string, string[]> _localizedStrings = new Dictionary<string, string[]>();
        string[] _allLanguages;
        bool _isLoaded = false;
        bool _couldNotLoad = false;

        bool _editInUI = false;

        public bool EditInUI
        {
            get => _editInUI;
            set
            {
                if (_editInUI != value)
                {
                    _editInUI = value;
                    if (!value)
                    {
                        Save();
                    }
                }
            }
        }

        // Use
        public static Localization Load(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Localization l = AssetDatabase.LoadAssetAtPath<Localization>(path);
            if (l == null)
            {
                l = ScriptableObject.CreateInstance<Localization>();
                l._couldNotLoad = true;
                return l;
            }
            l.Load();
            return l;
        }

        void Load()
        {
            // Load languages
            int langCount = (Languages != null) ? Languages.Length : 0;
            _allLanguages = new string[Languages.Length + 1];
            _allLanguages[0] = DefaultLanguage;
            if (langCount > 0) Array.Copy(Languages, 0, _allLanguages, 1, Languages.Length);

            _localizedStrings = new Dictionary<string, string[]>();
            bool sawNullKey = false;

            int keyCount = (_keys != null) ? _keys.Length : 0;
            for (int i = 0; i < keyCount; i++)
            {
                string key = _keys[i];
                if (key == null)
                {
                    sawNullKey = true;
                    continue; // Skip invalid entry instead of throwing Null Exceptions
                }
                if (key.Length == 0) continue;

                string[] ar = new string[langCount];
                int srcIndex = i * langCount;

                if (_values != null && langCount > 0 && srcIndex < _values.Length)
                {
                    int copyLen = Math.Min(langCount, _values.Length - srcIndex);
                    Array.Copy(_values, srcIndex, ar, 0, copyLen);
                }

                _localizedStrings[_keys[i]] = ar;
            }

            if (sawNullKey)
            {
                string assetPath = AssetDatabase.GetAssetPath(this);
                ThryLogger.Log($"Localization Asset '{assetPath}' contains null entries in _keys. They were ignored to avoid Null Exception Errors. Re-save/Re-import the Locale Asset to clean it up.");
            }

            _isLoaded = true;
        }

        public static Localization Create()
        {
            Localization l = ScriptableObject.CreateInstance<Localization>();
            l._allLanguages = new string[l.Languages.Length + 1];
            l._allLanguages[0] = l.DefaultLanguage;
            Array.Copy(l.Languages, 0, l._allLanguages, 1, l.Languages.Length);
            l._localizedStrings = new Dictionary<string, string[]>();
            return l;
        }

        public void DrawDropdown(Rect r)
        {
            if (_couldNotLoad)
            {
                EditorGUI.HelpBox(r, "Could not load localization file", MessageType.Warning);
                return;
            }
            EditorGUI.BeginChangeCheck();
            SelectedLanguage = EditorGUI.Popup(r, SelectedLanguage + 1, _allLanguages) - 1;
            if (EditorGUI.EndChangeCheck())
            {
                ShaderEditor.Active.Reload();
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }

        public string Get(MaterialProperty prop, string defaultValue)
        {
            return Get(prop.name, defaultValue);
        }

        public string Get(MaterialProperty prop, FieldInfo field, string defaultValue)
        {
            string id = prop.name + "." + field.DeclaringType + "." + field.Name;
            return Get(id, defaultValue);
        }

        public string Get(string id, string defaultValue)
        {
            if (id == null) return defaultValue;
            if (_localizedStrings.TryGetValue(id, out string[] ar))
            {
                if (ar.Length > SelectedLanguage && SelectedLanguage > -1)
                {
                    // Treat empty translations as missing and fallback to the shader's English label.
                    string value = ar[SelectedLanguage];
                    if (string.IsNullOrEmpty(value)) return defaultValue;
                    return value;
                }
            }
            return defaultValue;
        }

        public void Set(MaterialProperty prop, string value)
        {
            Set(prop.name, value);
        }

        public void Set(MaterialProperty prop, FieldInfo field, string value)
        {
            string id = prop.name + "." + field.DeclaringType + "." + field.Name;
            Set(id, value);
        }

        public void Set(string id, string value)
        {
            if (string.IsNullOrEmpty(id))
            {
                // Avoid writing corrupted data as null keys cause ArgumentNullException during Load().
                ThryLogger.LogWarn("Tried to set a Localization entry with a null/empty id. Ignoring...");
                return;
            }
            
            if (!_localizedStrings.ContainsKey(id))
            {
                _localizedStrings.Add(id, new string[Languages.Length]);
            }
            // Normalize empty strings to null so they naturally fallback to English
            if (string.IsNullOrEmpty(value)) value = null;

            ThryLogger.LogDetail($"{Languages[SelectedLanguage]}[{id}] => {value}");
            _localizedStrings[id][SelectedLanguage] = value;
        }

        // Managment

        void AddLanguage(string language)
        {
            if (System.Array.IndexOf(Languages, language) == -1)
            {
                System.Array.Resize(ref Languages, Languages.Length + 1);
                Languages[Languages.Length - 1] = language;
                string[] keys = _localizedStrings.Keys.ToArray();
                foreach (string key in keys)
                {
                    string[] ar = _localizedStrings[key];
                    System.Array.Resize(ref ar, ar.Length + 1);
                    ar[ar.Length - 1] = null;
                    _localizedStrings[key] = ar;
                }
                Save();
            }
        }

        void RemoveLanguage(string language)
        {
            int index = System.Array.IndexOf(Languages, language);
            if (index != -1)
            {
                if (Languages.Length > 1)
                {
                    for (int i = index; i < Languages.Length - 1; i++)
                    {
                        Languages[i] = Languages[i + 1];
                    }
                    System.Array.Resize(ref Languages, Languages.Length - 1);
                    string[] keys = _localizedStrings.Keys.ToArray();
                    foreach (string key in keys)
                    {
                        string[] ar = _localizedStrings[key];
                        for (int i = index; i < ar.Length - 1; i++)
                        {
                            ar[i] = ar[i + 1];
                        }
                        System.Array.Resize(ref ar, ar.Length - 1);
                        _localizedStrings[key] = ar;
                    }
                }
                else
                {
                    Languages = new string[0];
                    _localizedStrings = new Dictionary<string, string[]>();
                }
                Save();
            }
        }

        public void Save()
        {
            _keys = _localizedStrings.Keys.ToArray();
            _values = new string[_keys.Length * Languages.Length];
            for (int i = 0; i < _keys.Length; i++)
            {
                string[] ar = _localizedStrings[_keys[i]];
                Array.Copy(ar, 0, _values, i * Languages.Length, ar.Length);
            }
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        void Clear()
        {
            _keys = new string[0];
            _values = new string[0];
            Languages = new string[0];
            _localizedStrings.Clear();
        }

        [MenuItem("Assets/Thry/Shaders/Create Locale File", false)]
        static void CreateLocale()
        {
            Localization locale = ScriptableObject.CreateInstance<Localization>();
            Shader[] shaders = Selection.objects.Select(o => o as Shader).ToArray();
            string fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(shaders[0]));
            string folderPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(shaders[0]));
            locale.ValidateWithShaders = shaders;
            AssetDatabase.CreateAsset(locale, folderPath + "/" + fileName + "_Locale.asset");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Assets/Thry/Shaders/Create Locale File", true)]
        static bool ValidateCreateLocale()
        {
            return Selection.objects.All(o => o is Shader);
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
            List<(string key, string defaultValue, string newValue)> _missingKeys = new List<(string key, string defaultValue, string newValue)>();
            Dictionary<string, string> _defaultPropertyContent = new Dictionary<string, string>();
            int _selectedLanguageIndex = 0;
            string _searchById = "";
            string _searchByTranslation = "";
            string[] _searchResults = new string[0];

            string _translateByValueIn = "";
            string _translateByValueOut = "";
            string _autoTranslateLanguageShortCode = "EN";

            UnityWebRequest _spreadsheetRequest;
            Localization _spreadsheetTarget;
            string _prevSpreadsheetUrl;

            string ToCSVString(string s)
            {
                if (s == null)
                    return "";
                return "\"" + s.Replace("\"", "“") + "\"";
            }

            string FromCSVString(string s)
            {
                return s.Trim('"').Replace("“", "\"");
            }

            static bool IsSourceHeader(string header)
            {
                if (string.IsNullOrWhiteSpace(header)) return false;
                header = header.Trim();
                return header.Equals("English", StringComparison.OrdinalIgnoreCase) || header.Equals("Source", StringComparison.OrdinalIgnoreCase) || header.Equals("Label", StringComparison.OrdinalIgnoreCase) || header.Equals("UI Label", StringComparison.OrdinalIgnoreCase) || header.Equals("Default", StringComparison.OrdinalIgnoreCase);
            }

            // Minimal CSV Line Parser that respects quotes.
            static List<string> SplitCsvLine(string line)
            {
                List<string> result = new List<string>();
                if (line == null) return result;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                bool inQuotes = false;
                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    if (c == '\"')
                    {
                        // Escaped quote ("" -> ")
                        if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                        {
                            sb.Append('\"');
                            i++;
                        }
                        else inQuotes = !inQuotes;
                    }
                    else if (c == ',' && !inQuotes)
                    {
                        result.Add(sb.ToString());
                        sb.Length = 0;
                    }
                    else sb.Append(c);
                }
                result.Add(sb.ToString());
                return result;
            }

            static void WriteLocaleBackupFile(Localization locale, string csvText)
            {
                if (locale == null) return;
                if (csvText == null) csvText = "";

                string assetPath = AssetDatabase.GetAssetPath(locale);
                if (string.IsNullOrEmpty(assetPath)) return;

                string folderRel = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(folderRel)) folderRel = "Assets";

                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string folderAbs = Path.Combine(projectRoot, folderRel);

                string backupAbs = Path.Combine(folderAbs, "Locale.csv");

                File.WriteAllText(backupAbs, csvText, new System.Text.UTF8Encoding(true));

                string backupRel = (folderRel + "/Locale.csv").Replace('\\', '/');
                AssetDatabase.ImportAsset(backupRel);
            }

            static void WriteLocaleTimestamp(Localization locale)
            {
                if (locale == null) return;

                string assetPath = AssetDatabase.GetAssetPath(locale);
                if (string.IsNullOrEmpty(assetPath)) return;

                string folderRel = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(folderRel)) folderRel = "Assets";

                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string folderAbs = Path.Combine(projectRoot, folderRel);
                string timestampAbs = Path.Combine(folderAbs, "LocaleTimestamp.txt");

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.WriteAllText(timestampAbs, timestamp, new System.Text.UTF8Encoding(true));

                string timestampRel = (folderRel + "/LocaleTimestamp.txt").Replace('\\', '/');
                AssetDatabase.ImportAsset(timestampRel);
            }

            static string ReadLocaleTimestamp(Localization locale)
            {
                if (locale == null) return null;

                string assetPath = AssetDatabase.GetAssetPath(locale);
                if (string.IsNullOrEmpty(assetPath)) return null;

                string folderRel = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(folderRel)) folderRel = "Assets";

                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string folderAbs = Path.Combine(projectRoot, folderRel);
                string timestampAbs = Path.Combine(folderAbs, "LocaleTimestamp.txt");

                if (!File.Exists(timestampAbs)) return null;

                try
                {
                    string txt = File.ReadAllText(timestampAbs, System.Text.Encoding.UTF8);
                    if (string.IsNullOrWhiteSpace(txt)) return null;
                    return txt.Trim();
                }
                catch
                {
                    return null;
                }
            }

            void LoadFromCSVText(Localization locale, string csvText)
            {
                if (string.IsNullOrEmpty(csvText) || locale == null) return;

                string[] lines = csvText
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n")
                    .Split(new[] { '\n' }, StringSplitOptions.None)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToArray();

                if (lines.Length == 0) return;

                locale.Clear();

                List<string> header = SplitCsvLine(lines[0]).Select(FromCSVString).ToList();
                if (header.Count < 2) return;

                int languagesStartIndex = (header.Count > 1 && IsSourceHeader(header[1])) ? 2 : 1;
                for (int i = languagesStartIndex; i < header.Count; i++)
                {
                    locale.AddLanguage(header[i]);
                }

                int languageCount = header.Count - languagesStartIndex;
                locale._values = new string[(lines.Length - 1) * languageCount];
                locale._keys = new string[lines.Length - 1];

                for (int i = 1; i < lines.Length; i++)
                {
                    List<string> cells = SplitCsvLine(lines[i]).Select(FromCSVString).ToList();
                    if (cells.Count == 0) continue;

                    string key = cells[0];
                    locale._keys[i - 1] = key;

                    for (int j = 0; j < languageCount; j++)
                    {
                        int cellIndex = languagesStartIndex + j;
                        string value = (cellIndex < cells.Count) ? cells[cellIndex] : "";
                        locale._values[(i - 1) * languageCount + j] = value;
                    }
                }

                locale.Load();
                locale.Save();
                WriteLocaleTimestamp(locale);
            }

            void ImportOnlineSpreadsheet(Localization locale)
            {
                if (locale == null) return;

                if (string.IsNullOrWhiteSpace(locale.SpreadsheetCsvUrl))
                {
                    EditorUtility.DisplayDialog(
                        "Spreadsheet URL Missing",
                        "Make sure this asset is pointing to the correct external CSV URL first before proceeding. If this data has been lost, you can find the URL on the Documentation at:\n'poiyomi.com/general/ui-language'",
                        "OK"
                    );
                    return;
                }

                _prevSpreadsheetUrl = locale.SpreadsheetCsvUrl;

                if (_spreadsheetRequest != null)
                {
                    EditorUtility.DisplayDialog(
                        "Sync Already Running",
                        "A spreadsheet sync is already in progress.",
                        "OK"
                    );
                    return;
                }

                _spreadsheetTarget = locale;
                _spreadsheetRequest = UnityWebRequest.Get(locale.SpreadsheetCsvUrl);
                _spreadsheetRequest.timeout = 30;

                var op = _spreadsheetRequest.SendWebRequest();
                op.completed += _ =>
                {
                    try
                    {
                        if (_spreadsheetRequest.result != UnityWebRequest.Result.Success)
                        {
                            Undo.RecordObject(locale, "Revert Spreadsheet CSV URL");
                            locale.SpreadsheetCsvUrl = _prevSpreadsheetUrl;
                            EditorUtility.SetDirty(locale);
                            AssetDatabase.SaveAssets();
                            ThryLogger.LogErr($"Spreadsheet CSV Download failed {_spreadsheetRequest.error} at URL: {locale.SpreadsheetCsvUrl}");
                            EditorUtility.DisplayDialog(
                                "Spreadsheet Sync Failed",
                                $"Could not download external CSV from: {_spreadsheetRequest.error}",
                                "OK"
                            );
                            return;
                        }

                        string csv = _spreadsheetRequest.downloadHandler.text;
                        WriteLocaleBackupFile(locale, csv);
                        LoadFromCSVText(locale, csv);
                        Undo.RecordObject(locale, "Commit Spreadsheet CSV URL");
                        EditorUtility.SetDirty(locale);
                        AssetDatabase.SaveAssets();
                        EditorUtility.DisplayDialog(
                            "Spreadsheet Sync Success",
                            "Successfully fetched and updated Localization from external URL.",
                            "OK"
                        );
                    }
                    finally
                    {
                        _spreadsheetRequest.Dispose();
                        _spreadsheetRequest = null;
                        _spreadsheetTarget = null;
                    }
                };
            }

            static bool ShouldIgnoreKey(string key)
            {
                if (string.IsNullOrEmpty(key)) return false;
                return key.StartsWith("s_end_", StringComparison.Ordinal) || key.StartsWith("m_end_", StringComparison.Ordinal) || key.StartsWith("ss_end_", StringComparison.Ordinal);
            }

            void ExportAsCSV(Localization locale)
            {
                // Ensure shader property labels are up to date for the "English" (source) column. Needed so that community contribution is easy!
                UpdateData(locale);

                string path = EditorUtility.SaveFilePanel("Export as CSV", "", locale.name, "csv");
                if (string.IsNullOrEmpty(path) == false)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();

                    // Header: Property, English (source), then each locale language
                    sb.Append(ToCSVString("Property"));
                    sb.Append("," + ToCSVString("English"));
                    foreach (string language in locale.Languages) sb.Append("," + ToCSVString(language));
                    sb.AppendLine();

                    for (int i = 0; i < locale._keys.Length; i++)
                    {
                        string key = locale._keys[i];
                        if (ShouldIgnoreKey(key)) continue;
                        sb.Append(ToCSVString(key));

                        // Column 2: UI Label as written in the shader (MaterialProperty.displayName)
                        string label = "";
                        if (_defaultPropertyContent.TryGetValue(key, out string defaultLabel)) label = defaultLabel;
                        sb.Append("," + ToCSVString(label));

                        // Remaining Columns: Translations
                        for (int j = 0; j < locale.Languages.Length; j++) sb.Append("," + ToCSVString(locale._values[i * locale.Languages.Length + j]));
                        sb.AppendLine();
                    }
                    // UTF-8 with BOM (Excel-friendly)
                    File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(true));
                }
            }

            // DEBUG FUNCTION: Export a CSV File only with Properties that don't currently exist in Localization.
            void ExportMissingAsCSV(Localization locale)
            {
                locale.Load();
                UpdateData(locale);

                HashSet<string> existing = new HashSet<string>();
                if (locale._keys != null) foreach (var k in locale._keys) if (!string.IsNullOrEmpty(k)) existing.Add(k);

                string path = EditorUtility.SaveFilePanel("Debug: Export Missing Properties as CSV", "", locale.name + "_Missing", "csv");
                if (string.IsNullOrEmpty(path)) return;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                sb.Append(ToCSVString("Property"));
                sb.Append("," + ToCSVString("English"));
                foreach (string language in locale.Languages) sb.Append("," + ToCSVString(language));
                sb.AppendLine();

                foreach (var kvp in _defaultPropertyContent)
                {
                    string key = kvp.Key;
                    if (ShouldIgnoreKey(key)) continue;
                    if (existing.Contains(key)) continue;

                    string english = kvp.Value ?? "";
                    sb.Append(ToCSVString(key));
                    sb.Append("," + ToCSVString(english));

                    for (int j = 0; j < locale.Languages.Length; j++) sb.Append(",");

                    sb.AppendLine();
                }

                File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(true));
            }

            void LoadFromCSV(Localization locale)
            {
                string path = EditorUtility.OpenFilePanel("Load from CSV", "", "csv");
                if (string.IsNullOrEmpty(path) == false)
                {
                    // Read as UTF-8 (handles BOM as well)
                    string csvText = File.ReadAllText(path, System.Text.Encoding.UTF8);
                    LoadFromCSVText(locale, csvText);
                }
            }

            void UpdateMissing(Localization locale)
            {
                _missingKeys.Clear();
                foreach (string key in locale._localizedStrings.Keys)
                {
                    if (ShouldIgnoreKey(key)) continue;
                    if (string.IsNullOrEmpty(locale._localizedStrings[key][_selectedLanguageIndex]))
                    {
                        if (_defaultPropertyContent.ContainsKey(key) && !string.IsNullOrWhiteSpace(_defaultPropertyContent[key]))
                        {
                            _missingKeys.Add((key, _defaultPropertyContent[key], _defaultPropertyContent[key]));
                        }
                    }
                }
            }

            void UpdateData(Localization locale)
            {
                locale.Load();

                // Gather all keys from all shaders
                List<MaterialProperty> allProps = new List<MaterialProperty>();
                foreach (Shader s in locale.ValidateWithShaders)
                {
                    allProps.AddRange(
                        MaterialEditor.GetMaterialProperties(new Material[] { new Material(s) })
                    );
                }
                // Make unique by propname
                allProps = allProps.GroupBy(p => p.name).Select(g => g.First()).ToList();
                _defaultPropertyContent.Clear();

                // add all keys from shader
                foreach (var prop in allProps)
                {
                    string key = prop.name;
                    if (ShouldIgnoreKey(key)) continue;
                    string value = prop.displayName;
                    int seperatorIndex = value.IndexOf("--", StringComparison.Ordinal);
                    if (seperatorIndex != -1)
                    {
                        value = value.Substring(0, seperatorIndex).Trim();
                    }

                    if (key.StartsWith("footer_")) continue;
                    if (key == ShaderEditor.PROPERTY_NAME_MASTER_LABEL) continue;
                    if (key == ShaderEditor.PROPERTY_NAME_LOCALE) continue;
                    if (key == ShaderEditor.PROPERTY_NAME_ON_SWAP_TO_ACTIONS) continue;
                    if (key == ShaderEditor.PROPERTY_NAME_SHADER_VERSION) continue;
                    if (!string.IsNullOrWhiteSpace(value) && !locale._localizedStrings.ContainsKey(key))
                    {
                        locale._localizedStrings.Add(key, new string[locale.Languages.Length]);
                    }
                    _defaultPropertyContent.Add(key, value);
                }
                // make missing keys a list of all keys that have an empty string in the selected language
                UpdateMissing(locale);
            }

            private void OnEnable()
            {
                Localization locale = (Localization)target;
                locale.Load();
                UpdateData(locale);
            }

            private void Awake()
            {
                Localization locale = (Localization)target;
                locale.Load();
                UpdateData(locale);
            }

            public override void OnInspectorGUI()
            {
                // Needed for SerializedProperty edits to persist.
                serializedObject.Update();

                Localization locale = (Localization)target;
                if (!locale._isLoaded)
                {
                    UpdateData(locale);
                }

                if (GUILayout.Button("Save", GUILayout.Height(50)))
                {
                    locale.Save();
                }
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                GUIShaders(locale);

                EditorGUILayout.Space(20);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField("Languages", EditorStyles.boldLabel);
                GUILanguages(locale);

                EditorGUILayout.Space(20);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField("Import / Export", EditorStyles.boldLabel);
                GUICSV(locale);

                if (locale.Languages.Length == 0)
                {
                    // Persist SerializedProperty changes before leaving.
                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                EditorGUILayout.Space(20);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                locale.EditInUI = EditorGUILayout.Toggle("Edit inside material UI", locale.EditInUI);

                EditorGUILayout.Space(20);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField("Select Language To Edit", EditorStyles.boldLabel);
                GUIEditLanguageSelection(locale);

                EditorGUILayout.Space(20);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField("Missing Entries", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("This will search all properties and list all that have no translation for the selected language.", MessageType.Info);
                GUIMissingEntries(locale);

                EditorGUILayout.Space(20);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField("Automatic Translation using Google", EditorStyles.boldLabel);
                GUIGoogleTranslate(locale);

                EditorGUILayout.Space(20);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField("Translate entries by value", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("This will search all properties and translate all that have the exact display name with the selected value. Suggested usecase: Panning, UV", MessageType.Info);
                GUIValueTranslate(locale);

                EditorGUILayout.Space(20);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField("Existing Entries", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("This will search all properties allows editing or removing them.", MessageType.Info);
                GUIEdit(locale);

                // Persist SerializedProperty changes.
                serializedObject.ApplyModifiedProperties();

            }

            void GUIShaders(Localization locale)
            {
                SerializedProperty shadersProp = serializedObject.FindProperty("ValidateWithShaders");

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(shadersProp, includeChildren: true);
                if (EditorGUI.EndChangeCheck())
                {
                    // Apply immediately so locale.ValidateWithShaders reflects the new list in this GUI pass.
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(locale);
                    AssetDatabase.SaveAssets();

                    // Refresh cached property labels / missing entries based on the new shaders.
                    locale.Load();
                    UpdateData(locale);
                }

                if (GUILayout.Button("Load Properties from Shaders"))
                {
                    // For each shader create a material & material editor so that the data is loaded into the localization object
                    foreach (Shader s in locale.ValidateWithShaders)
                    {
                        ShaderEditor se = new ShaderEditor();
                        se.FakePartialInitilizationForLocaleGathering(s);
                    }
                }
            }

            void GUILanguages(Localization locale)
            {
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
            }

            void GUIEditLanguageSelection(Localization locale)
            {
                EditorGUI.BeginChangeCheck();
                _selectedLanguageIndex = EditorGUILayout.Popup("Language to edit", _selectedLanguageIndex, locale.Languages);
                if (EditorGUI.EndChangeCheck())
                {
                    _missingKeys.Clear();
                }

                if (GUILayout.Button("Update Missing Entries"))
                {
                    UpdateData(locale);
                }
            }

            void GUICSV(Localization locale)
            {
                EditorGUILayout.LabelField("Spreadsheet Sync", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Use this area to load Localization data from an external CSV file on the internet (ideally from Google Docs Spreadsheet), then click Import.\n\n" +
                    "Example Format:\nhttps://docs.google.com/spreadsheets/d/<ID>/gviz/tq?tqx=out:csv&sheet=<TAB_NAME>",
                    MessageType.Info
                );

                locale.SpreadsheetCsvUrl = EditorGUILayout.TextField("URL", locale.SpreadsheetCsvUrl);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = !string.IsNullOrWhiteSpace(locale.SpreadsheetCsvUrl);
                    if (GUILayout.Button("Import Spreadsheet from URL")) ImportOnlineSpreadsheet(locale);
                    if (GUILayout.Button("Open URL", GUILayout.Width(90))) Application.OpenURL(locale.SpreadsheetCsvUrl);
                }

                GUI.enabled = true;

                EditorGUILayout.Space();
                
                if (GUILayout.Button("Load from CSV")) LoadFromCSV(locale);

                if (locale.Languages.Length == 0)
                {
                    // Persist SerializedProperty changes before leaving.
                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                if (GUILayout.Button("Export as CSV")) ExportAsCSV(locale);

                if (GUILayout.Button("Export Missing Properties as CSV")) ExportMissingAsCSV(locale);

                EditorGUILayout.Space();

                string lastUpdated = ReadLocaleTimestamp(locale);
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.TextField("Last Updated", string.IsNullOrEmpty(lastUpdated) ? "-" : lastUpdated);
            }

            void GUIMissingEntries(Localization locale)
            {
                (string, string, string) kvToRemove = default;
                for (int i = 0; i < _missingKeys.Count && i < 10; i++)
                {
                    var kv = _missingKeys[i];
                    EditorGUILayout.BeginHorizontal();
                    kv.newValue = EditorGUILayout.DelayedTextField(kv.key, kv.newValue);
                    if (GUILayout.Button("Skip", GUILayout.Width(50)))
                    {
                        kvToRemove = kv;
                    }
                    if (GUILayout.Button("Apply", GUILayout.Width(50)))
                    {
                        if (!locale._localizedStrings.ContainsKey(kv.key))
                        {
                            locale._localizedStrings.Add(kv.key, new string[locale.Languages.Length]);
                        }
                        locale._localizedStrings[kv.key][_selectedLanguageIndex] = kv.newValue;
                        kvToRemove = kv;
                    }
                    _missingKeys[i] = kv;
                    EditorGUILayout.EndHorizontal();
                }
                if (_missingKeys.Count > 10)
                {
                    EditorGUILayout.LabelField("...");
                }
                if (kvToRemove != default)
                {
                    _missingKeys.Remove(kvToRemove);
                }
            }

            void GUIGoogleTranslate(Localization locale)
            {
                _autoTranslateLanguageShortCode = EditorGUILayout.TextField("Language Short Code", _autoTranslateLanguageShortCode);
                EditorGUILayout.HelpBox("Short code must be valid short code. See https://cloud.google.com/translate/docs/languages for a list of valid short codes.", MessageType.Info);
                if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    Application.OpenURL("https://cloud.google.com/translate/docs/languages");
                }
                if (GUILayout.Button("Auto Translate"))
                {
                    int _missingKeysCount = _missingKeys.Count;
                    int i = 0;
                    foreach ((string key, string defaultValue, string newValue) in _missingKeys)
                    {
                        EditorUtility.DisplayProgressBar("Auto Translate", $"Translating {i}/{_missingKeysCount}", (float)i / _missingKeysCount);
                        try
                        {
                            if (!locale._localizedStrings.ContainsKey(key))
                            {
                                locale._localizedStrings.Add(key, new string[locale.Languages.Length]);
                            }
                            locale._localizedStrings[key][_selectedLanguageIndex] = WebHelper.Translate(defaultValue, _autoTranslateLanguageShortCode);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                        i += 1;
                    }
                    EditorUtility.ClearProgressBar();
                    locale.Save();
                }
            }

            void GUIValueTranslate(Localization locale)
            {
                _translateByValueIn = EditorGUILayout.TextField("Search for", _translateByValueIn);
                _translateByValueOut = EditorGUILayout.TextField("Translate with", _translateByValueOut);
                if (GUILayout.Button("Execute"))
                {
                    foreach (var kv in _defaultPropertyContent)
                    {
                        if (kv.Value == _translateByValueIn)
                        {
                            locale._localizedStrings[kv.Key][_selectedLanguageIndex] = _translateByValueOut;
                        }
                    }
                    UpdateMissing(locale);
                }
            }

            void GUIEdit(Localization locale)
            {
                EditorGUI.BeginChangeCheck();
                _searchById = EditorGUILayout.TextField("Search by id", _searchById);
                _searchByTranslation = EditorGUILayout.TextField("Search by translation", _searchByTranslation);
                if (EditorGUI.EndChangeCheck())
                {
                    List<string> res = new List<string>();
                    bool searchById = _searchById.Length > 0;
                    bool searchByTranslation = _searchByTranslation.Length > 0;
                    foreach (string key in locale._localizedStrings.Keys)
                    {
                        if (locale._localizedStrings[key][_selectedLanguageIndex] == null) continue;
                        if (
                            (searchByTranslation && locale._localizedStrings[key][_selectedLanguageIndex].IndexOf(_searchByTranslation, StringComparison.OrdinalIgnoreCase) != -1)
                         || (searchById && key.IndexOf(_searchById, StringComparison.OrdinalIgnoreCase) != -1)
                         )
                        {
                            res.Add(key);
                        }
                    }
                    _searchResults = res.ToArray();
                }
                EditorGUILayout.Space(5);
                if (_searchById.Length > 0 || _searchByTranslation.Length > 0)
                {
                    int count = 0;
                    foreach (string key in _searchResults)
                    {
                        if (count > 50)
                        {
                            EditorGUILayout.LabelField("...");
                            break;
                        }
                        EditorGUILayout.BeginHorizontal();
                        string value = EditorGUILayout.DelayedTextField(key, locale._localizedStrings[key][_selectedLanguageIndex]);
                        if (GUILayout.Button("Remove", GUILayout.Width(65)))
                        {
                            locale._localizedStrings[key][_selectedLanguageIndex] = "";
                        }
                        EditorGUILayout.EndHorizontal();
                        locale._localizedStrings[key][_selectedLanguageIndex] = value;
                        count++;
                    }
                }
            }
        }
    }
}

