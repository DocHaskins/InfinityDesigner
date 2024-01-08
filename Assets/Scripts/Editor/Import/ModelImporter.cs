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
    private int fbxProcessingLimit = 10;

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

        GUILayout.BeginHorizontal();
        GUILayout.Label("FBX Processing Limit:", GUILayout.Width(150));
        fbxProcessingLimit = EditorGUILayout.IntField(fbxProcessingLimit);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Assign Materials to FBX"))
        {
            AssignMaterialsToFbx();
        }

        
    }

    private Dictionary<string, Material> LoadMaterialsFromFolder(string folderPath)
    {
        var materialFiles = Directory.GetFiles(folderPath, "*.mat", SearchOption.AllDirectories);
        var materials = new Dictionary<string, Material>();

        foreach (var file in materialFiles)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(file);
            if (material != null)
            {
                var baseName = Path.GetFileNameWithoutExtension(file);
                materials[baseName] = material;
            }
        }

        return materials;
    }

    private Dictionary<string, List<string>> LoadTexturesFromFolder(string folderPath)
    {
        var textureFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                                    .Where(file => IsTextureFile(file))
                                    .ToList();
        var textures = new Dictionary<string, List<string>>();

        foreach (var file in textureFiles)
        {
            var baseName = GetBaseName(file);
            if (!textures.ContainsKey(baseName))
                textures[baseName] = new List<string>();
            textures[baseName].Add(file);
        }

        return textures;
    }

    private void CreateAndBindMaterials(int limit = 100)
    {
        string texturesPath = "Assets/Resources/Textures";
        string materialsPath = "Assets/Resources/Materials";

        // Load existing materials
        var existingMaterials = LoadMaterialsFromFolder(materialsPath);

        // Load textures from the Textures folder
        var textureFiles = LoadTexturesFromFolder(texturesPath);

        int createdMaterialsCount = 0;

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

            Material material;
            if (!existingMaterials.TryGetValue(baseName, out material))
            {
                // Create new material if it doesn't exist
                material = new Material(Shader.Find("HDRP/Lit"));
                string materialPath = Path.Combine(materialsPath, baseName + ".mat");
                AssetDatabase.CreateAsset(material, materialPath);
                Debug.Log($"Created HDRP Material: {materialPath}");
                createdMaterialsCount++;
            }
            else
            {
                Debug.Log($"Found existing material: {material.name}");
            }

            // Assign textures to material
            AssignTexturesToMaterial(material, textures, baseName);
        }

        AssetDatabase.Refresh();
        Debug.Log($"{createdMaterialsCount} materials were created and binding complete.");
    }

    private string meshReferencesDirectory = "Assets/StreamingAssets/Mesh references";
    private static string modelsDirectory = "Assets/Resources/Models";

    private void AssignMaterialsToFbx()
    {
        var fbxPaths = Directory.GetFiles(modelsDirectory, "*.fbx", SearchOption.AllDirectories);
        int processedFbxCount = 0;

        foreach (var fbxPath in fbxPaths)
        {
            if (processedFbxCount >= fbxProcessingLimit)
            {
                Debug.Log($"FBX processing limit of {fbxProcessingLimit} reached. Stopping further processing.");
                break;
            }

            Debug.Log($"Processing FBX at path: {fbxPath}");
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fbxPath);
            string jsonPath = Path.Combine(meshReferencesDirectory, fileNameWithoutExtension + ".json");

            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"Mesh reference JSON not found: {jsonPath}");
                continue;
            }

            string jsonContent = File.ReadAllText(jsonPath);
            var modelData = JsonUtility.FromJson<ModelData>(jsonContent);

            AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;

            if (importer != null && modelData != null)
            {
                // Change import mode to embedded materials
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                SerializedObject serializedObject = new SerializedObject(importer);
                SerializedProperty materialsProperty = serializedObject.FindProperty("m_ExternalObjects");

                foreach (var slotPair in modelData.slotPairs)
                {
                    foreach (var modelInfo in slotPair.slotData.models)
                    {
                        foreach (var materialResource in modelInfo.materialsResources)
                        {
                            int materialIndex = materialResource.number - 1;

                            foreach (var resource in materialResource.resources)
                            {
                                string materialPath = Path.Combine(modelsDirectory, resource.name);
                                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                                if (material != null && materialIndex >= 0 && materialIndex < materialsProperty.arraySize)
                                {
                                    SerializedProperty materialProperty = materialsProperty.GetArrayElementAtIndex(materialIndex);
                                    materialProperty.FindPropertyRelative("second").objectReferenceValue = material;
                                    Debug.Log($"Loaded material '{material.name}' from path: {materialPath}");
                                }
                                else
                                {
                                    Debug.LogError($"Failed to load material from path: {materialPath} or index out of bounds. Material null: {material == null}");
                                }
                            }
                        }
                    }
                }

                serializedObject.ApplyModifiedProperties();
                importer.SaveAndReimport(); // Apply the changes immediately
                Debug.Log($"Updated materials for FBX: {fbxPath}");
            }
            else
            {
                Debug.LogError($"Importer for the FBX file at '{fbxPath}' could not be found or modelData is null.");
            }
            processedFbxCount++;
        }

        Debug.Log($"Processed {processedFbxCount} FBX files out of {fbxPaths.Length} available.");
        AssetDatabase.SaveAssets();
        Debug.Log("FBX material assignment complete.");
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
        string[] specialNames = new string[] {
        "sh_biter_", "sh_man_", "sh_scan_man_", "multihead007_npc_carl_",
        "sh_wmn_", "sh_scan_wmn_", "sh_dlc_opera_wmn_", "nnpc_wmn_worker",
        "sh_scan_kid_", "sh_scan_girl_", "sh_scan_boy_", "sh_chld_"
    };

        bool useCustomShader = textureFiles.Any(file => specialNames.Any(name => Path.GetFileName(file).StartsWith(name)));

        if (useCustomShader)
        {
            // Carry over settings if the material is already HDRP/Lit
            if (material.shader.name == "HDRP/Lit")
            {
                // Store current settings that need to be retained
                var baseColorMap = material.GetTexture("_BaseColorMap");
                var normalMap = material.GetTexture("_NormalMap");
                var maskMap = material.GetTexture("_MaskMap");

                // Switch to the custom shader
                material.shader = Shader.Find("Shader Graphs/Skin");

                // Reapply the retained settings
                material.SetTexture("_BaseColorMap", baseColorMap);
                material.SetTexture("_NormalMap", normalMap);
                material.SetTexture("_MaskMap", maskMap);

                // Other custom settings for the shader
                material.SetFloat("_Modifier", 0);
                material.SetFloat("_Blend", 0.9f);
                material.SetFloat("_AO_Multiply", 1.0f);
            }
            else
            {
                // Set the custom shader for materials not originally using HDRP/Lit
                material.shader = Shader.Find("Shader Graphs/Skin");
                material.SetFloat("_Modifier", 0);
            }
        }
        else
        {
            // Ensure the material uses HDRP/Lit shader if not using custom shader
            if (material.shader.name != "HDRP/Lit")
            {
                material.shader = Shader.Find("HDRP/Lit");
            }
        }

        // Apply textures to the material
        foreach (var textureFile in textureFiles)
        {
            Texture2D texture = LoadTexture(textureFile);

            if (textureFile.Contains(baseName + "_dif"))
            {
                material.SetTexture("_BaseColorMap", texture);
                Debug.Log($"Assigned diffuse texture '{texture.name}' to material '{material.name}'");
            }
            else if (textureFile.Contains(baseName + "_nrm"))
            {
                material.SetTexture("_NormalMap", texture);
                Debug.Log($"Assigned normal texture '{texture.name}' to material '{material.name}'");
            }
            else if (textureFile.Contains(baseName + "_ocl"))
            {
                material.SetTexture("_OclMap", texture); // Assuming "_ocl" maps to "_OclMap"
                Debug.Log($"Assigned occlusion texture '{texture.name}' to material '{material.name}'");
            }
            else if (textureFile.Contains(baseName + "_msk") && useCustomShader)
            {
                material.SetTexture("_MaskMap", texture);
                Debug.Log($"Assigned mask texture '{texture.name}' to material '{material.name}'");
            }
        }

        // Additional properties for the custom shader
        if (useCustomShader)
        {
            material.SetFloat("_Blend", 0.9f);
            material.SetFloat("_AO_Multiply", 1.0f);
            // Set other properties as needed
        }

        Shader hdrpShader = Shader.Find("HDRP/Lit");
        if (hdrpShader == null)
        {
            Debug.LogError("HDRP/Lit shader not found. Make sure HDRP is correctly set up in your project.");
            return;
        }
        material.shader = hdrpShader;

        bool hasDiffuseTexture = false;
        bool hasClippingTexture = false;

        foreach (var textureFile in textureFiles)
        {
            Texture2D texture = LoadTexture(textureFile);

            if (textureFile.Contains(baseName + "_dif"))
            {
                material.SetTexture("_BaseColorMap", texture);
                Debug.Log($"Assigned diffuse texture '{texture.name}' to material '{material.name}'");
                hasDiffuseTexture = true;
            }
            else if (textureFile.Contains(baseName + "_nrm"))
            {
                material.SetTexture("_NormalMap", texture);
                Debug.Log($"Assigned normal texture '{texture.name}' to material '{material.name}'");
            }
            else if (textureFile.Contains(baseName + "_rgh"))
            {
                material.SetTexture("_MaskMap", texture);
                Debug.Log($"Assigned roughness texture '{texture.name}' to material '{material.name}'");
            }
            else if (textureFile.Contains(baseName + "_ems"))
            {
                material.SetTexture("_EmissiveColorMap", texture);
                Debug.Log($"Assigned emissive texture '{texture.name}' to material '{material.name}'");
            }
            else if (textureFile.Contains(baseName + "_clp"))
            {
                material.SetTexture("_BaseColorMap", texture);
                Debug.Log($"Assigned diffuse texture '{texture.name}' to material '{material.name}'");
                hasClippingTexture = true;
            }
        }

        if (hasDiffuseTexture && hasClippingTexture)
        {
            material.SetFloat("_SurfaceType", 1); // Set Surface Type to Transparent
            material.SetFloat("_AlphaCutoffEnable", 1); // Enable Alpha Clipping
            material.SetFloat("_AlphaCutoff", 0.6f); // Set Alpha Cutoff value
            material.EnableKeyword("_BACK_THEN_FRONT_RENDERING"); // Enable Back then Front Rendering
            material.SetFloat("_ReceivesSSRTransparent", 1); // Enable Receive SSR Transparent
            Debug.Log($"Configured material '{material.name}' for transparency with alpha clipping, back then front rendering, and SSR.");
        }

        if (!hasDiffuseTexture)
        {
            string opcTexturePath = textureFiles.FirstOrDefault(f => f.EndsWith(baseName + "_opc.png"));
            if (!string.IsNullOrEmpty(opcTexturePath))
            {
                Texture2D opcTexture = LoadTexture(opcTexturePath);
                material.SetTexture("_BaseColorMap", opcTexture);
                Debug.Log($"Assigned OPC texture '{opcTexture.name}' to material '{material.name}'");
            }
        }

        // Set default remapping values
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