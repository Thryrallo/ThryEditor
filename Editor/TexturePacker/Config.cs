using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor.TexturePacker
{
    [Serializable]
    public class TexturePackerConfig
    {
        public TextureSource[] Sources;
        public OutputTarget[] Targets;
        public List<Connection> Connections;
        public FileOutput FileOutput;
        public ImageAdjust ImageAdjust;

        public KernelPreset KernelPreset;
        public KernelSettings KernelSettings;

        public string Serialize()
        {
            return "ThryTexturePackerConfig:" + JsonUtility.ToJson(this);
        }

        public static TexturePackerConfig Deserialize(string json)
        {
            if (json.StartsWith("ThryTexturePackerConfig:"))
            {
                return JsonUtility.FromJson<TexturePackerConfig>(json.Substring("ThryTexturePackerConfig:".Length));
            }
            return null;
        }

        public static TexturePackerConfig GetNewConfig()
        {
            TexturePackerConfig config = new TexturePackerConfig();
            config.Sources = new TextureSource[]
            {
                new TextureSource(),
                new TextureSource(),
                new TextureSource(),
                new TextureSource(),
            };
            config.Targets = new OutputTarget[]
        {
                new OutputTarget(fallback: 0),
                new OutputTarget(fallback: 0),
                new OutputTarget(fallback: 0),
                new OutputTarget(fallback: 1),
        };
            config.Connections = new List<Connection>();
            config.FileOutput = new FileOutput(
                saveFolder: "Assets/Textures/Packed",
                fileName: "output",
                saveType: SaveType.PNG,
                colorSpace: ColorSpace.Linear,
                filterMode: FilterMode.Bilinear,
                alphaIsTransparency: true,
                saveQuality: 75,
                resolution: new Vector2Int(16, 16)
            );
            config.ImageAdjust = new ImageAdjust();
            config.KernelPreset = KernelPreset.None;
            config.KernelSettings = null;
            return config;
        }

        public static bool TryGetFromTexture(Texture2D tex, out TexturePackerConfig config)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            if (!string.IsNullOrEmpty(path))
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    string json = importer.userData;
                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            config = TexturePackerConfig.Deserialize(json);
                            if (config.Sources.Length > 0)
                            {
                                return true;
                            }
                        }
                        catch (Exception) { }
                    }
                }
            }
            config = null;
            return false;
        }

        public void SaveToImporter(TextureImporter importer)
        {
            if (importer == null || this == null) return;
            importer.userData = this.Serialize();
            if(!s_allTexturesWithConfigs.Contains(importer))
            {
                s_allTexturesWithConfigs.Add(importer);
            }
        }

        private static List<TextureImporter> s_allTexturesWithConfigs;
        public static List<TextureImporter> AllImportersWithConfigs
        {
            get
            {
                if (s_allTexturesWithConfigs == null)
                {
                    s_allTexturesWithConfigs = new List<TextureImporter>();
                    string[] guids = AssetDatabase.FindAssets("t:Texture2D");
                    for (int i = 0; i < guids.Length; i++)
                    {
                        string guid = guids[i];
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                        EditorUtility.DisplayProgressBar("Finding Textures", $"Processing {i + 1}/{guids.Length}", (float)(i + 1) / guids.Length);
                        if (importer != null)
                        {
                            if (importer.userData.StartsWith("ThryTexturePackerConfig:"))
                            {
                                s_allTexturesWithConfigs.Add(importer);
                            }
                        }
                    }
                    EditorUtility.ClearProgressBar();
                }
                return s_allTexturesWithConfigs;
            }
        }
    }
}