#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cinemachine;
using System.Diagnostics;
using System.Collections;

public class JsonLoaderWindow : EditorWindow
{
    private string selectedJson;
    private List<MinimalModelData> minimalModelInfos = new List<MinimalModelData>();
    private List<string> jsonFiles = new List<string>();
    private List<string> filteredJsonFiles = new List<string>();
    private int selectedIndex = 0;
    private string selectedType = "All";
    private string selectedCategory = "All";
    private string selectedClass = "All";
    private string selectedSex = "All";
    private string selectedRace = "All";
    private string searchTerm = "";
    private bool enableCustomContent = false;
    private HashSet<string> types = new HashSet<string>();
    private HashSet<string> categories = new HashSet<string>();
    private HashSet<string> classes = new HashSet<string>();
    private HashSet<string> sexes = new HashSet<string>();
    private HashSet<string> races = new HashSet<string>();
    private IEnumerator ProcessJsonCoroutine;
    private string screenshotsFolderPath;

    [MenuItem("Tools/Json Loader")]
    public static void ShowWindow()
    {
        GetWindow<JsonLoaderWindow>("Json Loader");
    }

    void OnEnable()
    {
        // Initialize the JSON data only once when the window is enabled
        LoadJsonData();
        screenshotsFolderPath = Path.Combine(Application.dataPath, "Screenshots");
        if (!Directory.Exists(screenshotsFolderPath))
        {
            Directory.CreateDirectory(screenshotsFolderPath);
        }
    }

    void LoadJsonData()
    {
        jsonFiles.Clear();
        minimalModelInfos.Clear();
        classes.Clear();
        sexes.Clear();
        races.Clear();
        types.Clear();
        categories.Clear();

        string jsonsFolderPath = Path.Combine(Application.streamingAssetsPath, "Jsons");
        if (!Directory.Exists(jsonsFolderPath))
        {
            UnityEngine.Debug.LogError("Jsons folder not found in StreamingAssets: " + jsonsFolderPath);
            return;
        }

        foreach (var typeDir in Directory.GetDirectories(jsonsFolderPath))
        {
            string typeName = Path.GetFileName(typeDir);
            types.Add(typeName);

            foreach (var categoryDir in Directory.GetDirectories(typeDir))
            {
                string categoryName = Path.GetFileName(categoryDir);
                categories.Add(categoryName);

                foreach (var file in Directory.GetFiles(categoryDir, "*.json", SearchOption.AllDirectories))
                {
                    string relativePath = $"{typeName}/{categoryName}/{Path.GetFileName(file)}";
                    ProcessJsonFile(relativePath);  // Pass the relative path
                }
            }
        }

        types.Add("All");
        categories.Add("All");

        // Call UpdateFilteredOptions to populate classes, sexes, and races based on type and category
        UpdateFilteredJsonFiles();
    }

    void ProcessJsonFile(string relativeJsonFilePath)
    {
        // Replace %20 with spaces in the relative JSON file path
        relativeJsonFilePath = relativeJsonFilePath.Replace("%20", " ");

        // Correct the base path for JSON files
        string jsonsBasePath = Path.Combine(Application.streamingAssetsPath, "Jsons");

        // Construct the full JSON file path
        string jsonFilePath = Path.Combine(jsonsBasePath, relativeJsonFilePath);

        // Check if the JSON file exists before attempting to read it
        if (File.Exists(jsonFilePath))
        {
            string jsonData = File.ReadAllText(jsonFilePath);
            ModelData modelData = JsonUtility.FromJson<ModelData>(jsonData);

            if (modelData.modelProperties != null)
            {
                minimalModelInfos.Add(new MinimalModelData
                {
                    FileName = relativeJsonFilePath, // Use the relative path directly
                    Properties = modelData.modelProperties
                });

                classes.Add(modelData.modelProperties.@class ?? "Unknown");
                sexes.Add(modelData.modelProperties.sex ?? "Unknown");
                races.Add(modelData.modelProperties.race ?? "Unknown");
            }
        }
        else
        {
            UnityEngine.Debug.LogError("JSON file not found: " + jsonFilePath);
        }
    }

    string GetRelativePath(string fullPath, string basePath)
    {
        Uri fullUri = new Uri(fullPath);
        Uri baseUri = new Uri(basePath);
        string relativePath = baseUri.MakeRelativeUri(fullUri).ToString();

        // Replace backslashes with forward slashes
        return relativePath.Replace('\\', '/');
    }

