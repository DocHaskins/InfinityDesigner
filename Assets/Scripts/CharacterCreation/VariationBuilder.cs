using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace doppelganger
{
    public class VariationBuilder : MonoBehaviour
    {
        [Header("Managers")]
        public CharacterBuilder characterBuilder;

        [Header("Interface")]
        public GameObject modelInfoPanelPrefab;
        public GameObject variationMaterialDropdownPrefab;

        public void OpenModelInfoPanel(string slotName)
        {
            GameObject modelInfoPanel = Instantiate(modelInfoPanelPrefab, FindObjectOfType<Canvas>().transform, false);
            modelInfoPanel.name = slotName + "ModelInfoPanel";

            if (characterBuilder.currentlyLoadedModels.TryGetValue(slotName, out GameObject currentModel))
            {
                TextMeshProUGUI meshNameText = modelInfoPanel.transform.Find("MeshName").GetComponent<TextMeshProUGUI>();
                meshNameText.text = currentModel.name;

                Transform materialSpawn = modelInfoPanel.transform.Find("VariationSubPanel/materialSpawn");

                SkinnedMeshRenderer[] renderers = currentModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var renderer in renderers)
                {
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        Material material = renderer.sharedMaterials[i];
                        GameObject dropdownGameObject = Instantiate(variationMaterialDropdownPrefab, materialSpawn);
                        dropdownGameObject.name = $"{renderer.name}_MaterialDropdown_{i}";

                        TextMeshProUGUI nameText = dropdownGameObject.transform.Find("Name").GetComponent<TextMeshProUGUI>();
                        nameText.text = material.name;

                        TMP_Dropdown tmpDropdown = dropdownGameObject.transform.Find("Dropdown").GetComponent<TMP_Dropdown>();
                        SetupDropdownWithMaterials(tmpDropdown, material.name);
                    }
                }
            }
            else
            {
                Debug.LogError($"Model for slot {slotName} not found.");
            }
        }

        void SetupDropdownWithMaterials(TMP_Dropdown tmpDropdown, string currentMaterialName)
        {
            tmpDropdown.ClearOptions();

            // Placeholder for material names, replace with actual material names from your project
            List<string> materialNames = new List<string> { "Material1", "Material2", currentMaterialName };

            tmpDropdown.AddOptions(materialNames);

            int currentIndex = materialNames.IndexOf(currentMaterialName);
            if (currentIndex != -1)
            {
                tmpDropdown.value = currentIndex;
            }
        }
    }
}
