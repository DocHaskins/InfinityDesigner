using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AppearanceDisplay : MonoBehaviour
{
    private AppearanceWriter appearanceWriter;
    private CharacterAppearance currentAppearance;
    private PresetSummary_Scroller scroller;

    public RawImage outfitImage;
    public TMP_Text nameText;
    public TMP_Text hintText;
    public Image colorImage;
    public TMP_InputField idInputField;
    public Toggle overridesOutfitToggle;
    public TMP_Dropdown modelFppDropdown;
    public TMP_Dropdown modelTppDropdown;
    public TMP_Dropdown colorDropdown;

    public Dictionary<string, Color> colorNameToColor = new Dictionary<string, Color> {
    { "White", Color.white },
    { "Green", Color.green },
    { "Blue", Color.blue },
    { "Violet", new Color(0.93f, 0.51f, 0.93f) },
    { "Orange", new Color(1f, 0.64f, 0f) },
    { "Platinum", new Color(0.9f, 0.89f, 0.89f) }
};

    private void Awake()
    {
        appearanceWriter = FindObjectOfType<AppearanceWriter>();

        if (appearanceWriter == null)
        {
            Debug.LogError("Failed to find AppearanceWriter in the scene.");
        }
    }

    public void Setup(CharacterAppearance appearance, PresetSummary_Scroller scroller)
    {
        this.scroller = scroller;
        currentAppearance = appearance;
        Debug.Log($"Setting up appearance: {appearance.Name} with ID {appearance.id}");

        if (nameText != null) nameText.text = appearance.Name;
        if (hintText != null) hintText.text = appearance.hint;
        if (idInputField != null)
        {
            idInputField.text = appearance.id.ToString();
            Debug.Log($"Set ID Input Field to: {appearance.id}");
        }

        if (overridesOutfitToggle != null)
        {
            overridesOutfitToggle.isOn = appearance.overridesOutfit;
        }

        if (modelFppDropdown != null)
        {
            PopulateModelDropdowns(appearance);
        }

        if (modelTppDropdown != null)
        {
            PopulateModelDropdowns(appearance);
        }

        if (overridesOutfitToggle != null) overridesOutfitToggle.isOn = appearance.overridesOutfit;
        string imagePath = Path.Combine(Application.streamingAssetsPath, "Jsons/Human/Player", appearance.modelTpp.Replace(".model", ".png"));
        if (outfitImage == null)
        {
            Debug.LogError("outfitImage is not assigned!");
        }
        else
        {
            outfitImage.texture = LoadTexture(imagePath);
            if (outfitImage.texture == null)
            {
                Debug.LogError($"Failed to load texture from path: {imagePath}");
            }
            else
            {
                Debug.Log($"Loaded texture from path: {imagePath}");
            }
        }
        outfitImage.texture = LoadTexture(imagePath);
        PopulateModelDropdowns(appearance);
        PopulateColorDropdown();
        SetSelectedColorByName(appearance.color);
    }

    private void PopulateColorDropdown()
    {
        colorDropdown.ClearOptions();
        List<string> colorNames = new List<string>(colorNameToColor.Keys);
        colorDropdown.AddOptions(colorNames);
    }

    private void SetSelectedColorByName(string colorName)
    {
        int index = colorDropdown.options.FindIndex(option => option.text.Equals(colorName, StringComparison.OrdinalIgnoreCase));
        if (index != -1)
        {
            colorDropdown.value = index;
            colorImage.color = colorNameToColor[colorName];
        }
        else
        {
            Debug.LogWarning($"Color name '{colorName}' not found in the available colors.");
        }
    }

    Texture2D LoadTexture(string filePath)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
        }
        return tex;
    }

    public void PopulateModelDropdowns(CharacterAppearance appearance)
    {
        string jsonsPath = Path.Combine(Application.streamingAssetsPath, "Jsons/Human/Player");
        DirectoryInfo dir = new DirectoryInfo(jsonsPath);
        FileInfo[] files = dir.GetFiles("*.json");
        List<string> filenames = new List<string>();

        foreach (FileInfo file in files)
        {
            filenames.Add(Path.GetFileNameWithoutExtension(file.Name));
        }

        modelFppDropdown.ClearOptions();
        modelFppDropdown.AddOptions(filenames);
        modelTppDropdown.ClearOptions();
        modelTppDropdown.AddOptions(filenames);

        int fppIndex = filenames.IndexOf(appearance.modelFpp.Replace(".model", ""));
        if (fppIndex != -1) modelFppDropdown.value = fppIndex;

        int tppIndex = filenames.IndexOf(appearance.modelTpp.Replace(".model", ""));
        if (tppIndex != -1) modelTppDropdown.value = tppIndex;
    }

    void OnValueChange()
    {
        Debug.Log("Value changed, attempting to save changes.");
        SaveChanges();
    }

    public void SaveChanges()
    {
        if (appearanceWriter == null)
        {
            Debug.LogError("AppearanceWriter reference not set.");
            return;
        }

        string selectedColorName = colorDropdown.options[colorDropdown.value].text;

        CharacterAppearance updatedAppearance = new CharacterAppearance
        {
            id = int.Parse(idInputField.text),
            Name = nameText.text,
            color = selectedColorName,
            modelFpp = modelFppDropdown.options[modelFppDropdown.value].text,
            modelTpp = modelTppDropdown.options[modelTppDropdown.value].text,
            overridesOutfit = overridesOutfitToggle.isOn,
        };

        Debug.Log($"Attempting to update appearance: {updatedAppearance.Name}");
        appearanceWriter.UpdateAppearance(updatedAppearance);
    }

    public void OnSelected()
    {
        Debug.Log($"Prefab selected: {currentAppearance?.Name}");
        AttachListeners();
    }

    private void AttachListeners()
    {
        if (idInputField != null) idInputField.onValueChanged.AddListener(_ => OnValueChange());
        if (overridesOutfitToggle != null) overridesOutfitToggle.onValueChanged.AddListener(_ => OnValueChange());
        if (modelFppDropdown != null) modelFppDropdown.onValueChanged.AddListener(_ => OnValueChange());
        if (modelTppDropdown != null) modelTppDropdown.onValueChanged.AddListener(_ => OnValueChange());
    }

    public void DetachListeners()
    {
        if (idInputField != null) idInputField.onValueChanged.RemoveListener(_ => OnValueChange());
        if (overridesOutfitToggle != null) overridesOutfitToggle.onValueChanged.RemoveListener(_ => OnValueChange());
        if (modelFppDropdown != null) modelFppDropdown.onValueChanged.RemoveListener(_ => OnValueChange());
        if (modelTppDropdown != null) modelTppDropdown.onValueChanged.RemoveListener(_ => OnValueChange());
    }

    private void OnDestroy()
    {
        DetachListeners(); // Ensure listeners are removed when the prefab is destroyed
    }
}