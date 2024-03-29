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
                    materialsData = variationOutput.materialsData,
                    materialsResources = new List<MaterialResource>()
                };

                // Log all current modelSpecificChanges
                foreach (var kvp in variationBuilder.modelSpecificChanges)
                {
                    string modelChangesJson = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented);
                    Debug.Log($"ModelSpecificChanges for Key: {kvp.Key}, Detailed Changes: \n{modelChangesJson}");
                }

                foreach (var materialData in variationOutput.materialsData)
                {
                    MaterialResource materialResource = new MaterialResource
                    {
                        number = materialData.number,
                        resources = new List<Resource>()
                    };

                    // Check if there's a change for the current material slot
                    if (variationBuilder.modelSpecificChanges.TryGetValue(currentlyLoadedModelName, out ModelChange materialChanges) &&
                        materialChanges.MaterialsByRenderer.TryGetValue(materialData.number - 1, out MaterialChange materialChange)) // Adjusting for zero-based indexing
                    {
                        // Ensure .mat extension is added if missing
                        string materialNameWithExtension = materialChange.NewName.EndsWith(".mat") ? materialChange.NewName : $"{materialChange.NewName}.mat";

                        Resource resource = new Resource
                        {
                            name = materialNameWithExtension,
                            selected = true,
                            layoutId = 4,
                            loadFlags = "S",
                            rttiValues = materialChange.TextureChanges
                        };

                        materialResource.resources.Add(resource);
                    }
                    else
                    {
                        // Fallback to using the original material name from materialsData
                        Resource fallbackResource = new Resource
                        {
                            name = materialData.name,
                            selected = true,
                            layoutId = 4,
                            loadFlags = "S",
                            rttiValues = new List<RttiValue>() // No changes
                        };

                        materialResource.resources.Add(fallbackResource);
                    }

                    newVariation.materialsResources.Add(materialResource);
                }

                variationOutput.variations.Add(newVariation);

                string newJsonData = JsonConvert.SerializeObject(variationOutput, Formatting.Indented);
                File.WriteAllText(materialJsonFilePath, newJsonData);
                Debug.Log($"New variation created on slider for model: {variationBuilder.currentModel.name} on slot: {interfaceManager.currentSlider}");
                interfaceManager.CreateOrUpdateVariationSlider(interfaceManager.currentSlider, variationBuilder.currentModel.name);
                Debug.Log($"New variation saved for model: {currentlyLoadedModelName} with ID: {newVariation.id}");
            }
            else
            {
                Debug.LogError("No model or model name specified for saving variation.");
            }
        }
    }
}