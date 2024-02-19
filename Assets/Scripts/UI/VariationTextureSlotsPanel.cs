using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
            this.currentMaterial = renderer.sharedMaterials.FirstOrDefault(m => m.name == material.name) ?? material;
            this.currentModel = model;
            this.TargetRenderer = renderer;
            this.currentSlotName = slotName;
            variationBuilder.RegisterPanelScript(renderer, this); // Register this panel script
            UpdatePanel();
        }

        public void UpdatePanel()
        {
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

            // Setup button listener to pass slotName along with current context
            Button textureButton = slotInstance.transform.Find("TextureButton").GetComponent<Button>();
            textureButton.onClick.RemoveAllListeners();
            textureButton.onClick.AddListener(() => {
                OpenTextureSelection(slotName);
            });
        }

        private void OpenTextureSelection(string slotName)
        {
            Debug.Log($"OpenTextureSelection for slot {slotName} on material {currentMaterial.name}, Shader {currentMaterial.shader.name}");
            this.currentSlotName = slotName;
            if (textureScroller != null)
            {
                // Update the textureScroller with the current context
                textureScroller.PrepareForSelection(this, slotName);
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