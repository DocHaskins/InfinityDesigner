using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnlimitedScrollUI.Example;
using static ModelData;

namespace doppelganger
{
    public class VariationTextureSlotsPanel : MonoBehaviour
    {
        private TextureScroller textureScroller;
        private VariationBuilder variationBuilder;

        public GameObject slotPrefab;
        [SerializeField] GameObject textureScrollerPanelPrefab;
        private GameObject currentPanel;
        public Material currentMaterial;
        public string currentSlotName;
        public GameObject currentModel;

        public List<RttiValue> currentMaterialResources = new List<RttiValue>();

        private readonly string[] textureSlots = {
        "_msk", "_idx", "_gra", "_spc", "_clp", "_rgh", "_ocl", "_ems", "_dif_1", "_dif", "_nrm",
        "_BaseColorMap", "_NormalMap", "_MaskMap", "_EmissiveColorMap"
    };

        void Awake()
        {
            // Find the VariationBuilder instance in the scene
            variationBuilder = FindObjectOfType<VariationBuilder>();
        }

        public void ToggleVisibility(Material material, GameObject dropdownGameObject)
        {
            // Check if the panel is already active and if it's showing the current material
            bool panelIsActive = currentPanel != null && currentPanel.activeSelf;
            bool showingCurrentMaterial = currentMaterial == material;

            if (panelIsActive && showingCurrentMaterial)
            {
                // If the panel is active and showing the current material, simply toggle it off
                currentPanel.SetActive(false);
            }
            else
            {
                // Either the panel is not active or it's showing a different material
                // Clear existing slots only if we are switching materials
                if (!showingCurrentMaterial)
                {
                    ClearExistingSlots();
                    currentMaterial = material; // Update the reference to the current material
                }

                // Generate slots for the material if not already showing the current material
                if (!showingCurrentMaterial)
                {
                    SetupPanel(material);
                }
            }
        }

        public void SetupPanel(Material material)
        {
            // Disable existing slots instead of destroying them
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }

            // Generate new slots or reuse existing ones based on the provided material
            foreach (string slotName in textureSlots)
            {
                if (material.HasProperty(slotName))
                {
                    Texture texture = material.GetTexture(slotName);
                    GameObject slotInstance = null;

                    // Try to find an existing slot that matches the slotName and reuse it
                    foreach (Transform child in transform)
                    {
                        if (child.name == slotName)
                        {
                            slotInstance = child.gameObject;
                            break;
                        }
                    }

                    // If no existing slot was found, create a new one
                    if (slotInstance == null)
                    {
                        slotInstance = Instantiate(slotPrefab, transform);
                        slotInstance.name = slotName; // Set the name of the slotInstance to match the slotName for easy identification
                    }

                    // Update the slotInstance's visibility and contents
                    slotInstance.SetActive(true);
                    UpdateSlot(slotInstance, material, slotName, texture);
                }
            }
        }

