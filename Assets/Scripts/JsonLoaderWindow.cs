using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cinemachine;

public class JsonLoaderWindow : EditorWindow
{
    private string selectedJson;
    private List<MinimalModelData> minimalModelInfos = new List<MinimalModelData>();
    private List<string> jsonFiles = new List<string>();
    private List<string> filteredJsonFiles = new List<string>();
    private int selectedIndex = 0;
    private string selectedClass = "All";
    private string selectedSex = "All";
    private string selectedRace = "All";
    private string searchTerm = "";
    private bool enableCustomContent = false;
    private HashSet<string> classes = new HashSet<string>();
    private HashSet<string> sexes = new HashSet<string>();
    private HashSet<string> races = new HashSet<string>();

    [MenuItem("Tools/Json Loader")]
    public static void ShowWindow()
    {
        GetWindow<JsonLoaderWindow>("Json Loader");
    }

    void OnEnable()
    {
        // Initialize the JSON data only once when the window is enabled
        LoadJsonData();
    }

    void LoadJsonData()
    {
        jsonFiles.Clear();
        minimalModelInfos.Clear();
        classes.Clear();
        sexes.Clear();
        races.Clear();

        string jsonsFolderPath = Path.Combine(Application.streamingAssetsPath, "Jsons");
        if (!Directory.Exists(jsonsFolderPath))
        {
            Debug.LogError("Jsons folder not found in StreamingAssets: " + jsonsFolderPath);
            return;
        }

        foreach (var file in Directory.GetFiles(jsonsFolderPath, "*.json"))
        {
            string jsonPath = Path.Combine(jsonsFolderPath, file);
            string jsonData = File.ReadAllText(jsonPath);
            ModelData modelData = JsonUtility.FromJson<ModelData>(jsonData);

            if (modelData.modelProperties != null)
            {
                minimalModelInfos.Add(new MinimalModelData
                {
                    FileName = Path.GetFileName(file),
                    Properties = modelData.modelProperties
                });

                classes.Add(modelData.modelProperties.@class ?? "Unknown");
                sexes.Add(modelData.modelProperties.sex ?? "Unknown");
                races.Add(modelData.modelProperties.race ?? "Unknown");
            }
        }

        classes.Add("All");
        sexes.Add("All");
        races.Add("All");

        UpdateFilteredJsonFiles();
    }

    void UpdateFilteredJsonFiles()
    {
        filteredJsonFiles = minimalModelInfos.Where(info =>
        {
            // Exclude files that start with "db_"
            if (info.FileName.StartsWith("db_", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!enableCustomContent && info.FileName.StartsWith("ialr_", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!enableCustomContent && (info.FileName.EndsWith("_clown", StringComparison.OrdinalIgnoreCase) ||
                                         info.FileName.EndsWith("_vampire", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            bool classMatch = selectedClass == "All" || info.Properties.@class == selectedClass;
            bool sexMatch = selectedSex == "All" || info.Properties.sex == selectedSex;
            bool raceMatch = selectedRace == "All" || info.Properties.race == selectedRace;
            bool searchMatch = string.IsNullOrEmpty(searchTerm) || info.FileName.ToLower().Contains(searchTerm.ToLower());

            return classMatch && sexMatch && raceMatch && searchMatch;
        })
        .Select(info => info.FileName)
        .ToList();
    }

    void OnGUI()
    {
        GUILayout.Label("Load JSON File", EditorStyles.boldLabel);

        if (GUILayout.Button("Update JSON Data"))
        {
            LoadJsonData();
        }

        bool filtersChanged = false;

        // Toggle for Custom Content
        bool prevEnableCustomContent = enableCustomContent;
        enableCustomContent = EditorGUILayout.Toggle("Enable Custom Content", enableCustomContent);
        if (prevEnableCustomContent != enableCustomContent)
        {
            filtersChanged = true;
        }

        // Search field
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(50));
        string prevSearchTerm = searchTerm;
        searchTerm = GUILayout.TextField(searchTerm);
        GUILayout.EndHorizontal();
        if (prevSearchTerm != searchTerm)
        {
            filtersChanged = true;
        }

        // Dropdown for Class
        string prevSelectedClass = selectedClass;
        selectedClass = DropdownField("Filter by Class", selectedClass, classes);
        if (prevSelectedClass != selectedClass)
        {
            filtersChanged = true;
        }

        // Dropdown for Sex
        string prevSelectedSex = selectedSex;
        selectedSex = DropdownField("Filter by Sex", selectedSex, sexes);
        if (prevSelectedSex != selectedSex)
        {
            filtersChanged = true;
        }

        // Dropdown for Race
        string prevSelectedRace = selectedRace;
        selectedRace = DropdownField("Filter by Race", selectedRace, races);
        if (prevSelectedRace != selectedRace)
        {
            filtersChanged = true;
        }

        if (filtersChanged)
        {
            UpdateFilteredJsonFiles();
            selectedIndex = 0; // Reset the selectedIndex when the list changes
        }

        // Dropdown to select JSON file
        selectedIndex = EditorGUILayout.Popup("Select JSON File", selectedIndex, filteredJsonFiles.ToArray());
        if (selectedIndex >= 0 && selectedIndex < filteredJsonFiles.Count)
        {
            selectedJson = filteredJsonFiles[selectedIndex];
        }

        if (GUILayout.Button("Load"))
        {
            if (!string.IsNullOrEmpty(selectedJson))
            {
                // Create a new GameObject and attach ModelLoader to it
                GameObject loaderObject = new GameObject("ModelLoaderObject");
                ModelLoader loader = loaderObject.AddComponent<ModelLoader>();
                loader.jsonFileName = selectedJson;
                loader.LoadModelFromJson();

                var cameraTool = FindObjectOfType<CinemachineCameraZoomTool>();
                if (cameraTool != null)
                {
                    cameraTool.UpdateTargetPoints();
                }
            }
        }

        if (GUILayout.Button("Unload"))
        {
            // Find the ModelLoaderObject and call the UnloadModel method
            GameObject loaderObject = GameObject.Find("ModelLoaderObject");
            if (loaderObject != null)
            {
                ModelLoader loader = loaderObject.GetComponent<ModelLoader>();
                if (loader != null)
                {
                    loader.UnloadModel();
                }
                else
                {
                    Debug.LogError("ModelLoader component not found on ModelLoaderObject.");
                }
            }
            else
            {
                Debug.LogError("ModelLoaderObject not found in the scene.");
            }
        }
    }
    private string DropdownField(string label, string selectedValue, HashSet<string> options)
    {
        string[] optionArray = options.ToArray();
        int index = Array.IndexOf(optionArray, selectedValue);
        index = EditorGUILayout.Popup(label, index, optionArray);
        return optionArray[index];
    }
}