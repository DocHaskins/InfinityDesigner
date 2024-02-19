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

        public void TogglePanel(string slotName, Material material, SkinnedMeshRenderer renderer)
        {
            // Toggle the panel's visibility
            panelGameObject.SetActive(!panelGameObject.activeSelf);

            if (panelGameObject.activeSelf)
            {
                // Initialize or update the panel only if it's being opened
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