using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class MaterialVariationEditor : BaseTwoPanelEditorWindow
{
    private TextAsset jsonData;
    private Dictionary<string, Dictionary<string, string>> materialDatabase;
    private Dictionary<string, List<string>> tppVariations = new Dictionary<string, List<string>>();
    private Dictionary<string, List<string>> fppVariations = new Dictionary<string, List<string>>();
    private bool showResults = false;
    private string searchString = "";
    private List<string> filteredBaseNames = new List<string>();
    private string selectedBaseName = null;
    private Vector2 baseListScrollPosition;
    private enum DisplayMode { BaseNames, MissingTextures }
    private DisplayMode currentDisplayMode = DisplayMode.BaseNames;
    private List<string> missingTextures = new List<string>();
    private List<string> missingTexturesDetails = new List<string>();

    private int currentPage = 0;
    private int itemsPerPage = 20;
    private int totalItems = 0;

    private static readonly string[] variationSuffixes = new string[]
    {
        "_tpp", "_fpp", "_1", "_2", "_3", "_01", "_02", "_03", "_04", "_05", "_06", "_07", "_08", "_09",
        "_r1", "_r2", "_r3", "_r4", "_r5",
        "_easter_red", "_military", "_monster", "_peacekeeper", "_post_apo", "_librarian", 
        "_dark_a", "_dark_b", "_dl1_a", "_dl1_b","_dl1_c", "_wrestler_a","_wrestler_b","_wrestler_c","_wrestler_harran_a",
        "_dirt", "_frs", "_pattern", "ronin"
    };

    [MenuItem("Model Tools/Material Variation Builder")]
    public static void ShowWindow()
    {
        GetWindow<MaterialVariationEditor>("Material Variation Builder");
    }

    protected override void DrawLeftPanel()
    {
        GUILayout.Label("Material Variation Builder", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        jsonData = (TextAsset)EditorGUILayout.ObjectField("JSON Data", jsonData, typeof(TextAsset), false);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Analyze JSON", buttonStyle))
        {
            ClearResults();
            AnalyzeJson();
            showResults = true;
            currentPage = 0;
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Check Missing Textures", buttonStyle))
        {
            ClearResults();
            CheckForMissingTextures();
            showResults = true;
            currentPage = 0;
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Clear Results", buttonStyle))
        {
            ClearResults();
        }

        EditorGUILayout.Space(10);

        if (showResults)
        {
            // Search field
            GUI.SetNextControlName("SearchField");
            searchString = EditorGUILayout.TextField("Search", searchString);
            if (GUI.GetNameOfFocusedControl() == "SearchField" && Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
            {
                searchString = "";
                GUI.FocusControl(null);
            }

            FilterBaseNames();

            // Calculate total items and update pagination
            totalItems = filteredBaseNames.Count;
            int totalPages = Mathf.CeilToInt((float)totalItems / itemsPerPage);
            currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

            // Display paginated list
            baseListScrollPosition = EditorGUILayout.BeginScrollView(baseListScrollPosition);
            int start = currentPage * itemsPerPage;
            int end = Mathf.Min(start + itemsPerPage, totalItems);

            for (int i = start; i < end; i++)
            {
                var baseName = filteredBaseNames[i];
                if (GUILayout.Button(baseName, selectedBaseName == baseName ? EditorStyles.boldLabel : EditorStyles.label))
                {
                    selectedBaseName = baseName;
                }
            }
            EditorGUILayout.EndScrollView();

            // Pagination controls
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Previous", GUILayout.Width(100)))
            {
                if (currentPage > 0)
                {
                    currentPage--;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Page {currentPage + 1} of {totalPages}", GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Next", GUILayout.Width(100)))
            {
                if (currentPage < totalPages - 1)
                {
                    currentPage++;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Export base names button
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Export Json Base Names", buttonStyle))
            {
                ExportBaseNames();
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Export Missing Textures Report", buttonStyle))
            {
                ExportBaseNamesWithMissingTextures();
            }
        }
    }

    private void ExportBaseNames()
    {
        string path = EditorUtility.SaveFilePanel("Export Base Names", "", "BaseNames.txt", "txt");
        if (!string.IsNullOrEmpty(path))
        {
            string[] baseNames = tppVariations.Keys.OrderBy(name => name).ToArray();
            File.WriteAllLines(path, baseNames);
            Debug.Log($"Base names exported to: {path}");
        }
    }

    protected override void DrawRightPanel()
    {
        if (showResults && materialDatabase != null && selectedBaseName != null)
        {
            GUILayout.Label($"Base Material: {selectedBaseName}", EditorStyles.boldLabel);

            EditorGUILayout.Space(10);

            if (tppVariations.ContainsKey(selectedBaseName))
            {
                GUILayout.Label("TPP Variations:", EditorStyles.boldLabel);
                foreach (var variation in tppVariations[selectedBaseName])
                {
                    GUILayout.Label($" - {variation}");
                    if (materialDatabase.ContainsKey(variation))
                    {
                        foreach (var prop in materialDatabase[variation])
                        {
                            GUILayout.Label($"    {prop.Key}: {prop.Value}");
                        }
                    }
                }
            }

            EditorGUILayout.Space(10);

            if (fppVariations.ContainsKey(selectedBaseName))
            {
                GUILayout.Label("FPP Variations:", EditorStyles.boldLabel);
                foreach (var variation in fppVariations[selectedBaseName])
                {
                    GUILayout.Label($" - {variation}");
                    if (materialDatabase.ContainsKey(variation))
                    {
                        foreach (var prop in materialDatabase[variation])
                        {
                            GUILayout.Label($"    {prop.Key}: {prop.Value}");
                        }
                    }
                }
            }
        }

        if (showResults && selectedBaseName != null && missingTextures.Contains(selectedBaseName))
        {
            GUILayout.Label($"Missing Texture: {selectedBaseName}", EditorStyles.boldLabel);
            GUILayout.Label($"Texture expected at: Assets/Resources/Textures/{selectedBaseName}.png");
        }
    }

    private void FilterBaseNames()
    {
        IEnumerable<string> baseNames = missingTextures.Any() ? missingTextures : tppVariations.Keys;
        filteredBaseNames = baseNames
            .Where(name => name.ToLower().Contains(searchString.ToLower()))
            .OrderBy(name => name)
            .ToList();

        totalItems = filteredBaseNames.Count;
    }

    private string GetBaseName(string materialName)
    {
        string pattern = $@"^(.*?)({string.Join("|", variationSuffixes.Select(Regex.Escape))})";
        Match match = Regex.Match(materialName, pattern);

        if (match.Success)
        {
            return match.Groups[1].Value.TrimEnd('_');
        }

        return materialName.TrimEnd('_');
    }

    private void AnalyzeJson()
    {
        if (jsonData == null)
        {
            Debug.LogError("JSON Data is not assigned.");
            return;
        }

        materialDatabase = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonData.text);

        tppVariations.Clear();
        fppVariations.Clear();

        HashSet<string> possibleBaseNames = new HashSet<string>();
        foreach (var material in materialDatabase.Keys)
        {
            string baseName = GetBaseName(material);
            possibleBaseNames.Add(baseName);
        }

        foreach (var material in materialDatabase.Keys)
        {
            string baseName = GetBaseName(material);

            if (possibleBaseNames.Contains(material))
            {
                baseName = material;
            }

            if (material.Contains("_tpp"))
            {
                AddVariation(tppVariations, baseName, material);
            }
            else if (material.Contains("_fpp"))
            {
                AddVariation(fppVariations, baseName, material);
            }
            else
            {
                AddVariation(tppVariations, baseName, material);
            }
        }

        tppVariations = tppVariations.Where(kvp => kvp.Value.Count > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        fppVariations = fppVariations.Where(kvp => kvp.Value.Count > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        filteredBaseNames = tppVariations.Keys.ToList();

        Debug.Log("JSON analysis completed.");
    }

    private void AddVariation(Dictionary<string, List<string>> variations, string baseName, string variation)
    {
        if (!variations.ContainsKey(baseName))
        {
            variations[baseName] = new List<string>();
        }
        if (!variations[baseName].Contains(variation))
        {
            variations[baseName].Add(variation);
        }
    }

    private void BuildNewJsonData()
    {
        foreach (var baseName in tppVariations.Keys.Concat(fppVariations.Keys).Distinct())
        {
            Dictionary<string, string> baseProperties = new Dictionary<string, string>();

            // Collect properties from all variations
            CollectPropertiesFromVariations(baseProperties, tppVariations, baseName);
            CollectPropertiesFromVariations(baseProperties, fppVariations, baseName);

            // Populate missing properties in all variations
            PopulateVariations(tppVariations, baseName, baseProperties);
            PopulateVariations(fppVariations, baseName, baseProperties);
        }

        SaveJson();
        Debug.Log("New JSON data built and saved.");
    }

    private void CollectPropertiesFromVariations(Dictionary<string, string> properties, Dictionary<string, List<string>> variations, string baseName)
    {
        if (variations.ContainsKey(baseName))
        {
            foreach (var variation in variations[baseName])
            {
                if (materialDatabase.ContainsKey(variation))
                {
                    foreach (var prop in materialDatabase[variation])
                    {
                        if (!properties.ContainsKey(prop.Key))
                        {
                            properties[prop.Key] = prop.Value;
                        }
                    }
                }
            }
        }
    }

    private void PopulateVariations(Dictionary<string, List<string>> variations, string baseName, Dictionary<string, string> baseProperties)
    {
        if (variations.ContainsKey(baseName))
        {
            foreach (var variation in variations[baseName])
            {
                if (materialDatabase.ContainsKey(variation))
                {
                    PopulateMissingProperties(materialDatabase[variation], baseProperties);
                }
            }
        }
    }

    private void PopulateMissingProperties(Dictionary<string, string> variant, Dictionary<string, string> baseProperties)
    {
        foreach (var baseProp in baseProperties)
        {
            if (!variant.ContainsKey(baseProp.Key))
            {
                variant[baseProp.Key] = baseProp.Value;
            }
        }
    }

    private void CheckForMissingTextures()
    {
        if (jsonData == null)
        {
            Debug.LogError("JSON Data is not assigned.");
            return;
        }

        if (materialDatabase == null)
        {
            materialDatabase = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonData.text);
        }

        string textureFolder = "Assets/Resources/Textures/";
        missingTextures.Clear();
        missingTexturesDetails.Clear();

        foreach (var materialEntry in materialDatabase)
        {
            string baseName = materialEntry.Key;
            Dictionary<string, string> textures = materialEntry.Value;

            foreach (var texture in textures)
            {
                string textureFileName = texture.Value;
                string texturePath = Path.Combine(textureFolder, textureFileName);

                if (!File.Exists(texturePath))
                {
                    string detail = $"Base Name: {baseName} - Missing texture: {textureFileName}";
                    missingTexturesDetails.Add(detail);
                    if (!missingTextures.Contains(baseName))
                    {
                        missingTextures.Add(baseName);
                    }
                }
            }
        }

        filteredBaseNames = missingTextures.ToList();

        if (missingTextures.Count > 0)
        {
            Debug.Log("Missing textures found.");
        }
        else
        {
            Debug.Log("No missing textures found.");
        }
    }

    private void ExportBaseNamesWithMissingTextures()
    {
        string path = EditorUtility.SaveFilePanel("Export Missing Textures Report", "", "MissingTexturesReport.txt", "txt");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllLines(path, missingTexturesDetails);
            Debug.Log($"Missing textures report exported to: {path}");
        }
    }

    private void ClearResults()
    {
        filteredBaseNames.Clear();
        missingTextures.Clear();
        selectedBaseName = null;
        showResults = false;
    }

    private void SaveJson()
    {
        string json = JsonConvert.SerializeObject(materialDatabase, Formatting.Indented);
        string path = EditorUtility.SaveFilePanel("Save Updated JSON", "", "UpdatedMaterials.json", "json");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
            Debug.Log($"Updated JSON data saved to {path}");
        }
    }
}