#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using static ModelData;
using System;
using System.Linq;

public class JsonCreatorWindow : EditorWindow
{
    [MenuItem("Tools/Json Creator")]
    public static void ShowWindow()
    {
        GetWindow<JsonCreatorWindow>("Json Creator");
    }

    string selectedFolder = "";

    void OnGUI()
    {
        if (GUILayout.Button("Select Folder"))
        {
            selectedFolder = EditorUtility.OpenFolderPanel("Select Folder", "", "");
        }

        EditorGUILayout.Space();

        if (!string.IsNullOrEmpty(selectedFolder) && GUILayout.Button("Process Models"))
        {
            ProcessModelsInFolder(selectedFolder);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Organize JSONs"))
        {
            OrganizeJsonFiles();
        }
    }

    void OrganizeJsonFiles()
    {
        string jsonsDir = Path.Combine(Application.dataPath, "StreamingAssets/Jsons");
        string[] jsonFiles = Directory.GetFiles(jsonsDir, "*.json");

        Dictionary<string, Dictionary<string, List<string>>> modelsByCategoryAndGender = new Dictionary<string, Dictionary<string, List<string>>>()
    {
        {"Infected", new Dictionary<string, List<string>>()},
        {"Player", new Dictionary<string, List<string>>()},
        {"Man", new Dictionary<string, List<string>>()},
        {"Wmn", new Dictionary<string, List<string>>()},
        {"Child", new Dictionary<string, List<string>>()},
        {"Other", new Dictionary<string, List<string>>()}
    };

        Dictionary<string, List<string>> maleModels = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> femaleModels = new Dictionary<string, List<string>>();

        var categoryPrefixes = new Dictionary<string, string[]>
    {
        {"Infected", new[] {"zmb_", "sh_man_viral_", "viral_", "biter_", "sh_biter_", "man_zmb_", "wmn_zmb_"}},
        {"Player", new[] {"player_", "sh_player_", "chr_player_"}},
        {"Man", new[] {"man_", "sh_man_", "npc_man_", "sh_scan_man_", "multihead007_npc_carl_"}},
        {"Wmn", new[] {"sh_wmn_", "npc_wmn_", "sh_scan_wmn_", "wmn_", "sh_dlc_opera_wmn_", "nnpc_wmn_worker"}},
        {"Child", new[] {"child_", "sh_scan_kid_", "sh_scan_girl_", "sh_scan_boy_", "sh_npc_mia_young", "sh_chld_"}}
    };

        foreach (var file in jsonFiles)
        {
            try
            {
                string jsonData = File.ReadAllText(file);
                ModelData modelData = JsonConvert.DeserializeObject<ModelData>(jsonData);

                if (modelData.modelProperties == null || modelData.slotPairs == null)
                {
                    Debug.LogWarning($"Skipped file {file} due to missing properties or slotPairs.");
                    continue;
                }

                string gender = modelData.modelProperties.sex?.ToLower() ?? "other";
                string modelName = Path.GetFileNameWithoutExtension(file).ToLower();
                string category = DetermineModelCategory(modelName, categoryPrefixes);

                foreach (var slotPair in modelData.slotPairs)
                {
                    string filterText = string.IsNullOrEmpty(slotPair.slotData.filterText) ? "other" : slotPair.slotData.filterText.ToLower();
                    foreach (var model in slotPair.slotData.models)
                    {
                        AddModelToCategory(modelsByCategoryAndGender, category, gender, model.name, filterText);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing file {file}: {ex.Message}");
            }
        }

        foreach (var category in modelsByCategoryAndGender.Keys)
        {
            foreach (var gender in modelsByCategoryAndGender[category].Keys)
            {
                SortAndRemoveDuplicates(modelsByCategoryAndGender[category][gender]);
                SaveModelsByCategoryAndGender(category, gender, modelsByCategoryAndGender[category][gender]);
            }
        }

        Debug.Log("JSON files organized and saved.");
    }

    void AddModelToCategory(Dictionary<string, Dictionary<string, List<string>>> modelsByCategoryAndGender,
                        string category, string gender, string modelName, string filterText)
    {
        if (!modelsByCategoryAndGender.ContainsKey(category))
            modelsByCategoryAndGender[category] = new Dictionary<string, List<string>>();

        if (!modelsByCategoryAndGender[category].ContainsKey(gender))
            modelsByCategoryAndGender[category][gender] = new List<string>();

        if (!modelsByCategoryAndGender[category][gender].Contains(modelName))
            modelsByCategoryAndGender[category][gender].Add(modelName);
    }

    void AddModelToDictionary(Dictionary<string, List<string>> dict, string key, string modelName)
    {
        if (!dict.ContainsKey(key))
            dict[key] = new List<string>();

        if (!dict[key].Contains(modelName))
            dict[key].Add(modelName);
    }

    string DetermineModelCategory(string modelName, Dictionary<string, string[]> categoryPrefixes)
    {
        foreach (var category in categoryPrefixes)
        {
            if (category.Value.Any(prefix => modelName.StartsWith(prefix)))
                return category.Key;
        }
        return "Other";
    }

    void SortAndRemoveDuplicates(List<string> models)
    {
        models.Sort();
    }

    void SaveModelsByCategoryAndGender(string category, string gender, List<string> models)
    {
        string outputDir = Path.Combine(Application.dataPath, $"StreamingAssets/SlotData/{category}_{gender}");
        Directory.CreateDirectory(outputDir);

        string filePath = Path.Combine(outputDir, $"ALL.json");
        string jsonContent = JsonConvert.SerializeObject(new { meshes = models }, Formatting.Indented);
        File.WriteAllText(filePath, jsonContent);
    }

    void ProcessModelsInFolder(string folderPath)
    {
        string[] files = Directory.GetFiles(folderPath, "*.model", SearchOption.AllDirectories);

        foreach (string modelFile in files)
        {
            Debug.Log($"Processing model file: {modelFile}");
            string jsonData = File.ReadAllText(modelFile);
            JObject modelObject = JObject.Parse(jsonData);

            var processedData = ProcessModelData(modelObject);
            string outputJson = JsonConvert.SerializeObject(processedData, Newtonsoft.Json.Formatting.Indented);

            string outputDir = Path.Combine(Application.dataPath, "StreamingAssets/Jsons");
            Directory.CreateDirectory(outputDir);

            string outputFilePath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(modelFile) + ".json");
            File.WriteAllText(outputFilePath, outputJson);
            Debug.Log($"Processed data saved to: {outputFilePath}");
        }
    }

    ModelData ProcessModelData(JObject modelObject)
    {
        // Extract skeletonName from the preset object
        string skeletonName = modelObject["preset"]["skeletonName"]?.ToString();

        // Extract modelProperties from the data object
        JObject dataObject = modelObject["data"] as JObject;
        ModelProperties modelProperties = ExtractModelProperties(dataObject["properties"] as JArray);

        // Extract slotPairs directly from the root object
        List<SlotDataPair> slotPairs = ExtractSlotPairs(modelObject["slots"] as JArray);

        // Construct and return the ModelData object
        return new ModelData
        {
            skeletonName = skeletonName,
            modelProperties = modelProperties,
            slotPairs = slotPairs
        };
    }

    ModelProperties ExtractModelProperties(JArray propertiesArray)
    {
        ModelProperties properties = new ModelProperties();
        foreach (JObject prop in propertiesArray)
        {
            string propName = prop["name"]?.ToString();
            switch (propName)
            {
                case "class":
                    properties.@class = prop["value"]?.ToString();
                    break;
                case "race":
                    properties.race = prop["value"]?.ToString();
                    break;
                case "sex":
                    properties.sex = prop["value"]?.ToString();
                    break;
                    // Add other properties as needed
            }
        }
        return properties;
    }


    List<SlotDataPair> ExtractSlotPairs(JArray slotsArray)
    {
        var slotPairs = new List<SlotDataPair>();
        foreach (JObject slot in slotsArray)
        {
            SlotDataPair slotPair = new SlotDataPair
            {
                key = slot["name"]?.ToString(),
                slotData = new SlotData
                {
                    slotUid = (int)slot["slotUid"],
                    name = slot["name"]?.ToString(),
                    filterText = slot["filterText"]?.ToString(),
                    models = ExtractModels(slot["meshResources"]["resources"] as JArray)
                }
            };
            slotPairs.Add(slotPair);
        }
        return slotPairs;
    }

    List<ModelInfo> ExtractModels(JArray modelsArray)
    {
        var models = new List<ModelInfo>();
        foreach (JObject model in modelsArray)
        {
            ModelInfo modelInfo = new ModelInfo
            {
                name = model["name"]?.ToString(),
                materialsData = ExtractMaterialsData(model["materialsData"] as JArray),
                materialsResources = ExtractMaterialsResources(model["materialsResources"] as JArray)
            };
            models.Add(modelInfo);
        }
        return models;
    }

    List<MaterialData> ExtractMaterialsData(JArray materialsDataArray)
    {
        var materialsData = new List<MaterialData>();
        foreach (JObject materialDataObject in materialsDataArray)
        {
            var materialData = new MaterialData
            {
                number = (int)materialDataObject["number"],
                name = materialDataObject["name"]?.ToString()
            };
            materialsData.Add(materialData);
        }
        return materialsData;
    }

    List<MaterialResource> ExtractMaterialsResources(JArray materialsResourcesArray)
    {
        var materialsResources = new List<MaterialResource>();
        foreach (JObject materialResourceObject in materialsResourcesArray)
        {
            var materialResource = new MaterialResource
            {
                number = (int)materialResourceObject["number"],
                resources = ExtractResources(materialResourceObject["resources"] as JArray)
            };
            materialsResources.Add(materialResource);
        }
        return materialsResources;
    }

    List<Resource> ExtractResources(JArray resourcesArray)
    {
        var resources = new List<Resource>();
        foreach (JObject resourceObject in resourcesArray)
        {
            var resource = new Resource
            {
                name = resourceObject["name"]?.ToString(),
                selected = (bool)resourceObject["selected"],
                layoutId = (int)resourceObject["layoutId"],
                loadFlags = resourceObject["loadFlags"]?.ToString(),
                rttiValues = ExtractRttiValues(resourceObject["rttiValues"] as JArray)
            };
            resources.Add(resource);
        }
        return resources;
    }

    List<RttiValue> ExtractRttiValues(JArray rttiValuesArray)
    {
        var rttiValues = new List<RttiValue>();
        if (rttiValuesArray == null)
        {
            Debug.LogWarning("rttiValuesArray is null.");
            return rttiValues; // Returning an empty list as there's nothing to process.
        }

        foreach (JObject rttiValueObject in rttiValuesArray)
        {
            if (rttiValueObject == null)
            {
                Debug.LogWarning("rttiValueObject is null, skipping this iteration.");
                continue;
            }

            var rttiValue = new RttiValue
            {
                name = rttiValueObject["name"]?.ToString(),
                type = (int)(rttiValueObject["type"] ?? 0),
                val_str = rttiValueObject["val_str"]?.ToString()
            };
            rttiValues.Add(rttiValue);
        }
        return rttiValues;
    }
}
#endif