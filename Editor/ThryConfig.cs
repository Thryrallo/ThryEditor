using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ThryConfig : MonoBehaviour {

    public const string CONFIG_FILE_PATH = "./Assets/.ThryConfig.json";
    private static Config config;

    [InitializeOnLoad]
    public class Startup
    {
        static Startup()
        {
            if (!File.Exists(CONFIG_FILE_PATH))
            {
                ThrySettings.firstTimePopup();
            }
        }
    }


    public class Config
    {
        public bool useBigTextures = false;
        public bool useRenderQueueSelection = true;
        public bool isVrchatUser = true;

        public void save()
        {
            StreamWriter writer = new StreamWriter(CONFIG_FILE_PATH, false);
            writer.WriteLine(this.SaveToString());
            writer.Close();
        }

        public string SaveToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    public static Config GetConfig()
    {
        if (config == null) config = LoadConfig();
        return config;
    }

    //load the config from file
    private static Config LoadConfig()
    {
        Config config = null;
        if (File.Exists(CONFIG_FILE_PATH))
        {
            StreamReader reader = new StreamReader(CONFIG_FILE_PATH);
            config = JsonUtility.FromJson<Config>(reader.ReadToEnd());
            reader.Close();
        }
        else
        {
            File.CreateText(CONFIG_FILE_PATH).Close();
            config = new Config();
            config.save();
        }
        return config;
    }
}
