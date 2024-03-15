using System;
using System.Linq;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using UnlimitedScrollUI.Example;
using static ModelData;

namespace doppelganger
{
    public class VariationTextureSlotsPanel : MonoBehaviour
    {
        [Header("Managers")]
        public TextureScroller textureScroller;
        public VariationBuilder variationBuilder;

        [Header("Rendering")]
        public SkinnedMeshRenderer TargetRenderer;

        [Header("UI Prefabs")]
        public GameObject slotPrefab;
        [SerializeField] private GameObject textureScrollerPanelPrefab;
        public GameObject variationEMSTextureSliderPrefab;

        [Header("Current State")]
        private bool emsSliderCreated;
        private GameObject currentPanel;
        public GameObject currentModel;
        public Material currentMaterial;
        public string currentSlotName;

        [Header("UI Elements")]
        public TextMeshProUGUI currentButtonClickedText;

        private readonly string[] textureSlots = {
        "_msk", "_msk_1", "_idx", "_gra", "_spc", "_clp", "_rgh", "_ocl", "_ems", "_dif_1", "_dif", "_nrm",
        "_BaseColorMap", "_NormalMap", "_MaskMap", "_EmissiveColorMap"
    };

        private readonly string[] orderedSlotNames = {
    "_dif", "_dif_1", "_nrm", "_spc", "_rgh", "_msk", "_msk_1", "_gra", "_idx", "_clp", "_ocl", "_ems"
};

        void Awake()
        {
            variationBuilder = FindObjectOfType<VariationBuilder>();
            textureScroller = FindObjectOfType<TextureScroller>();
            if (textureScroller != null)
            {
                textureScroller.TextureSelected += (texture, slotName) => GetTextureChange(texture, slotName);
                textureScroller.MaterialSelected += (material) => GetMaterialChange(material);
            }
        }

        public void SetVariationBuilder(VariationBuilder builder)
        {
            this.variationBuilder = builder;
        }

        public void RefreshMaterial(Material updatedMaterial)
        {
            this.currentMaterial = updatedMaterial;
        }

        public void SetMaterialModelAndRenderer(Material material, GameObject model, SkinnedMeshRenderer renderer, string slotName)
        {
            this.currentMaterial = material;
            if (this.currentMaterial == null)
            {
                Debug.LogError("Failed to set CurrentMaterial in SetMaterialModelAndRenderer.");
                return;
            }
            this.currentModel = model;
            this.TargetRenderer = renderer;
            this.currentSlotName = slotName;

            SubscribeToTextureScrollerEvents();

            Debug.Log($"Setting context: Material: {material.name}, Model: {model.name}, Renderer: {renderer.name}, SlotName: {slotName}");
            UpdatePanel();
        }

        private void SubscribeToTextureScrollerEvents()
        {
            if (textureScroller != null)
            {
                textureScroller.TextureSelected -= GetTextureChange;
                textureScroller.MaterialSelected -= GetMaterialChange;

                textureScroller.TextureSelected += GetTextureChange;
                textureScroller.MaterialSelected += GetMaterialChange;
            }
        }

