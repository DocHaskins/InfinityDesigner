using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using doppelganger;

public class DropdownFilter : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Dropdown dropdown;
    public CharacterBuilder characterBuilder; // Reference to the CharacterBuilder

    private List<TMP_Dropdown.OptionData> allOptions = new List<TMP_Dropdown.OptionData>();

    void Start()
    {
        // Initialize allOptions with current presets from CharacterBuilder
        UpdateOptionsFromCharacterBuilder();

        // Change listener to onEndEdit to trigger when Enter is pressed or input field loses focus
        inputField.onEndEdit.AddListener(delegate { FilterDropdownOptions(inputField.text); });
    }

    void UpdateOptionsFromCharacterBuilder()
    {
        // Assume CharacterBuilder has a method to get the current preset list as OptionData
        allOptions = characterBuilder.GetCurrentPresetOptions();
        dropdown.options = new List<TMP_Dropdown.OptionData>(allOptions); // Update dropdown with all options
        dropdown.RefreshShownValue();
    }

    void FilterDropdownOptions(string input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            // Filter based on input
            dropdown.options = allOptions.Where(option => option.text.ToLower().Contains(input.ToLower())).ToList();
        }
        else
        {
            // If input is empty, show all options
            dropdown.options = new List<TMP_Dropdown.OptionData>(allOptions);
        }

        dropdown.RefreshShownValue();
        dropdown.Show(); // Consider keeping the dropdown always visible after filtering
    }
}