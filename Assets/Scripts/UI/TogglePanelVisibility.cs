using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace doppelganger
{
    public class TogglePanelVisibility : MonoBehaviour
    {
        [Header("Managers")]
        public VariationBuilder variationBuilder;
        public TextureScroller textureScroller;

        [Header("Hierarchy References")]
        public Transform spawnPoint;

        [Header("UI Elements")]
        public GameObject variationTextureSlotPanelPrefab;
        public GameObject panelGameObject;

        [Header("Current Model")]
        private GameObject currentModel;
        private SkinnedMeshRenderer currentRenderer;
        private Material currentMaterial;
        private string currentSlotName;

        public void Setup(VariationBuilder builder, GameObject model, SkinnedMeshRenderer renderer, Material material, string slotName)
        {
            variationBuilder = builder;
            currentModel = model;
            currentRenderer = renderer;
            currentMaterial = material;
            currentSlotName = slotName;
        }

        public bool TogglePanel()
        {
            if (panelGameObject == null)
            {
                Debug.Log("Instantiating new panelGameObject.");
                panelGameObject = Instantiate(variationTextureSlotPanelPrefab, spawnPoint, false);

                // Give the panel a unique name for identification
                string uniquePanelName = "Panel_" + currentSlotName + "_" + currentRenderer.name; // Customize this based on your needs
                panelGameObject.name = uniquePanelName;

                VariationTextureSlotsPanel panelScript = panelGameObject.GetComponent<VariationTextureSlotsPanel>();
                if (panelScript != null)
                {
                    panelScript.SetMaterialModelAndRenderer(currentMaterial, currentModel, currentRenderer, currentSlotName);
                    variationBuilder.RegisterPanelScript(currentRenderer, panelScript);
                    if (textureScroller != null)
                    {
                        textureScroller.SetCurrentSelectionPanel(panelScript);
                    }
                    panelScript.UpdatePanel();
                }
                else
                {
                    Debug.LogError("Failed to get VariationTextureSlotsPanel component on instantiated panelGameObject.");
                }

                panelGameObject.SetActive(true); // Set to active since we just instantiated it.
                return true; // Panel is now active.
            }
            else
            {
                bool newState = !panelGameObject.activeSelf;
                panelGameObject.SetActive(newState);

                if (newState)
                {
                    if (textureScroller != null)
                    {
                        textureScroller.SetCurrentSelectionPanel(panelGameObject.GetComponent<VariationTextureSlotsPanel>());
                    }
                }
                else
                {
                    if (textureScroller != null && textureScroller.GetCurrentSelectionPanel() == panelGameObject.GetComponent<VariationTextureSlotsPanel>())
                    {
                        textureScroller.ClearCurrentSelectionPanel();
                    }
                }
                return newState;
            }
        }

        public void DeactivatePanel()
        {
            if (panelGameObject != null)
            {
                panelGameObject.SetActive(false);
            }
        }
    }
}