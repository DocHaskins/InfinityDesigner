using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
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
        public Material CurrentlySelectedMaterial { get; private set; }
        public static List<GameObject> allDropdowns = new List<GameObject>();
        private Dictionary<GameObject, bool> originalActiveStates = new Dictionary<GameObject, bool>();
        public List<RttiValue> currentMaterialResources = new List<RttiValue>();
        public Dictionary<string, List<RttiValue>> materialChanges = new Dictionary<string, List<RttiValue>>();
        public Dictionary<string, ModelChange> modelSpecificChanges = new Dictionary<string, ModelChange>();
        public Dictionary<int, VariationTextureSlotsPanel> slotToPanelMap = new Dictionary<int, VariationTextureSlotsPanel>();

        public List<string> GetAvailableMaterialNamesForRenderer(SkinnedMeshRenderer renderer, string slotName)
        {
            // Initial list with "null.mat" to represent a transparent material intentionally.
            List<string> availableMaterials = new List<string>() { "null.mat" };

            HashSet<string> uniqueMaterials = new HashSet<string>();
            string slotDataDirectory = Path.Combine(Application.streamingAssetsPath, "SlotData");
            string slotDataFileName = $"{slotName}.json";
            string slotDataPath = FindFileInDirectory(slotDataDirectory, slotDataFileName);

            if (!string.IsNullOrEmpty(slotDataPath))
            {
                string slotDataJson = File.ReadAllText(slotDataPath);
                SlotModelData slotModelData = JsonUtility.FromJson<SlotModelData>(slotDataJson);

                foreach (string modelNameWithPath in slotModelData.meshes)
                {
                    string modelName = Path.GetFileNameWithoutExtension(modelNameWithPath);

                    string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", modelName + ".json");
                    if (File.Exists(materialJsonFilePath))
                    {
                        string materialJsonData = File.ReadAllText(materialJsonFilePath);
                        ModelInfo modelInfo = JsonUtility.FromJson<ModelInfo>(materialJsonData);

                        if (modelInfo.variations != null)
                        {
                            foreach (Variation variation in modelInfo.variations)
                            {
                                foreach (MaterialResource materialResource in variation.materialsResources)
                                {
                                    foreach (Resource resource in materialResource.resources)
                                    {
                                        string materialName = Path.GetFileNameWithoutExtension(resource.name);
                                        if (!string.IsNullOrEmpty(materialName) && materialName != "null")
                                        {
                                            uniqueMaterials.Add(materialName);
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

            // Ensure the original material (if any) is placed at index 0, followed by "null.mat"
            // Assuming the original material's name is stored in renderer's sharedMaterial at index 0.
            if (renderer.sharedMaterials.Length > 0 && renderer.sharedMaterials[0] != null)
            {
                string originalMaterialName = renderer.sharedMaterials[0].name.Replace(" (Instance)", "");
                if (!availableMaterials.Contains(originalMaterialName))
                {
                    availableMaterials.Insert(0, originalMaterialName);
                }
            }

            // Add unique materials while preserving the order: original, null.mat, and then others.
            foreach (string materialName in uniqueMaterials)
            {
                if (!availableMaterials.Contains(materialName))
                {
                    availableMaterials.Add(materialName);
                }
            }

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
            int rendererCounter = 1; // Start counter at 1 for the first renderer

            foreach (var renderer in renderers)
            {
                for (int materialIndex = 0; materialIndex < renderer.sharedMaterials.Length; materialIndex++)
                {
                    Material currentMaterial = renderer.sharedMaterials[materialIndex];
                    if (currentMaterial != null)
                    {
                        GameObject dropdownGameObject = Instantiate(variationMaterialDropdownPrefab, materialSpawn);
                        TMP_Dropdown tmpDropdown = dropdownGameObject.GetComponentInChildren<TMP_Dropdown>();
                        SetupDropdownWithMaterials(tmpDropdown, renderer, currentMaterial.name, materialIndex, slotName);

                        Button optionsButton = dropdownGameObject.transform.Find("Button_Options").GetComponent<Button>();
                        TogglePanelVisibility toggleScript = optionsButton.GetComponent<TogglePanelVisibility>() ?? optionsButton.gameObject.AddComponent<TogglePanelVisibility>();
                        toggleScript.variationBuilder = this;
                        toggleScript.spawnPoint = materialSpawn;
                        toggleScript.dropdownGameObject = dropdownGameObject;
                        toggleScript.variationTextureSlotPanelPrefab = variationTextureSlotPanelPrefab;

                        GameObject panelGameObject = Instantiate(variationTextureSlotPanelPrefab, toggleScript.texturePrefabSpawnPoint.transform, false);
                        panelGameObject.name = $"Panel_Renderer{rendererCounter}_Material{materialIndex}";

                        VariationTextureSlotsPanel panelScript = panelGameObject.GetComponent<VariationTextureSlotsPanel>();
                        if (panelScript != null)
                        {
                            panelScript.InitializePanel(currentModel, currentMaterial, slotName);
                            Debug.Log($"Panel created for Renderer {rendererCounter}, Material {materialIndex}, Model {currentModel.name}");
                        }
                        panelGameObject.SetActive(false);
                        toggleScript.panelGameObject = panelGameObject;

                        tmpDropdown.onValueChanged.AddListener((int selectedIndex) =>
                        {
                            string selectedMaterialName = tmpDropdown.options[selectedIndex].text;
                            Material selectedMaterial = GetSelectedMaterial(selectedMaterialName, currentModel);
                            if (selectedMaterial != null)
                            {
                                ApplyMaterialDirectly(renderer, selectedMaterialName);
                                Debug.Log($"Dropdown Value Changed for Material Index {materialIndex}: {selectedMaterialName}");
                                toggleScript.UpdatePanelSetup(panelGameObject, slotName, selectedMaterial, currentModel);
                            }
                        });

                        optionsButton.onClick.AddListener(() =>
                        {
                            string selectedMaterialName = tmpDropdown.options[tmpDropdown.value].text;
                            Material selectedMaterial = GetSelectedMaterial(selectedMaterialName, currentModel);
                            if (selectedMaterial != null)
                            {
                                toggleScript.InitializePanel(currentModel, selectedMaterial, slotName);
                                toggleScript.UpdatePanelSetup(panelGameObject, slotName, selectedMaterial, currentModel);
                                toggleScript.TogglePanel(slotName, selectedMaterial, currentModel);
                                toggleScript.ToggleOtherDropdowns(!panelGameObject.activeSelf);
                            }
                        });

                        // Set the renderer number as the label
                        TextMeshProUGUI nameText = dropdownGameObject.transform.Find("Button_Options/Name").GetComponent<TextMeshProUGUI>();
                        nameText.text = rendererCounter.ToString();
                        Debug.Log($"'{panelGameObject.name}' created for Renderer {rendererCounter}, Material Slot {materialIndex}.");
                    }
                }
                rendererCounter++; // Increment for the next renderer
            }
        }

        Material GetSelectedMaterial(string materialName, GameObject currentModel)
        {
            SkinnedMeshRenderer[] renderers = currentModel.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material.name.Equals(materialName, StringComparison.OrdinalIgnoreCase) ||
                        material.name.Replace(" (Instance)", "").Equals(materialName, StringComparison.OrdinalIgnoreCase))
                    {
                        return material;
                    }
                }
            }

            Debug.LogError($"Material '{materialName}' not found in the current model.");
            return null;
        }

        void SetupDropdownWithMaterials(TMP_Dropdown tmpDropdown, SkinnedMeshRenderer renderer, string currentMaterialName, int materialSlot, string slotName)
        {
            List<string> availableMaterialNames = GetAvailableMaterialNamesForRenderer(renderer, slotName);

            // First, clear existing options to repopulate the dropdown.
            tmpDropdown.ClearOptions();

            // Insert the original material name at index 0 if it's not already "null.mat".
            string originalMaterialName = renderer.sharedMaterials.Length > 0 ? renderer.sharedMaterials[0].name.Replace(" (Instance)", "") : "null.mat";
            if (!availableMaterialNames.Contains(originalMaterialName) && originalMaterialName != "null.mat")
            {
                availableMaterialNames.Insert(0, originalMaterialName);
            }

            // Ensure "null.mat" is at index 1.
            if (!availableMaterialNames.Contains("null.mat"))
            {
                availableMaterialNames.Insert(1, "null.mat");
            }
            else if (availableMaterialNames.IndexOf("null.mat") != 1)
            {
                availableMaterialNames.Remove("null.mat");
                availableMaterialNames.Insert(1, "null.mat");
            }

            // Add options to the dropdown.
            tmpDropdown.AddOptions(availableMaterialNames);

            // Select the original material by default.
            tmpDropdown.value = availableMaterialNames.IndexOf(originalMaterialName);
            tmpDropdown.RefreshShownValue();

            // Setup the listener for selection changes.
            tmpDropdown.onValueChanged.RemoveAllListeners();
            tmpDropdown.onValueChanged.AddListener(index => {
                string selectedMaterialName = availableMaterialNames[index];
                ApplyMaterialDirectly(renderer, selectedMaterialName);
            });
        }

        void ApplyMaterialDirectly(SkinnedMeshRenderer renderer, string materialName)
        {
            Material newMaterial = materialName.Equals("null.mat")
                ? Resources.Load<Material>("Materials/null")
                : LoadMaterialByName(materialName);

            if (newMaterial == null)
            {
                Debug.LogError($"Material '{materialName}' not found.");
                return;
            }

            // Since only one material slot exists, directly set to index 0
            Material[] materials = renderer.sharedMaterials;
            if (materials.Length > 0)
            {
                materials[0] = newMaterial;
                renderer.sharedMaterials = materials;
                Debug.Log($"Applied '{materialName}' to slot '0' of '{renderer.gameObject.name}'.");
            }
            else
            {
                Debug.LogError($"Renderer '{renderer.name}' does not have any material slots.");
            }
        }

        public void RecordMaterialChange(GameObject model, int slotNumber, string newMaterialName)
        {
            string modelName = model.name.Replace("(Clone)", "");
            if (!modelSpecificChanges.ContainsKey(modelName))
            {
                modelSpecificChanges[modelName] = new ModelChange();
            }

            if (!modelSpecificChanges[modelName].Materials.ContainsKey(slotNumber))
            {
                modelSpecificChanges[modelName].Materials[slotNumber] = new MaterialChange { MaterialName = newMaterialName };
            }
            else
            {
                // If the slot already exists, update the material name (assuming material changes are relevant).
                modelSpecificChanges[modelName].Materials[slotNumber].MaterialName = newMaterialName;
            }

            Debug.Log($"Material Change Recorded: Model: {modelName}, Slot: {slotNumber}, Material: {newMaterialName}");
        }

        public void RecordTextureChange(string slotName, Texture2D texture, Material material, GameObject model)
        {
            string textureName = texture != null ? texture.name : "None";
            string modelName = model.name.Replace("(Clone)", "");
            string materialName = material.name;
            int slotNumber = GetSlotNumberFromMaterial(material, model); // Implement this method based on your slot management logic

            // Ensure the model has an entry in modelSpecificChanges
            if (!modelSpecificChanges.ContainsKey(modelName))
            {
                modelSpecificChanges[modelName] = new ModelChange();
            }

            var modelChange = modelSpecificChanges[modelName];

            // Ensure the material slot has an entry in the ModelChange
            if (!modelChange.Materials.ContainsKey(slotNumber))
            {
                modelChange.Materials[slotNumber] = new MaterialChange { MaterialName = materialName };
            }

            var materialChange = modelChange.Materials[slotNumber];

            // Add or update the texture change in the MaterialChange
            var existingTextureChange = materialChange.TextureChanges.FirstOrDefault(tc => tc.name == slotName);
            if (existingTextureChange != null)
            {
                existingTextureChange.val_str = textureName + ".png";
            }
            else
            {
                materialChange.TextureChanges.Add(new RttiValue { name = slotName, type = 7, val_str = textureName + ".png" });
            }

            Debug.Log($"Recorded texture change for material '{materialName}' on model '{modelName}': {slotName} = {textureName}");
        }
        private int GetSlotNumberFromMaterial(Material material, GameObject model)
        {
            SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].sharedMaterials.Contains(material))
                {
                    return i + 1;
                }
            }
            return -1;
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
