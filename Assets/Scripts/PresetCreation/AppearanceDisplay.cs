using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AppearanceDisplay : MonoBehaviour
{
    public AppearanceWriter appearanceWriter;

    public RawImage outfitImage;
    public TMP_Text nameText;
    public TMP_Text hintText;
    public Image colorImage;
    public TMP_InputField idInputField;
    public Toggle overridesOutfitToggle;
    public TMP_Dropdown modelFppDropdown;
    public TMP_Dropdown modelTppDropdown;

    public void Setup(CharacterAppearance appearance)
    {
        if (nameText != null) nameText.text = appearance.Name;
        if (hintText != null) hintText.text = appearance.hint;
        if (idInputField != null) idInputField.text = appearance.id.ToString();

        if (colorImage != null && ColorUtility.TryParseHtmlString(appearance.color, out Color newCol))
        {
            colorImage.color = newCol;
        }

        if (overridesOutfitToggle != null) overridesOutfitToggle.isOn = appearance.overridesOutfit;
        string imagePath = Path.Combine(Application.streamingAssetsPath, "Jsons/Human/Player", appearance.modelTpp.Replace(".model", ".png"));
        Debug.Log(imagePath);
        outfitImage.texture = LoadTexture(imagePath);
        PopulateModelDropdowns(appearance);
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
        SaveChanges();
    }

    public void SaveChanges()
    {
        if (appearanceWriter == null)
        {
            Debug.LogError("AppearanceWriter reference not set.");
            return;
        }

        // Create a new CharacterAppearance instance from the current UI state
        CharacterAppearance updatedAppearance = new CharacterAppearance
        {
            // Fill out fields from UI components, e.g.:
            id = int.Parse(idInputField.text),
            Name = nameText.text,
            color = ColorUtility.ToHtmlStringRGBA(colorImage.color),
            modelFpp = modelFppDropdown.options[modelFppDropdown.value].text,
            modelTpp = modelTppDropdown.options[modelTppDropdown.value].text,
            overridesOutfit = overridesOutfitToggle.isOn,
            // etc.
        };

        // Update the appearance
        appearanceWriter.UpdateAppearance(updatedAppearance);
    }
}