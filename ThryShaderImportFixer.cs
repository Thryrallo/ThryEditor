using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class ThryShaderImportFixer : AssetPostprocessor
{
    [MenuItem("Thry/Backup Materials")]
    static void Init()
    {
        backupAllMaterials();
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        List<string> importedShaders = new List<string>();

        foreach (string str in importedAssets)
        {
           Object asset = AssetDatabase.LoadAssetAtPath<Object>(str);
            if (asset!=null&&asset.GetType() == typeof(Shader))
            {
                Shader shader = (Shader)asset;
                importedShaders.Add(shader.name);
            }
        }

        if (!File.Exists(MATERIALS_BACKUP_FILE_PATH))
        {
            backupAllMaterials();
            return;
        }
        StreamReader reader = new StreamReader(MATERIALS_BACKUP_FILE_PATH);

        string l;
        while ((l = reader.ReadLine()) != null)
        {
            if (l == "") continue;
            string[] materialData = l.Split(new string[] { ":" }, System.StringSplitOptions.None);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialData[0]));
            if (importedShaders.Contains(materialData[1]))
            {
                Shader shader = Shader.Find(materialData[1]);
                material.shader = shader;
                material.renderQueue = int.Parse(materialData[2]);
                ThryEditor.UpdateRenderQueue(material, shader);
            }
        }
        ThryHelper.RepaintAllMaterialEditors();

        reader.Close();
    }

    //save mats
    public const string MATERIALS_BACKUP_FILE_PATH = "./Assets/.materialsBackup.txt";

    public static void backupAllMaterials()
    {
        if(!File.Exists(MATERIALS_BACKUP_FILE_PATH))File.CreateText(MATERIALS_BACKUP_FILE_PATH).Close();
        EditorUtility.DisplayProgressBar("Backup materials", "", 0);
        StreamWriter writer = new StreamWriter(MATERIALS_BACKUP_FILE_PATH, false);

        string[] materialGuids = AssetDatabase.FindAssets("t:material");
        for (int mG = 0; mG < materialGuids.Length; mG++)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialGuids[mG]));
            writer.WriteLine(mG + ":" + ThryHelper.getDefaultShaderName(material.shader.name) + ":" + material.renderQueue);
            EditorUtility.DisplayProgressBar("Backup materials", "", (float)(mG+1)/materialGuids.Length);
        }

        writer.Close();
        EditorUtility.ClearProgressBar();
    }

    public static void backupSingleMaterial(Material m)
    {
        string[] mats = ThryHelper.readFileIntoString(MATERIALS_BACKUP_FILE_PATH).Split(new string[] { "\n" }, System.StringSplitOptions.None);
        bool updated = false;
        string matGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m.GetInstanceID()));
        string newString = "";
        for (int mat = 0; mat < mats.Length; mat++)
        {
            if (mats[mat].Contains(matGuid))
            {
                updated = true;
                mats[mat] = matGuid + ":" + ThryHelper.getDefaultShaderName(m.shader.name) + ":" + m.renderQueue;
            }
            newString += mats[mat]+"\n";
        }
        if(!updated) newString += matGuid + ":" + ThryHelper.getDefaultShaderName(m.shader.name) + ":" + m.renderQueue;
        else newString = newString.Substring(0, newString.LastIndexOf("\n"));
        ThryHelper.writeStringToFile(newString,MATERIALS_BACKUP_FILE_PATH);
    }

    public static void restoreAllMaterials()
    {
        if (!File.Exists(MATERIALS_BACKUP_FILE_PATH))
        {
            backupAllMaterials();
            return;
        }
        StreamReader reader = new StreamReader(MATERIALS_BACKUP_FILE_PATH);

        string l;
        while ((l = reader.ReadLine()) != null)
        {
            string[] materialData = l.Split(new string[] { ":" }, System.StringSplitOptions.None);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialData[0]));
            Shader shader = Shader.Find(materialData[1]);
            material.shader = shader;
            material.renderQueue = int.Parse(materialData[2]);
            ThryEditor.UpdateRenderQueue(material, shader);
        }
        ThryHelper.RepaintAllMaterialEditors();

        reader.Close();
    }
}
