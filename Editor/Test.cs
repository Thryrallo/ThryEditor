using System.Collections;
using System.Collections.Generic;
#if NET_SET_TWO_POINT_ZERO
using System.Drawing;
using System.Drawing.Imaging;
#endif
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class Test : EditorWindow
    {
        [MenuItem("Thry/Test")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            Test window = (Test)EditorWindow.GetWindow(typeof(Test));
            window.Show();
        }

        string texname;

        void OnGUI()
        {
            GUILayout.Label("Texture name:");
            texname = GUILayout.TextArea(texname);
            if (GUILayout.Button("Try it"))
            {
                string[] id = AssetDatabase.FindAssets(texname + " t:texture");
                if (id.Length == 0)
                {
                    Debug.Log("could not find texture");
                }
                else
                {
                    string path = AssetDatabase.GUIDToAssetPath(id[0]);
                    Debug.Log(path);
                    Helper.MakeTextureReadible(path);
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    bool vertical = IsGradiantVertical(texture);
                    Debug.Log("vertical: " + vertical);
                    IterateGradiant(texture, vertical);
                }
            }
            if (GUILayout.Button("gif to frames"))
            {
                string[] id = AssetDatabase.FindAssets(texname + " t:texture");
                if (id.Length == 0)
                {
                    Debug.Log("could not find texture");
                }
                else
                {
                    string path = AssetDatabase.GUIDToAssetPath(id[0]);
                    Converter.GifToTextureArray(path);
                }
            }
        }

        private static bool IsGradiantVertical(Texture2D texture)
        {
            return texture.height > texture.width;
        }

        private static void IterateGradiant(Texture2D texture, bool vertical)
        {
            int length = vertical? texture.height: texture.width;
            UnityEngine.Color lastColor = texture.GetPixel(0,0);
            for (int i = 0; i < length; i++)
            {
                UnityEngine.Color sectionColor = texture.GetPixel(vertical ? 0:i,vertical? i : 0);
                Debug.Log(i + ":" + sectionColor.ToString() +" Diff: "+ Helper.Subtract(sectionColor, lastColor));
                lastColor = sectionColor;
            }
        }

    }
}