    void UpdateFilteredJsonFiles()
    {
        filteredJsonFiles.Clear();

        foreach (var info in minimalModelInfos)
        {
            bool typeMatch = selectedType == "All" || info.FileName.StartsWith(selectedType + "/", StringComparison.OrdinalIgnoreCase);
            bool categoryMatch = selectedCategory == "All" || info.FileName.Contains("/" + selectedCategory + "/");
            bool classMatch = selectedClass == "All" || info.Properties.@class == selectedClass;
            bool sexMatch = selectedSex == "All" || info.Properties.sex == selectedSex;
            bool raceMatch = selectedRace == "All" || info.Properties.race == selectedRace;

            if (typeMatch && categoryMatch && classMatch && sexMatch && raceMatch)
            {
                filteredJsonFiles.Add(info.FileName);
            }
        }

        // Automatically select the first file in the list if any files are available
        if (filteredJsonFiles.Count > 0)
        {
            selectedIndex = 0;
            selectedJson = filteredJsonFiles[0];
        }
        else
        {
            selectedIndex = -1;
            selectedJson = null;
        }
    }

    void OnGUI()
    {
        GUILayout.Label("JSON File Loader", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("JSON Options", EditorStyles.boldLabel);
        if (GUILayout.Button("Update JSON Data"))
        {
            LoadJsonData();
        }
        if (GUILayout.Button("Open json"))
        {
            OpenSelectedJsonFile();
        }

        GUILayout.Space(10);
        

        GUILayout.Label("Load IALR Data", EditorStyles.boldLabel);
        bool filtersChanged = false;

        // Toggle for Custom Content
        bool prevEnableCustomContent = enableCustomContent;
        enableCustomContent = EditorGUILayout.Toggle("Enable Custom Content", enableCustomContent);
        if (prevEnableCustomContent != enableCustomContent)
        {
            filtersChanged = true;
        }
        GUILayout.Space(10);
        GUILayout.Label("Filter Settings for JSON", EditorStyles.boldLabel);
        GUILayout.Space(2);
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
        GUILayout.Space(2);

        string prevSelectedType = selectedType;
        selectedType = DropdownField("Filter by Type", selectedType, types);
        if (prevSelectedType != selectedType)
        {
            filtersChanged = true;
        }

        // Dropdown for Category
        string prevSelectedCategory = selectedCategory;
        selectedCategory = DropdownField("Filter by Category", selectedCategory, categories);
        if (prevSelectedCategory != selectedCategory)
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
        GUILayout.Space(10);
        GUILayout.Label("Instatiate Data", EditorStyles.boldLabel);
        if (GUILayout.Button("Load"))
        {
            if (!string.IsNullOrEmpty(selectedJson))
            {
                // Create a new GameObject and attach ModelLoader to it
                GameObject loaderObject = new GameObject("ModelLoaderObject");
                ModelLoader loader = loaderObject.AddComponent<ModelLoader>();
                loader.jsonFileName = selectedJson;
                loader.LoadModelFromJson();
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
                    UnityEngine.Debug.LogError("ModelLoader component not found on ModelLoaderObject.");
                }
            }
            else
            {
                UnityEngine.Debug.LogError("ModelLoaderObject not found in the scene.");
            }
        }
        GUILayout.Space(20);
        GUILayout.Label("Additional Options", EditorStyles.boldLabel);        
    }

    private string DropdownField(string label, string selectedValue, HashSet<string> options)
    {
        string[] optionArray = options.ToArray();
        int index = Array.IndexOf(optionArray, selectedValue);

        // Handle case where selectedValue is not in options
        if (index == -1)
        {
            if (optionArray.Length > 0)
            {
                index = 0; 
            }
            else
            {
                return selectedValue;
            }
        }

        index = EditorGUILayout.Popup(label, index, optionArray);

        // Safeguard against empty optionArray
        if (optionArray.Length > 0)
        {
            return optionArray[index];
        }
        else
        {
            return selectedValue;
        }
    }

    private void OpenSelectedJsonFile()
    {
        if (!string.IsNullOrEmpty(selectedJson))
        {
            string jsonFilePath = Path.Combine(Application.streamingAssetsPath, "Jsons", selectedJson);
            if (File.Exists(jsonFilePath))
            {
                // Open the file with the default application
                Process.Start(jsonFilePath);
            }
            else
            {
                UnityEngine.Debug.LogError("Json file not found: " + jsonFilePath);
            }

        }
        else
        {
            UnityEngine.Debug.LogError("No json file selected.");
        }
    }

    


}
#endif