        private void UpdateSlot(GameObject slotInstance, Material material, string slotName, Texture texture)
        {

            TextMeshProUGUI slotText = slotInstance.transform.Find("SlotNameText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI buttonText = slotInstance.transform.Find("TextureButton/Text").GetComponent<TextMeshProUGUI>();

            slotText.text = slotName; // Ensure this is the correct slot name
            buttonText.text = texture ? texture.name : "None";

            Button textureButton = slotInstance.transform.Find("TextureButton").GetComponent<Button>();

            // Update the listener to ensure the correct slotName is used
            textureButton.onClick.RemoveAllListeners();
            textureButton.onClick.AddListener(delegate { OnTextureButtonClicked(slotName, material); });
        }

        public void UpdatePanel()
        {
            ClearExistingSlots();

            if (currentMaterial != null)
            {
                Debug.Log($"Current material: {currentMaterial.name}");

                // Iterate through each slotName defined in textureSlots
                foreach (string slotName in textureSlots)
                {
                    if (currentMaterial.HasProperty(slotName))
                    {
                        Texture texture = currentMaterial.GetTexture(slotName);
                        CreateSlot(currentMaterial, slotName);
                    }
                }
            }
            else
            {
                Debug.LogWarning("No current material set.");
            }

            Debug.Log("Panel update complete.");
        }

        private void ClearExistingSlots()
        {
            // Disable existing slots instead of destroying them
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }

        private void CreateSlot(Material material, string slotName)
        {
            if (material.HasProperty(slotName))
            {
                Texture texture = material.GetTexture(slotName);
                GameObject slotInstance = Instantiate(slotPrefab, transform);

                TextMeshProUGUI slotText = slotInstance.transform.Find("SlotNameText").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI buttonText = slotInstance.transform.Find("TextureButton/Text").GetComponent<TextMeshProUGUI>();

                slotText.text = slotName; // Ensure this is the correct slot name
                buttonText.text = texture ? texture.name : "None";

                Button textureButton = slotInstance.transform.Find("TextureButton").GetComponent<Button>();

                // Update the listener to ensure the correct slotName is used
                textureButton.onClick.RemoveAllListeners();
                textureButton.onClick.AddListener(delegate { OnTextureButtonClicked(slotName, material); });
            }
        }

        void OnTextureButtonClicked(string slotName, Material material)
        {
            currentSlotName = slotName;
            currentMaterial = material;

            Debug.Log($"currentSlotName {currentSlotName},currentMaterial {currentMaterial} ");

            // Find an existing TextureScroller in the scene
            GameObject scrollerGameObject = GameObject.FindWithTag("TextureScroller");
            TextureScroller textureScroller = null;

            if (scrollerGameObject != null)
            {
                // If found, get the TextureScroller component
                textureScroller = scrollerGameObject.GetComponentInChildren<TextureScroller>();
            }
            else
            {
                // Find the Canvas in the scene (assuming there's at least one)
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    Debug.LogError("No Canvas found in the scene for the TextureScrollerPanel.");
                    return;
                }
            }

            if (textureScroller != null)
            {
                textureScroller.SetCurrentSlotName(slotName);
                textureScroller.TextureSelected += (texture, model, _) => {
                    ApplyTextureToMaterial(currentMaterial, model, textureScroller.currentSlotName, texture);
                    UpdatePanel();
                };
                textureScroller.currentModel = currentModel;
                textureScroller.searchTerm = slotName;
                textureScroller.RefreshTextures(slotName);
            }
            else
            {
                Debug.LogError("TextureScroller component not found.");
            }
        }


        void ApplyTextureToMaterial(Material material, GameObject model, string slotName, Texture2D texture)
        {
            Debug.Log($"Applying texture {texture.name} to material {material.name}, slot {slotName}, on model {model.name}");

            var propertyBlock = new MaterialPropertyBlock();
            SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true); // Use true to include inactive children if necessary
            bool textureApplied = false;

            foreach (var renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials; // Consider using sharedMaterials to avoid creating material copies

                for (int i = 0; i < materials.Length; i++)
                {
                    // Check if this is the exact material instance we want to modify
                    if (materials[i] == material && material.HasProperty(slotName))
                    {
                        renderer.GetPropertyBlock(propertyBlock, i);
                        propertyBlock.SetTexture(slotName, texture);
                        renderer.SetPropertyBlock(propertyBlock, i);

                        Debug.Log($"Applied {texture.name} to {slotName} on renderer {renderer.name} for material index {i}.");

                        // Record the texture change if needed
                        if (variationBuilder != null)
                        {
                            variationBuilder.RecordTextureChange(slotName, texture, material);
                        }
                        else
                        {
                            Debug.LogError("VariationBuilder instance not found.");
                        }

                        textureApplied = true;
                        break; // Exit the loop after applying the texture to the intended material
                    }
                }

                if (textureApplied)
                {
                    break; // Stop checking other renderers once the texture has been applied to the intended material
                }
            }

            if (!textureApplied)
            {
                Debug.LogWarning($"No material found on {model.name} capable of receiving the texture {texture.name} for slot '{slotName}'.");
            }
        }
    }
}