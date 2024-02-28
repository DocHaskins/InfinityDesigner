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
        public TextMeshProUGUI selectedMaterialName;
        public Transform materialSpawn;
        public GameObject modelInfoPanelPrefab;
        public GameObject variationMaterialLabelPrefab;
        public GameObject currentModelInfoPanel;
        public GameObject variationTextureSlotPanelPrefab;

        public GameObject currentModel;
        public Transform currentSpawn;
        public SkinnedMeshRenderer currentRenderer;
        public string currentModelName;
        public string currentSlot;
        public bool isPanelOpen = false;
        public string openPanelSlotName = "";
        public Material CurrentlySelectedMaterial { get; private set; }
        public static List<GameObject> allLabels = new List<GameObject>();
        private Dictionary<int, Material> originalMaterials = new Dictionary<int, Material>();
        public List<RttiValue> currentMaterialResources = new List<RttiValue>();
        public Dictionary<string, List<RttiValue>> materialChanges = new Dictionary<string, List<RttiValue>>();
        public Dictionary<string, ModelChange> modelSpecificChanges = new Dictionary<string, ModelChange>();
        private Dictionary<SkinnedMeshRenderer, VariationTextureSlotsPanel> rendererPanelMap = new Dictionary<SkinnedMeshRenderer, VariationTextureSlotsPanel>();


        public void UpdateModelInfoPanel(string slotName)
        {
            if (modelInfoPanel != null)
            {
                //Debug.Log("ModelInfoPanel found, updating content.");
                UpdateModelInfoPanelContent(slotName);
            }
            string currentSlot = slotName;
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
                PopulateMaterialProperties(materialSpawn, loadedModel, slotName);
            }
        }

        void PopulateMaterialProperties(Transform materialSpawn, GameObject currentModel, string slotName)
        {
            Debug.Log("PopulateMaterialProperties started.");
            SkinnedMeshRenderer[] renderers = currentModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int rendererCounter = 1;

            foreach (var renderer in renderers)
            {
                Material currentMaterial = renderer.sharedMaterials[0]; // Assuming each renderer has at least one material.
                GameObject labelGameObject = Instantiate(variationMaterialLabelPrefab, materialSpawn);
                selectedMaterialName = labelGameObject.transform.Find("materialLabel").GetComponent<TextMeshProUGUI>();
                selectedMaterialName.text = currentMaterial.name;
                allLabels.Add(labelGameObject);

                Toggle optionsToggle = labelGameObject.transform.Find("Button_Options").GetComponent<Toggle>(); // Assuming you have a Toggle component
                TogglePanelVisibility toggleScript = optionsToggle.GetComponent<TogglePanelVisibility>() ?? optionsToggle.gameObject.AddComponent<TogglePanelVisibility>();
                toggleScript.Setup(this, currentModel, renderer, currentMaterial, slotName);
                toggleScript.textureScroller = textureScroller; // Assuming this is a reference to your TextureScroller instance.

                TextMeshProUGUI nameText = labelGameObject.transform.Find("Button_Options/Name").GetComponent<TextMeshProUGUI>();
                nameText.text = $"{rendererCounter}";

                optionsToggle.onValueChanged.RemoveAllListeners();
                optionsToggle.onValueChanged.AddListener((isOn) => {
                    Debug.Log($"Toggle_Options changed for material: {currentMaterial.name}, Renderer: {renderer.name}: {isOn}");
                    if (isOn)
                    {
                        currentRenderer = renderer;
                        bool isPanelActive = toggleScript.TogglePanel();
                        selectedMaterialName = labelGameObject.transform.Find("materialLabel").GetComponent<TextMeshProUGUI>();
                        Debug.Log($"Panel active state for material: {currentMaterial.name}, Renderer: {renderer.name}: {isPanelActive}");

                        // Deactivate other labels
                        foreach (GameObject otherLabel in allLabels)
                        {
                            if (otherLabel != labelGameObject) // Check if it's not the current label.
                            {
                                otherLabel.SetActive(false);
                            }
                        }
                    }
                    else
                    {
                        currentRenderer = null;
                        selectedMaterialName = null;
                        toggleScript.DeactivatePanel();
                        textureScroller.ClearCurrentSelectionPanel(); // Clear the current selection when toggled off.
                                                                      // Reactivate other labels
                        foreach (GameObject otherLabel in allLabels)
                        {
                            otherLabel.SetActive(true); // Reactivate other labels since the current one is turned off.
                        }
                    }
                });

                rendererCounter++;
            }
            Debug.Log("PopulateMaterialProperties completed.");
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

        public void ApplyMaterialDirectly(Material newMaterial)
        {
            string originalName = currentRenderer.sharedMaterials[0].name;
            Debug.Log($"Original material name: {originalName}, targetRenderer {currentRenderer}, newMaterial {newMaterial}");

            if (newMaterial != null && currentRenderer != null)
            {
                // Debug logs to ensure correct operation
                Debug.Log($"Original material name: {currentRenderer.material.name}");
                Debug.Log($"New material loaded: {newMaterial.name}");

                // Replace the target renderer's material
                currentRenderer.material = newMaterial; // Directly set the new material

                // Additional debug logs if needed
                Debug.Log($"Applied '{newMaterial.name}' to {currentRenderer.gameObject.name}.");

                // Additional application logic
                string modelName = currentModelName.Replace("(Clone)", "");
                string currentSlider = interfaceManager.currentSlider;
                //UpdateModelInfoPanel(currentSlider);

                // Log the current model name and slider being used
                Debug.Log($"Model Name: {modelName}, Current Slider: {currentSlider}");

                int rendererIndex = GetRendererIndexByName(currentRenderer.name);
                if (rendererIndex == -1)
                {
                    Debug.LogError($"Renderer '{currentRenderer.name}' not found in the current model.");
                    return;
                }
                selectedMaterialName.text = newMaterial.name;
                // Log the index of the renderer
                Debug.Log($"Renderer index in the current model: {rendererIndex}");

                RecordMaterialChange(modelName, originalName, newMaterial.name, rendererIndex);
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

            // Create a new material instance from the renderer's first material
            Material materialToModify = new Material(renderer.materials[0]);

            // Apply texture change directly to this new material instance
            if (texture == null)
            {
                materialToModify.SetTexture(slotName, null);
            }
            else
            {
                materialToModify.SetTexture(slotName, texture);
            }

            // Reflect changes by reassigning the modified material instance back to the renderer
            Material[] materials = renderer.materials; // Use materials, not sharedMaterials
            materials[0] = materialToModify;
            renderer.materials = materials; // Assign the instance material array back to the renderer

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
                existingChange.type = textureName == "null" ? existingChange.type : 7; // Set type to 7 for PNG changes
                Debug.Log($"Updated texture change for model: {modelName}, slot: {finalSlotName}, renderer index: {rendererIndex}, material: {materialName}, to texture: {finalTextureName}");
            }
            else
            {
                // If no change is recorded for this slot, add a new one
                materialChange.TextureChanges.Add(new RttiValue { name = finalSlotName, val_str = finalTextureName, type = textureName == "null" ? 0 : 7 });
                Debug.Log($"Recorded new texture change for model: {modelName}, slot: {finalSlotName}, renderer index: {rendererIndex}, material: {materialName}, texture: {finalTextureName}");
            }
        }

        public void ClearAllChangesForModel()
        {
            string modelName = currentModelName.Replace("(Clone)", "");
            if (modelSpecificChanges.ContainsKey(modelName))
            {
                // Remove all changes for this model
                modelSpecificChanges.Remove(modelName);
                Debug.Log($"All changes cleared for model: {modelName}.");

                // Reapply original materials and reset dropdowns
                interfaceManager.ResetVariationSliderAndUpdate(currentSlot + "_VariationSlider");

                UpdateModelInfoPanel(currentSlot);
                Debug.Log($"Original materials reapplied and dropdowns reset for model: {modelName}.");
            }
            else
            {
                Debug.LogWarning($"No changes found for model: {modelName} to clear.");
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
