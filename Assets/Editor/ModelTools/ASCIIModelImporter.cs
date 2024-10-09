using UnityEngine;
using UnityEditor;
using System.IO;
using SFB;

public class ASCIIModelImporter : EditorWindow
{
    private string selectedFilePath = "";

    [MenuItem("Tools/Model Tools/ASCII Model Importer")]
    public static void ShowWindow()
    {
        GetWindow<ASCIIModelImporter>("ASCII Model Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Import ASCII Model", EditorStyles.boldLabel);

        if (GUILayout.Button("Browse for ASCII file"))
        {
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select ASCII Model", "", "ascii", false);
            if (paths.Length > 0)
            {
                selectedFilePath = paths[0];
            }
        }

        EditorGUILayout.LabelField("Selected File:", selectedFilePath);

        if (!string.IsNullOrEmpty(selectedFilePath))
        {
            if (GUILayout.Button("Import and Create Object"))
            {
                ImportAndCreateObject();
            }
        }
    }

    void ImportAndCreateObject()
    {
        // Create a new GameObject
        GameObject newObject = new GameObject(Path.GetFileNameWithoutExtension(selectedFilePath));

        // Add MeshFilter and MeshRenderer components
        newObject.AddComponent<MeshFilter>();
        newObject.AddComponent<MeshRenderer>();

        // Add the XPSASCIIModelLoader component
        ASCIIModelLoader loader = newObject.AddComponent<ASCIIModelLoader>();

        // Set the file path
        loader.filePath = selectedFilePath;

        // Trigger the loading process
        loader.LoadASCIIModel();

        // Focus on the new object in the scene view
        Selection.activeGameObject = newObject;
        SceneView.FrameLastActiveSceneView();

        Debug.Log("XPS ASCII model imported and created in the scene.");
    }
}