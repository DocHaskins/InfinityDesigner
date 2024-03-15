using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

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

    public class ModelChange
    {
        public Dictionary<int, MaterialChange> MaterialsByRenderer { get; set; } = new Dictionary<int, MaterialChange>();
    }

    public class MaterialChange
    {
        public string OriginalName { get; set; }
        public string NewName { get; set; }
        public List<RttiValue> TextureChanges { get; set; } = new List<RttiValue>();
    }

    [Serializable]
    public class ModelSlotLookup
    {
        public Dictionary<string, List<SlotInfo>> modelSlots = new Dictionary<string, List<SlotInfo>>();
    }

    [Serializable]
    public class SlotUidFrequency
    {
        public int slotUid;
        public int frequency;
    }

    [Serializable]
    public class SlotInfo
    {
        public int slotUid;
        public string name;
        public string filterText;
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
        public string filterText;
        public List<ModelInfo> models;
    }

    [Serializable]
    public class ModelInfo
    {
        public string name;
        public List<MaterialData> materialsData;
        public List<MaterialResource> materialsResources;
        
        [JsonIgnore]
        public List<Variation> variations;
    }

    [Serializable]
    public class AllFiltersModelData
    {
        public List<string> meshes;
    }

    [Serializable]
    public class MaterialsIndex
    {
        public List<string> materials;
    }

    [Serializable]
    public class VariationInfo
    {
        public string id;
        public List<MaterialData> materialsData;
        public List<MaterialResource> materialsResources;

        // Constructor to initialize lists
        public VariationInfo()
        {
            materialsData = new List<MaterialData>();
            materialsResources = new List<MaterialResource>();
        }
    }

    [Serializable]
    public class VariationOutput
    {
        public List<MaterialData> materialsData = new List<MaterialData>();
        public List<Variation> variations = new List<Variation>();
    }

    [Serializable]
    public class Variation
    {
        public string id;
        public List<MaterialData> materialsData = new List<MaterialData>();
        public List<MaterialResource> materialsResources = new List<MaterialResource>();
    }

    [Serializable]
    public class SlotModelData
    {
        public List<string> meshes;
    }

    [Serializable]
    public class MeshReferenceData
    {
        public List<MaterialData> materialsData;
        public List<Variation> variations = new List<Variation>();
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
        public List<Resource> resources = new List<Resource>();
    }

    [Serializable]
    public class Resource
    {
        public string name;
        public bool selected;
        public int layoutId;
        public string loadFlags;
        public List<RttiValue> rttiValues;

        public Resource()
        {
            rttiValues = new List<RttiValue>();
        }
    }

    [Serializable]
    public class RttiValue
    {
        public string name;
        public int type;
        public string val_str;
    }
}
