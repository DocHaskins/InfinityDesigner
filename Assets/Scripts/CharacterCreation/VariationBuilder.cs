using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        // Panels and UI elements
        public GameObject modelInfoPanel;
        public TextMeshProUGUI meshNameText;
        public TextMeshProUGUI selectedMaterialName;
        public Transform materialSpawn;
        public GameObject modelInfoPanelPrefab;
        public GameObject variationMaterialLabelPrefab;
        public GameObject currentModelInfoPanel;
        public GameObject variationTextureSlotPanelPrefab;

        // Current model and its properties
        public GameObject currentModel;
        public Transform currentSpawn;
        public SkinnedMeshRenderer currentRenderer;
        public Material nullMat;
        public string currentModelName;
        public string currentSlot;
        public bool isPanelOpen = false;
        public string openPanelSlotName = "";
        public Material CurrentlySelectedMaterial { get; private set; }

        // Lists and dictionaries
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
                var toggle = child.GetComponentInChildren<Toggle>();
                if (toggle != null)
                {
                    toggle.onValueChanged.RemoveAllListeners();
                }
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
            allLabels.Clear();

            SkinnedMeshRenderer[] renderers = currentModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int rendererCounter = 1;

            foreach (var renderer in renderers)
            {
                Material currentMaterial = renderer.sharedMaterials[0];
                GameObject labelGameObject = Instantiate(variationMaterialLabelPrefab, materialSpawn);

                string uniquePanelName = "Panel_" + slotName + "_" + currentModel.name;
                labelGameObject.name = uniquePanelName;
                selectedMaterialName = labelGameObject.transform.Find("materialLabel").GetComponent<TextMeshProUGUI>();
                selectedMaterialName.text = currentMaterial.name.Replace("(Instance)","");
                allLabels.Add(labelGameObject);

                Toggle optionsToggle = labelGameObject.transform.Find("Button_Options").GetComponent<Toggle>();
                TogglePanelVisibility toggleScript = optionsToggle.GetComponent<TogglePanelVisibility>() ?? optionsToggle.gameObject.AddComponent<TogglePanelVisibility>();
                toggleScript.Setup(this, currentModel, renderer, currentMaterial, slotName);
                toggleScript.textureScroller = textureScroller;

                TextMeshProUGUI nameText = labelGameObject.transform.Find("Button_Options/Name").GetComponent<TextMeshProUGUI>();
                nameText.text = $"{rendererCounter}";

                GameObject optionsButton = labelGameObject.transform.Find("Button_Options").gameObject; // Get the options button
                AddRightClickListener(optionsButton, () => {
                    ApplyMaterialDirectly(nullMat);
                });

                optionsToggle.onValueChanged.RemoveAllListeners();
                optionsToggle.onValueChanged.AddListener((isOn) => {
                    Debug.Log($"Toggle_Options changed for material: {currentMaterial.name}, Renderer: {renderer.name}: {isOn}");
                    if (isOn)
                    {
                        currentRenderer = renderer;
                        bool isPanelActive = toggleScript.TogglePanel();
                        selectedMaterialName = labelGameObject.transform.Find("materialLabel").GetComponent<TextMeshProUGUI>();
                        Debug.Log($"Panel active state for material: {currentMaterial.name}, Renderer: {renderer.name}: {isPanelActive}");

                        foreach (GameObject otherLabel in allLabels)
                        {
                            if (otherLabel != labelGameObject)
                            {
                                otherLabel.SetActive(!isOn);
                            }
                        }
                    }
                    else
                    {
                        currentRenderer = null;
                        selectedMaterialName = null;
                        toggleScript.DeactivatePanel();
                        textureScroller.ClearCurrentSelectionPanel();
                        foreach (GameObject otherLabel in allLabels)
                        {
                            otherLabel.SetActive(true);
                        }
                    }
                });

                rendererCounter++;
            }
        }

        private void AddRightClickListener(GameObject gameObject, Action action)
        {
            EventTrigger trigger = gameObject.GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerDown;
            entry.callback.AddListener((eventData) => {
                PointerEventData pointerEventData = (PointerEventData)eventData;
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                {
                    action.Invoke();
                }
            });
            trigger.triggers.Add(entry);
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
            if (currentRenderer)
            {
                string originalName = currentRenderer.sharedMaterials[0].name;
                Debug.Log($"Original material name: {originalName}, targetRenderer {currentRenderer}, newMaterial {newMaterial}");

                if (newMaterial != null && currentRenderer != null)
                {
                    if (!currentRenderer.enabled)
                    {
                        Debug.Log($"Enabling renderer: {currentRenderer.name} before applying new material.");
                        currentRenderer.enabled = true;
                    }

                    Debug.Log($"Original material name: {currentRenderer.material.name}");
                    Debug.Log($"New material loaded: {newMaterial.name}");

                    currentRenderer.material = newMaterial;

                    Debug.Log($"Applied '{newMaterial.name}' to {currentRenderer.gameObject.name}.");

                    string modelName = currentModelName.Replace("(Clone)", "");
                    string currentSlider = interfaceManager.currentSlider;

                    //if (modelSpecificChanges.ContainsKey(modelName))
                    //{
                    //    modelSpecificChanges.Remove(modelName);
                    //    Debug.Log($"All changes cleared for model: {modelName}.");
                    //}

                    Debug.Log($"Model Name: {modelName}, Current Slider: {currentSlider}");

                    int rendererIndex = GetRendererIndexByName(currentRenderer.name);
                    if (rendererIndex == -1)
                    {
                        Debug.LogError($"Renderer '{currentRenderer.name}' not found in the current model.");
                        return;
                    }
                    selectedMaterialName.text = newMaterial.name.Replace(" (Instance)", "");
                    Debug.Log($"Renderer index in the current model: {rendererIndex}");

                    RecordMaterialChange(modelName.Replace("(Clone)", ""), originalName, newMaterial.name, rendererIndex);
                }
            }
            else
            {
                Debug.LogError($"Select a material slot (#'s) to apply this material");
            }
        }

        public void SetEmissiveIntensity(float intensity, string slotName)
        {
            if (currentRenderer)
            {
                if (currentRenderer.material != null && slotName.Equals("_ems", StringComparison.OrdinalIgnoreCase))
                {
                    // Set the intensity of the emissive property
                    currentRenderer.material.SetFloat("_ems_intensity", intensity);
                    RecordScaleChange(currentModel.name, currentRenderer.material, intensity);
                    Debug.Log($"Set Emissive Intensity to {intensity} for material: {currentRenderer.material.name}");
                }
                else
                {
                    if (currentRenderer.material == null)
                    {
                        Debug.LogError("Failed to set emissive intensity: Current material is null.");
                    }
                    if (!slotName.Equals("_ems", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.LogError($"Failed to set emissive intensity: Incorrect slot name {slotName}.");
                    }
                }
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

            if (renderer.materials.Length == 0)
            {
                Debug.LogError($"Renderer '{renderer.name}' does not have any materials.");
                return;
            }

            Material materialToModify = renderer.materials[0];

            if (texture == null)
            {
                materialToModify.SetTexture(slotName, null);
            }
            else
            {
                materialToModify.SetTexture(slotName, texture);
            }

            Debug.Log($"Applied texture '{texture?.name ?? "null"}' to slot '{slotName}' on '{renderer.gameObject.name}'.");

            string modelName = currentModel.name.Replace("(Clone)", ""); // Assuming 'currentModel' is a field for the current model
            string materialName = materialToModify.name;
            string textureName = texture == null ? "null" : texture.name;
            RecordTextureChange(modelName, materialName.Replace(" (Instance)",""), slotName, textureName, rendererIndex);
        }

        public void RecordMaterialChange(string modelName, string originalMaterialName, string newMaterialName, int rendererIndex)
        {
            //Debug.Log($"Processing RecordMaterialChange for model: {modelName}, originalMaterialName: {originalMaterialName}, newMaterialName: {newMaterialName}, rendererIndex: {rendererIndex}");
            if (!modelSpecificChanges.TryGetValue(modelName, out ModelChange modelChange))
            {
                modelChange = new ModelChange();
                modelSpecificChanges[modelName] = modelChange;
            }

            List<RttiValue> currentTextureChanges = new List<RttiValue>();

            if (characterBuilder.ModelIndexChanges.TryGetValue(modelName, out var indexChanges) &&
                indexChanges.TryGetValue(rendererIndex, out int correctedIndex))
            {
                if (modelChange.MaterialsByRenderer.TryGetValue(correctedIndex, out MaterialChange existingChangeForCorrectedIndex))
                {
                    currentTextureChanges = new List<RttiValue>(existingChangeForCorrectedIndex.TextureChanges);
                }
            }

            if (!modelChange.MaterialsByRenderer.TryGetValue(rendererIndex, out MaterialChange existingMaterialChange))
            {
                existingMaterialChange = new MaterialChange
                {
                    OriginalName = originalMaterialName,
                    NewName = newMaterialName.Replace("(Instance)", ""),
                    TextureChanges = currentTextureChanges
                };
            }
            else
            {
                existingMaterialChange.OriginalName = originalMaterialName;
                existingMaterialChange.NewName = newMaterialName.Replace("(Instance)", "");
                if (currentTextureChanges.Count > 0)
                {
                    existingMaterialChange.TextureChanges = currentTextureChanges;
                }
            }

            modelChange.MaterialsByRenderer[rendererIndex] = existingMaterialChange;

            //// Debug logging
            //foreach (var kvp in modelSpecificChanges)
            //{
            //    string modelChangesJson = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented);
            //    Debug.Log($"ModelSpecificChanges for Key: {kvp.Key}, Detailed Changes: \n{modelChangesJson}");
            //}
            //Debug.Log($"[RecordMaterialChange] Material change recorded for model: {modelName}, renderer index: {rendererIndex}, from: {originalMaterialName} to: {newMaterialName}. Preserved texture changes: {currentTextureChanges.Count}");
        }


        public void RecordScaleChange(string modelName, Material currentMaterial, float scaleValue)
        {
            string newModelName = modelName.Replace("(Clone)", "");
            int rendererIndex = GetRendererIndexByName(currentRenderer.name);
            if (rendererIndex == -1)
            {
                Debug.LogError($"Renderer '{currentRenderer.name}' not found in the current model.");
                return;
            }

            if (!modelSpecificChanges.TryGetValue(newModelName, out var modelChange))
            {
                modelChange = new ModelChange();
                modelSpecificChanges[newModelName] = modelChange;
            }

            if (!modelChange.MaterialsByRenderer.TryGetValue(rendererIndex, out var materialChange))
            {
                materialChange = new MaterialChange
                {
                    OriginalName = currentMaterial.name,
                    NewName = currentMaterial.name,
                    TextureChanges = new List<RttiValue>()
                };
                modelChange.MaterialsByRenderer[rendererIndex] = materialChange;
            }

            string scaleChangeName = "ems_0_scale"; // The name format for scale changes

            var existingChange = materialChange.TextureChanges.FirstOrDefault(tc => tc.name == scaleChangeName);
            if (existingChange != null)
            {
                existingChange.val_str = scaleValue.ToString("F8");
                existingChange.type = 2;
                Debug.Log($"Updated scale change for model: {newModelName}, renderer index: {rendererIndex}, scale: {scaleValue.ToString("F8")}");
            }
            else
            {
                materialChange.TextureChanges.Add(new RttiValue { name = scaleChangeName, val_str = scaleValue.ToString("F8"), type = 2 });
                Debug.Log($"Recorded new scale change for model: {newModelName}, renderer index: {rendererIndex}, scale: {scaleValue.ToString("F8")}");
            }
        }

        public void RecordTextureChange(string modelName, string materialName, string slotName, string textureName, int rendererIndex)
        {
            string newModelName = modelName.Replace("(Clone)", "");
            //Debug.Log($"Processing RecordTextureChange for model: {newModelName}, slot: {slotName}, renderer index: {rendererIndex}, material: {materialName}, to texture: {textureName}");

            if (characterBuilder.ModelIndexChanges.TryGetValue(newModelName, out var indexChanges))
            {
                if (indexChanges.TryGetValue(rendererIndex, out int correctedIndex))
                {
                    Debug.Log($"Index change detected for model: {newModelName}. Original index: {rendererIndex}, New index: {correctedIndex}");
                    rendererIndex = correctedIndex;
                }
            }

            if (!modelSpecificChanges.TryGetValue(newModelName, out var modelChange))
            {
                modelChange = new ModelChange();
                modelSpecificChanges[newModelName] = modelChange;
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

            string finalSlotName = slotName.EndsWith("_tex") ? slotName : ConvertSlotNameToFull(slotName);

            string finalTextureName = EnsurePngExtension(textureName, 7);

            var existingChange = materialChange.TextureChanges.FirstOrDefault(tc => tc.name == finalSlotName);
            if (existingChange != null)
            {
                existingChange.val_str = finalTextureName;
                existingChange.type = finalTextureName == "null" ? existingChange.type : 7; // Set type to 7 for PNG changes
                //Debug.Log($"Updated texture change for model: {newModelName}, slot: {finalSlotName}, renderer index: {rendererIndex}, material: {materialName}, to texture: {finalTextureName}");
            }
            else
            {
                materialChange.TextureChanges.Add(new RttiValue { name = finalSlotName, val_str = finalTextureName, type = finalTextureName == "null" ? 0 : 7 });
                //Debug.Log($"Recorded new texture change for model: {newModelName}, slot: {finalSlotName}, renderer index: {rendererIndex}, material: {materialName}, texture: {finalTextureName}");
            }
            //foreach (var kvp in modelSpecificChanges)
            //{
            //    string modelChangesJson = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented);
            //    Debug.Log($"ModelSpecificChanges for Key: {kvp.Key}, Detailed Changes: \n{modelChangesJson}");
            //}
        }

        private string ConvertSlotNameToFull(string slotName)
        {
            string slotBase = slotName.TrimStart('_');
            Dictionary<string, string> mapping = new Dictionary<string, string>
    {
        {"dif", "dif_0_tex"}, {"dif_1", "dif_1_tex"}, {"nrm", "nrm_0_tex"}, {"spc", "spc_0_tex"}, {"rgh", "rgh_0_tex"},
        {"msk", "msk_0_tex"}, {"msk_1", "msk_1_tex"}, {"gra", "gra_0_tex"}, {"idx", "idx_0_tex"}, {"clp", "clp_0_tex"},
        {"ocl", "ocl_0_tex"}, {"ems", "ems_0_tex"}
    };

            return mapping.TryGetValue(slotBase, out var fullSlotName) ? fullSlotName : slotName;
        }

        private string EnsurePngExtension(string textureName, int type)
        {
            if (type == 7 && !textureName.Equals("null") && !textureName.EndsWith(".png"))
            {
                return textureName + ".png";
            }
            return textureName;
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
