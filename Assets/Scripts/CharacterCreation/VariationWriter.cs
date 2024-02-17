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

        public void SaveNewVariation()
        {
            variationBuilder = FindObjectOfType<VariationBuilder>();
            if (variationBuilder.currentModel != null && !string.IsNullOrEmpty(variationBuilder.currentModelName))
            {
                string currentlyLoadedModelName = Path.GetFileNameWithoutExtension(variationBuilder.currentModelName).Replace("(Clone)", "");
                string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", $"{currentlyLoadedModelName}.json");
                VariationOutput variationOutput = new VariationOutput();

                if (File.Exists(materialJsonFilePath))
                {
                    string materialJsonData = File.ReadAllText(materialJsonFilePath);
                    variationOutput = JsonUtility.FromJson<VariationOutput>(materialJsonData);
                }

                int nextVariationId = variationOutput.variations.Any() ? variationOutput.variations.Max(v => int.TryParse(v.id, out int id) ? id : 0) + 1 : 1;

                Variation newVariation = new Variation
                {
                    id = nextVariationId.ToString(),
                    materialsData = variationOutput.materialsData, // Keep existing materials data
                    materialsResources = new List<MaterialResource>()
                };

                foreach (var materialChange in variationBuilder.materialChanges)
                {
                    string materialName = materialChange.Key;
                    List<RttiValue> changes = materialChange.Value;

                    // Prepare the resources list by including only texture changes
                    List<Resource> resources = new List<Resource>{
                new Resource{
                    name = materialName + ".mat",
                    selected = true,
                    layoutId = 4,
                    loadFlags = "S",
                    rttiValues = changes.Where(change => change.name.Contains("_tex")).ToList() // Filter for texture changes
                }
            };

                    newVariation.materialsResources.Add(new MaterialResource
                    {
                        number = newVariation.materialsResources.Count + 1,
                        resources = resources
                    });
                }

                // Merge changes with existing variations before adding the new variation
                MergeChangesWithExisting(newVariation, variationOutput);

                // Optionally, the new variation could still be added if there are unique changes that were not merged
                variationOutput.variations.Add(newVariation);

                string newJsonData = JsonConvert.SerializeObject(variationOutput, Formatting.Indented);
                File.WriteAllText(materialJsonFilePath, newJsonData);

                Debug.Log($"New variation saved for model: {currentlyLoadedModelName} with ID: {newVariation.id}");
                // Consider selectively clearing recorded changes
                // variationBuilder.materialChanges.Clear();
            }
            else
            {
                Debug.LogError("No model or model name specified for saving variation.");
            }
        }

        private void MergeChangesWithExisting(Variation newVariation, VariationOutput variationOutput)
        {
            foreach (var newResource in newVariation.materialsResources)
            {
                foreach (var existingVariation in variationOutput.variations)
                {
                    foreach (var existingResource in existingVariation.materialsResources)
                    {
                        if (existingResource.resources.Any(r => r.name == newResource.resources.First().name))
                        {
                            // Found an existing resource that matches the new one; merge changes
                            var existing = existingResource.resources.First(r => r.name == newResource.resources.First().name);
                            foreach (var newValue in newResource.resources.First().rttiValues)
                            {
                                if (!existing.rttiValues.Any(v => v.name == newValue.name))
                                {
                                    existing.rttiValues.Add(newValue); // Add new changes
                                }
                                else
                                {
                                    // Update existing value if necessary
                                    var existingValue = existing.rttiValues.First(v => v.name == newValue.name);
                                    existingValue.val_str = newValue.val_str;
                                }
                            }
                            return; // Stop processing as we've merged this resource
                        }
                    }
                }
            }

            // If no existing resources matched, this is a new change, so add it directly
            variationOutput.variations.Last().materialsResources.Add(newVariation.materialsResources.First());
        }
    }
}