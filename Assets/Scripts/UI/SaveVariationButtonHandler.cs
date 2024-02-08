using doppelganger;
using UnityEngine;
using UnityEngine.UI;

public class SaveVariationButtonHandler : MonoBehaviour
{
    private VariationWriter variationWriter;

    void Start()
    {
        // Find VariationBuilder in the scene
        variationWriter = FindObjectOfType<VariationWriter>();
        if (variationWriter == null)
        {
            Debug.LogError("SaveVariationButtonHandler: Failed to find VariationBuilder in the scene.");
            return;
        }

        Button saveButton = GetComponent<Button>();
        if (saveButton != null)
        {
            // Use a lambda to call SaveNewVariation with correct context
            saveButton.onClick.AddListener(() => variationWriter.SaveNewVariation());
        }
        else
        {
            Debug.LogError("SaveVariationButtonHandler: Button component not found.");
        }
    }
}