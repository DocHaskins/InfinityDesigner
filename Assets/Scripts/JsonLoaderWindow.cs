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
    private int currentJsonIndex = 0;
    private bool isProcessing = false;
    private bool cancelRequested = false;
    private string selectedClass = "All";
    private string selectedSex = "All";
    private string selectedRace = "All";
    private string searchTerm = "";
    private bool enableCustomContent = false;
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

        string jsonsFolderPath = Path.Combine(Application.streamingAssetsPath, "Jsons");
        if (!Directory.Exists(jsonsFolderPath))
        {
            UnityEngine.Debug.LogError("Jsons folder not found in StreamingAssets: " + jsonsFolderPath);
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

        if (GUILayout.Button("Load All and Take Screenshots"))
        {
            if (!isProcessing)
            {
                cancelRequested = false; // Reset cancellation flag
                currentJsonIndex = 0;
                isProcessing = true;
                EditorApplication.update += ProcessCurrentJson;
            }
        }

        // New button to request cancellation
        if (GUILayout.Button("Cancel Process"))
        {
            cancelRequested = true;
        }
        GUILayout.Space(20);

        if (GUILayout.Button("Check Textures"))
        {
            CheckAllTextures();
        }
    }
    private void ProcessCurrentJson()
    {
        if (cancelRequested)
        {
            // Abort the process if cancellation is requested
            isProcessing = false;
            EditorApplication.update -= ProcessCurrentJson;
            UnityEngine.Debug.Log("Processing canceled by user.");
            return;
        }
        if (currentJsonIndex < filteredJsonFiles.Count)
        {
            string jsonFile = filteredJsonFiles[currentJsonIndex];
            ProcessJsonCoroutine = ProcessJson(jsonFile, OnJsonProcessed);
            EditorApplication.update += ExecuteProcessJsonCoroutine;
        }
        else
        {
            isProcessing = false;
            EditorApplication.update -= ProcessCurrentJson;
            UnityEngine.Debug.Log("Finished processing all JSON files.");
        }
    }

    private void OnJsonProcessed()
    {
        currentJsonIndex++;
        ProcessCurrentJson(); // Move to the next JSON file
    }

    private IEnumerator ProcessJson(string jsonFileName, Action onCompleted)
    {
        UnityEngine.Debug.Log("Starting to process JSON: " + jsonFileName);

        GameObject loaderObject = new GameObject("ModelLoaderObject");
        ModelLoader loader = loaderObject.AddComponent<ModelLoader>();
        loader.jsonFileName = jsonFileName;
        loader.LoadModelFromJson();
        yield return new WaitForSeconds(1240.5f);

        UnityEngine.Debug.Log("Waiting for model to load: " + jsonFileName);
        yield return new WaitUntil(() => loader.IsModelLoaded);
        yield return new WaitForSeconds(1240.5f);

        UnityEngine.Debug.Log("Model loaded. Taking screenshot: " + jsonFileName);
        TakeScreenshotAndSave(jsonFileName);

        UnityEngine.Debug.Log("Screenshot taken. Waiting before unload: " + jsonFileName);
        yield return new WaitForSeconds(1240.5f);

        UnityEngine.Debug.Log("Unloading model: " + jsonFileName);
        loader.UnloadModel();

        UnityEngine.Debug.Log("Model unloaded. Waiting before next JSON: " + jsonFileName);
        yield return new WaitForSeconds(1240.5f);

        UnityEngine.Debug.Log("Destroying loader object: " + jsonFileName);
        DestroyImmediate(loaderObject);
        yield return new WaitForSeconds(1240.5f);

        UnityEngine.Debug.Log("JSON processing completed: " + jsonFileName);
        onCompleted?.Invoke();
    }

    private void ExecuteProcessJsonCoroutine()
    {
        if (ProcessJsonCoroutine != null && !ProcessJsonCoroutine.MoveNext())
        {
            UnityEngine.Debug.Log("Coroutine completed for JSON index: " + currentJsonIndex);
            EditorApplication.update -= ExecuteProcessJsonCoroutine;
            ProcessJsonCoroutine = null; // Clear the coroutine
        }
    }

    private void TakeScreenshotAndSave(string jsonFileName)
    {
        string screenshotName = Path.GetFileNameWithoutExtension(jsonFileName) + ".png";
        string screenshotPath = Path.Combine(screenshotsFolderPath, screenshotName);

        ScreenCapture.CaptureScreenshot(screenshotPath);
        UnityEngine.Debug.Log("Saved screenshot: " + screenshotPath);
    }

    private string DropdownField(string label, string selectedValue, HashSet<string> options)
    {
        string[] optionArray = options.ToArray();
        int index = Array.IndexOf(optionArray, selectedValue);
        index = EditorGUILayout.Popup(label, index, optionArray);
        return optionArray[index];
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

    private void CheckAllTextures()
    {
        string jsonsFolderPath = Application.streamingAssetsPath + "/Jsons";
        string resourcesTexturesPath = "Textures"; // Path relative to the Resources folder
        string failedTexturesFilePath = Application.streamingAssetsPath + "/textures_failed.txt";
        HashSet<string> missingTextures = new HashSet<string>();

        if (!Directory.Exists(jsonsFolderPath))
        {
            UnityEngine.Debug.LogError("Jsons folder not found: " + jsonsFolderPath);
            return;
        }

        foreach (var file in Directory.GetFiles(jsonsFolderPath, "*.json"))
        {
            string jsonPath = Path.Combine(jsonsFolderPath, file);
            string jsonData = File.ReadAllText(jsonPath);
            ModelData modelData = JsonUtility.FromJson<ModelData>(jsonData);

            if (modelData != null && modelData.GetSlots() != null)
            {
                foreach (var slot in modelData.GetSlots())
                {
                    foreach (var modelInfo in slot.Value.models)
                    {
                        foreach (var materialResource in modelInfo.materialsResources)
                        {
                            foreach (var resource in materialResource.resources)
                            {
                                foreach (var rttiValue in resource.rttiValues)
                                {
                                    string textureName = rttiValue.val_str;
                                    if (!string.IsNullOrEmpty(textureName))
                                    {
                                        string texturePath = resourcesTexturesPath + "/" + Path.GetFileNameWithoutExtension(textureName);
                                        Texture2D texture = Resources.Load<Texture2D>(texturePath);

                                        if (texture == null)
                                        {
                                            missingTextures.Add(textureName);
                                        }
                                    }
                                    else
                                    {
                                        UnityEngine.Debug.LogWarning($"Texture name is null or empty in JSON file: {file}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Write missing textures to file
        if (missingTextures.Count > 0)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(failedTexturesFilePath, false))
                {
                    foreach (var texture in missingTextures)
                    {
                        writer.WriteLine(texture);
                        UnityEngine.Debug.LogError($"Texture '{texture}' not found in Resources.");
                    }
                }
                UnityEngine.Debug.LogError($"Missing textures list written to {failedTexturesFilePath}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error writing to file: {ex.Message}");
            }
        }
        else
        {
            UnityEngine.Debug.Log("All textures found.");
        }
    }


}
#endif