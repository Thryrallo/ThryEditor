// Material/Shader Inspector for Unity 2017/2018
// Copyright (C) 2019 Thryrallo

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class UnityHelper
    {
        public static string FindPathOfAssetWithExtension(string filename)
        {
            string[] guids = AssetDatabase.FindAssets(filename.RemoveFileExtension());
            foreach (string s in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(s);
                if (path.EndsWith(filename))
                    return path;
            }
            return filename;
        }

        public static List<string> FindAssetOfFilesWithExtension(string filename)
        {
            List<string> ret = new List<string>();
            string[] guids = AssetDatabase.FindAssets(filename.RemoveFileExtension());
            foreach (string s in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(s);
                if (path.EndsWith(filename))
                    ret.Add(path);
            }
            return ret;
        }

        public static void SetDefineSymbol(string symbol, bool active)
        {
            SetDefineSymbol(symbol, active, false);
        }

        public static void SetDefineSymbol(string symbol, bool active, bool refresh_if_changed)
        {
            try
            {
                string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                        BuildTargetGroup.Standalone);
                if (!symbols.Contains(symbol) && active)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(
                                  BuildTargetGroup.Standalone, symbols + ";" + symbol);
                    AssetDatabase.Refresh();
                }
                else if (symbols.Contains(symbol) && !active)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(
                                  BuildTargetGroup.Standalone, Regex.Replace(symbols, @";?" + @symbol, ""));
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception e)
            {
                e.ToString();
            }
        }

        public static void RepaintInspector(System.Type t)
        {
            Editor[] ed = (Editor[])Resources.FindObjectsOfTypeAll<Editor>();
            for (int i = 0; i < ed.Length; i++)
            {
                if (ed[i].GetType() == t)
                {
                    ed[i].Repaint();
                    return;
                }
            }
        }

        public static void RepaintEditorWindow(Type t)
        {
            EditorWindow window = FindEditorWindow(t);
            if (window != null) window.Repaint();
        }

        public static EditorWindow FindEditorWindow(System.Type t)
        {
            EditorWindow[] ed = (EditorWindow[])Resources.FindObjectsOfTypeAll<EditorWindow>();
            for (int i = 0; i < ed.Length; i++)
            {
                if (ed[i].GetType() == t)
                {
                    return ed[i];
                }
            }
            return null;
        }

        public static int CalculateLengthOfText(string message, Font font= null)
        {
            if (font == null)
                font = GUI.skin.font;
            int totalLength = 0;

            CharacterInfo characterInfo = new CharacterInfo();

            char[] arr = message.ToCharArray();

            foreach (char c in arr)
            {
                font.GetCharacterInfo(c, out characterInfo, font.fontSize);
                totalLength += characterInfo.advance;
            }

            return totalLength;
        }

        public static void ToggleKeyword(Material material, string keyword, bool turn_on)
        {
            bool is_on = material.IsKeywordEnabled(keyword);
            if (is_on && !turn_on)
                material.DisableKeyword(keyword);
            else if (!is_on && turn_on)
                material.EnableKeyword(keyword);
        }

        public static void ToggleKeyword(Material[] materials, string keyword, bool on)
        {
            foreach (Material m in materials)
                ToggleKeyword(m,keyword,on);
        }

        public static void ToggleKeyword(MaterialProperty p, string keyword, bool on)
        {
            foreach (UnityEngine.Object o in p.targets)
                ToggleKeyword((Material)o, keyword, on);
        }

        public static void CopyPropertyValueFromMaterial(MaterialProperty p, Material source)
        {
            switch (p.type)
            {
                case MaterialProperty.PropType.Float:
                case MaterialProperty.PropType.Range:
                    float f = source.GetFloat(p.name);
                    p.floatValue = f;
                    string[] drawer = ShaderHelper.GetDrawer(p);
                    if (drawer != null && drawer.Length > 1 && drawer[0] == "Toggle" && drawer[1] != "__")
                        ToggleKeyword(p, drawer[1], f == 1);
                    break;
                case MaterialProperty.PropType.Color:
                    Color c = source.GetColor(p.name);
                    p.colorValue = c;
                    break;
                case MaterialProperty.PropType.Vector:
                    Vector4 vector = source.GetVector(p.name);
                    p.vectorValue = vector;
                    break;
                case MaterialProperty.PropType.Texture:
                    Texture t = source.GetTexture(p.name);
                    Vector2 offset = source.GetTextureOffset(p.name);
                    Vector2 scale = source.GetTextureScale(p.name);
                    p.textureValue = t;
                    p.textureScaleAndOffset = new Vector4(scale.x, scale.y, offset.x, offset.y);
                    break;
            } 
        }
    }

}