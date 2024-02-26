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
        public GameObject variationMaterialLabelPrefab;
        public GameObject currentModelInfoPanel;
        public GameObject variationTextureSlotPanelPrefab;

        public GameObject currentModel;
        public Transform currentSpawn;
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
            SkinnedMeshRenderer[] renderers = currentModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int rendererCounter = 1; // Start counter at 1 for the first renderer

            foreach (var renderer in renderers)
            {
                int rendererIndex = GetRendererIndexByName(renderer.name);
                if (!originalMaterials.ContainsKey(rendererIndex))
                {
                    originalMaterials[rendererIndex] = renderer.sharedMaterial != null ? new Material(renderer.sharedMaterial) : null;
                }

                Material currentMaterial = renderer.sharedMaterials[0];
                GameObject labelGameObject = Instantiate(variationMaterialLabelPrefab, materialSpawn);
                allLabels.Add(labelGameObject);
                TextMeshProUGUI materialLabel = labelGameObject.transform.Find("materialLabel").GetComponent<TextMeshProUGUI>(); // Ensure there is a child named "materialLabel"
                materialLabel.text = currentMaterial.name; // Set the label to the current material name

                Button optionsButton = labelGameObject.transform.Find("Button_Options").GetComponent<Button>();
                TogglePanelVisibility toggleScript = optionsButton.GetComponent<TogglePanelVisibility>() ?? optionsButton.gameObject.AddComponent<TogglePanelVisibility>();
                toggleScript.variationBuilder = this;
                toggleScript.spawnPoint = materialSpawn;
                toggleScript.dropdownGameObject = labelGameObject;
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

                // When options button is clicked, show the panel to allow material changes
                optionsButton.onClick.AddListener(() => {
                    toggleScript.TogglePanel();
                    panelScript.SetMaterialModelAndRenderer(currentMaterial, currentModel, renderer, slotName);
                    toggleScript.ToggleOtherDropdowns(!panelGameObject.activeSelf);
                });

                // Update label name to reflect the renderer and material
                TextMeshProUGUI nameText = labelGameObject.transform.Find("Button_Options/Name").GetComponent<TextMeshProUGUI>(); // Make sure the Button_Options object has a child named "Name"
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

        public void ApplyMaterialDirectly(SkinnedMeshRenderer targetRenderer, string newMaterialName)
        {
            Debug.Log("ApplyMaterialDirectly called"); // Log when the method is called

            if (targetRenderer == null)
            {
                Debug.LogError("No renderer provided.");
                return;
            }

            string originalName = targetRenderer.sharedMaterials[0].name;
            Debug.Log($"Original material name: {originalName}"); // Log the original material name

            // Check if the renderer has materials to avoid null reference exception
            if (targetRenderer.sharedMaterials.Length == 0)
            {
                Debug.LogError($"Renderer {targetRenderer.gameObject.name} does not have any materials.");
                return;
            }

            // Load the new material
            Material newMaterial = newMaterialName.Equals("null.mat")
                ? Resources.Load<Material>("Materials/null")
                : Resources.Load<Material>($"Materials/{newMaterialName}");

            if (newMaterial == null)
            {
                Debug.LogError($"Material '{newMaterialName}' not found.");
                return;
            }

            Debug.Log($"New material loaded: {newMaterial.name}"); // Log that the new material was successfully loaded

            Material[] materials = targetRenderer.sharedMaterials;
            Debug.Log($"Number of materials before change: {materials.Length}"); // Log the number of materials before the change

            materials[0] = newMaterial;
            targetRenderer.sharedMaterials = materials;

            Debug.Log($"Applied '{newMaterialName}' to {targetRenderer.gameObject.name}. Number of materials after change: {targetRenderer.sharedMaterials.Length}"); // Log the change

            // Additional application logic
            string modelName = currentModelName.Replace("(Clone)", "");
            string currentSlider = interfaceManager.currentSlider;
            UpdateModelInfoPanel(currentSlider);

            // Log the current model name and slider being used
            Debug.Log($"Model Name: {modelName}, Current Slider: {currentSlider}");

            int rendererIndex = GetRendererIndexByName(targetRenderer.name);
            if (rendererIndex == -1)
            {
                Debug.LogError($"Renderer '{targetRenderer.name}' not found in the current model.");
                return;
            }

            // Log the index of the renderer
            Debug.Log($"Renderer index in the current model: {rendererIndex}");

            RecordMaterialChange(modelName, originalName, newMaterialName, rendererIndex);

            if (rendererPanelMap.TryGetValue(targetRenderer, out VariationTextureSlotsPanel panelScript))
            {
                panelScript.RefreshMaterial(newMaterial);
                Debug.Log($"Material refreshed in panel for {targetRenderer.gameObject.name}"); // Log material refresh in panel
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
