using UnityEngine;
using System.Collections.Generic;

public class ParentSkeleton : MonoBehaviour
{
    public static readonly HashSet<string> IncludedBones = new HashSet<string>
{
    "pelvis", "spine", "spine1", "spine2", "spine3",
    "neck", "neck1", "neck2", "head", "r_eye", "l_eye",
    "l_clavicle", "l_upperarm", "l_forearm", "l_hand",
    "r_clavicle", "r_upperarm", "r_forearm", "r_hand",
    "l_thigh", "r_thigh", "l_calf", "r_calf",
    "l_foot", "r_foot", "l_sole_helper", "r_sole_helper"
};
    
    [System.Serializable]
    public class BoneData
    {
        public Transform boneTransform;
        // Additional bone data can be added if needed
    }

    private List<BoneData> serializedBones = new List<BoneData>();

    void Start()
    {
        SerializeBones();
    }

    public void SerializeBones()
    {
        Transform pelvis = transform.Find("pelvis");
        if (pelvis != null)
        {
            AddBoneData(pelvis, includeChildren: true);
        }
    }

    public void AddBoneData(Transform bone, bool includeChildren)
    {
        if (bone.name == "head")
        {
            includeChildren = false; // Do not include children of 'head'
        }

        if (IncludedBones.Contains(bone.name))
        {
            serializedBones.Add(new BoneData
            {
                boneTransform = bone
            });
        }

        if (includeChildren)
        {
            foreach (Transform child in bone)
            {
                AddBoneData(child, includeChildren);
            }
        }
    }

    // Public method to access the bone data
    public List<BoneData> GetBoneData()
    {
        return serializedBones;
    }
}