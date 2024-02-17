using System.Collections.Generic;
using UnityEngine;
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
            // Ensure panelGameObject is not null before attempting to toggle its visibility
            if (panelGameObject != null)
            {
                bool panelIsActive = panelGameObject.activeSelf;
                panelGameObject.SetActive(!panelIsActive);
            }
            else
            {
                Debug.LogError("panelGameObject is null. Ensure it's correctly assigned before calling TogglePanel.");
            }

            // If additional logic for updating panel setup is needed, ensure it's safely executed
            if (panelGameObject != null && panelGameObject.activeSelf)
            {
                UpdatePanelSetup(panelGameObject, slotName, material, currentModel);
            }
        }

        public void UpdatePanelSetup(GameObject panel, string slotName, Material material, GameObject currentModel)
        {
            if (panel.activeSelf)
            {
                var panelScript = panel.GetComponent<VariationTextureSlotsPanel>();
                if (panelScript != null)
                {
                    panelScript.currentModel = currentModel;
                    panelScript.currentSlotName = slotName;
                    panelScript.currentMaterial = material;
                    panelScript.UpdatePanel();
                }
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