        public void UpdatePanel()
        {
            if (textureScroller.currentSelectionPanel == this)
            {
                ClearExistingSlots();
                emsSliderCreated = false;
                if (currentMaterial != null)
                {
                    Debug.Log($"VariationTextureSlotsPanel: UpdatePanel: Current material: {currentMaterial.name}");

                    // Create slots based on the preferred order
                    foreach (string slotName in orderedSlotNames)
                    {
                        if (currentMaterial.HasProperty(slotName))
                        {
                            Texture texture = currentMaterial.GetTexture(slotName);
                            CreateSlot(slotName, texture);
                        }
                    }

                    // Optionally, create slots for any remaining material properties not covered by preferredOrder
                    foreach (string slotName in textureSlots)
                    {
                        if (!orderedSlotNames.Contains(slotName) && currentMaterial.HasProperty(slotName))
                        {
                            Texture texture = currentMaterial.GetTexture(slotName);
                            CreateSlot(slotName, texture);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("No current material set.");
                }
            }
            else
            {
                Debug.Log("This panel is not the current selection panel, so it will not be updated.");
            }
        }

        private void ClearExistingSlots()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void CreateSlot(string slotName, Texture texture)
        {
            GameObject slotInstance = Instantiate(slotPrefab, transform);
            TextMeshProUGUI slotText = slotInstance.transform.Find("SlotNameText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI buttonText = slotInstance.transform.Find("TextureButton/Text").GetComponent<TextMeshProUGUI>();
            slotText.text = slotName;
            buttonText.text = texture ? texture.name : "None";

            Button textureButton = slotInstance.transform.Find("TextureButton").GetComponent<Button>();
            textureButton.onClick.RemoveAllListeners();
            textureButton.onClick.AddListener(() => {
                OpenTextureSelection(slotName, buttonText); // Existing functionality remains unchanged
            });

            EventTrigger eventTrigger = textureButton.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry rightClickEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };

            rightClickEntry.callback.AddListener((data) => {
                if (((PointerEventData)data).button == PointerEventData.InputButton.Right)
                {
                    GetTextureChange(null, slotName); // Remove texture on right-click
                    buttonText.text = "None"; // Reset button text
                }
            });
            eventTrigger.triggers.Add(rightClickEntry);

            if (currentMaterial.HasProperty("_ems") && slotName.Equals("_ems", StringComparison.OrdinalIgnoreCase) && !emsSliderCreated)
            {
                GameObject emsSliderInstance = Instantiate(variationEMSTextureSliderPrefab, transform);
                Slider emsSlider = emsSliderInstance.transform.Find("emsSlider").GetComponent<Slider>();
                if (emsSlider != null)
                {
                    // Set up the slider
                    emsSlider.minValue = 0.0f;
                    emsSlider.maxValue = 1.0f;
                    emsSlider.value = 0.7f;
                    emsSlider.onValueChanged.AddListener((value) =>
                    {
                        variationBuilder.SetEmissiveIntensity(value, slotName);
                    });
                    emsSliderCreated = true;

                    // Move the slider instance to directly follow the slot instance in the UI hierarchy
                    emsSliderInstance.transform.SetSiblingIndex(slotInstance.transform.GetSiblingIndex() + 1);
                }
                else
                {
                    Debug.LogError("Failed to find Slider component on 'emsSlider' child within VariationEMSTextureSlider instance.");
                }
            }
        }

        private void OpenTextureSelection(string slotName, TextMeshProUGUI buttonText)
        {
            Debug.Log($"OpenTextureSelection for slot {slotName}");
            this.currentSlotName = slotName;
            this.currentButtonClickedText = buttonText; // Store the buttonText for later use
            if (textureScroller != null)
            {
                textureScroller.PrepareForSelection(this, slotName);
            }
        }

        public void GetMaterialChange(Material material)
        {
            if (material != null)
            {
                Debug.Log($"Applying material change: {material.name}");

                variationBuilder.ApplyMaterialDirectly(material);
                currentMaterial = material;
                RefreshMaterial(material);
                UpdatePanel();
            }
            else
            {
                if (material == null)
                    Debug.LogError("Failed to apply material change: Material is null.");
            }
        }

        public void GetTextureChange(Texture2D texture, string slotName)
        {
            if (TargetRenderer != null && currentMaterial != null)
            {
                // Attempt to find the material on the renderer that matches the currentMaterial by name
                Material foundMaterial = TargetRenderer.sharedMaterials.FirstOrDefault(m => m.name.Split(' ')[0] == currentMaterial.name.Split(' ')[0]);

                if (foundMaterial != null)
                {
                    // Delegate the task to VariationBuilder, providing the found material instance
                    variationBuilder.ApplyTextureChange(TargetRenderer, slotName, texture);

                    // Refresh the currentMaterial reference to ensure it points to the updated instance
                    currentMaterial = foundMaterial;

                    // Update the button text of the currently active slot to reflect the selected texture name
                    if (currentButtonClickedText != null)
                    {
                        currentButtonClickedText.text = texture ? texture.name : "None";
                    }

                    RefreshMaterial(currentMaterial);
                }
                else
                {
                    Debug.LogError($"Material {currentMaterial.name} not found on renderer {TargetRenderer.gameObject.name}. Make sure you're referencing the correct material instance.");
                }
            }
            else
            {
                Debug.LogError("Context is incomplete for applying texture change. Ensure correct slot and material.");
            }
        }
    }
}