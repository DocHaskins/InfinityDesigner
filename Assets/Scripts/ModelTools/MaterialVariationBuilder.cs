using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MaterialVariationData
{
    public Dictionary<string, string> properties = new Dictionary<string, string>();
}

[System.Serializable]
public class MaterialVariationDatabase
{
    public Dictionary<string, MaterialVariationData> materials = new Dictionary<string, MaterialVariationData>();
}

public class MaterialVariationBuilder : MonoBehaviour
{
    public TextAsset jsonData;

    private MaterialVariationDatabase materialDatabase;

    void Start()
    {
        LoadJson();
        BuildVariations();
        SaveJson();
    }

    void LoadJson()
    {
        materialDatabase = JsonUtility.FromJson<MaterialVariationDatabase>(jsonData.text);
    }

    void BuildVariations()
    {
        foreach (var baseEntry in materialDatabase.materials)
        {
            string baseName = baseEntry.Key;
            var baseProperties = baseEntry.Value.properties;

            foreach (var variantEntry in materialDatabase.materials)
            {
                string variantName = variantEntry.Key;
                if (variantName.StartsWith(baseName) && variantName != baseName)
                {
                    PopulateMissingProperties(variantEntry.Value.properties, baseProperties);
                }
            }
        }
    }

    void PopulateMissingProperties(Dictionary<string, string> variant, Dictionary<string, string> baseProperties)
    {
        foreach (var baseProp in baseProperties)
        {
            if (!variant.ContainsKey(baseProp.Key))
            {
                variant.Add(baseProp.Key, baseProp.Value);
            }
        }
    }

    void SaveJson()
    {
        string json = JsonUtility.ToJson(materialDatabase, true);
        System.IO.File.WriteAllText(Application.dataPath + "/UpdatedMaterials.json", json);
    }
}
