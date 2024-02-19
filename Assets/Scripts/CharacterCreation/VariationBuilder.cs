using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
using static ModelData;
using static TreeEditor.TextureAtlas;

namespace doppelganger
{
    public class VariationBuilder : MonoBehaviour
    {
        [Header("Managers")]
        public CharacterBuilder characterBuilder;
        public CharacterBuilder_InterfaceManager interfaceManager;
        public TextureScroller textureScroller;

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
        private Dictionary<SkinnedMeshRenderer, VariationTextureSlotsPanel> rendererPanelMap = new Dictionary<SkinnedMeshRenderer, VariationTextureSlotsPanel>();


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
                Material currentMaterial = renderer.sharedMaterials[0]; // Assuming each renderer has only one material slot (index 0)
                GameObject dropdownGameObject = Instantiate(variationMaterialDropdownPrefab, materialSpawn);
                allDropdowns.Add(dropdownGameObject);
                TMP_Dropdown tmpDropdown = dropdownGameObject.GetComponentInChildren<TMP_Dropdown>();
                SetupDropdownWithMaterials(tmpDropdown, renderer, currentMaterial.name, 0, slotName); // Using 0 for materialIndex

                Button optionsButton = dropdownGameObject.transform.Find("Button_Options").GetComponent<Button>();
                TogglePanelVisibility toggleScript = optionsButton.GetComponent<TogglePanelVisibility>() ?? optionsButton.gameObject.AddComponent<TogglePanelVisibility>();
                toggleScript.variationBuilder = this;
                toggleScript.spawnPoint = materialSpawn;
                toggleScript.dropdownGameObject = dropdownGameObject;
                toggleScript.variationTextureSlotPanelPrefab = variationTextureSlotPanelPrefab;

                GameObject panelGameObject = Instantiate(variationTextureSlotPanelPrefab, toggleScript.texturePrefabSpawnPoint.transform, false);
                panelGameObject.name = $"Panel_Renderer{rendererCounter}_Material0";

                VariationTextureSlotsPanel panelScript = panelGameObject.GetComponent<VariationTextureSlotsPanel>();
                if (panelScript != null)
                {
                    panelScript.SetMaterialModelAndRenderer(currentMaterial, currentModel, renderer, slotName);
                    panelScript.SetVariationBuilder(this);
                }
                panelGameObject.SetActive(false);
                toggleScript.panelGameObject = panelGameObject;

                // Listener for material change through the dropdown
                tmpDropdown.onValueChanged.AddListener((int selectedIndex) => {
                    string selectedMaterialName = tmpDropdown.options[selectedIndex].text;
                    Material selectedMaterial = GetSelectedMaterial(selectedMaterialName, currentModel);
                    if (selectedMaterial != null)
                    {
                        ApplyMaterialDirectly(renderer, selectedMaterialName); // This now also updates the panel script
                    }
                });

                // Toggling panel visibility and ensuring it's updated with the current material
                optionsButton.onClick.AddListener(() =>
                {
                    string selectedMaterialName = tmpDropdown.options[tmpDropdown.value].text;
                    Material selectedMaterial = GetSelectedMaterial(selectedMaterialName, currentModel);
                    if (selectedMaterial != null)
                    {
                        // The TogglePanel method now handles initializing or updating the panel as needed
                        toggleScript.TogglePanel(slotName, selectedMaterial, renderer);
                        toggleScript.ToggleOtherDropdowns(!panelGameObject.activeSelf);
                    }
                });

