using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using static ModelData;
using System;

namespace doppelganger
{
    public class VariationWriter : MonoBehaviour
    {
        private VariationBuilder variationBuilder;
        public CharacterBuilder_InterfaceManager interfaceManager;

        private void Awake()
        {
            variationBuilder = FindObjectOfType<VariationBuilder>();
        }

        public void SaveNewVariation()
        {
            Debug.Log("[SaveNewVariation] Saving new variation started.");

            if (variationBuilder.currentModel != null && !string.IsNullOrEmpty(variationBuilder.currentModelName))
            {
                string currentlyLoadedModelName = Path.GetFileNameWithoutExtension(variationBuilder.currentModelName).Replace("(Clone)", "");
                string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", $"{currentlyLoadedModelName}.json");

                Debug.Log($"[SaveNewVariation] Current model name: {currentlyLoadedModelName}");
                Debug.Log($"[SaveNewVariation] Material JSON file path: {materialJsonFilePath}");

                VariationOutput variationOutput = new VariationOutput();

                if (File.Exists(materialJsonFilePath))
                {
                    string materialJsonData = File.ReadAllText(materialJsonFilePath);
                    Debug.Log($"[SaveNewVariation] Material JSON data loaded.");
                    variationOutput = JsonConvert.DeserializeObject<VariationOutput>(materialJsonData);
                }
                else
                {
                    Debug.LogWarning($"[SaveNewVariation] Material JSON file not found.");
                }

                int nextVariationId = variationOutput.variations.Any() ? variationOutput.variations.Max(v => int.TryParse(v.id, out int id) ? id : 0) + 1 : 1;
                Debug.Log($"[SaveNewVariation] Next variation ID: {nextVariationId}");

                Variation newVariation = new Variation
                {
                    id = nextVariationId.ToString(),
                    materialsData = variationOutput.materialsData,
                    materialsResources = new List<MaterialResource>()
                };

                Debug.Log($"[SaveNewVariation] Preparing new variation with ID: {newVariation.id}");

                // Log all current modelSpecificChanges
                foreach (var kvp in variationBuilder.modelSpecificChanges)
                {
                    string modelChangesJson = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented);
                    Debug.Log($"[SaveNewVariation] ModelSpecificChanges for Key: {kvp.Key}, Detailed Changes: \n{modelChangesJson}");
                }

                foreach (var materialData in variationOutput.materialsData)
                {
                    Debug.Log($"[SaveNewVariation] Processing MaterialData for material {materialData.name} with index {materialData.number}");
                    MaterialResource materialResource = new MaterialResource
                    {
                        number = materialData.number,
                        resources = new List<Resource>()
                    };

                    Debug.Log($"[SaveNewVariation] Processing material data: {materialData.name} with index {materialData.number}");

                    if (variationBuilder.modelSpecificChanges.TryGetValue(currentlyLoadedModelName, out ModelChange materialChanges) &&
                        materialChanges.MaterialsByRenderer.TryGetValue(materialData.number - 1, out MaterialChange materialChange)) // Adjusting for zero-based indexing
                    {
                        List<RttiValue> validRttiValues = new List<RttiValue>();

                        foreach (var textureChange in materialChange.TextureChanges)
                        {
                            RttiValue newRttiValue = new RttiValue { name = textureChange.name, type = textureChange.type, val_str = textureChange.val_str };
                            validRttiValues.Add(newRttiValue);
                        }

                        string materialNameWithExtension = materialChange.NewName.EndsWith(".mat") ? materialChange.NewName : materialChange.NewName + ".mat";
                        Resource resource = new Resource
                        {
                            name = materialNameWithExtension,
                            selected = true,
                            layoutId = 4,
                            loadFlags = "S",
                            rttiValues = validRttiValues
                        };

                        materialResource.resources.Add(resource);
                        Debug.Log($"[SaveNewVariation] Added new resource to MaterialResource: {resource.name} with changes.");
                    }
                    else
                    {
                        Resource fallbackResource = new Resource
                        {
                            name = materialData.name,
                            selected = true,
                            layoutId = 4,
                            loadFlags = "S",
                            rttiValues = new List<RttiValue>()
                        };

                        materialResource.resources.Add(fallbackResource);
                        Debug.Log($"[SaveNewVariation] Added fallback resource to MaterialResource: {fallbackResource.name}.");
                    }

                    newVariation.materialsResources.Add(materialResource);
                }

                variationOutput.variations.Add(newVariation);

                string newJsonData = JsonConvert.SerializeObject(variationOutput, Formatting.Indented);
                File.WriteAllText(materialJsonFilePath, newJsonData);
                Debug.Log($"New variation created on slider for model: {variationBuilder.currentModel.name} on slot: {interfaceManager.currentSlider}");
                if (variationOutput.variations != null)
                {
                    interfaceManager.UpdateVariationSlider(interfaceManager.currentSlider, variationOutput.variations.Count);
                    interfaceManager.SetVariationSliderValue(interfaceManager.currentSlider, nextVariationId);
                }
                Debug.Log($"New variation saved for model: {currentlyLoadedModelName} with ID: {newVariation.id}");

                Debug.Log($"[SaveNewVariation] New variation {newVariation.id} saved with the following details:");
                foreach (var material in newVariation.materialsResources)
                {
                    Debug.Log($"Material {material.number}: {material.resources.First().name}");
                }
            }
            else
            {
                Debug.LogError("No model or model name specified for saving variation.");
            }
        }
    }
}