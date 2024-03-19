using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SFB;

public class AppearanceLoader : MonoBehaviour
{
    public AppearanceWriter appearanceWriter;
    public PresetSummary_Scroller scroller;
    public List<CharacterAppearance> characterAppearances = new List<CharacterAppearance>();
    public string filePath;
    private int lastUsedId = -1;

    void Start()
    {
        OpenFileBrowser();
    }

    void OpenFileBrowser()
    {
        // Open file browser to select the playerappearances.scr file
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Player Appearances File", "", "scr", false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            StartCoroutine(ReadAppearancesFile(paths[0]));
            filePath = paths[0];
            appearanceWriter.filePath = paths[0];
        }
        else
        {
            Debug.LogError("No file was selected.");
        }
    }

    IEnumerator ReadAppearancesFile(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        CharacterAppearance currentAppearance = null;

        foreach (var line in lines)
        {
            // Remove whitespaces and parse the line
            string trimmedLine = line.Trim();
            //Debug.Log($"Reading line: {trimmedLine}");
            if (trimmedLine.StartsWith("Appearance"))
            {
                if (currentAppearance != null)
                {
                    characterAppearances.Add(currentAppearance);
                }
                var nameSplit = trimmedLine.Split('"');
                if (nameSplit.Length > 1)
                {
                    string name = nameSplit[1]; // Get the name between quotes
                    currentAppearance = new CharacterAppearance { Name = name };
                }
            }
            else if (trimmedLine.StartsWith("ModelFpp") && currentAppearance != null)
            {
                var split = trimmedLine.Split('"');
                if (split.Length > 1)
                {
                    currentAppearance.modelFpp = split[1];
                }
            }
            else if (trimmedLine.StartsWith("ModelTpp") && currentAppearance != null)
            {
                var split = trimmedLine.Split('"');
                if (split.Length > 1)
                {
                    currentAppearance.modelTpp = split[1];
                }
            }
            else if (trimmedLine.StartsWith("OverridesOutfit") && currentAppearance != null)
            {
                var split = trimmedLine.Split('(');
                if (split.Length > 1)
                {
                    string boolStr = split[1].TrimEnd(')', ';');
                    if (bool.TryParse(boolStr, out bool result))
                    {
                        currentAppearance.overridesOutfit = result;
                    }
                }
            }
            else if (trimmedLine.StartsWith("Color") && currentAppearance != null)
            {
                var split = trimmedLine.Split('"');
                if (split.Length > 1)
                {
                    currentAppearance.color = split[1];
                }
            }
            else if (trimmedLine.StartsWith("ID") && currentAppearance != null)
            {
                var split = trimmedLine.Split('(');
                if (split.Length > 1)
                {
                    string numStr = split[1].TrimEnd(')', ';');
                    if (int.TryParse(numStr, out int idVal))
                    {
                        currentAppearance.id = idVal;
                    }
                }
            }
            else if (trimmedLine.StartsWith("RequiredDLCs") && currentAppearance != null)
            {
                var dlcsSplit = trimmedLine.Replace("RequiredDLCs(", "").TrimEnd(')', ';').Split(',');
                foreach (string dlc in dlcsSplit)
                {
                    if (!string.IsNullOrWhiteSpace(dlc))
                    {
                        currentAppearance.requiredDLCs.Add(dlc.Trim('"').Trim());
                    }
                }
            }
            if (currentAppearance != null)
            {
                Debug.Log($"Parsed appearance: {currentAppearance.Name} with ID {currentAppearance.id}");
            }
        }

        if (currentAppearance != null)
        {
            characterAppearances.Add(currentAppearance);
            Debug.Log($"Added final appearance: {currentAppearance.Name}");
        }

        foreach (var appearance in characterAppearances)
        {
            lastUsedId = Mathf.Max(lastUsedId, appearance.id);
        }

        // Pass to the scroller
        if (scroller != null)
        {
            scroller.LoadPresets(characterAppearances);
        }
        else
        {
            Debug.LogError("Scroller reference is not set in AppearanceLoader.");
        }

        yield return null;
    }

    public int GetNextAvailableId()
    {
        return lastUsedId + 1;
    }
}