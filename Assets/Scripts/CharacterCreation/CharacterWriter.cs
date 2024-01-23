using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using doppelganger;
using System;

public class CharacterWriter : MonoBehaviour
{
    public CharacterBuilder characterBuilder;
    public ModelWriter modelWriter;

    private Dictionary<string, string> sliderToSlotMapping = new Dictionary<string, string>()
    {
        {"ALL_head", "HEAD"},
        {"ALL_hair", "HEAD_PART_1"},
        {"ALL_hair_base", "HEAD_PART_1"},
        {"ALL_hair_1", "HEAD_PART_1"},
        {"ALL_hair_2", "HEAD_PART_1"},
        {"ALL_facial_hair", "HEAD_PART_1"},
        {"ALL_earrings", "HEAD_PART_1"},
        {"ALL_glasses", "HEAD_PART_1"},
        {"ALL_hat", "HAT"},
        {"ALL_mask", "HEADCOVER"},
        {"ALL_armor_helmet", "HEADCOVER"},
        {"ALL_hands", "HANDS"},
        {"ALL_gloves", "HANDS_PART_1"},
        {"ALL_rings", "HANDS_PART_1"},
        {"ALL_backpack", "TORSO_PART_1"},
        {"ALL_torso", "TORSO"},
        {"ALL_cape", "TORSO_PART_1"},
        {"ALL_necklace", "TORSO_PART_1"},
        {"ALL_torso_access", "TORSO_PART_1"},
        {"ALL_torso_extra", "TORSO_PART_1"},
        {"ALL_armor_torso", "TORSO_PART_1"},
        {"ALL_armor_torso_lowerleft", "ARMS_PART_1"},
        {"ALL_armor_torso_lowerright", "ARMS_PART_1"},
        {"ALL_armor_torso_upperleft", "ARMS_PART_1"},
        {"ALL_armor_torso_upperright", "ARMS_PART_1"},
        {"ALL_sleeve", "ARMS_PART_1"},
        {"ALL_legs", "LEGS"},
        {"ALL_leg_access", "LEGS_PART_1"},
        {"ALL_shoes", "LEGS_PART_1"},
        {"ALL_decals", "OTHER_PART_1"},
        {"ALL_tattoo", "OTHER_PART_1"},
        // Add other mappings here
    };
    private Dictionary<string, List<string>> fallbackSlots = new Dictionary<string, List<string>>()
{
    {"HEADCOVER", new List<string> { "HEADCOVER_PART_1", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"HAT", new List<string> { "HEADCOVER_PART_1", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"HEADCOVER_PART_1", new List<string> { "HEADCOVER", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"HEAD_PART_1", new List<string> { "HEADCOVER", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"TORSO_PART_1", new List<string> { "TORSO_PART_1", "TORSO_PART_2", "TORSO_PART_3", "TORSO_PART_4", "TORSO_PART_5", "TORSO_PART_6", "TORSO_PART_7", "TORSO_PART_8", "TORSO_PART_9", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"ARMS_PART_1", new List<string> { "ARMS_PART_2", "ARMS_PART_3", "ARMS_PART_4", "ARMS_PART_5", "ARMS_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"OTHER_PART_1", new List<string> { "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"LEGS_PART_1", new List<string> { "PANTS", "PANTS_PART_1", "LEGS_PART_2", "LEGS_PART_3", "LEGS_PART_4", "LEGS_PART_5", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"HANDS_PART_1", new List<string> { "HANDS_PART_2", "HANDS_PART_3", "HANDS_PART_4", "HANDS_PART_5", "HANDS_PART_6", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"HANDS", new List<string> { "HANDS_PART_1", "HANDS_PART_2", "HANDS_PART_3", "HANDS_PART_4", "HANDS_PART_5", "HANDS_PART_6", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
};

    void Start()
    {
        if (characterBuilder == null)
        {
            characterBuilder = FindObjectOfType<CharacterBuilder>();
            Debug.Log("CharacterBuilder found: " + (characterBuilder != null));
        }
    }

    public void WriteCurrentConfigurationToJson()
    {
        if (characterBuilder == null)
        {
            Debug.LogError("CharacterBuilder is null. Cannot write configuration.");
            return;
        }

        var sliderValues = characterBuilder.GetSliderValues();
        var currentlyLoadedModels = characterBuilder.GetCurrentlyLoadedModels();

        var slotPairs = new List<ModelData.SlotDataPair>();
        var usedSlots = new HashSet<string>();

        // Create a list to hold the ALL_head slot pair if found
        List<ModelData.SlotDataPair> allHeadSlotPairs = new List<ModelData.SlotDataPair>();

        int nextSlotUid = 101; // Initialize the next slot's uid to 101

        foreach (var slider in sliderValues)
        {
            Debug.Log($"Processing slider: {slider.Key} with value: {slider.Value}");

            if (slider.Value > 0)
            {
                if (currentlyLoadedModels.TryGetValue(slider.Key, out GameObject model))
                {
                    if (sliderToSlotMapping.TryGetValue(slider.Key, out string slotKey))
                    {
                        ModelData.SlotDataPair slotPair;

                        if (slotKey == "HEAD")
                        {
                            slotPair = CreateSlotDataPair(model, slotKey);
                            slotPair.slotData.slotUid = 100; // HEAD slotUid remains 100
                            slotPairs.Insert(0, slotPair); // Insert HEAD at the beginning
                            Debug.Log("Assigned HEAD slot for " + slider.Key);
                        }
                        else
                        {
                            string assignedSlot = GetAvailableSlot(slotKey, usedSlots);
                            if (assignedSlot == null)
                            {
                                Debug.LogWarning($"No available slots for {slider.Key}");
                                continue;
                            }

                            slotPair = CreateSlotDataPair(model, assignedSlot);
                            slotPair.slotData.slotUid = nextSlotUid++; // Assign and increment slotUid for non-HEAD slots
                            slotPairs.Add(slotPair);

                            Debug.Log($"Assigned {assignedSlot} slot for {slider.Key}");
                        }

                        usedSlots.Add(slotKey); // Mark this slot as used
                    }
                    else
                    {
                        Debug.LogWarning($"No slot mapping found for slider {slider.Key}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Model not found in currentlyLoadedModels for slider {slider.Key}");
                }
            }
        }

        // Insert the ALL_head slot pairs before other slot pairs
        slotPairs.InsertRange(1, allHeadSlotPairs);

        if (slotPairs.Count == 0)
        {
            Debug.LogWarning("No slot pairs were added. JSON will be empty.");
        }

        var outputData = new ModelData { slotPairs = slotPairs };
        string json = JsonConvert.SerializeObject(outputData, Formatting.Indented);
        string outputPath = Path.Combine(Application.streamingAssetsPath, "Output", "player_tpp_skeleton.json");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllText(outputPath, json);
        Debug.Log($"Character configuration saved to {outputPath}");

        // After writing the JSON, call ModelWriter to convert it to .model format
        if (modelWriter != null)
        {
            string modelOutputPath = Path.Combine(Application.streamingAssetsPath, "Output", "player_tpp_skeleton.model");
            modelWriter.ConvertJsonToModelFormat(outputPath, modelOutputPath);
        }
        else
        {
            Debug.LogError("ModelWriter not set in CharacterWriter.");
        }
    }

    private string GetAvailableSlot(string initialSlot, HashSet<string> usedSlots)
    {
        if (!usedSlots.Contains(initialSlot))
        {
            return initialSlot;
        }

        if (fallbackSlots.TryGetValue(initialSlot, out List<string> fallbacks))
        {
            foreach (var slot in fallbacks)
            {
                if (!usedSlots.Contains(slot))
                {
                    return slot;
                }
            }
        }
        return null; // Return null if no slots are available
    }

    private ModelData.SlotDataPair CreateSlotDataPair(GameObject model, string slotKey)
    {
        Debug.Log($"Creating SlotDataPair for {model.name} in slot {slotKey}");

        // Format the model name: remove "(Clone)" and add ".msh"
        string formattedModelName = FormatModelName(model.name);

        var slotData = new ModelData.SlotData
        {
            name = slotKey,
            models = new List<ModelData.ModelInfo>
        {
            new ModelData.ModelInfo
            {
                name = formattedModelName,
                materialsData = GetMaterialsDataFromStreamingAssets(formattedModelName), // Get materials from JSON in streaming assets
            }
        }
        };

        slotData.models[0].materialsResources = GetMaterialsResourcesFromModel(model);

        return new ModelData.SlotDataPair
        {
            key = slotKey,
            slotData = slotData
        };
    }

    private List<ModelData.MaterialData> GetMaterialsDataFromStreamingAssets(string modelName)
    {
        List<ModelData.MaterialData> materialsData = new List<ModelData.MaterialData>();

        // Remove the ".msh" extension from modelName
        modelName = Path.GetFileNameWithoutExtension(modelName);

        // Print original materials from streaming assets
        string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", modelName + ".json");
        if (File.Exists(materialJsonFilePath))
        {
            string materialJsonData = File.ReadAllText(materialJsonFilePath);
            ModelData.ModelInfo modelInfo = JsonUtility.FromJson<ModelData.ModelInfo>(materialJsonData);
            if (modelInfo != null && modelInfo.materialsData != null)
            {
                Debug.Log("Original Materials from Streaming Assets:");

                // Iterate through the materialsData in the JSON file
                foreach (var materialData in modelInfo.materialsData)
                {
                    // Ensure each material name ends with ".mat"
                    string materialName = materialData.name;
                    if (!materialName.EndsWith(".mat"))
                    {
                        materialName += ".mat";
                    }
                    Debug.Log($"Material Name: {materialName}");
                    materialsData.Add(new ModelData.MaterialData
                    {
                        number = materialData.number, // Use the correct material number
                        name = materialName
                    });
                }
            }
            else
            {
                Debug.LogError("Invalid JSON data or missing materialsData in the JSON file.");
            }
        }
        else
        {
            Debug.LogError("Material JSON file not found: " + materialJsonFilePath);
        }

        return materialsData;
    }

    private string FormatModelName(string modelName)
    {
        // Remove "(Clone)" from the model name
        string cleanName = modelName.Replace("(Clone)", "").Trim();

        // Add ".msh" extension
        return cleanName + ".msh";
    }


    private List<ModelData.MaterialResource> GetMaterialsResourcesFromModel(GameObject model)
    {
        List<ModelData.MaterialResource> materialsResources = new List<ModelData.MaterialResource>();
        var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();

        int materialNumber = 1; // Start numbering from 1

        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.sharedMaterials)
            {
                // Ensure each material name ends with ".mat"
                string materialName = material.name;
                if (!materialName.EndsWith(".mat"))
                {
                    materialName += ".mat";
                }

                materialsResources.Add(new ModelData.MaterialResource
                {
                    number = materialNumber,
                    resources = new List<ModelData.Resource>
                {
                    new ModelData.Resource
                    {
                        name = materialName
                    }
                }
                });

                materialNumber++; // Increment the material number
            }
        }

        return materialsResources;
    }
}