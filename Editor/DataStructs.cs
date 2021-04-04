﻿// Material/Shader Inspector for Unity 2017/2018
// Copyright (C) 2019 Thryrallo

using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Thry
{
    public class PATH
    {
        public const string TEXTURES_DIR = "Assets/textures";
        public const string RSP_NEEDED_PATH = "Assets/";

        public const string DELETING_DIR = "Thry/trash";

        public const string PERSISTENT_DATA = "Thry/persistent_data";
        public const string AFTER_COMPILE_DATA = "Thry/after_compile_data";
        public const string MATERIALS_BACKUP_FILE = "Thry/materialsBackup";
        public const string THRY_EDITOR_SHADERS = "Thry/shaders";

        public const string GRADIENT_INFO_FILE = "Thry/gradients";
        public const string TEXT_INFO_FILE = "Thry/text_textures";
        public const string MODULES_LOCATION__DATA = "Thry/modules_location_data";

        public const string LINKED_MATERIALS_FILE = "Thry/linked_materials.json";

        public const string TEMP_VRC_SDK_PACKAGE = "./vrc_sdk_package.unitypackage";
    }

    public class URL
    {
        public const string MODULE_COLLECTION = "https://thryeditor.thryrallo.de/files/modules.json";
        public const string SETTINGS_MESSAGE_URL = "http://thryeditor.thryrallo.de/message.json";

        public const string DATA_SHARE_SEND = "http://thryeditor.thryrallo.de/send_analytics.php";
        public const string DATA_SHARE_GET_MY_DATA = "https://thryeditor.thryrallo.de/get_my_data.php";
        public const string COUNT_PROJECT = "http://thryeditor.thryrallo.de/count_project.php";
        public const string COUNT_USER = "http://thryeditor.thryrallo.de/count_user.php";
    }

    public class DEFINE_SYMBOLS
    {
        public const string IMAGING_EXISTS = "IMAGING_DLL_EXISTS";
    }

    public class RESOURCE_NAME
    {
        public const string DROPDOWN_SETTINGS_TEXTURE = "thry_settings_dropdown";
        public const string SETTINGS_ICON_TEXTURE = "thry_settings_icon";
        public const string WHITE_RECT = "thry_white_rect";
        public const string DARK_RECT = "thry_dark_rect";
        public const string ACTICE_LINK_ICON = "thry_link_icon_active";
        public const string INACTICE_LINK_ICON = "thry_link_icon_inactive";
        public const string VISIVILITY_ICON = "thry_visiblity_icon";
        public const string SEARCH_ICON = "thry_magnifying_glass_icon";
        public const string PRESETS_ICON = "thry_presets_icon";
        public const string TEXTURE_ARROW = "thry_arrow";
        public const string TEXTURE_ANIMTED = "thry_animated_icon";
    }

    public struct EditorData
    {
        public MaterialEditor editor;
        public MaterialProperty[] properties;
        public ShaderEditor gui;
        public Material[] materials;
        public Shader shader;
        public Shader defaultShader;
        public ShaderPart currentProperty;
        public Dictionary<string, ShaderProperty> propertyDictionary;
        public List<ShaderPart> shaderParts;
        public List<ShaderPart> searchedShaderParts;
        public List<ShaderProperty> textureArrayProperties;
        public bool firstCall;
        public bool show_HeaderHider;
        public bool use_ShaderOptimizer;
        public bool isLockedMaterial;
        public string animPropertySuffix;
    }

    public class DrawingData
    {
        public static TextureProperty currentTexProperty;
        public static Rect lastGuiObjectRect;
        public static Rect lastGuiObjectHeaderRect;
        public static bool lastPropertyUsedCustomDrawer;
        public static bool is_enabled = true;
    }

    public class GradientData
    {
        public Texture preview_texture;
        public Gradient gradient;
    }

    //--------------Shader Data Structs--------------------

    public class PropertyOptions
    {
        public int offset = 0;
        public string tooltip = "";
        public DefineableAction altClick;
        public DefineableCondition condition_show = new DefineableCondition();
        public string condition_showS;
        public DefineableCondition condition_enable = null;
        public PropertyValueAction[] on_value_actions;
        public string on_value;
        public DefineableAction[] actions;
        public ButtonData button_right;
        public TextureData texture;
        public string[] reference_properties;
        public string reference_property;
        public bool force_texture_options = false;
        public bool hide_in_inspector = false;
        public bool is_hideable = false;
        public bool is_visible_simple = false;
        public string file_name;
    }

    public class ButtonData
    {
        public string text = "";
        public TextureData texture = null;
        public DefineableAction action = new DefineableAction();
        public string hover = "";
        public DefineableCondition condition_show = new DefineableCondition();
    }

    public class TextureData
    {
        public string name = null;

        public int width = 128;
        public int height = 128;

        public char channel = 'r';

        public int ansioLevel = 1;
        public FilterMode filterMode = FilterMode.Bilinear;
        public TextureWrapMode wrapMode = TextureWrapMode.Repeat;

        public void ApplyModes(Texture texture)
        {
            texture.filterMode = filterMode;
            texture.wrapMode = wrapMode;
            texture.anisoLevel = ansioLevel;
        }
        public void ApplyModes(string path)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.filterMode = filterMode;
            importer.wrapMode = wrapMode;
            importer.anisoLevel = ansioLevel;
            importer.SaveAndReimport();
        }

        public Texture loaded_texture;
        public Texture GetTextureFromName()
        {
            if (loaded_texture == null)
            {
                string path = FileHelper.FindFile(name, "texture");
                if (path != null)
                    loaded_texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
                else
                    loaded_texture = new Texture2D(1,1);
            }
            return loaded_texture;
        }
    }

    public class PropertyValueAction
    {
        public string value;
        public DefineableAction[] actions;

        public bool Execute(MaterialProperty p)
        {
            if((p.floatValue.ToString()==value)
                || ( p.colorValue.ToString() == value)
                || ( p.vectorValue.ToString() == value )
                || (p.textureValue != null && p.textureValue.ToString() == value))
            {
                foreach (DefineableAction a in actions)
                    a.Perform();
                return true;
            }
            return false;
        }

        // value,property1=value1,property2=value2
        public static PropertyValueAction Parse(string s)
        {
            s = s.Trim();
            string[] parts = s.Split(',');
            if (parts.Length > 0)
            {
                PropertyValueAction propaction = new PropertyValueAction();
                propaction.value = parts[0];
                List<DefineableAction> actions = new List<DefineableAction>();
                for (int i = 1; i < parts.Length; i++)
                {
                    actions.Add(DefineableAction.Parse(parts[i]));
                }
                propaction.actions = actions.ToArray();
                return propaction;
            }
            return null;
        }

        public static PropertyValueAction[] ParseToArray(string s)
        {
            //s = v,p1=v1,p2=v2;v3
            List<PropertyValueAction> propactions = new List<PropertyValueAction>();
            string[] parts = s.Split(';');
            foreach (string p in parts)
            {
                PropertyValueAction propertyValueAction = PropertyValueAction.Parse(p);
                if (propertyValueAction != null)
                    propactions.Add(propertyValueAction);
            }
            return propactions.ToArray();
        }
    }

    public class DefineableAction
    {
        public DefineableActionType type = DefineableActionType.NONE;
        public string data = "";
        public void Perform()
        {
            switch (type)
            {
                case DefineableActionType.URL:
                    Application.OpenURL(data);
                    break;
                case DefineableActionType.SET_PROPERTY:
                    string[] set = Regex.Split(data, @"=");
                    if (set.Length > 1)
                        MaterialHelper.SetMaterialValue(set[0].Trim(), set[1].Trim());
                    break;
                case DefineableActionType.SET_TAG:
                    string[] keyValue = Regex.Split(data, @"=");
                    foreach (Material m in ShaderEditor.currentlyDrawing.materials)
                        m.SetOverrideTag(keyValue[0].Trim(), keyValue[1].Trim());
                    break;
                case DefineableActionType.SET_SHADER:
                    Shader shader = Shader.Find(data);
                    if (shader != null)
                    {
                        foreach (Material m in ShaderEditor.currentlyDrawing.materials)
                            m.shader = shader;
                    }
                    break;
            }
        }

        public static DefineableAction Parse(string s)
        {
            s = s.Trim();
            DefineableAction action = new DefineableAction();
            if (s.StartsWith("https") || s.StartsWith("www"))
            {
                action.type = DefineableActionType.URL;
                action.data = s;
            }
            else if (s.StartsWith("tag::"))
            {
                action.type = DefineableActionType.SET_TAG;
                action.data = s.Replace("tag::", "");
            }
            else if (s.StartsWith("shader="))
            {
                action.type = DefineableActionType.SET_SHADER;
                action.data = s.Replace("shader=", "");
            }
            else if (s.Contains("="))
            {
                action.type = DefineableActionType.SET_PROPERTY;
                action.data = s;
            }
            return action;
        }
    }

    public enum DefineableActionType
    {
        NONE,
        URL,
        SET_PROPERTY,
        SET_SHADER,
        SET_TAG
    }

    public class DefineableCondition
    {
        public DefineableConditionType type = DefineableConditionType.NONE;
        public string data = "";
        public DefineableCondition condition1;
        public DefineableCondition condition2;

        bool? cachedTest = null;

        public bool Test()
        {
            if(cachedTest != null)
                return (bool) cachedTest;

            switch (type)
            {
                case DefineableConditionType.NONE:
                    return (bool)(cachedTest = true);
                case DefineableConditionType.TRUE:
                    return (bool)(cachedTest = true);
                case DefineableConditionType.FALSE:
                    return (bool)(cachedTest = false);
            }
            string comparator = GetComparetor();
            string[] parts = Regex.Split(data, comparator);
            string obj = parts[0];
            string value = parts[parts.Length-1];
            switch (type)
            {
                case DefineableConditionType.PROPERTY_BOOL:
                    ShaderProperty prop = ShaderEditor.currentlyDrawing.propertyDictionary[obj];
                    if (prop == null) return (bool)(cachedTest = false);

                    if (comparator == "##") return (bool)(cachedTest = prop.materialProperty.floatValue == 1);
                    float f = Parser.ParseFloat(parts[1]);
                    if (comparator == "==") return (bool)(cachedTest = prop.materialProperty.floatValue == f);
                    if (comparator == "!=") return (bool)(cachedTest = prop.materialProperty.floatValue != f);
                    if (comparator == "<") return (bool)(cachedTest = prop.materialProperty.floatValue < f);
                    if (comparator == ">") return (bool)(cachedTest = prop.materialProperty.floatValue > f);
                    if (comparator == ">=") return (bool)(cachedTest = prop.materialProperty.floatValue >= f);
                    if (comparator == "<=") return (bool)(cachedTest = prop.materialProperty.floatValue <= f);
                    break;
                case DefineableConditionType.EDITOR_VERSION:
                    int c_ev = Helper.compareVersions(Config.Get().verion, value);
                    if (comparator == "==") return (bool)(cachedTest = c_ev == 0);
                    if (comparator == "!=") return (bool)(cachedTest = c_ev != 0);
                    if (comparator == "<") return (bool)(cachedTest = c_ev == 1);
                    if (comparator == ">") return (bool)(cachedTest = c_ev == -1);
                    if (comparator == ">=") return (bool)(cachedTest = c_ev == -1 || c_ev == 0);
                    if (comparator == "<=") return (bool)(cachedTest = c_ev == 1 || c_ev == 0);
                    break;
                case DefineableConditionType.VRC_SDK_VERSION:
                    if (VRCInterface.Get().sdk_information.type == VRCInterface.VRC_SDK_Type.NONE)
                        return (bool)(cachedTest = false);
                    int c_vrc = Helper.compareVersions(VRCInterface.Get().sdk_information.installed_version, value);
                    if (comparator == "==") return (bool)(cachedTest = c_vrc == 0);
                    if (comparator == "!=") return (bool)(cachedTest = c_vrc != 0);
                    if (comparator == "<") return (bool)(cachedTest = c_vrc == 1);
                    if (comparator == ">") return (bool)(cachedTest = c_vrc == -1);
                    if (comparator == ">=") return (bool)(cachedTest = c_vrc == -1 || c_vrc == 0);
                    if (comparator == "<=") return (bool)(cachedTest = c_vrc == 1 || c_vrc == 0);
                    break;
                case DefineableConditionType.TEXTURE_SET:
                    ShaderProperty shaderProperty = ShaderEditor.currentlyDrawing.propertyDictionary[data];
                    if (shaderProperty == null) return (bool)(cachedTest = false);
                    return (bool)(cachedTest = shaderProperty.materialProperty.textureValue != null);
                case DefineableConditionType.DROPDOWN:
                    ShaderProperty dropdownProperty = ShaderEditor.currentlyDrawing.propertyDictionary[obj];
                    if (dropdownProperty == null) return (bool)(cachedTest = false);
                    if (comparator == "##") return (bool)(cachedTest = dropdownProperty.materialProperty.floatValue == 1);
                    if (comparator == "==") return (bool)(cachedTest = "" + dropdownProperty.materialProperty.floatValue == parts[1]);
                    if (comparator == "!=") return (bool)(cachedTest = "" + dropdownProperty.materialProperty.floatValue != parts[1]);
                    break;
                case DefineableConditionType.AND:
                    if(condition1!=null&&condition2!=null) return (bool)(cachedTest = condition1.Test() && condition2.Test());
                    break;
                case DefineableConditionType.OR:
                    if (condition1 != null && condition2 != null) return (bool)(cachedTest = condition1.Test() || condition2.Test());
                    break;
            }

            return (bool)(cachedTest = true);
        }
        private string GetComparetor()
        {
            if (data.Contains("=="))
                return "==";
            if (data.Contains("!="))
                return "!=";
            if (data.Contains(">="))
                return ">=";
            if (data.Contains("<="))
                return "<=";
            if (data.Contains(">"))
                return ">";
            if (data.Contains("<"))
                return "<";
            return "##";
        }

        public override string ToString()
        {
            switch (type)
            {
                case DefineableConditionType.PROPERTY_BOOL:
                    return data;
                case DefineableConditionType.EDITOR_VERSION:
                    return "EDITOR_VERSION" + data;
                case DefineableConditionType.VRC_SDK_VERSION:
                    return "VRC_SDK_VERSION" + data;
                case DefineableConditionType.AND:
                    if (condition1 != null && condition2 != null) return "("+condition1.ToString() + "&&" + condition2.ToString()+")";
                    break;
                case DefineableConditionType.OR:
                    if (condition1 != null && condition2 != null) return "("+condition1.ToString()+"||"+condition2.ToString()+")";
                    break;
            }
            return "";
        }

        public static DefineableCondition Parse(string s)
        {
            s = Strip(s);

            int depth = 0;
            for (int i = 0; i < s.Length - 1; i++)
            {
                char c = s[i];
                char cc = s[i + 1];
                if (c == '(')
                    depth++;
                else if (c == ')')
                    depth--;

                if (depth == 0)
                {
                    if (c == '&' && cc == '&')
                    {
                        DefineableCondition con = new DefineableCondition();
                        con.type = DefineableConditionType.AND;
                        con.condition1 = Parse(s.Substring(0, i));
                        con.condition2 = Parse(s.Substring(i + 2, s.Length - i - 2));
                        return con;
                    }
                    if (c == '|' && cc == '|')
                    {
                        DefineableCondition con = new DefineableCondition();
                        con.type = DefineableConditionType.OR;
                        con.condition1 = Parse(s.Substring(0, i));
                        con.condition2 = Parse(s.Substring(i + 2, s.Length - i - 2));
                        return con;
                    }
                }
            }
            for (int i = 0; i < s.Length - 1; i++)
            {
                char c = s[i];
                char cc = s[i + 1];
                if (c == '(')
                    depth++;
                else if (c == ')')
                    depth--;

                if (depth == 0)
                {
                    if (c == '>' || c=='<' || c=='=' || c == '!')
                    {
                        DefineableCondition con = new DefineableCondition();
                        con.data = s;
                        con.type = DefineableConditionType.PROPERTY_BOOL;
                        if (s.StartsWith("VRCSDK"))
                        {
                            con.type = DefineableConditionType.VRC_SDK_VERSION;
                            con.data = s.Replace("VRCSDK", "");
                        }else if (s.StartsWith("ThryEditor"))
                        {
                            con.type = DefineableConditionType.VRC_SDK_VERSION;
                            con.data = s.Replace("ThryEditor", "");
                        }
                        return con;
                    }
                }
            }
            return new DefineableCondition();
        }

        private static string Strip(string s)
        {
            s = s.Trim();
            if (s.StartsWith("(") == false)
                return s;
            bool stripKlammer = true;
            int depth = 0;
            int i = 0;
            foreach (char c in s)
            {
                if (c == '(')
                    depth++;
                else if (c == ')')
                    depth--;
                if (depth == 0 && i != 0 && i != s.Length - 1)
                    stripKlammer = false;
                i++;
            }
            if (stripKlammer)
                return Strip(s.Substring(1, s.Length - 2));
            return s;
        }
    }

    public enum DefineableConditionType
    {
        NONE,
        TRUE,
        FALSE,
        PROPERTY_BOOL,
        EDITOR_VERSION,
        VRC_SDK_VERSION,
        TEXTURE_SET,
        DROPDOWN,
        AND,
        OR
    }

    public class Module
    {
        public string id;
        public string url = "";
        public string author;
        public string path;
        public bool is_being_installed_or_removed = false;
        public bool available_requirement_fullfilled = true;
        public bool update_available = false;
        public ModuleLocationData location_data;
        public ModuleInfo available_module = null;
        public ModuleInfo installed_module = null;
        public bool ui_expanded = false;
    }

    public class ModuleInfo
    {
        public string name = "";
        public string version = "0";
        public string description = "";
        public string classname = "";
        public DefineableCondition requirement;
        public List<string> files;
    }

    public class ModuleLocationData
    {
        public string guid;
        public string classname;
        public string[] files;
    }

    public enum TextureDisplayType
    {
        small,big,stylized_big
    }
}