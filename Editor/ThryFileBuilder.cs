using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Thry {
    public class ThryFileCreator {

        [MenuItem("Thry/ShaderUI Creator Helper/Create Label Boiler", false, priority = 40)]
        public static void CreateLabel()
        {
            string[] names = GetProperties();
            string data = "";
            foreach (string n in names)
            {
                data += n + ":=" + n + "--{tooltip:}";
                data += "\n";
            }
            Save(data, "_label");
        }
        [MenuItem("Thry/ShaderUI Creator Helper/Create Label Boiler", true, priority = 40)]
        static bool CreateLabelVaildate()
        {
            return ValidateSelection();
        }

        [MenuItem("Thry/ShaderUI Creator Helper/Create Label Boiler + Locale Boiler", false, priority = 40)]
        public static void CreateLabelLocale()
        {
            string[] names = GetProperties();
            string label_data = "";
            string locale_data = ",English\n";
            foreach (string n in names)
            {
                label_data += n + ":=locale::" + n + "_text--{tooltip:locale::"+n+"_tooltip}";
                label_data += "\n";
                locale_data += n + "_text," + n;
                locale_data += "\n";
                locale_data += n + "_tooltip,";
                locale_data += "\n";
            }
            Save(label_data, "_label");
            Save(locale_data, "_locale");
        }
        [MenuItem("Thry/ShaderUI Creator Helper/Create Label Boiler + Locale Boiler", true, priority = 40)]
        static bool CreateLabelLocaleValidate()
        {
            return ValidateSelection();
        }

        private static bool ValidateSelection()
        {
            if (Selection.activeObject == null)
                return false;
            string path = AssetDatabase.GetAssetPath(Selection.activeObject).ToLower();
            return path.EndsWith(".shader");
        }

        private static string[] GetProperties()
        {
            Shader shader = (Shader)Selection.activeObject;
            int count = ShaderUtil.GetPropertyCount(shader);
            List<string> menus = new List<string>();
            List<string> props = new List<string>();
            for (int i = 0; i < count; i++)
            {
                string n = ShaderUtil.GetPropertyName(shader, i);
                if (n.StartsWith("m_") || n.StartsWith("g_"))
                    menus.Add(n);
                else
                    props.Add(n);
            }
            menus.AddRange(props);
            return menus.ToArray();
        }

        private static void Save(string data, string add_string)
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            path = Path.GetDirectoryName(path)+ "/"+ Path.GetFileNameWithoutExtension(path) + add_string;
            Debug.Log(path);
            FileHelper.WriteStringToFile(data, path);
            AssetDatabase.Refresh();
        }
    }
}