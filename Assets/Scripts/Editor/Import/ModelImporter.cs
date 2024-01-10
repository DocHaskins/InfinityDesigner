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
                // Check if a similar material exists
                string similarMaterialName = FindSimilarMaterialName(baseName, existingMaterials.Keys);
                if (!string.IsNullOrEmpty(similarMaterialName) && existingMaterials.TryGetValue(similarMaterialName, out Material similarMaterial))
                {
                    // Clone the similar material
                    material = new Material(similarMaterial);
                    Debug.Log($"Cloned similar material: {similarMaterialName} for {baseName}");
                }
                else
                {
                    // Decide which shader to use
                    Shader shaderToUse;
                    if (ShouldUseCustomShader(baseName))
                    {
                        shaderToUse = Shader.Find("Shader Graphs/Skin");
                    }
                    else if (ShouldUseHairShader(baseName))
                    {
                        shaderToUse = Shader.Find("HDRP/Hair");
                    }
                    else
                    {
                        shaderToUse = Shader.Find("HDRP/Lit");
                    }

                    // Create new material with decided shader
                    material = new Material(shaderToUse);
                    string materialPath = Path.Combine(materialsPath, baseName + ".mat");
                    AssetDatabase.CreateAsset(material, materialPath);
                    Debug.Log($"Created HDRP Material: {materialPath}");
                    createdMaterialsCount++;
                }
            }

            // Assign textures to material
            AssignTexturesToMaterial(material, textures, baseName);
        }

        AssetDatabase.Refresh();
        Debug.Log($"{createdMaterialsCount} materials were created and binding complete.");
    }

    private bool ShouldUseCustomShader(string resourceName)
    {
        // Define names that should use the custom shader
        string[] specialNames = {
        "sh_biter_", "sh_man_", "sh_scan_man_", "multihead007_npc_carl_",
        "sh_wmn_", "sh_scan_wmn_", "sh_dlc_opera_wmn_", "nnpc_wmn_worker",
        "sh_scan_kid_", "sh_scan_girl_", "sh_scan_boy_", "sh_chld_"
    };

        if (resourceName.Contains("hair"))
        {
            return false;
        }

        return specialNames.Any(name => resourceName.StartsWith(name));
    }
    private bool ShouldUseHairShader(string resourceName)
    {
        string[] hairNames = {
        "sh_man_hair_", "chr_npc_hair_", "chr_hair_", "man_facial_hair_",
        "man_hair_", "npc_aiden_hair", "npc_hair_", "sh_wmn_hair_",
        "sh_wmn_zmb_hair", "wmn_hair_", "viral_hair_", "wmn_viral_hair_",
        "zmb_bolter_a_hair_", "zmb_banshee_hairs_"
    };
        return hairNames.Any(name => resourceName.StartsWith(name));
    }

    private string FindSimilarMaterialName(string baseName, IEnumerable<string> existingMaterialNames)
    {
        Debug.Log($"Finding similar material for: {baseName}");
        int lastIndex = baseName.LastIndexOf('_');
        if (lastIndex > 0)
        {
            string trimmedName = baseName.Substring(0, lastIndex);
            Debug.Log($"Trimmed name: {trimmedName}");

            foreach (var name in existingMaterialNames)
            {
                Debug.Log($"Checking against existing material: {name}");
                if (name.StartsWith(trimmedName) && !name.Equals(baseName))
                {
                    Debug.Log($"Found similar material: {name}");
                    return name;
                }
            }
        }
        else
        {
            Debug.Log($"No underscore found in {baseName}, unable to trim name.");
        }
        Debug.Log($"No similar material found for {baseName}");
        return null;
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

    private void AssignTexturesToMaterial(Material material, List<string> textureFiles, string baseName)
    {
        string[] specialNames = new string[] {
        "sh_biter_", "sh_man_", "sh_scan_man_", "multihead007_npc_carl_",
        "sh_wmn_", "sh_scan_wmn_", "sh_dlc_opera_wmn_", "nnpc_wmn_worker",
        "sh_scan_kid_", "sh_scan_girl_", "sh_scan_boy_", "sh_chld_"
    };

        bool useCustomShader = textureFiles.Any(file => specialNames.Any(name => Path.GetFileName(file).StartsWith(name)));
        material.shader = useCustomShader ? Shader.Find("Shader Graphs/Skin") : Shader.Find("HDRP/Lit");

        bool hasDiffuseTexture = false;
        bool hasRoughnessTexture = textureFiles.Any(file => file.Contains(baseName + "_rgh"));
        bool hasSpecularTexture = textureFiles.Any(file => file.Contains(baseName + "_spc"));

        foreach (var textureFile in textureFiles)
        {
            Texture2D texture = LoadTexture(textureFile);

            if (textureFile.Contains(baseName + "_dif"))
            {
                if (useCustomShader)
                {
                    material.SetTexture("_dif", texture);
                }
                else
                {
                    material.SetTexture("_BaseColorMap", texture);
                }
                hasDiffuseTexture = true;
            }
            else if (textureFile.Contains(baseName + "_nrm"))
            {
                if (useCustomShader)
                {
                    material.SetTexture("_nrm", texture);
                }
                else
                {
                    material.SetTexture("_NormalMap", texture);
                }
            }
            else if (textureFile.Contains(baseName + "_ocl") && useCustomShader)
            {
                material.SetTexture("_ocl", texture);
            }
            else if (textureFile.Contains(baseName + "_msk") && useCustomShader)
            {
                material.SetTexture("_mask", texture);
            }
            else if (textureFile.Contains(baseName + "_rgh") && !useCustomShader)
            {
                material.SetTexture("_MaskMap", texture);
            }
            else if (textureFile.Contains(baseName + "_ems") && !useCustomShader)
            {
                material.SetTexture("_EmissiveColorMap", texture);
            }
        }

        if (!hasRoughnessTexture && hasSpecularTexture)
        {
            string spcTexturePath = textureFiles.FirstOrDefault(f => f.Contains(baseName + "_spc"));
            if (spcTexturePath != null)
            {
                Texture2D spcTexture = LoadTexture(spcTexturePath);
                material.SetTexture("_MaskMap", spcTexture); // Assuming _MaskMap is the correct property

                // Set specific material properties
                material.SetFloat("_MetallicRemapMin", 0.6f);
                material.SetFloat("_MetallicRemapMax", 1.0f);
                material.SetFloat("_SmoothnessRemapMin", 0.25f);
                material.SetFloat("_SmoothnessRemapMax", 0.5f);
            }
        }

        if (!hasDiffuseTexture && !useCustomShader)
        {
            string grdTexturePath = textureFiles.FirstOrDefault(f => f.Contains(baseName + "_grd"));
            if (grdTexturePath != null)
            {
                Texture2D grdTexture = LoadTexture(grdTexturePath);
                material.SetTexture("_BaseColorMap", grdTexture);
            }
        }

        if (useCustomShader)
        {
            material.SetFloat("_Modifier", 0);
            material.SetFloat("_Blend", 0.9f);
            material.SetFloat("_AO_Multiply", 1.0f);
        }

        // Configure additional properties for non-custom shader
        if (!useCustomShader)
        {
            bool hasClippingTexture = textureFiles.Any(file => file.Contains(baseName + "_clp"));
            if (hasClippingTexture)
            {
                material.SetFloat("_AlphaCutoffEnable", 1);
                material.SetFloat("_AlphaCutoff", 0.6f);
                material.EnableKeyword("_BACK_THEN_FRONT_RENDERING");
                material.SetFloat("_ReceivesSSRTransparent", 1);
            }

            // Default remapping values
            material.SetFloat("_MetallicRemapMin", 0.0f);
            material.SetFloat("_MetallicRemapMax", 0.4f);
            material.SetFloat("_SmoothnessRemapMin", 0.0f);
            material.SetFloat("_SmoothnessRemapMax", 0.25f);
            material.SetFloat("_AORemapMin", 0.0f);
            material.SetFloat("_AORemapMax", 1.0f);
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