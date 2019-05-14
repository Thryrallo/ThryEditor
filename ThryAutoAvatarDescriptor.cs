using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AutoAvatarDescriptor : MonoBehaviour {

    private static string[] BLEND_SHAPE_NAMES = new string[] { "sil", "pp", "ff", "th", "dd", "kk", "ch", "ss", "nn", "rr", "aa", "e", "ih", "oh", "ou" };

    [MenuItem("Thry/Auto setup Avatar Descriptor")]
    static void Init()
    {
        GameObject parent = Selection.activeGameObject;
        VRCSDK2.VRC_AvatarDescriptor descriptor = (VRCSDK2.VRC_AvatarDescriptor)parent.GetComponent(typeof(VRCSDK2.VRC_AvatarDescriptor));
        if (descriptor != null)
        {
            if (parent != null && descriptor.VisemeSkinnedMesh==null)
            {
                SkinnedMeshRenderer body = null;
                SkinnedMeshRenderer head = null;
                foreach (Transform child in parent.transform)
                {
                    SkinnedMeshRenderer skinnedMeshRenderer = child.gameObject.GetComponent<SkinnedMeshRenderer>();
                    if (skinnedMeshRenderer != null && child.name.ToLower() == "body") body = skinnedMeshRenderer;
                    if (skinnedMeshRenderer != null && (child.name.ToLower() == "head" || child.name.ToLower() == "face")) head = skinnedMeshRenderer;

                }
                if (head!=null | body!=null) descriptor.lipSync = VRCSDK2.VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;
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
                    for (int b=0;b< mesh.blendShapeCount; b++)
                    {
                        if (mesh.GetBlendShapeName(b).Contains(BLEND_SHAPE_NAMES[i]))
                        {
                            string compareBlendName = mesh.GetBlendShapeName(b).Replace("vrc.", "");
                            float score = ((float)BLEND_SHAPE_NAMES[i].Length / compareBlendName.Length);
                            if(score>closestScore)
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
}
