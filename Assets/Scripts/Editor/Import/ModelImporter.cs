using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;

public class ModelImporterWindow : EditorWindow
{
    private int materialCreationLimit = 100; // Default value

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

        GUILayout.BeginHorizontal();
        GUILayout.Label("Material Creation Limit:", GUILayout.Width(150));
        materialCreationLimit = EditorGUILayout.IntField(materialCreationLimit);
        GUILayout.EndHorizontal();

        // Button for creating and binding materials with the specified limit
        if (GUILayout.Button("Create and Bind Materials"))
        {
            CreateAndBindMaterials(materialCreationLimit);
        }

        if (GUILayout.Button("Assign Materials to FBX"))
        {
            AssignMaterialsToFbx();
        }

        
    }

    private void CreateAndBindMaterials(int limit = 100)
    {
        string modelsPath = "Assets/Models";
        var allFiles = Directory.GetFiles(modelsPath, "*.*", SearchOption.AllDirectories);
        var textureFiles = new Dictionary<string, List<string>>();
        int createdMaterialsCount = 0;

        // Organize textures by base names
        foreach (var file in allFiles)
        {
            if (IsTextureFile(file))
            {
                var baseName = GetBaseName(file);
                if (!textureFiles.ContainsKey(baseName))
                    textureFiles[baseName] = new List<string>();
                textureFiles[baseName].Add(file);
            }
        }

        // Process each set of textures up to the limit
        foreach (var entry in textureFiles)
        {
            if (createdMaterialsCount >= limit)
            {
                Debug.Log($"Limit of {limit} materials reached. Stopping material creation.");
                break;
            }

            var baseName = entry.Key;
            var textures = entry.Value;

            string materialPath = Path.Combine(modelsPath, baseName + ".mat");
            Material material;

            // Check for existing material and create if not exists
            if (!File.Exists(materialPath))
            {
                material = new Material(Shader.Find("HDRP/Lit"));
                AssetDatabase.CreateAsset(material, materialPath);
                Debug.Log($"Created HDRP Material: {materialPath}");
                createdMaterialsCount++;
            }
            else
            {
                material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    Debug.LogError($"Failed to load existing material at path: {materialPath}");
                    continue;
                }
                Debug.Log($"Found existing material: {materialPath}");
            }

            // Assign textures to material
            AssignTexturesToMaterial(material, textures, baseName);
        }

        AssetDatabase.Refresh();
        Debug.Log($"{createdMaterialsCount} materials were created and binding complete.");
    }

    private void AssignMaterialsToFbx()
    {
        string modelsPath = "Assets/Models";
        var materialPaths = Directory.GetFiles(modelsPath, "*.mat", SearchOption.AllDirectories);

        // Log the found material paths for debugging
        foreach (var matPath in materialPaths)
        {
            Debug.Log($"Found material at path: {matPath}");
        }

        // Create a dictionary to map the material name to its path
        var materialNameToPath = materialPaths.ToDictionary(
            path => Path.GetFileNameWithoutExtension(path),
            path => path
        );

        var fbxPaths = Directory.GetFiles(modelsPath, "*.fbx", SearchOption.AllDirectories);
        foreach (var fbxPath in fbxPaths)
        {
            Debug.Log($"Processing FBX at path: {fbxPath}");

            AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;

            if (importer != null)
            {
                // Set material location to external before applying the changes
                importer.materialLocation = ModelImporterMaterialLocation.External;
                importer.SaveAndReimport(); // Apply the changes immediately

                SerializedObject serializedObject = new SerializedObject(importer);
                SerializedProperty materialsProperty = serializedObject.FindProperty("m_ExternalObjects");

                Debug.Log($"Number of material slots in '{fbxPath}': {materialsProperty.arraySize}");

                for (int i = 0; i < materialsProperty.arraySize; i++)
                {
                    SerializedProperty materialProperty = materialsProperty.GetArrayElementAtIndex(i);
                    string materialName = materialProperty.FindPropertyRelative("first.name").stringValue;

                    Debug.Log($"Original material slot name: '{materialName}'");

                    // Process the material name to match with the created materials
                    string processedMaterialName = ProcessMaterialName(materialName);
                    Debug.Log($"Processed material name for matching: '{processedMaterialName}'");

                    if (materialNameToPath.TryGetValue(processedMaterialName, out var materialPath))
                    {
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (material != null)
                        {
                            materialProperty.FindPropertyRelative("second").objectReferenceValue = material;
                            Debug.Log($"Assigned material '{processedMaterialName}' to '{fbxPath}' at slot index {i}");
                        }
                        else
                        {
                            Debug.LogError($"Material asset not found at path: {materialPath}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"No matching material found for name: '{processedMaterialName}'");
                    }
                }
                serializedObject.ApplyModifiedProperties();
                Debug.Log($"Updated materials for: {serializedObject.targetObject}");
            }
            else
            {
                Debug.LogError($"Importer for the FBX file at '{fbxPath}' could not be found.");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Material assignment complete.");
    }
    private string ProcessMaterialName(string originalName)
    {
        // Use regex to replace the unwanted parts of the material name
        // Corrected regex pattern with double backslashes for C# string literals
        string pattern = @"sm_\d+_|\_\d+_.+$"; // This is the corrected pattern
        try
        {
            string processedName = Regex.Replace(originalName, pattern, "");
            Debug.LogError($"Processing the file names: Original {originalName}, New {processedName}");
            return processedName;
        }
        catch (ArgumentException ex)
        {
            // Log the error message and the pattern that caused the error
            Debug.LogError($"Regex error in pattern: {pattern}\nError Message: {ex.Message}");
            return originalName; // Return the original name if there was a problem
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
        Directory.CreateDirectory(modelsPath); // Ensure the Models directory exists

        foreach (var fbxFile in fbxFiles)
        {
            string newFbxPath = Path.Combine(modelsPath, Path.GetFileName(fbxFile));
            if (fbxFile != newFbxPath) // Check if the new path is different to avoid unnecessary operations
            {
                string error = AssetDatabase.MoveAsset(fbxFile, newFbxPath);
                if (string.IsNullOrEmpty(error))
                {
                    Debug.Log($"Moved FBX: {newFbxPath}");
                }
                else
                {
                    Debug.LogError($"Failed to move FBX: {fbxFile} to {newFbxPath}. Error: {error}");
                }
            }
            else
            {
                Debug.Log($"FBX already in place: {newFbxPath}");
            }
        }


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
        // Find the HDRP Lit shader
        Shader hdrpShader = Shader.Find("HDRP/Lit");
        if (hdrpShader == null)
        {
            Debug.LogError("HDRP/Lit shader not found. Make sure HDRP is correctly set up in your project.");
            return;
        }
        material.shader = hdrpShader;

        // Define the texture types and their corresponding shader properties for HDRP
        var textureTypes = new Dictionary<string, string>
    {
        {"_dif", "_BaseColorMap"},
        {"_nrm", "_NormalMap"},
        // The "_rgh" texture will be assigned to "_MaskMap" in HDRP
        {"_rgh", "_MaskMap"},
        {"_ems", "_EmissiveColorMap"},
        // Assuming "_spc" is for Specular which is not directly used in HDRP, but you can use "_SpecularColorMap"
        // Assuming "_ocl" is for Occlusion which can be part of the MaskMap in HDRP
    };

        // Iterate through each texture type and assign textures to material
        foreach (var textureType in textureTypes)
        {
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

        // Assign default values for Metallic, Smoothness, and AO remapping.
        // These values are typically set in the range [0, 1], but you need to confirm what the default values are for your project.
        // The values below are examples and may need adjustment.
        material.SetFloat("_MetallicRemapMin", 0.0f);
        material.SetFloat("_MetallicRemapMax", 0.4f);
        material.SetFloat("_SmoothnessRemapMin", 0.0f);
        material.SetFloat("_SmoothnessRemapMax", 0.25f);
        material.SetFloat("_AORemapMin", 0.0f);
        material.SetFloat("_AORemapMax", 1.0f);
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