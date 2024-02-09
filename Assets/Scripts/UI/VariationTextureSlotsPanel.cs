using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnlimitedScrollUI.Example;

namespace doppelganger
{
    public class VariationTextureSlotsPanel : MonoBehaviour
    {
        private TextureScroller textureScroller;

        public GameObject slotPrefab;
        [SerializeField] GameObject textureScrollerPanelPrefab;
        private GameObject currentPanel;
        private Material currentMaterial;
        public string currentSlotName;
        public GameObject currentModel;

        private readonly string[] textureSlots = {
        "_msk", "_idx", "_gra", "_spc", "_clp", "_rgh", "_ocl", "_ems", "_dif_1", "_dif", "_nrm",
        "_BaseColorMap", "_NormalMap", "_MaskMap", "_EmissiveColorMap"
    };

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
            // Clear existing slots
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // Generate new slots based on the provided material
            foreach (string slotName in textureSlots)
            {
                if (material.HasProperty(slotName))
                {
                    Texture texture = material.GetTexture(slotName);
                    // Log the slot name and the texture name (if available)
                    string textureName = texture ? texture.name : "None";
                    //Debug.Log($"Slot {slotName} has texture: {textureName}");

                    CreateSlot(material, slotName);
                }
            }
        }

        private void ClearExistingSlots()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void CreateSlot(Material material, string slotName)
        {
            if (material.HasProperty(slotName))
            {
                Texture texture = material.GetTexture(slotName);
                GameObject slotInstance = Instantiate(slotPrefab, transform);

                TextMeshProUGUI slotText = slotInstance.transform.Find("SlotNameText").GetComponent<TextMeshProUGUI>();
                // Assuming "TextureButton/Text" is the path to the TextMeshPro component within the button
                TextMeshProUGUI buttonText = slotInstance.transform.Find("TextureButton/Text").GetComponent<TextMeshProUGUI>();

                slotText.text = $"{slotName}";
                buttonText.text = texture ? texture.name : "None";

                // Setup the listener for the button
                Button textureButton = slotInstance.transform.Find("TextureButton").GetComponent<Button>();
                textureButton.onClick.AddListener(() => OnTextureButtonClicked(slotName, material));
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

                // Instantiate the TextureScrollerPanel as a child of the found Canvas
                GameObject textureScrollerPanelObject = Instantiate(textureScrollerPanelPrefab, canvas.transform, false);
                textureScroller = textureScrollerPanelObject.GetComponentInChildren<TextureScroller>();
            }

            if (textureScroller != null)
            {
                textureScroller.SetCurrentSlotName(slotName);
                textureScroller.TextureSelected += (texture, model, _) => {
                    ApplyTextureToMaterial(currentMaterial, model, slotName, texture); // Use captured slotName
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
            var propertyBlock = new MaterialPropertyBlock();
            SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            bool textureApplied = false;

            foreach (var renderer in renderers)
            {
                // Apply the texture using a MaterialPropertyBlock
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (renderer.materials[i].HasProperty(slotName))
                    {
                        renderer.GetPropertyBlock(propertyBlock, i);
                        propertyBlock.SetTexture(slotName, texture);
                        renderer.SetPropertyBlock(propertyBlock, i);
                        Debug.Log($"Applied {texture.name} to {slotName} on renderer {renderer.name} for material index {i}.");
                        textureApplied = true;
                        // No break here to apply the texture to all materials that have the slotName property
                    }
                }

                if (textureApplied)
                {
                    // Optionally break out of the loop once the texture has been applied to at least one material
                    // break;
                }
            }

            if (!textureApplied)
            {
                Debug.LogWarning($"No material found on {model.name} with slot '{slotName}' capable of receiving the texture {texture.name}.");
            }
        }
    }
}