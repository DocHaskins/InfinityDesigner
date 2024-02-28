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

namespace doppelganger
{
    public class VariationTextureSlotsPanel : MonoBehaviour
    {
        public TextureScroller textureScroller;
        public VariationBuilder variationBuilder;

        public SkinnedMeshRenderer TargetRenderer { get; private set; }
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
                textureScroller.TextureSelected += (texture, renderer, material, slotName) => GetTextureChange(texture, slotName);
                textureScroller.MaterialSelected += (material) => GetMaterialChange(material);
            }
        }

        void OnEnable()
        {
            variationBuilder = FindObjectOfType<VariationBuilder>();
            textureScroller = FindObjectOfType<TextureScroller>();
            if (textureScroller != null)
            {
                // Subscribe to events when the panel is enabled
                textureScroller.TextureSelected += OnTextureSelected;
                textureScroller.MaterialSelected += GetMaterialChange;
            }
        }

        void OnDisable()
        {
            if (textureScroller != null)
            {
                // Unsubscribe from events when the panel is disabled
                textureScroller.TextureSelected -= OnTextureSelected;
                textureScroller.MaterialSelected -= GetMaterialChange;
            }
        }

        private void OnTextureSelected(Texture2D texture, SkinnedMeshRenderer renderer, Material material, string slotName)
        {
            // This is a new method that handles texture selection.
            GetTextureChange(texture, slotName);
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
            Debug.Log($"SetMaterialModelAndRenderer: material: {material} Current model: {model.name}, renderer: {renderer.name}, slotName: {slotName}");
            this.currentMaterial = new Material(renderer.sharedMaterials.FirstOrDefault(m => m.name == material.name) ?? material);
            this.currentModel = model;
            this.TargetRenderer = renderer;
            this.currentSlotName = slotName;
            variationBuilder.RegisterPanelScript(renderer, this); // Register this panel script
            UpdatePanel();
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

            // Setup UI elements
            TextMeshProUGUI slotText = slotInstance.transform.Find("SlotNameText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI buttonText = slotInstance.transform.Find("TextureButton/Text").GetComponent<TextMeshProUGUI>();
            slotText.text = slotName;
            buttonText.text = texture ? texture.name : "None";

            Button textureButton = slotInstance.transform.Find("TextureButton").GetComponent<Button>();
            textureButton.onClick.RemoveAllListeners();
            textureButton.onClick.AddListener(() => {
                OpenTextureSelection(slotName, buttonText); // Pass buttonText to OpenTextureSelection
            });

            // Add an EventTrigger component for detecting right-clicks
            EventTrigger eventTrigger = textureButton.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry rightClickEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };

            // Define what happens on a right-click
            rightClickEntry.callback.AddListener((data) => {
                if (((PointerEventData)data).button == PointerEventData.InputButton.Right)
                {
                    // Directly use GetTextureChange with null to clear the texture
                    GetTextureChange(null, slotName);
                    buttonText.text = "None"; // Update the button text to indicate no texture
                }
            });

            // Add the entry to the event trigger
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
            Debug.Log($"GetMaterialChange {material}, TargetRenderer {TargetRenderer}");
            if (TargetRenderer != null && material != null)
            {
                // Use the ApplyMaterialDirectly method to apply the material to the target renderer
                variationBuilder.ApplyMaterialDirectly(TargetRenderer.name, material.name);

                // Update the currentMaterial reference to the new material
                currentMaterial = material;

                // Optionally, refresh the UI or do additional updates as needed
                RefreshMaterial(currentMaterial);
            }
            else
            {
                //Debug.LogError("Incomplete context for applying material change.");
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
                Debug.LogError("Incomplete context for applying texture change.");
            }
        }


#if UNITY_EDITOR
        private void LogShaderProperties(Material material)
        {
            Debug.Log($"Logging properties for shader: {material.shader.name}");
            int propertyCount = UnityEditor.ShaderUtil.GetPropertyCount(material.shader);
            for (int i = 0; i < propertyCount; i++)
            {
                var propName = UnityEditor.ShaderUtil.GetPropertyName(material.shader, i);
                var propType = UnityEditor.ShaderUtil.GetPropertyType(material.shader, i);
                Debug.Log($"Property {i}: Name = {propName}, Type = {propType}");
            }
        }
#endif
    }
}