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

                // Load existing data or initialize new structure
                if (File.Exists(materialJsonFilePath))
                {
                    string materialJsonData = File.ReadAllText(materialJsonFilePath);
                    variationOutput = JsonUtility.FromJson<VariationOutput>(materialJsonData);
                }

                // Determine the next variation ID
                int nextVariationId = variationOutput.variations.Any() ? variationOutput.variations.Max(v => int.TryParse(v.id, out int id) ? id : 0) + 1 : 1;

                Variation newVariation = new Variation
                {
                    id = nextVariationId.ToString(),
                    materialsData = variationOutput.materialsData,
                    materialsResources = new List<MaterialResource>()
                };

                int materialIndex = 1;
                foreach (SkinnedMeshRenderer renderer in variationBuilder.currentModel.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        string materialName = $"{material.name}.mat"; // Ensure .mat extension

                        // Prepare materialsResources
                        newVariation.materialsResources.Add(new MaterialResource
                        {
                            number = materialIndex,
                            resources = new List<Resource> {
                        new Resource {
                            name = materialName.Replace(" (Instance)",""),
                            selected = true,
                            layoutId = 4,
                            loadFlags = "S", 
                            rttiValues = variationBuilder.currentMaterialResources
                        }
                    }
                        });

                        materialIndex++;
                    }
                }

                // Add the new variation
                variationOutput.variations.Add(newVariation);

                // Serialize and save the updated JSON
                string newJsonData = JsonConvert.SerializeObject(variationOutput, Formatting.Indented);
                File.WriteAllText(materialJsonFilePath, newJsonData);
                variationBuilder.currentMaterialResources.Clear();
                Debug.Log($"New variation saved for model: {variationBuilder.currentModelName} with ID: {newVariation.id}");
            }
            else
            {
                Debug.LogError("No model or model name specified for saving variation.");
            }
        }
    }
}