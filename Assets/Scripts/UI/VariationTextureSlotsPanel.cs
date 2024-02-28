using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
using UnlimitedScrollUI.Example;
using static ModelData;
using UnityEngine.Rendering;

namespace doppelganger
{
    public class VariationTextureSlotsPanel : MonoBehaviour
    {
        public TextureScroller textureScroller;
        public VariationBuilder variationBuilder;

        public SkinnedMeshRenderer TargetRenderer;
        public GameObject slotPrefab;
        [SerializeField] private GameObject textureScrollerPanelPrefab;
        private GameObject currentPanel;
        public GameObject currentModel;
        public Material currentMaterial;
        public string currentSlotName;
        public TextMeshProUGUI currentButtonClickedText;

        private readonly string[] textureSlots = {
        "_msk", "_idx", "_gra", "_spc", "_clp", "_rgh", "_ocl", "_ems", "_dif_1", "_dif", "_nrm",
        "_BaseColorMap", "_NormalMap", "_MaskMap", "_EmissiveColorMap"
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
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            ClearExistingSlots();
            
            if (currentMaterial != null)
            {
                Debug.Log($"VariationTextureSlotsPanel: UpdatePanel: Current material: {currentMaterial.name}");

                foreach (string slotName in textureSlots)
                {
                    if (currentMaterial.HasProperty(slotName))
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
                OpenTextureSelection(slotName, buttonText);
            });

            EventTrigger eventTrigger = textureButton.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry rightClickEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };

            rightClickEntry.callback.AddListener((data) => {
                if (((PointerEventData)data).button == PointerEventData.InputButton.Right)
                {
                    GetTextureChange(null, slotName);
                    buttonText.text = "None";
                }
            });
            eventTrigger.triggers.Add(rightClickEntry);
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