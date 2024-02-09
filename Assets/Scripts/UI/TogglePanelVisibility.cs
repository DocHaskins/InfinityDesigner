using UnityEngine;
using UnityEngine.UI;

namespace doppelganger
{

    public class TogglePanelVisibility : MonoBehaviour
    {
        public Transform spawnPoint;
        public GameObject dropdownGameObject;
        public GameObject variationTextureSlotPanelPrefab;
        private static GameObject existingPanel = null; // Make static to keep track across instances

        // Adjusted to toggle visibility instead of destroying and recreating
        public void TogglePanel(string slotName, Material material, GameObject currentModel)
        {
            // Check if the panel already exists
            if (existingPanel != null)
            {
                // Toggle the visibility of the existing panel
                bool isActive = existingPanel.activeSelf;
                existingPanel.SetActive(!isActive);

                // Re-enable siblings if hiding the panel
                ToggleSiblings(isActive);
            }
            else
            {
                // Instantiate a new panel if it doesn't exist
                existingPanel = Instantiate(variationTextureSlotPanelPrefab, spawnPoint, false);
                SetupPanel(existingPanel, slotName, material, currentModel);

                // Disable siblings
                ToggleSiblings(false);
            }

            if (existingPanel != null && existingPanel.activeSelf)
            {
                var panelScript = existingPanel.GetComponent<VariationTextureSlotsPanel>();
                if (panelScript != null)
                {
                    panelScript.currentModel = currentModel;
                    panelScript.currentSlotName = slotName;
                    panelScript.currentMaterial = material;
                    panelScript.UpdatePanel();
                }
            }
        }

        private void SetupPanel(GameObject panel, string slotName, Material material, GameObject currentModel)
        {
            VariationTextureSlotsPanel panelScript = panel.GetComponent<VariationTextureSlotsPanel>();
            if (panelScript != null)
            {
                panelScript.currentModel = currentModel;
                panelScript.currentSlotName = slotName;
                panelScript.SetupPanel(material);
            }
        }

        private void ToggleSiblings(bool enable)
        {
            // Toggle the active state of all sibling elements, except the dropdown and the panel itself
            foreach (Transform child in spawnPoint)
            {
                if (child.gameObject != dropdownGameObject && child.gameObject != existingPanel)
                {
                    child.gameObject.SetActive(enable);
                }
            }
        }
    }
}