                TextMeshProUGUI nameText = dropdownGameObject.transform.Find("Button_Options/Name").GetComponent<TextMeshProUGUI>();
                nameText.text = $"{rendererCounter}";
                rendererCounter++; // Increment for the next renderer
            }
        }
        public void RegisterPanelScript(SkinnedMeshRenderer renderer, VariationTextureSlotsPanel panelScript)
        {
            if (!rendererPanelMap.ContainsKey(renderer))
            {
                rendererPanelMap.Add(renderer, panelScript);
            }
            else
            {
                rendererPanelMap[renderer] = panelScript; // Update existing entry
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

        public int GetRendererIndexByName(string rendererName)
        {
            SkinnedMeshRenderer[] renderers = currentModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].name.Equals(rendererName, StringComparison.OrdinalIgnoreCase))
                {
                    // Return the index of the found renderer
                    return i;
                }
            }
            // Return -1 or handle this scenario as needed if the renderer was not found
            return -1;
        }

        void ApplyMaterialDirectly(SkinnedMeshRenderer renderer, string newMaterialName)
        {
            if (renderer.sharedMaterials.Length == 0)
            {
                Debug.LogError("Renderer does not have any materials.");
                return;
            }

            string originalName = renderer.sharedMaterials[0].name;
            Material newMaterial = newMaterialName.Equals("null.mat")
                ? Resources.Load<Material>("Materials/null")
                : LoadMaterialByName(newMaterialName);

            if (newMaterial == null)
            {
                Debug.LogError($"Material '{newMaterialName}' not found.");
                return;
            }

            // Get the index of the renderer within the current model
            int rendererIndex = GetRendererIndexByName(renderer.name);
            if (rendererIndex == -1)
            {
                Debug.LogError($"Renderer '{renderer.name}' not found in the current model.");
                return;
            }

            // Apply the new material
            Material[] materials = renderer.sharedMaterials;
            materials[0] = newMaterial;
            renderer.sharedMaterials = materials;
            Debug.Log($"Applied '{newMaterialName}' to slot '0' of '{renderer.gameObject.name}'.");

            // Adjusted to pass renderer index to the recording method
            string modelName = currentModelName.Replace("(Clone)", "");
            RecordMaterialChange(modelName, originalName, newMaterialName, rendererIndex);

            if (rendererPanelMap.TryGetValue(renderer, out VariationTextureSlotsPanel panelScript))
            {
                panelScript.RefreshMaterial(newMaterial);
            }
        }

        public void ApplyTextureChange(SkinnedMeshRenderer renderer, string slotName, Texture2D texture)
        {
            int rendererIndex = GetRendererIndexByName(renderer.name);
            if (rendererIndex == -1)
            {
                Debug.LogError($"Renderer '{renderer.name}' not found in the current model.");
                return;
            }

            string modelName = currentModelName.Replace("(Clone)", "");
            string materialName = renderer.sharedMaterials[0].name; // Assuming the material to change is always at index 0

            // Apply the texture change
            Material[] materials = renderer.sharedMaterials;
            Material clonedMaterial = new Material(materials[0]);
            clonedMaterial.SetTexture(slotName, texture);
            materials[0] = clonedMaterial;
            renderer.sharedMaterials = materials;

            Debug.Log($"Successfully applied texture {texture.name} to slot {slotName} on {renderer.gameObject.name}.");

            // Record the texture change along with any material change
            RecordTextureChange(modelName, materialName, slotName, texture.name, rendererIndex);
        }

        void RecordMaterialChange(string modelName, string originalMaterialName, string newMaterialName, int rendererIndex)
        {
            if (!modelSpecificChanges.TryGetValue(modelName, out ModelChange modelChange))
            {
                modelChange = new ModelChange();
                modelSpecificChanges[modelName] = modelChange;
            }

            if (!modelChange.MaterialsByRenderer.ContainsKey(rendererIndex))
            {
                modelChange.MaterialsByRenderer[rendererIndex] = new MaterialChange
                {
                    OriginalName = originalMaterialName,
                    NewName = newMaterialName,
                    TextureChanges = new List<RttiValue>()
                };
            }
            else
            {
                // If there's already a material change recorded, update the NewName and keep texture changes
                modelChange.MaterialsByRenderer[rendererIndex].NewName = newMaterialName;
            }

            Debug.Log($"Material change recorded for model: {modelName}, renderer index: {rendererIndex}, from: {originalMaterialName} to: {newMaterialName}");
        }

        void RecordTextureChange(string modelName, string materialName, string slotName, string textureName, int rendererIndex)
        {
            if (!modelSpecificChanges.TryGetValue(modelName, out var modelChange))
            {
                modelChange = new ModelChange();
                modelSpecificChanges[modelName] = modelChange;
            }

            if (!modelChange.MaterialsByRenderer.ContainsKey(rendererIndex))
            {
                Debug.LogError($"Attempting to record a texture change for a non-existing material change at renderer index {rendererIndex}.");
                return;
            }

            string modifiedSlotName = slotName.Replace("_", "");
            string finalSlotName = modifiedSlotName + "_0_tex";

            var materialChange = modelChange.MaterialsByRenderer[rendererIndex];

            // Check if a change for this slot already exists
            var existingChange = materialChange.TextureChanges.FirstOrDefault(tc => tc.name.Equals(finalSlotName, StringComparison.OrdinalIgnoreCase));

            if (existingChange != null)
            {
                // Update existing entry
                existingChange.val_str = textureName + ".png";
                Debug.Log($"Updated texture change for model: {modelName}, slot: {finalSlotName}, renderer index: {rendererIndex}, material: {materialName}, to texture: {textureName}");
            }
            else
            {
                // Add new texture change
                materialChange.TextureChanges.Add(new RttiValue { name = finalSlotName, val_str = textureName + ".png" });
                Debug.Log($"Recorded new texture change for model: {modelName}, slot: {finalSlotName}, renderer index: {rendererIndex}, material: {materialName}, texture: {textureName}");
            }
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
