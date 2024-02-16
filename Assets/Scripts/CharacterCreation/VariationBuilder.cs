using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ModelData;

namespace doppelganger
{
    public class VariationBuilder : MonoBehaviour
    {
        [Header("Managers")]
        public CharacterBuilder characterBuilder;
        public CharacterBuilder_InterfaceManager interfaceManager;

        [Header("Interface")]
        public GameObject modelInfoPanel;
        public TextMeshProUGUI meshNameText;
        public Transform materialSpawn;
        public GameObject modelInfoPanelPrefab;
        public GameObject variationMaterialDropdownPrefab;
        public GameObject currentModelInfoPanel;
        public GameObject variationTextureSlotPanelPrefab;

        public GameObject currentModel;
        public Transform currentSpawn;
        public string currentModelName;
        public bool isPanelOpen = false;
        public string openPanelSlotName = "";

        private Dictionary<GameObject, GameObject> dropdownToPanelMap = new Dictionary<GameObject, GameObject>();
        private Dictionary<GameObject, bool> originalActiveStates = new Dictionary<GameObject, bool>();
        public List<RttiValue> currentMaterialResources = new List<RttiValue>();

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
                            foreach (Variation variation in modelInfo.variations)
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

        public void UpdateModelInfoPanel(string slotName)
        {
            if (modelInfoPanel != null)
            {
                //Debug.Log("ModelInfoPanel found, updating content.");
                UpdateModelInfoPanelContent(slotName);
            }
        }


        public void UpdateModelInfoPanelContent(string slotName)
        {
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
                    //Debug.Log($"Created dropdowns {dropdownGameObject} at {materialSpawn}");
                    TMP_Dropdown tmpDropdown = dropdownGameObject.GetComponentInChildren<TMP_Dropdown>();
                    List<string> additionalMaterialNames = GetAvailableMaterialNamesForSlot(slotNumber, slotName);
                    SetupDropdownWithMaterials(tmpDropdown, currentMaterial.name, slotNumber, slotName);

                    Button optionsButton = dropdownGameObject.transform.Find("Button_Options").GetComponent<Button>();
                    TogglePanelVisibility toggleScript = optionsButton.GetComponent<TogglePanelVisibility>();
                    if (toggleScript == null)
                    {
                        toggleScript = optionsButton.gameObject.AddComponent<TogglePanelVisibility>();
                        toggleScript.spawnPoint = materialSpawn;
                        toggleScript.dropdownGameObject = dropdownGameObject;
                        toggleScript.variationTextureSlotPanelPrefab = variationTextureSlotPanelPrefab;
                    }
                    else
                    {
                        // Ensure spawnPoint is properly assigned
                        toggleScript.spawnPoint = materialSpawn;
                        toggleScript.dropdownGameObject = dropdownGameObject;
                        toggleScript.variationTextureSlotPanelPrefab = variationTextureSlotPanelPrefab;
                    }

                    optionsButton.onClick.AddListener(() => toggleScript.TogglePanel(slotName, currentMaterial, currentModel));

                    // Correctly capture slotNumber for the listener
                    int capturedSlotNumber = slotNumber;

                    // Inline listener logic to apply the selected material
                    tmpDropdown.onValueChanged.AddListener(delegate (int index) {
                        string selectedMaterialName = tmpDropdown.options[index].text;
                        Debug.Log($"Dropdown Value Changed for Slot #{capturedSlotNumber}: {selectedMaterialName}");
                        ApplyMaterialToSlot(currentModel, capturedSlotNumber, selectedMaterialName);
                    });

                    

                    TextMeshProUGUI nameText = dropdownGameObject.transform.Find("Name").GetComponent<TextMeshProUGUI>();
                    nameText.text = "Slot #" + slotNumber;

                    slotNumber++;
                }
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
                VariationTextureSlotsPanel panelScript = FindObjectOfType<VariationTextureSlotsPanel>(); // Find the active panel in the scene
                if (panelScript != null)
                {
                    panelScript.currentMaterial = selectedMaterial;
                    panelScript.UpdatePanel();
                }
            }
            else
            {
                Debug.LogWarning($"Renderer for slot {slotNumber} not found!");
            }
            
        }

        public void RecordTextureChange(string slotName, Texture2D texture, Material material)
        {
            string textureName = texture != null ? texture.name : "None";
            string formattedSlotName = slotName.Replace("_", "") + "_0_tex";

            // Ensure only one entry per slotName-material combination
            var existingEntry = currentMaterialResources.FirstOrDefault(r => r.name == formattedSlotName);
            if (existingEntry != null)
            {
                // Update existing entry
                existingEntry.val_str = textureName + ".png";
                Debug.Log($"Updated {formattedSlotName} with new texture: {textureName}");
            }
            else
            {
                // Add new entry if not exist
                currentMaterialResources.Add(new RttiValue
                {
                    name = formattedSlotName,
                    type = 7, // Assuming type 7 is for textures
                    val_str = textureName + ".png"
                });
                Debug.Log($"Added new texture entry: {formattedSlotName} = {textureName}");
            }
        }

        Material LoadMaterialByName(string materialName)
        {
            string MaterialName = Path.GetFileNameWithoutExtension(materialName);
            Material loadedMaterial = Resources.Load<Material>("Materials/" + MaterialName);
            return loadedMaterial;
        }

        
    }
}
