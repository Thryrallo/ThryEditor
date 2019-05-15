using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class AutoAvatarDescriptor : MonoBehaviour {

    private static string[] BLEND_SHAPE_NAMES = new string[] { "sil", "pp", "ff", "th", "dd", "kk", "ch", "ss", "nn", "rr", "aa", "e", "ih", "oh", "ou" };

    [MenuItem("Thry/Auto setup Avatar Descriptor")]
    static void Init()
    {
        GameObject parent = Selection.activeGameObject;
        VRCSDK2.VRC_AvatarDescriptor descriptor = (VRCSDK2.VRC_AvatarDescriptor)parent.GetComponent(typeof(VRCSDK2.VRC_AvatarDescriptor));
        if (descriptor != null)
        {
            if (parent != null)
            {
                List<GameObject> eyeObjects = searchGameObjectsByName(parent, "eye");
                Vector3 viewPointPos = new Vector3();
                foreach (GameObject eyeO in eyeObjects) viewPointPos = vectorAddWeightedVector(viewPointPos, eyeO.transform.position, 1.0 / eyeObjects.Count);
                viewPointPos = vectorAddWeightedVector(viewPointPos, parent.transform.position, -1);
                descriptor.ViewPosition = viewPointPos;

                descriptor.Animations = VRCSDK2.VRC_AvatarDescriptor.AnimationSet.Female;
            }
            if (parent != null && descriptor.VisemeSkinnedMesh == null)
            {
                SkinnedMeshRenderer body = null;
                SkinnedMeshRenderer head = null;
                foreach (Transform child in parent.transform)
                {
                    SkinnedMeshRenderer skinnedMeshRenderer = child.gameObject.GetComponent<SkinnedMeshRenderer>();
                    if (skinnedMeshRenderer != null && child.name.ToLower() == "body") body = skinnedMeshRenderer;
                    if (skinnedMeshRenderer != null && (child.name.ToLower() == "head" || child.name.ToLower() == "face")) head = skinnedMeshRenderer;

                }
                if (head != null | body != null) descriptor.lipSync = VRCSDK2.VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;
                if (head != null) descriptor.VisemeSkinnedMesh = head;
                else if (body != null) descriptor.VisemeSkinnedMesh = body;
                descriptor.VisemeBlendShapes = new string[15];
            }
            if (descriptor.VisemeSkinnedMesh != null && descriptor.VisemeBlendShapes.Length > 0)
            {
                Mesh mesh = descriptor.VisemeSkinnedMesh.sharedMesh;
                for (int i = 0; i < BLEND_SHAPE_NAMES.Length; i++)
                {
                    float closestScore = 0;
                    for (int b = 0; b < mesh.blendShapeCount; b++)
                    {
                        if (mesh.GetBlendShapeName(b).Contains(BLEND_SHAPE_NAMES[i]))
                        {
                            string compareBlendName = mesh.GetBlendShapeName(b).Replace("vrc.", "");
                            float score = ((float)BLEND_SHAPE_NAMES[i].Length / compareBlendName.Length);
                            if (score > closestScore)
                            {
                                descriptor.VisemeBlendShapes[i] = mesh.GetBlendShapeName(b);
                                closestScore = score;
                            }


                        }
                    }
                }
            }
        }
        else
        {
            if (parent != null) parent.AddComponent(typeof(VRCSDK2.VRC_AvatarDescriptor));
            Init();
        }
    }

    public static Vector3 vectorAddWeightedVector(Vector3 baseVec, Vector3 add, double weight)
    {
        return vectorAddWeightedVector(baseVec, add, (float)weight);
    }

    public static Vector3 vectorAddWeightedVector(Vector3 baseVec, Vector3 add, float weight)
    {
        return new Vector3(baseVec.x + add.x * weight, baseVec.y + add.y * weight, baseVec.z + add.z * weight);
    }

    public static List<GameObject> searchGameObjectsByName(GameObject parent, string name)
    {
        List<GameObject> list = new List<GameObject>();
        for(int i = 0; i < parent.transform.childCount; i++)
        {
            GameObject child = parent.transform.GetChild(i).gameObject;
            searchGameObjectsByName(child, name, list);
        }
        return list;
    }

    public static void searchGameObjectsByName(GameObject parent, string name, List<GameObject> list)
    {
        if (parent.name.ToLower().Contains(name)) list.Add(parent);
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            GameObject child = parent.transform.GetChild(i).gameObject;
            searchGameObjectsByName(child, name, list);
        }
    }

}
