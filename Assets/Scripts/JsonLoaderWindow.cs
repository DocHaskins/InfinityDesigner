using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class JsonLoaderWindow : EditorWindow
{
    private string selectedJson;
    private List<string> jsonFiles = new List<string>();
    private List<string> filteredJsonFiles = new List<string>();
    private int selectedIndex = 0;
    private string selectedClass = "All";
    private string selectedSex = "All";
    private string selectedRace = "All";
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
        jsonFiles.Clear();
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
            jsonFiles.Add(Path.GetFileName(file));

            string jsonPath = Path.Combine(jsonsFolderPath, file);
            string jsonData = File.ReadAllText(jsonPath);
            ModelData modelData = JsonUtility.FromJson<ModelData>(jsonData);
            if (modelData.modelProperties != null)
            {
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
        filteredJsonFiles = jsonFiles.Where(file =>
        {
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "Jsons", file);
            string jsonData = File.ReadAllText(jsonPath);
            ModelData modelData = JsonUtility.FromJson<ModelData>(jsonData);

            bool classMatch = selectedClass == "All" || modelData.modelProperties?.@class == selectedClass;
            bool sexMatch = selectedSex == "All" || modelData.modelProperties?.sex == selectedSex;
            bool raceMatch = selectedRace == "All" || modelData.modelProperties?.race == selectedRace;

            return classMatch && sexMatch && raceMatch;
        }).ToList();
    }

    void OnGUI()
    {
        GUILayout.Label("Load JSON File", EditorStyles.boldLabel);

        // Dropdown for Class
        selectedClass = DropdownField("Filter by Class", selectedClass, classes);
        // Dropdown for Sex
        selectedSex = DropdownField("Filter by Sex", selectedSex, sexes);
        // Dropdown for Race
        selectedRace = DropdownField("Filter by Race", selectedRace, races);

        UpdateFilteredJsonFiles();

        selectedIndex = EditorGUILayout.Popup("Select JSON File", selectedIndex, filteredJsonFiles.ToArray());
        if (filteredJsonFiles.Count > 0)
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