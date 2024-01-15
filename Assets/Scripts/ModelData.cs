using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ModelData
{
    public string skeletonName;
    public ModelProperties modelProperties;
    public List<SlotDataPair> slotPairs;

    public Dictionary<string, SlotData> GetSlots()
    {
        Dictionary<string, SlotData> slots = new Dictionary<string, SlotData>();
        foreach (var pair in slotPairs)
        {
            if (!slots.ContainsKey(pair.key))
            {
                slots.Add(pair.key, pair.slotData);
            }
            else
            {
                Debug.LogWarning($"Duplicate slot key '{pair.key}' found in JSON. Skipping duplicate entry.");
            }
        }
        return slots;
    }

    [Serializable]
    public class ModelProperties
    {
        public string @class;
        public string race;
        public string sex;
    }

    [Serializable]
    public class SlotDataPair
    {
        public string key;
        public SlotData slotData;
    }

    [Serializable]
    public class SlotData
    {
        public int slotUid;
        public string name;
        public List<ModelInfo> models;
    }

    [Serializable]
    public class SkeletonData
    {
        public List<SkeletonBoneData> bones;
    }

    [Serializable]
    public class SkeletonBoneData
    {
        public string boneName;
        public string[] position;
        public string[] rotation;
    }

    [Serializable]
    public class BoneData
    {
        public string boneName;
        public string[] position;
        public string[] rotation;
    }

    [Serializable]
    public class ModelInfo
    {
        public string name;
        public List<MaterialData> materialsData;
        public List<MaterialResource> materialsResources;
        public List<BoneData> bones;
    }

    [Serializable]
    public class MaterialData
    {
        public int number;
        public string name;
    }

    [Serializable]
    public class MaterialResource
    {
        public int number;
        public List<Resource> resources;
    }

    [Serializable]
    public class Resource
    {
        public string name;
        public List<RttiValue> rttiValues;
    }

    [Serializable]
    public class RttiValue
    {
        public string name;
        public int type;
        public string val_str;
    }
}
