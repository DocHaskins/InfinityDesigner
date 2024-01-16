using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BoneDictionaryEntry
{
    public string boneName;
    public Transform boneTransform;
}

public class PrefabSkeletonMapper : MonoBehaviour
{
    [SerializeField]
    private List<string> bonesToCache = new List<string>()
    {
        "pelvis", "l_thigh", "l_calf", "l_foot", "r_thigh", "r_calf", "r_foot",
        "spine", "spine1", "spine2", "spine3", "l_clavicle", "l_upperarm",
        "l_forearm", "l_hand", "neck", "head", "r_clavicle", "r_upperarm",
        "r_forearm", "r_hand"
    };

    [SerializeField] private List<BoneDictionaryEntry> serializedBoneDictionary;

    private Dictionary<string, Transform> boneDictionary;

    void Awake()
    {
        boneDictionary = new Dictionary<string, Transform>();
        foreach (var entry in serializedBoneDictionary)
        {
            if (entry.boneTransform != null)
            {
                boneDictionary.Add(entry.boneName, entry.boneTransform);
            }
        }
    }

    public void CacheBones()
    {
        Transform armature = transform.Find("Armature");
        if (armature != null)
        {
            CacheBonesRecursive(armature);
        }
    }

    public Dictionary<string, Transform> BoneDictionary
    {
        get { return boneDictionary; }
    }

    private void CacheBonesRecursive(Transform parent)
    {
        if (parent == null) return;

        foreach (Transform child in parent)
        {
            if (child == null) continue;

            if (bonesToCache.Contains(child.name) && !boneDictionary.ContainsKey(child.name))
            {
                boneDictionary.Add(child.name, child);
                serializedBoneDictionary.Add(new BoneDictionaryEntry { boneName = child.name, boneTransform = child });
            }
            CacheBonesRecursive(child); // Recursive call
        }
    }
}