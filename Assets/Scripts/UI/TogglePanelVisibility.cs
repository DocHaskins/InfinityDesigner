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
        public GameObject panelGameObject;
        public GameObject dropdownGameObject;
        public GameObject variationTextureSlotPanelPrefab;
        private static GameObject existingPanel = null;

        public void TogglePanel(string slotName, Material material, GameObject currentModel)
        {
            bool isPanelActiveBeforeToggle = panelGameObject.activeSelf;

            if (!isPanelActiveBeforeToggle)
            {
                // If the panel is about to be opened, set the material first
                var panelScript = panelGameObject.GetComponent<VariationTextureSlotsPanel>();
                if (panelScript != null)
                {
                    panelScript.SetMaterial(material);
                    panelScript.currentModel = currentModel;
                    panelScript.currentSlotName = slotName;
                    panelScript.UpdatePanel();
                }
            }

            // Toggle the panel's visibility after ensuring the material is set
            panelGameObject.SetActive(!isPanelActiveBeforeToggle);

            if (isPanelActiveBeforeToggle)
            {
                // Additional actions if needed when the panel is being closed
            }
        }

        public void InitializePanel(GameObject model, Material material, string slotName)
        {
            if (panelGameObject != null)
            {
                var panelScript = panelGameObject.GetComponent<VariationTextureSlotsPanel>();
                if (panelScript != null)
                {
                    // Direct assignment of the material to the panel script
                    panelScript.currentModel = model;
                    panelScript.currentMaterial = material;
                    panelScript.currentSlotName = slotName;
                    panelScript.UpdatePanel(); // Update the panel to reflect the new material's textures
                }
            }
        }

        public void UpdatePanelSetup(GameObject panel, string slotName, Material material, GameObject currentModel)
        {
            var panelScript = panel.GetComponent<VariationTextureSlotsPanel>();
            if (panelScript != null)
            {
                // Update the panel script with the current material and model
                panelScript.currentModel = currentModel;
                panelScript.currentMaterial = material;
                panelScript.currentSlotName = slotName;
                panelScript.UpdatePanel(); // Refresh the panel to show the correct texture slots
            }
        }

        public void ToggleOtherDropdowns(bool enable)
        {
            if (variationBuilder != null && VariationBuilder.allDropdowns != null)
            {
                for (int i = VariationBuilder.allDropdowns.Count - 1; i >= 0; i--)
                {
                    var dropdown = VariationBuilder.allDropdowns[i];
                    // Check if the GameObject reference is still valid
                    if (dropdown != null && dropdown != this.dropdownGameObject)
                    {
                        // Check if the GameObject has not been destroyed
                        if (dropdown.activeSelf != enable) // This also acts as an indirect check for existence
                        {
                            dropdown.SetActive(enable);
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("VariationBuilder reference or its dropdowns list is not set.");
            }
        }
    }
}