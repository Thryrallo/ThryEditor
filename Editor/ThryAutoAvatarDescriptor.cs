using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class AutoAvatarDescriptor : MonoBehaviour
{

    private static string[] BLEND_SHAPE_NAMES = new string[] { "sil", "pp", "ff", "th", "dd", "kk", "ch", "ss", "nn", "rr", "aa", "e", "ih", "oh", "ou" };
    private static Type avatarDescriptorType;
    private static bool hasVRCSdk = false;

    private enum AnimationSet { Male = 0, Female = 1, None = 2 };
    private enum LipSyncStyle { Default = 0, JawFlapBone = 1, JawFlapBlendShape = 2, VisemeBlendShape = 3 };

    private static FieldInfo viewPointPosInfo;
    private static FieldInfo animationSetInfo;
    private static FieldInfo lipSyncInfo;
    private static FieldInfo visemeMeshInfo;
    private static FieldInfo visemeBlendShapesInfo;

    [MenuItem("Thry/VRC/Auto setup Avatar Descriptor")]
    static void Init()
    {
        Debug.Log("has sdk: "+hasVRCSdk);
        if (hasVRCSdk)
        {
            GameObject parent = Selection.activeGameObject;
            if (parent == null) return;
            var descriptor = parent.GetComponent(avatarDescriptorType);
            if (descriptor != null)
            {
                autoFillDescriptor(parent, descriptor);
            }
            else
            {
                parent.AddComponent(avatarDescriptorType);
                Init();
            }
        }
    }

    [InitializeOnLoad]
    public class Startup
    {
        static Startup()
        {
            avatarDescriptorType = Type.GetType("VRCSDK2.VRC_AvatarDescriptor, VRCSDK2");
            hasVRCSdk = avatarDescriptorType != null;
            if (!hasVRCSdk) return;
            viewPointPosInfo = avatarDescriptorType.GetField("ViewPosition");
            animationSetInfo = avatarDescriptorType.GetField("Animations");
            lipSyncInfo = avatarDescriptorType.GetField("lipSync");
            visemeMeshInfo = avatarDescriptorType.GetField("VisemeSkinnedMesh");
            visemeBlendShapesInfo = avatarDescriptorType.GetField("VisemeBlendShapes");
        }
    }

    [InitializeOnLoadAttribute]
    public static class HierarchyMonitor
    {
        static HierarchyMonitor()
        {
            EditorApplication.hierarchyWindowChanged += OnHierarchyChanged;
        }

        static void OnHierarchyChanged()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) AutoAvatarDescriptor.OnHierarchyChange();
        }
    }

    static void OnHierarchyChange()
    {
        if (hasVRCSdk)
        {
            if (ThryConfig.GetConfig().vrchatAutoFillAvatarDescriptor)
            {
                GameObject parent = Selection.activeGameObject;
                if (parent == null) return;
                var descriptor = parent.GetComponent(avatarDescriptorType);
                if (descriptor != null)
                {
                    Vector3 ViewPosition = (Vector3)viewPointPosInfo.GetValue(descriptor);
                    if (ViewPosition.Equals(new Vector3(0, (float)1.6, (float)0.2)))
                        autoFillDescriptor(parent, descriptor);
                }
            }
        }
    }

    private static void autoFillDescriptor(GameObject parent, Component descriptor)
    {
        //set the viewpoint
        List<GameObject> eyeObjects = searchGameObjectsByName(parent, "eye");
        Vector3 viewPointPos = new Vector3();
        foreach (GameObject eyeO in eyeObjects) viewPointPos = vectorAddWeightedVector(viewPointPos, eyeO.transform.position, 1.0 / eyeObjects.Count);
        viewPointPos = vectorAddWeightedVector(viewPointPos, parent.transform.position, -1);
        viewPointPosInfo.SetValue(descriptor, viewPointPos);
        SkinnedMeshRenderer visemeMesh = (SkinnedMeshRenderer)visemeMeshInfo.GetValue(descriptor);
        string[] vismeBlendShapes;

        //set the default aniamtion set
        ThryConfig.Config config = ThryConfig.GetConfig();
        if (!config.vrchatForceFallbackAnimationSet)
        {
            int probabilityFemale = 0;
            int probabilityMale = 0;
            if (searchGameObjectsByName(parent, "breast").Count > 0) probabilityFemale += 2;
            else
            {
                List<GameObject> hair = searchGameObjectsByName(parent, "hair");
                if ((hair.Count == 1 && allChildsCount(hair[0]) > 20) || hair.Count > 15) probabilityFemale++;
                else switch (GenderGuesser.guessGender(parent.name))
                    {
                        case GenderGuesser.Gender.Female:
                            probabilityFemale++;
                            break;
                        case GenderGuesser.Gender.Male:
                            probabilityMale++;
                            break;
                    }
            }
            
            if (probabilityFemale > probabilityMale) setIntEnum(descriptor,animationSetInfo,AnimationSet.Female);
            else if (probabilityFemale < probabilityMale) setIntEnum(descriptor, animationSetInfo, AnimationSet.Male);
            else setIntEnum(descriptor, animationSetInfo, config.vrchatDefaultAnimationSetFallback);
        }
        else
        {
            setIntEnum(descriptor, animationSetInfo, config.vrchatDefaultAnimationSetFallback);
        }

        //set the viseme mesh
        if (visemeMesh == null)
        {
            SkinnedMeshRenderer body = null;
            SkinnedMeshRenderer head = null;
            foreach (Transform child in parent.transform)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = child.gameObject.GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer != null && child.name.ToLower() == "body") body = skinnedMeshRenderer;
                if (skinnedMeshRenderer != null && (child.name.ToLower() == "head" || child.name.ToLower() == "face")) head = skinnedMeshRenderer;

            }
            if (head != null | body != null) setIntEnum(descriptor, lipSyncInfo, LipSyncStyle.VisemeBlendShape);
            if (head != null) visemeMeshInfo.SetValue(descriptor, head);
            else if (body != null) visemeMeshInfo.SetValue(descriptor, body);
            visemeBlendShapesInfo.SetValue(descriptor, new string[15]);
        }
        visemeMesh = (SkinnedMeshRenderer)visemeMeshInfo.GetValue(descriptor);
        vismeBlendShapes = (string[])visemeBlendShapesInfo.GetValue(descriptor);
        //set the visemes
        if (visemeMesh != null && vismeBlendShapes.Length > 0)
        {
            Mesh mesh = visemeMesh.sharedMesh;
            for (int i = 0; i < BLEND_SHAPE_NAMES.Length; i++)
            {
                float closestScore = 0;
                for (int b = 0; b < mesh.blendShapeCount; b++)
                {
                    if (mesh.GetBlendShapeName(b).Contains(BLEND_SHAPE_NAMES[i]))
                    {
                        string compareBlendName = mesh.GetBlendShapeName(b).Replace("vrc.v_", "");
                        compareBlendName = compareBlendName.Replace("vrc.", "");
                        float score = ((float)BLEND_SHAPE_NAMES[i].Length / compareBlendName.Length);
                        if (score > closestScore)
                        {
                            vismeBlendShapes[i] = mesh.GetBlendShapeName(b);
                            closestScore = score;
                        }


                    }
                }
            }
            visemeBlendShapesInfo.SetValue(descriptor, vismeBlendShapes);
        }
    }

    public static void setIntEnum(System.Object obj, FieldInfo field, System.Object value)
    {
        Type enumType = field.GetValue(obj).GetType();
        field.SetValue(obj,Enum.Parse(enumType, "" + (int)value));
    }

    public static Vector3 vectorAddWeightedVector(Vector3 baseVec, Vector3 add, double weight)
    {
        return vectorAddWeightedVector(baseVec, add, (float)weight);
    }

    public static Vector3 vectorAddWeightedVector(Vector3 baseVec, Vector3 add, float weight)
    {
        return new Vector3(baseVec.x + add.x * weight, baseVec.y + add.y * weight, baseVec.z + add.z * weight);
    }

    public static int allChildsCount(GameObject parent)
    {
        int count = 0;
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            GameObject child = parent.transform.GetChild(i).gameObject;
            count += allChildsCount(child);
        }
        return count + 1;
    }

    public static List<GameObject> searchGameObjectsByName(GameObject parent, string name)
    {
        List<GameObject> list = new List<GameObject>();
        for (int i = 0; i < parent.transform.childCount; i++)
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
