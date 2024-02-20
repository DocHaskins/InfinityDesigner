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
                    ApplyMaterialDirectly(renderer, selectedMaterialName); // Assumes this method can directly use renderer.sharedMaterial
                });

                // Toggling panel visibility and ensuring it's updated with the current material
                optionsButton.onClick.AddListener(() => {
                        // The TogglePanel method now handles initializing or updating the panel as needed
                        toggleScript.TogglePanel();
                        toggleScript.ToggleOtherDropdowns(!panelGameObject.activeSelf);
                    
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

        void SetupDropdownWithMaterials(TMP_Dropdown tmpDropdown, SkinnedMeshRenderer renderer, string currentMaterialName, int materialSlot, string slotName)
        {
            List<string> availableMaterialNames = GetAvailableMaterialNamesForRenderer(renderer, slotName);

            // Clear existing options to repopulate the dropdown.
            tmpDropdown.ClearOptions();

            // Clone the original material to ensure it's not modified.
            Material originalMaterial = renderer.sharedMaterials[materialSlot];
            Material clonedMaterial = new Material(originalMaterial);
            string clonedMaterialName = clonedMaterial.name;

            // Apply the cloned material immediately to the renderer.
            Material[] materials = renderer.sharedMaterials;
            materials[materialSlot] = clonedMaterial;
            renderer.sharedMaterials = materials;

            // Add the cloned material name to the available options.
            availableMaterialNames.Insert(0, clonedMaterialName);

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

            // Select the cloned material by default in the dropdown.
            tmpDropdown.value = 0; // Cloned material is at index 0
            tmpDropdown.RefreshShownValue();

            // Listen for selection changes to apply materials directly.
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

            // Use the material directly from the renderer for modifications
            Material materialToModify = renderer.sharedMaterials[0];

            // Apply texture change directly to this material
            if (texture == null)
            {
                materialToModify.SetTexture(slotName, null);
            }
            else
            {
                materialToModify.SetTexture(slotName, texture);
            }

            // Reflect changes by reassigning modified material back to the renderer
            Material[] materials = renderer.sharedMaterials;
            materials[0] = materialToModify;
            renderer.sharedMaterials = materials;

            Debug.Log($"Applied texture '{texture?.name ?? "null"}' to slot '{slotName}' on '{renderer.gameObject.name}'.");

            // Record the change
            string modelName = currentModelName.Replace("(Clone)", "");
            string materialName = materialToModify.name;
            string textureName = texture == null ? "null" : texture.name;
            RecordTextureChange(modelName, materialName, slotName, textureName, rendererIndex);
        }

        public void RecordMaterialChange(string modelName, string originalMaterialName, string newMaterialName, int rendererIndex)
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

            if (!modelChange.MaterialsByRenderer.TryGetValue(rendererIndex, out var materialChange))
            {
                materialChange = new MaterialChange
                {
                    OriginalName = materialName,
                    NewName = materialName,
                    TextureChanges = new List<RttiValue>()
                };
                modelChange.MaterialsByRenderer[rendererIndex] = materialChange;
            }

            string finalSlotName = slotName.Replace("_", "") + "_0_tex";

            // Determine the final texture name, appending ".png" only if the texture name is not "null"
            string finalTextureName = textureName == "null" ? "null" : textureName + ".png";

            var existingChange = materialChange.TextureChanges.FirstOrDefault(tc => tc.name == finalSlotName);
            if (existingChange != null)
            {
                // If there is already a change recorded for this slot, update it
                existingChange.val_str = finalTextureName;
                Debug.Log($"Updated texture change for model: {modelName}, slot: {finalSlotName}, renderer index: {rendererIndex}, material: {materialName}, to texture: {finalTextureName}");
            }
            else
            {
                // If no change is recorded for this slot, add a new one
                materialChange.TextureChanges.Add(new RttiValue { name = finalSlotName, val_str = finalTextureName });
                Debug.Log($"Recorded new texture change for model: {modelName}, slot: {finalSlotName}, renderer index: {rendererIndex}, material: {materialName}, texture: {finalTextureName}");
            }
        }

        public void ClearAllChangesForModel()
        {
            currentModelName = currentModelName.Replace("(Clone)", "");
            if (modelSpecificChanges.ContainsKey(currentModelName))
            {
                // Remove all changes for this model
                modelSpecificChanges.Remove(currentModelName);
                Debug.Log($"All changes cleared for model: {currentModelName}.");
            }
            else
            {
                Debug.LogWarning($"No changes found for model: {currentModelName} to clear.");
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
