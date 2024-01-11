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

    [MenuItem("Tools/Asset Management")]
    public static void ShowWindow()
    {
        GetWindow<ModelImporterWindow>("Asset Management");
    }
    void OnGUI()
    {
        GUILayout.Label("Asset Management", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Material Creation Limit:", GUILayout.Width(150));
        materialCreationLimit = EditorGUILayout.IntField(materialCreationLimit);
        GUILayout.EndHorizontal();

        // Button for creating and binding materials with the specified limit
        if (GUILayout.Button("Create and Bind Materials"))
        {
            CreateAndBindMaterials(materialCreationLimit);
        }

        if (GUILayout.Button("Assign Custom Shader Textures"))
        {
            AssignCustomShaderTextures();
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

        var existingMaterials = LoadMaterialsFromFolder(materialsPath);
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

            // Determine which shader to use based on texture names
            Shader shaderToUse = DetermineShader(textures, baseName);

            Material material;
            string materialPath = Path.Combine(materialsPath, baseName + ".mat");

            if (!existingMaterials.TryGetValue(baseName, out material))
            {
                if (!File.Exists(materialPath))
                {
                    // Only create a new material if it doesn't already exist
                    material = new Material(shaderToUse);
                    AssetDatabase.CreateAsset(material, materialPath);
                    Debug.Log($"Created new material: {materialPath}");
                }
                else
                {
                    // Load the existing material instead of creating a new one
                    material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    Debug.Log($"Loaded existing material: {materialPath}");
                }
                createdMaterialsCount++;
            }

            // Assign textures to material
            AssignTexturesToMaterial(material, textures, baseName);
        }

        AssetDatabase.Refresh();
        Debug.Log($"{createdMaterialsCount} materials were created and binding complete.");
    }

    private Shader DetermineShader(List<string> textures, string baseName)
    {
        bool hasIdx = textures.Any(file => file.EndsWith("_idx.png"));
        bool hasDif = textures.Any(file => file.EndsWith("_dif.png"));
        bool hasGra = textures.Any(file => file.EndsWith("_gra.png"));

        if (ShouldUseCustomShader(baseName))
        {
            return Shader.Find("Shader Graphs/Skin");
        }
        else if (ShouldUseHairShader(baseName))
        {
            return Shader.Find("HDRP/Hair");
        }
        else if (hasGra)
        {
            return Shader.Find("Shader Graphs/Clothing");
        }
        else if (hasIdx && hasDif)
        {
            return Shader.Find("Shader Graphs/Clothing_dif");
        }
        else
        {
            return Shader.Find("HDRP/Lit");
        }
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

    private void AssignCustomShaderTextures()
    {
        Debug.Log("AssignCustomShaderTextures started");
        string materialsPath = "Assets/Resources/Materials";
        string texturesPath = "Assets/Resources/Textures";

        var materials = LoadMaterialsFromFolder(materialsPath);
        var textureFiles = LoadTexturesFromFolder(texturesPath);

        Debug.Log($"Total materials found: {materials.Count}");

        foreach (var materialEntry in materials)
        {
            Material material = materialEntry.Value;

            Debug.Log($"Processing material: {material.name}, Shader: {material.shader.name}");

            // Check if material uses any of the specific shaders
            if (material.shader.name == "Shader Graphs/Clothing" ||
                material.shader.name == "Shader Graphs/Clothing_dif" ||
                material.shader.name == "Shader Graphs/Skin")
            {
                string baseName = materialEntry.Key;
                Debug.Log($"Material '{baseName}' uses one of the specific shaders");

                List<string> matchingTextures = FindTexturesForMaterial(textureFiles, baseName);
                Debug.Log($"Matching textures for '{baseName}': {string.Join(", ", matchingTextures)}");

                AssignTexturesToMaterial(material, matchingTextures, baseName);
                EditorUtility.SetDirty(material);
            }
            else
            {
                Debug.Log($"Material '{material.name}' does not use one of the specific shaders");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("AssignCustomShaderTextures completed");
    }

    private List<string> FindTexturesForMaterial(Dictionary<string, List<string>> textureFiles, string baseName)
    {
        Debug.Log($"FindTexturesForMaterial");
        return textureFiles.ContainsKey(baseName) ? textureFiles[baseName] : new List<string>();
    }

    private void AssignTexturesToMaterial(Material material, List<string> textureFiles, string baseName)
    {
        string[] specialNames = new string[] {
        "sh_biter_", "sh_man_", "sh_scan_man_", "multihead007_npc_carl_",
        "sh_wmn_", "sh_scan_wmn_", "sh_dlc_opera_wmn_", "nnpc_wmn_worker",
        "sh_scan_kid_", "sh_scan_girl_", "sh_scan_boy_", "sh_chld_"
    };

        string[] customShaders = new string[] {
        "Shader Graphs/Clothing",
        "Shader Graphs/Clothing_dif",
        "Shader Graphs/Skin"
    };
        bool useCustomShader = customShaders.Contains(material.shader.name);

        bool hasDiffuseTexture = false;
        bool hasRoughnessTexture = textureFiles.Any(file => file.Contains(baseName + "_rgh"));
        bool hasSpecularTexture = textureFiles.Any(file => file.Contains(baseName + "_spc"));
        bool foundGraTexture = false;

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
            else if (textureFile.Contains(baseName + "_spc") && useCustomShader)
            {
                material.SetTexture("_spc", texture);
            }
            else if (textureFile.Contains(baseName + "_clp") && useCustomShader)
            {
                material.SetTexture("_clp", texture);
            }
            else if (textureFile.Contains(baseName + "_gra") && useCustomShader)
            {
                material.SetTexture("_gra", texture);
                foundGraTexture = true;
            }
            else if (textureFile.Contains(baseName + "_idx") && useCustomShader)
            {
                material.SetTexture("_idx", texture);
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

                // Set the HDR emissive color
                Color emissiveHDRColor = new Color(8f, 8f, 8f, 8f); // High intensity color for HDR
                material.SetColor("_EmissiveColor", emissiveHDRColor);

                // Set the LDR (Low Dynamic Range) emissive color
                Color emissiveLDRColor = Color.white; // Standard white color for LDR
                material.SetColor("_EmissiveColorLDR", emissiveLDRColor);

                // Enable and set the emission intensity
                material.SetInt("_UseEmissiveIntensity", 1);
                float emissionIntensity = Mathf.Pow(2f, 3f); // Convert 3 EV100 to HDRP's internal unit
                material.SetFloat("_EmissiveIntensity", emissionIntensity);
            }

            if (!foundGraTexture)
            {
                string grdTexturePath = textureFiles.FirstOrDefault(f => f.Contains(baseName + "_grd"));
                if (grdTexturePath != null)
                {
                    Texture2D grdTexture = LoadTexture(grdTexturePath);
                    if (useCustomShader)
                    {
                        material.SetTexture("_gra", grdTexture);
                    }
                }
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