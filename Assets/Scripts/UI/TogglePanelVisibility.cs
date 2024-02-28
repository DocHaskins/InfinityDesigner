using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace doppelganger
{
    public class TogglePanelVisibility : MonoBehaviour
    {
        public VariationBuilder variationBuilder;
        public Transform spawnPoint;
        public Transform texturePrefabSpawnPoint;
        public GameObject variationTextureSlotPanelPrefab;
        public GameObject panelGameObject;

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

                // Use the found TexturePrefabSpawnPoint as the parent for the instantiated prefab
                panelGameObject = Instantiate(variationTextureSlotPanelPrefab, spawnPoint, false);
                VariationTextureSlotsPanel panelScript = panelGameObject.GetComponent<VariationTextureSlotsPanel>();
                if (panelScript != null)
                {
                    panelScript.SetMaterialModelAndRenderer(currentMaterial, currentModel, currentRenderer, currentSlotName);
                    variationBuilder.RegisterPanelScript(currentRenderer, panelScript);
                }
                else
                {
                    Debug.LogError("Failed to get VariationTextureSlotsPanel component on instantiated panelGameObject.");
                }
                // Set to active since we just instantiated it.
                panelGameObject.SetActive(true);
                return true; // Panel is now active.
            }
            else
            {
                // Toggle current active state.
                bool newState = !panelGameObject.activeSelf;
                panelGameObject.SetActive(newState);
                return newState; // Return the new state.
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