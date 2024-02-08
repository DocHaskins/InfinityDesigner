using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using Newtonsoft.Json;
using static ModelData;

namespace doppelganger
{
    public class VariationBuilder : MonoBehaviour
    {
        [Header("Managers")]
        public CharacterBuilder characterBuilder;
        public CharacterBuilder_InterfaceManager interfaceManager;

        [Header("Interface")]
        public GameObject modelInfoPanelPrefab;
        public GameObject variationMaterialDropdownPrefab;
        public GameObject currentModelInfoPanel;

        public GameObject currentModel;
        public string currentModelName;
        public bool isPanelOpen = false;
        public string openPanelSlotName = "";

        public List<string> GetAvailableMaterialNamesForSlot(int slotNumber, string slotName)
        {
            List<string> availableMaterials = new List<string>();
            string slotDataDirectory = Path.Combine(Application.streamingAssetsPath, "SlotData");
            string slotDataFileName = $"{slotName}.json";
            string slotDataPath = FindFileInDirectory(slotDataDirectory, slotDataFileName);

            if (!string.IsNullOrEmpty(slotDataPath))
            {
                string slotDataJson = File.ReadAllText(slotDataPath);
                SlotModelData slotModelData = JsonUtility.FromJson<SlotModelData>(slotDataJson);

                foreach (string modelNameWithPath in slotModelData.meshes)
                {
                    // Get the modelName without the file extension
                    string modelName = Path.GetFileNameWithoutExtension(modelNameWithPath);

                    string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", modelName + ".json");
                    if (File.Exists(materialJsonFilePath))
                    {
                        //Debug.Log("Material JSON file found: " + materialJsonFilePath);
                        string materialJsonData = File.ReadAllText(materialJsonFilePath);
                        ModelInfo modelInfo = JsonUtility.FromJson<ModelInfo>(materialJsonData);

                        if (modelInfo.variations != null)
                        {
                            foreach (VariationInfo variation in modelInfo.variations)
                            {
                                foreach (MaterialResource materialResource in variation.materialsResources)
                                {
                                    //Debug.Log("Checking material resource for slot " + slotNumber + ": " + materialResource.number);
                                    if (materialResource.number == slotNumber)
                                    {
                                        foreach (Resource resource in materialResource.resources)
                                        {
                                            string materialName = resource.name;
                                            string MaterialName = Path.GetFileNameWithoutExtension(materialName);
                                            //Debug.Log("Adding material: " + MaterialName);
                                            if (MaterialName != "null")
                                            {
                                                availableMaterials.Add(MaterialName);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Material JSON file not found: " + materialJsonFilePath);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Slot data file not found: " + slotDataPath);
            }
            availableMaterials.Sort();
            availableMaterials.Insert(0, "null.mat");
            return availableMaterials;
        }

        string FindFileInDirectory(string directory, string fileName)
        {
            foreach (string filePath in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(filePath).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    //Debug.Log("Slot data path: " + filePath);
                    return filePath;
                }
            }

            return null; // File not found
        }

        public void ToggleModelInfoPanel(string slotName)
        {
            Debug.Log($"ToggleModelInfoPanel: slotName: {slotName}");
            if (isPanelOpen && currentModelInfoPanel != null && openPanelSlotName == slotName)
            {
                Destroy(currentModelInfoPanel);
                currentModelInfoPanel = null;
                isPanelOpen = false;
            }
            else
            {
                if (currentModelInfoPanel != null)
                {
                    Destroy(currentModelInfoPanel);
                }
                OpenModelInfoPanel(slotName);
                isPanelOpen = true;
                openPanelSlotName = slotName;

            }
        }

        public void OpenModelInfoPanel(string slotName)
        {
            // Check if ModelInfoPanel already exists
            if (currentModelInfoPanel == null)
            {
                // Instantiate a new modelInfoPanelPrefab if it does not exist
                currentModelInfoPanel = Instantiate(modelInfoPanelPrefab, FindObjectOfType<Canvas>().transform, false);
                TextMeshProUGUI meshNameText = currentModelInfoPanel.transform.Find("MeshName").GetComponent<TextMeshProUGUI>();
                if (string.IsNullOrEmpty(slotName) || !characterBuilder.currentlyLoadedModels.TryGetValue(slotName, out GameObject currentModel))
                {
                    meshNameText.text = "Variation Builder";
                    return;
                }
            }
            // Always reset the name to a generic one since we are reusing the panel
            currentModelInfoPanel.name = "VariationInfoPanel";

            UpdateModelInfoPanelContent(slotName); // Call a method to update panel content
        }


        public void UpdateModelInfoPanel(string slotName)
        {
            //Debug.Log($"UpdateModelInfoPanel: slotName: {slotName}");
            GameObject modelInfoPanel = GameObject.Find("VariationInfoPanel");

            if (modelInfoPanel != null)
            {
                //Debug.Log("ModelInfoPanel found, updating content.");
                UpdateModelInfoPanelContent(slotName);
            }
        }

        private void UpdateModelInfoPanelContent(string slotName)
        {
            TextMeshProUGUI meshNameText = currentModelInfoPanel.transform.Find("MeshName").GetComponent<TextMeshProUGUI>();
            Transform materialSpawn = currentModelInfoPanel.transform.Find("VariationSubPanel/materialSpawn");

            // Clear existing materials to repopulate
            foreach (Transform child in materialSpawn)
            {
                GameObject.Destroy(child.gameObject);
            }

            if (string.IsNullOrEmpty(slotName) || !characterBuilder.currentlyLoadedModels.TryGetValue(slotName, out GameObject currentModel))
            {
                meshNameText.text = "Variation Builder";
                return; // Early return to avoid further processing
            }

            if (characterBuilder.currentlyLoadedModels.TryGetValue(slotName, out GameObject loadedModel))
            {
                this.currentModel = loadedModel;
                this.currentModelName = loadedModel.name;

                meshNameText.text = currentModel.name.Replace("(Clone)", " ");
                PopulateMaterialDropdowns(materialSpawn, loadedModel, slotName);
            }
        }

        void SetupDropdownWithMaterials(TMP_Dropdown tmpDropdown, string currentMaterialName, int slotNumber, string slotName)
        {
            List<string> additionalMaterialNames = GetAvailableMaterialNamesForSlot(slotNumber, slotName);

            tmpDropdown.ClearOptions();
            List<string> dropdownOptions = new List<string> { currentMaterialName };
            // Append additional materials ensuring no duplicates with the current material
            foreach (var name in additionalMaterialNames)
            {
                if (!dropdownOptions.Contains(name))
                {
                    dropdownOptions.Add(name);
                }
            }
            dropdownOptions.AddRange(additionalMaterialNames.Where(name => !dropdownOptions.Contains(name)));

            tmpDropdown.AddOptions(dropdownOptions);
            tmpDropdown.value = dropdownOptions.IndexOf(currentMaterialName); // Ensure the current material is selected
            tmpDropdown.RefreshShownValue();

            // Remove existing listeners to avoid duplicate calls
            tmpDropdown.onValueChanged.RemoveAllListeners();

            // Add a new listener
            tmpDropdown.onValueChanged.AddListener(index => {
                string selectedMaterialName = tmpDropdown.options[index].text;
                ApplyMaterialToSlot(currentModel, slotNumber, selectedMaterialName);
            });
        }

        public void UpdateMaterialDropdowns(string slotName)
        {
            if (!characterBuilder.currentlyLoadedModels.TryGetValue(slotName, out GameObject currentModel)) return;

            GameObject modelInfoPanel = GameObject.Find(slotName + "VariationInfoPanel");
            if (modelInfoPanel == null) return;

            Transform materialSpawn = modelInfoPanel.transform.Find("VariationSubPanel/materialSpawn");
            SkinnedMeshRenderer[] renderers = currentModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            int slotNumber = 1; // Start numbering slots from 1
            foreach (Transform dropdownTransform in materialSpawn)
            {
                TMP_Dropdown tmpDropdown = dropdownTransform.Find("Dropdown").GetComponent<TMP_Dropdown>();
                if (tmpDropdown == null) continue;

                // Assuming there is a direct relationship between the order of dropdowns and material slots
                int materialIndex = (slotNumber - 1) % renderers.Length;
                int rendererIndex = (slotNumber - 1) / renderers.Length;
                if (rendererIndex < renderers.Length && materialIndex < renderers[rendererIndex].sharedMaterials.Length)
                {
                    Material material = renderers[rendererIndex].sharedMaterials[materialIndex];
                    // Update the dropdown to reflect the new material name
                    SetupDropdownWithMaterials(tmpDropdown, material.name, slotNumber, slotName);
                }
                slotNumber++;
            }
        }

        void PopulateMaterialDropdowns(Transform materialSpawn, GameObject currentModel, string slotName)
        {
            SkinnedMeshRenderer[] renderers = currentModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int slotNumber = 1; // Initialize slotNumber correctly
            foreach (var renderer in renderers)
            {
                Material currentMaterial = renderer.sharedMaterials.Length > 0 ? renderer.sharedMaterials[0] : null;
                if (currentMaterial != null)
                {
                    GameObject dropdownGameObject = Instantiate(variationMaterialDropdownPrefab, materialSpawn);
                    TMP_Dropdown tmpDropdown = dropdownGameObject.GetComponentInChildren<TMP_Dropdown>();
                    List<string> additionalMaterialNames = GetAvailableMaterialNamesForSlot(slotNumber, slotName);
                    SetupDropdownWithMaterials(tmpDropdown, currentMaterial.name, slotNumber, slotName);

                    // Correctly capture slotNumber for the listener
                    int capturedSlotNumber = slotNumber;

                    // Inline listener logic to apply the selected material
                    tmpDropdown.onValueChanged.AddListener(delegate (int index) {
                        string selectedMaterialName = tmpDropdown.options[index].text;
                        //Debug.Log($"Dropdown Value Changed for Slot #{capturedSlotNumber}: {selectedMaterialName}");
                        ApplyMaterialToSlot(currentModel, capturedSlotNumber, selectedMaterialName);
                    });

                    TextMeshProUGUI nameText = dropdownGameObject.transform.Find("Name").GetComponent<TextMeshProUGUI>();
                    nameText.text = "Slot #" + slotNumber;

                    slotNumber++;
                }
            }
        }

        void ApplyMaterialToSlot(GameObject model, int slotNumber, string selectedMaterialName)
        {
            //Debug.Log($"Selected Material: {selectedMaterialName}");
            SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (slotNumber - 1 < renderers.Length)
            {
                var renderer = renderers[slotNumber - 1]; // Adjust based on how you map slotNumber to renderer index
                Material selectedMaterial = LoadMaterialByName(selectedMaterialName);

                if (selectedMaterial != null)
                {
                    //Debug.Log($"Attempting to apply material: {selectedMaterialName}");
                    Material[] materials = renderer.sharedMaterials;
                    if (materials.Length > 0)
                    {
                        materials[0] = selectedMaterial; // Assuming you're applying to the first material slot
                        renderer.sharedMaterials = materials;
                        //Debug.Log($"Material applied successfully to slot {slotNumber}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Material {selectedMaterialName} not found!");
                }
            }
            else
            {
                Debug.LogWarning($"Renderer for slot {slotNumber} not found!");
            }
        }

        Material LoadMaterialByName(string materialName)
        {
            string MaterialName = Path.GetFileNameWithoutExtension(materialName);
            Material loadedMaterial = Resources.Load<Material>("Materials/" + MaterialName);
            return loadedMaterial;
        }

        public void SaveNewVariation()
        {
            if (currentModel != null && !string.IsNullOrEmpty(currentModelName))
            {
                string currentlyLoadedModelName = Path.GetFileNameWithoutExtension(currentModelName).Replace("(Clone)","");
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
                    id = nextVariationId.ToString(), // Use incremental ID
                    materialsData = new List<MaterialData>(variationOutput.materialsData), // Copy from existing to maintain the list
                    materialsResources = new List<MaterialResource>()
                };

                int materialIndex = 1;
                foreach (SkinnedMeshRenderer renderer in currentModel.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        string materialName = $"{material.name}.mat"; // Ensure .mat extension

                        // Check if this materialData already exists, if not, add it
                        if (!newVariation.materialsData.Any(md => md.name == materialName))
                        {
                            newVariation.materialsData.Add(new MaterialData { number = materialIndex, name = materialName });
                        }

                        // Prepare materialsResources
                        newVariation.materialsResources.Add(new MaterialResource
                        {
                            number = materialIndex,
                            resources = new List<Resource> {
                        new Resource {
                            name = materialName,
                            selected = true,
                            layoutId = 4, // Example values
                            loadFlags = "S" // Example values
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

                Debug.Log($"New variation saved for model: {currentModelName} with ID: {newVariation.id}");
            }
            else
            {
                Debug.LogError("No model or model name specified for saving variation.");
            }
        }
    }
}
