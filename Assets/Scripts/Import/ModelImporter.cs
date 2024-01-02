using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class ModelImporterWindow : EditorWindow
{
    [MenuItem("Tools/Model Importer")]
    public static void ShowWindow()
    {
        GetWindow<ModelImporterWindow>("Model Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Import Models from AssetImport", EditorStyles.boldLabel);

        if (GUILayout.Button("Import Models"))
        {
            string targetDirectory = Path.Combine(Application.dataPath, "AssetImport");
            if (Directory.Exists(targetDirectory))
            {
                ImportModelsAndTextures(targetDirectory);
            }
            else
            {
                Debug.LogError("AssetImport directory not found.");
            }
        }
    }

    private void ImportModelsAndTextures(string dirPath)
    {
        var allFiles = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories);
        var fbxFiles = new List<string>();
        var textureFiles = new Dictionary<string, List<string>>();

        foreach (var file in allFiles)
        {
            var relativePath = file.Replace(Application.dataPath, "Assets"); // Convert to relative path
            var baseName = GetBaseName(relativePath);

            if (Path.GetExtension(relativePath).ToLower() == ".fbx")
            {
                fbxFiles.Add(relativePath);
                if (!textureFiles.ContainsKey(baseName))
                    textureFiles[baseName] = new List<string>();
            }
            else if (IsTextureFile(relativePath))
            {
                if (!textureFiles.ContainsKey(baseName))
                    textureFiles[baseName] = new List<string>();
                textureFiles[baseName].Add(relativePath);
            }
        }

        string modelsPath = "Assets/Models";
        Directory.CreateDirectory(modelsPath); // Create the Models directory

        // Process textures to create materials
        foreach (var textureEntry in textureFiles)
        {
            var baseName = textureEntry.Key;
            var textures = textureEntry.Value;

            // Check if a diffuse texture exists
            var diffuseTexture = textures.FirstOrDefault(t => t.Contains("_dif"));
            if (diffuseTexture != null)
            {
                Material newMaterial = new Material(Shader.Find("HDRP/Lit"));
                AssignTexturesToMaterial(newMaterial, textures, baseName);  // Adjusted the order of parameters here

                string materialPath = Path.Combine(modelsPath, baseName + ".mat");
                AssetDatabase.CreateAsset(newMaterial, materialPath);
                Debug.Log($"Created Material: {materialPath}");

                // Move textures to the Models directory
                foreach (var textureFile in textures)
                {
                    string newTexturePath = Path.Combine(modelsPath, Path.GetFileName(textureFile));
                    AssetDatabase.MoveAsset(textureFile, newTexturePath);
                    Debug.Log($"Moved Texture: {newTexturePath}");
                }
            }
        }

        AssetDatabase.Refresh();
    }

    private string GetBaseName(string filePath)
    {
        var baseName = Path.GetFileNameWithoutExtension(filePath);
        var lastUnderscoreIndex = baseName.LastIndexOf('_');
        return lastUnderscoreIndex > -1 ? baseName.Substring(0, lastUnderscoreIndex) : baseName;
    }

    private bool IsTextureFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return extension == ".png" || extension == ".jpg" || extension == ".dds";
    }

    private string GetMaterialNameFromFile(string fileName)
    {
        // Extract the material name from the filename
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var lastUnderscoreIndex = baseName.LastIndexOf('_');
        return lastUnderscoreIndex > -1 ? baseName.Substring(0, lastUnderscoreIndex) : baseName;
    }

    private void AssignTexturesToMaterial(Material material, List<string> textureFiles, string baseName)
    {
        Shader hdrpShader = Shader.Find("HDRP/Lit");
        if (hdrpShader == null)
        {
            Debug.LogError("HDRP/Lit shader not found. Make sure HDRP is correctly set up in your project.");
            return;
        }
        material.shader = hdrpShader;

        // Define the texture types and their corresponding shader properties
        var textureTypes = new Dictionary<string, string>
    {
        {"_dif", "_BaseColorMap"},
        {"_nrm", "_NormalMap"},
        {"_rgh", "_RoughnessMap"},
        {"_spc", "_SpecularColorMap"},
        {"_ocl", "_OcclusionMap"}
    };

        // Iterate through each texture type
        foreach (var textureType in textureTypes)
        {
            // Find the texture path that matches the texture type
            string texturePath = textureFiles.FirstOrDefault(t => t.Contains(baseName + textureType.Key));
            if (!string.IsNullOrEmpty(texturePath))
            {
                Texture2D texture = LoadTexture(texturePath);
                if (texture != null)
                {
                    material.SetTexture(textureType.Value, texture);
                    Debug.Log($"Assigned '{textureType.Key}' texture '{texture.name}' to material '{material.name}'");
                }
            }
        }
    }

    private Texture2D LoadTexture(string path)
    {
        // Remove any duplicate file extensions
        string relativePath = RemoveDuplicateExtensions(path.Replace(Application.dataPath, "Assets"));

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);

        if (texture == null)
        {
            Debug.LogError($"Failed to load texture at path: {relativePath}");
        }
        else
        {
            Debug.Log($"Loaded Texture: {relativePath}");
        }

        return texture;
    }

    private string RemoveDuplicateExtensions(string path)
    {
        string extension = Path.GetExtension(path);
        string filenameWithoutExtension = Path.GetFileNameWithoutExtension(path);

        // Check if the file name without extension still ends with the same extension
        while (Path.GetExtension(filenameWithoutExtension) == extension)
        {
            filenameWithoutExtension = Path.GetFileNameWithoutExtension(filenameWithoutExtension);
        }

        // Reconstruct the path with the correct extension
        string directory = Path.GetDirectoryName(path);
        return Path.Combine(directory, filenameWithoutExtension + extension);
    }
}