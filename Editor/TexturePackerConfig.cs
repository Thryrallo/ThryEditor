using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Thry.TexturePacker;

namespace Thry
{
    public class TexturePackerConfig : ScriptableObject
    {
        public TextureSource[] Sources;
        public OutputConfig[] OutputConfigs;
        public Connection[] Connections;
        public ColorSpace ColorSpace;
        public FilterMode FilterMode;
        public SaveType SaveType;
        public float SaveQuality;
        public ImageAdjust ImageAdjust;
        public string SaveFolder;
        public string SaveName;
    }
}