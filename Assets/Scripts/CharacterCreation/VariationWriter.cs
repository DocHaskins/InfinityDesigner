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

        private void Awake()
        {
            variationBuilder = FindObjectOfType<VariationBuilder>();
        }

        public void SaveNewVariation()
        {
            if (variationBuilder.currentModel != null && !string.IsNullOrEmpty(variationBuilder.currentModelName))
            {
                string currentlyLoadedModelName = Path.GetFileNameWithoutExtension(variationBuilder.currentModelName).Replace("(Clone)", "");
                string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", $"{currentlyLoadedModelName}.json");
                VariationOutput variationOutput = new VariationOutput();

                if (File.Exists(materialJsonFilePath))
                {
                    string materialJsonData = File.ReadAllText(materialJsonFilePath);
                    variationOutput = JsonConvert.DeserializeObject<VariationOutput>(materialJsonData);
                }

                int nextVariationId = variationOutput.variations.Any() ? variationOutput.variations.Max(v => int.TryParse(v.id, out int id) ? id : 0) + 1 : 1;

                Variation newVariation = new Variation
                {
                    id = nextVariationId.ToString(),
                    materialsData = variationOutput.materialsData, // Assuming materialsData is correct and should not change
                    materialsResources = new List<MaterialResource>()
                };

                // Correctly handle material and texture changes
                if (variationBuilder.modelSpecificChanges.TryGetValue(currentlyLoadedModelName, out ModelChange materialChangesForModel))
                {
                    foreach (var materialData in variationOutput.materialsData)
                    {
                        Resource newResource = new Resource
                        {
                            name = materialData.name, // Default name
                            selected = true,
                            layoutId = 4,
                            loadFlags = "S",
                            rttiValues = new List<RttiValue>() // Initialize with empty list for textures
                        };

                        // If there's a material change for this slot, update the resource name and textures
                        if (materialChangesForModel.Materials.TryGetValue(materialData.number, out MaterialChange materialChange))
                        {
                            newResource.name = materialChange.MaterialName; // Update name if changed
                            newResource.rttiValues = materialChange.TextureChanges; // Include texture changes
                        }

                        // Add this resource to the materialsResources list for the new variation
                        MaterialResource materialResource = new MaterialResource
                        {
                            number = materialData.number,
                            resources = new List<Resource> { newResource }
                        };

                        newVariation.materialsResources.Add(materialResource);
                    }
                }
                else
                {
                    // No material changes detected, populate materialsResources with default data
                    foreach (var materialData in variationOutput.materialsData)
                    {
                        newVariation.materialsResources.Add(new MaterialResource
                        {
                            number = materialData.number,
                            resources = new List<Resource>
                        {
                            new Resource
                            {
                                name = materialData.name,
                                selected = true,
                                layoutId = 4,
                                loadFlags = "S",
                                rttiValues = new List<RttiValue>() // Initialize without changes
                            }
                        }
                        });
                    }
                }

                variationOutput.variations.Add(newVariation);

                // Use Newtonsoft.Json for serializing the updated data
                string newJsonData = JsonConvert.SerializeObject(variationOutput, Formatting.Indented);
                File.WriteAllText(materialJsonFilePath, newJsonData);

                Debug.Log($"New variation saved for model: {currentlyLoadedModelName} with ID: {newVariation.id}");
            }
            else
            {
                Debug.LogError("No model or model name specified for saving variation.");
            }
        }
    }
}