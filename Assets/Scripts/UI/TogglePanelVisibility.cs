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

        public void TogglePanel()
        {
            if (dropdownGameObject.activeSelf) // Only toggle the panel if the dropdownGameObject is active
            {
                bool isPanelActiveBeforeToggle = panelGameObject.activeSelf;
                panelGameObject.SetActive(!isPanelActiveBeforeToggle);
            }
        }

        public void ToggleOtherDropdowns(bool enable)
        {
            if (variationBuilder != null && VariationBuilder.allLabels != null)
            {
                for (int i = VariationBuilder.allLabels.Count - 1; i >= 0; i--)
                {
                    var dropdown = VariationBuilder.allLabels[i];
                    if (dropdown != null && dropdown != this.dropdownGameObject)
                    {
                        if (dropdown.activeSelf != enable)
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