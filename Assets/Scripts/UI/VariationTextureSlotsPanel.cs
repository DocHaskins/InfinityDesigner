using System.Collections.Generic;
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
        private TextureScroller textureScroller;
        private VariationBuilder variationBuilder;

        public GameObject slotPrefab;
        [SerializeField] private GameObject textureScrollerPanelPrefab;
        private GameObject currentPanel;
        public Material currentMaterial;
        public string currentSlotName;
        public GameObject currentModel; // This remains constant as per your new requirements

        private readonly string[] textureSlots = {
        "_msk", "_idx", "_gra", "_spc", "_clp", "_rgh", "_ocl", "_ems", "_dif_1", "_dif", "_nrm",
        "_BaseColorMap", "_NormalMap", "_MaskMap", "_EmissiveColorMap"
    };

        void Awake()
        {
            textureScroller = FindObjectOfType<TextureScroller>();
            variationBuilder = FindObjectOfType<VariationBuilder>();

            if (textureScroller != null)
            {
                // Ensure this matches the new method signature
                textureScroller.TextureSelected += OnTextureSelected;
            }
        }

        public void SetMaterial(Material material)
        {
            this.currentMaterial = material;
            Debug.Log($"SetMaterial called with {material.name}");
            UpdatePanel();
        }


        public void InitializePanel(GameObject model, Material material, string slotName)
        {
            this.currentModel = model;
            this.currentMaterial = material;
            this.currentSlotName = slotName;
            Debug.Log($"InitializePanel called with Model: {model.name}, Material: {material.name}, Slot: {slotName}");
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

            // Setup button listener
            Button textureButton = slotInstance.transform.Find("TextureButton").GetComponent<Button>();
            textureButton.onClick.RemoveAllListeners();
            textureButton.onClick.AddListener(() => {
                this.currentSlotName = slotName;
                OpenTextureSelection(slotName);
            });
        }

        private void OpenTextureSelection(string slotName)
        {
            // Direct the TextureScroller to refresh textures based on the current slot's needs
            if (textureScroller != null)
            {
                textureScroller.RefreshTextures(slotName);
            }
        }

        private void OnTextureSelected(Texture2D texture, GameObject model, string slotName)
        {
            this.currentSlotName = slotName;
            ApplyTextureToMaterial(texture, slotName);
        }

        private void ApplyTextureToMaterial(Texture2D texture, string slotName)
        {
            Debug.Log($"Attempting to apply texture {texture.name} to slot {slotName} of material {currentMaterial?.name ?? "null"}.");
            if (currentMaterial == null)
            {
                Debug.LogError("currentMaterial is null. Ensure it's assigned before calling ApplyTextureToMaterial.");
                return;
            }
            if (currentMaterial != null && currentMaterial.HasProperty(slotName))
            {
                Debug.Log($"Applying texture {texture.name} to material {currentMaterial.name} for slot {slotName}");

                // Create a new instance of the material
                Material materialInstance = new Material(currentMaterial);
                Debug.Log($"Created new material instance {materialInstance.name} from {currentMaterial.name}.");
                materialInstance.SetTexture(slotName, texture);
                Debug.Log($"Set texture {texture.name} to slot {slotName} on material instance {materialInstance.name}.");

                // Apply the material instance to the object
                ApplyMaterialInstanceToModel(materialInstance);

                Debug.Log($"Applied texture {texture.name} to {slotName}");

                if (variationBuilder != null)
                {
                    variationBuilder.RecordTextureChange(slotName, texture, materialInstance, currentModel);
                    Debug.Log($"Recorded texture change for slot {slotName}");
                }
            }
            else
            {
                Debug.LogWarning($"Material {currentMaterial?.name ?? "null"} does not have slot {slotName} or is null.");
            }
        }

        private void ApplyMaterialInstanceToModel(Material materialInstance)
        {
            SkinnedMeshRenderer renderer = currentModel.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                Debug.Log($"Applying material instance {materialInstance.name} to renderer on {currentModel.name}.");
                // Assume you want to apply the material instance to the first material slot
                Material[] materials = renderer.materials;
                materials[0] = materialInstance;
                renderer.materials = materials;

                // Update the currentMaterial to the new instance
                currentMaterial = materialInstance;
            }
            else
            {
                Debug.LogWarning($"Renderer not found on {currentModel?.name ?? "null"}.");
            }
        }

        void OnDestroy()
        {
            if (textureScroller != null)
            {
                textureScroller.TextureSelected -= OnTextureSelected; // Correctly unsubscribe
            }
        }
    }
}