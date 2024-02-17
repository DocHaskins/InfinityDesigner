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

        public static List<GameObject> allDropdowns = new List<GameObject>();
        private Dictionary<GameObject, bool> originalActiveStates = new Dictionary<GameObject, bool>();
        public List<RttiValue> currentMaterialResources = new List<RttiValue>();
        public Dictionary<string, List<RttiValue>> materialChanges = new Dictionary<string, List<RttiValue>>();

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
                    allDropdowns.Add(dropdownGameObject);
                    TMP_Dropdown tmpDropdown = dropdownGameObject.GetComponentInChildren<TMP_Dropdown>();
                    List<string> additionalMaterialNames = GetAvailableMaterialNamesForSlot(slotNumber, slotName);
                    SetupDropdownWithMaterials(tmpDropdown, currentMaterial.name, slotNumber, slotName);

                    Button optionsButton = dropdownGameObject.transform.Find("Button_Options").GetComponent<Button>();
                    TogglePanelVisibility toggleScript = optionsButton.GetComponent<TogglePanelVisibility>() ?? optionsButton.gameObject.AddComponent<TogglePanelVisibility>();
                    toggleScript.variationBuilder = this;
                    toggleScript.spawnPoint = materialSpawn;
                    toggleScript.dropdownGameObject = dropdownGameObject;
                    toggleScript.variationTextureSlotPanelPrefab = variationTextureSlotPanelPrefab;

                    GameObject panelGameObject = Instantiate(variationTextureSlotPanelPrefab, toggleScript.texturePrefabSpawnPoint.transform, false);
                    panelGameObject.SetActive(false); // Start with the panel disabled
                    toggleScript.panelGameObject = panelGameObject; // Adjust TogglePanelVisibility script to reference this new panel

                    // Ensure updating panel setup without toggling visibility when dropdown value changes
                    tmpDropdown.onValueChanged.AddListener((int selectedIndex) => {
                        string selectedMaterialName = tmpDropdown.options[selectedIndex].text;
                        Material selectedMaterial = GetSelectedMaterial(selectedMaterialName, currentModel);
                        if (selectedMaterial != null)
                        {
                            ApplyMaterialToSlot(currentModel, slotNumber, selectedMaterialName);
                            Debug.Log($"Dropdown Value Changed for Slot #{slotNumber}: {selectedMaterialName}");
                            // Update panel setup without toggling visibility
                            toggleScript.UpdatePanelSetup(panelGameObject, slotName, selectedMaterial, currentModel);
                        }
                    });

                    // Toggle visibility and ensure panel is updated when options button is clicked
                    optionsButton.onClick.AddListener(() => {
                        string selectedMaterialName = tmpDropdown.options[tmpDropdown.value].text; // Use current dropdown value
                        Material selectedMaterial = GetSelectedMaterial(selectedMaterialName, currentModel);
                        if (selectedMaterial != null)
                        {
                            // Optionally update the panel setup here if dropdown does not trigger on same selection
                            toggleScript.UpdatePanelSetup(panelGameObject, slotName, selectedMaterial, currentModel);
                            toggleScript.TogglePanel(slotName, selectedMaterial, currentModel); // Toggle visibility
                        }
                    });

                    TextMeshProUGUI nameText = dropdownGameObject.transform.Find("Button_Options/Name").GetComponent<TextMeshProUGUI>();
                    nameText.text = "" + slotNumber;
                    slotNumber++;
                }
            }
        }

        Material GetSelectedMaterial(string materialName, GameObject currentModel)
        {
            SkinnedMeshRenderer[] renderers = currentModel.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    // Log the material being checked
                    Debug.Log($"Checking material: {material.name}");

                    // Replace " (Instance)" with "" to handle instances of materials
                    if (material.name.Replace(" (Instance)", "") == materialName || material.name == materialName)
                    {
                        // Log when a matching material is found
                        Debug.Log($"Material '{materialName}' found in the current model.");
                        return material;
                    }
                }
            }

            // Log an error if the material is not found
            Debug.LogError($"Material '{materialName}' not found in the current model.");
            return null; // Return null if the material is not found
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
            SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (slotNumber - 1 < renderers.Length)
            {
                var renderer = renderers[slotNumber - 1];
                Material newMaterial = LoadMaterialByName(selectedMaterialName);

                if (newMaterial != null)
                {
                    // Directly apply the new material to the renderer
                    Material[] materials = renderer.sharedMaterials;
                    if (materials.Length > 0)
                    {
                        Material originalMaterial = materials[0]; // Capture the original material for change recording

                        // Record the change from original material to the new material
                        RecordMaterialChange(originalMaterial.name, newMaterial.name);

                        materials[0] = newMaterial; // Apply the new material to the first slot
                        renderer.sharedMaterials = materials; // Update the renderer's materials

                        // Find the VariationTextureSlotsPanel and update its currentMaterial
                        VariationTextureSlotsPanel panelScript = FindObjectOfType<VariationTextureSlotsPanel>();
                        if (panelScript != null)
                        {
                            panelScript.currentMaterial = newMaterial;
                            Debug.Log($"'{newMaterial}' sent to {panelScript}.");
                            panelScript.UpdatePanel();
                        }

                        Debug.Log($"Material '{selectedMaterialName}' applied successfully to slot {slotNumber}.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Material '{selectedMaterialName}' not found!");
                }
            }
            else
            {
                Debug.LogWarning($"Renderer for slot {slotNumber} not found!");
            }
        }

        void RecordMaterialChange(string originalMaterialName, string newMaterialName)
        {
            if (!materialChanges.ContainsKey(originalMaterialName))
            {
                materialChanges[originalMaterialName] = new List<RttiValue>();
            }

            RttiValue change = new RttiValue { name = "materialChange", type = 7, val_str = newMaterialName };
            materialChanges[originalMaterialName].Add(change);

            Debug.Log($"Recorded change for material '{originalMaterialName}' to '{newMaterialName}'");
        }

        // This method needs adjustment if it's still intended to use Material objects directly
        void UpdatePanelForMaterial(string materialName)
        {
            VariationTextureSlotsPanel panelScript = FindObjectOfType<VariationTextureSlotsPanel>();
            if (panelScript != null && panelScript.currentMaterial.name == materialName)
            {
                panelScript.UpdatePanel();
            }
        }

        public void RecordTextureChange(string slotName, Texture2D texture, Material material)
        {
            string textureName = texture != null ? texture.name : "None";
            string formattedSlotName = slotName.Replace("_", "") + "_0_tex";

            // Ensure you're using string identifiers consistently
            string materialName = material.name;
            if (!materialChanges.ContainsKey(materialName))
            {
                materialChanges[materialName] = new List<RttiValue>();
            }

            var existingEntry = materialChanges[materialName].FirstOrDefault(r => r.name == formattedSlotName);
            if (existingEntry != null)
            {
                existingEntry.val_str = textureName + ".png";
            }
            else
            {
                materialChanges[materialName].Add(new RttiValue { name = formattedSlotName, type = 7, val_str = textureName + ".png" });
            }

            Debug.Log($"Recorded texture change for material '{materialName}': {formattedSlotName} = {textureName}");
        }

        // Adjust LoadMaterialByName to ensure it correctly handles material loading
        Material LoadMaterialByName(string materialName)
        {
            // Ensure this method correctly loads materials by name, handling any necessary path adjustments
            Material loadedMaterial = Resources.Load<Material>("Materials/" + materialName);
            return loadedMaterial;
        }

    }
}
