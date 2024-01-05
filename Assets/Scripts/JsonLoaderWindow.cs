using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class JsonLoaderWindow : EditorWindow
{
    private string selectedJson;
    private List<string> jsonFiles = new List<string>();
    private int selectedIndex = 0;

    [MenuItem("Tools/Json Loader")]
    public static void ShowWindow()
    {
        GetWindow<JsonLoaderWindow>("Json Loader");
    }

    void OnEnable()
    {
        jsonFiles.Clear();
        string jsonsFolderPath = Path.Combine(Application.streamingAssetsPath, "Jsons");

        // Check if the Jsons directory exists
        if (!Directory.Exists(jsonsFolderPath))
        {
            Debug.LogError("Jsons folder not found in StreamingAssets: " + jsonsFolderPath);
            return;
        }

        foreach (var file in Directory.GetFiles(jsonsFolderPath, "*.json"))
        {
            jsonFiles.Add(Path.GetFileName(file));
        }

        if (jsonFiles.Count > 0)
        {
            selectedJson = jsonFiles[0];
        }
    }

    void OnGUI()
    {
        GUILayout.Label("Load JSON File", EditorStyles.boldLabel);

        selectedIndex = EditorGUILayout.Popup("Select JSON File", selectedIndex, jsonFiles.ToArray());
        if (jsonFiles.Count > 0)
        {
            selectedJson = jsonFiles[selectedIndex];
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
}