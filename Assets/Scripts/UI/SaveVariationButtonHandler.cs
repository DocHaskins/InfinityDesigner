using doppelganger;
using UnityEngine;
using UnityEngine.UI;

public class SaveVariationButtonHandler : MonoBehaviour
{
    private VariationBuilder variationBuilder;

    void Start()
    {
        // Find VariationBuilder in the scene
        variationBuilder = FindObjectOfType<VariationBuilder>();
        if (variationBuilder == null)
        {
            Debug.LogError("SaveVariationButtonHandler: Failed to find VariationBuilder in the scene.");
            return;
        }

        Button saveButton = GetComponent<Button>();
        if (saveButton != null)
        {
            // Use a lambda to call SaveNewVariation with correct context
            saveButton.onClick.AddListener(() => variationBuilder.SaveNewVariation());
        }
        else
        {
            Debug.LogError("SaveVariationButtonHandler: Button component not found.");
        }
    }
}