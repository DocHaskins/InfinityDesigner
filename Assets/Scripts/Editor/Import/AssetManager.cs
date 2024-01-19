#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using static PrefabUtilityScript;

public class AssetManager : EditorWindow
{
    private static string modelsDirectory = "Assets/Resources/Models";
    private static string materialsDirectory = "Assets/Resources/Materials";
    private static string prefabsDirectory = "Assets/Resources/Prefabs";
    private static string meshReferencesDirectory = "Assets/StreamingAssets/Mesh references";
    private int maxPrefabCount = 100;
    private int materialCreationLimit = 100;
    private int materialProcessingLimit = 100;
    private int minTexturesRequired = 0;

    [MenuItem("Tools/Asset Management")]
    public static void ShowWindow()
    {
        GetWindow<AssetManager>("Asset Management");
    }
    void OnGUI()
    {
        GUILayout.Label("Asset Management", EditorStyles.boldLabel);
        GUILayout.Space(20);
        GUILayout.Label("Prefab Creation", EditorStyles.boldLabel);
        GUILayout.Space(4);
        GUILayout.Label("Max .prefabs to Create:");
        maxPrefabCount = EditorGUILayout.IntField("Max Prefab Count", maxPrefabCount);

        if (GUILayout.Button("Create and Update Prefabs"))
        {
            CreateAndUpdatePrefabs(maxPrefabCount);
        }
        if (GUILayout.Button("Update Materials on All Prefabs"))
        {
            ApplyMaterialsToAllPrefabs();
        }

        GUILayout.Space(20);
        GUILayout.Label("Material Creation", EditorStyles.boldLabel);
        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Max .Mat to Create:", GUILayout.Width(150));
        materialCreationLimit = EditorGUILayout.IntField(materialCreationLimit);
        GUILayout.EndHorizontal();

        // Button for creating and binding materials with the specified limit
        if (GUILayout.Button("Create and Bind Materials"))
        {
            CreateAndBindMaterials(materialCreationLimit);
        }
        GUILayout.Space(20);
        GUILayout.Label("Custom Material Tools", EditorStyles.boldLabel);
        GUILayout.Space(4);
        minTexturesRequired = EditorGUILayout.IntField("Max textures for Assignment", minTexturesRequired);
        materialProcessingLimit = EditorGUILayout.IntField("Max .Mat to Process:", materialProcessingLimit);

        if (GUILayout.Button("Fix White Materials"))
        {
            FixWhiteMaterials();
        }
        if (GUILayout.Button("Check Missing Materials"))
        {
            CheckAllMaterials();
        }

        GUILayout.Space(20);
        GUILayout.Label("Texture Management", EditorStyles.boldLabel);
        GUILayout.Space(4);

        if (GUILayout.Button("Assign Custom Shader Textures"))
        {
            AssignCustomShaderTextures();
        }

        if (GUILayout.Button("Check Missing Textures"))
        {
            CheckAllTextures();
        }

    }

    private static void CreateAndUpdatePrefabs(int maxCount)
    {
        var fbxFiles = Directory.GetFiles(modelsDirectory, "*.fbx", SearchOption.AllDirectories)
                                .Take(maxCount)
                                .ToList();

        foreach (var fbxFilePath in fbxFiles)
        {
            string assetPath = fbxFilePath.Replace("\\", "/").Replace(Application.dataPath, "Assets");
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            // Modify the prefabPath to store prefabs in the prefabsDirectory
            string prefabName = Path.GetFileNameWithoutExtension(assetPath) + ".prefab";
            string prefabPath = Path.Combine(prefabsDirectory, prefabName);
            prefabPath = prefabPath.Replace("\\", "/"); // Ensure the path uses forward slashes

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefabAsset == null)
            {
                // Create the prefab in the new directory
                prefabAsset = PrefabUtility.SaveAsPrefabAsset(fbxAsset, prefabPath);
                Debug.Log($"Prefab created: {prefabPath}");
            }
            else
            {
                // Update the existing prefab
                PrefabUtility.SaveAsPrefabAsset(fbxAsset, prefabPath);
                Debug.Log($"Prefab updated: {prefabPath}");
            }

            AssignMaterialsToPrefab(prefabAsset, Path.GetFileNameWithoutExtension(fbxFilePath));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Processed and updated up to {maxCount} prefabs with materials.");
    }

    private static void AssignMaterialsToPrefab(GameObject prefab, string meshName)
    {
        string jsonPath = Path.Combine(meshReferencesDirectory, meshName + ".json");
        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"Mesh reference JSON not found: {jsonPath}");
            return;
        }

        string jsonContent = File.ReadAllText(jsonPath);
        var meshReferenceData = JsonUtility.FromJson<MeshReferenceData>(jsonContent);
        UpdateMaterials(prefab, meshReferenceData.materialsData);
    }

    private static void ApplyMaterialsToAllPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:GameObject", new[] { prefabsDirectory });
        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab != null)
            {
                string prefabName = Path.GetFileNameWithoutExtension(assetPath);
                AssignMaterialsToPrefab(prefab, prefabName);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Saved all asset changes.");
    }

    private static void UpdateMaterials(GameObject prefab, List<MaterialData> materialsList)
    {
        var prefabPath = AssetDatabase.GetAssetPath(prefab);
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var skinnedMeshRenderers = prefabAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        bool updatedMaterials = false; // Flag to check if any materials were updated

        foreach (var renderer in skinnedMeshRenderers)
        {
            Material[] materialsToUpdate = renderer.sharedMaterials;

            for (int i = 0; i < materialsToUpdate.Length; i++)
            {
                if (i < materialsList.Count)
                {
                    var materialData = materialsList[i];
                    string materialPath = Path.Combine(materialsDirectory, materialData.name);
                    materialPath = materialPath.Replace("\\", "/");
                    Material newMat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                    if (newMat != null)
                    {
                        materialsToUpdate[i] = newMat;
                        updatedMaterials = true; // Set flag to true as a material was updated
                        Debug.Log($"Assigned material '{materialData.name}' to '{renderer.gameObject.name}' in prefab '{prefab.name}' at index {i}");
                    }
                    else
                    {
                        Debug.LogError($"Material '{materialData.name}' not found at '{materialPath}' for renderer '{renderer.gameObject.name}' in prefab '{prefab.name}'.");
                    }
                }
            }

            renderer.sharedMaterials = materialsToUpdate;
        }

        if (updatedMaterials)
        {
            EditorUtility.SetDirty(prefabAsset);
            Debug.Log($"Prefab '{prefabAsset.name}' marked as dirty for material updates.");
        }
    }

    private void CheckAllMaterials()
    {
        string jsonsFolderPath = Application.streamingAssetsPath + "/Jsons";
        string resourcesMaterialsPath = "Materials";
        string failedMaterialsFilePath = Application.streamingAssetsPath + "/materials_failed.txt";
        HashSet<string> missingMaterials = new HashSet<string>();

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
                                if (!resource.name.StartsWith("sm_") && !resource.name.Equals("null.mat", StringComparison.OrdinalIgnoreCase))
                                {
                                    string matPath = resourcesMaterialsPath + "/" + Path.GetFileNameWithoutExtension(resource.name);
                                    Material material = Resources.Load<Material>(matPath);

                                    if (material == null)
                                    {
                                        missingMaterials.Add(matPath);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Write missing materials to file
        if (missingMaterials.Count > 0)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(failedMaterialsFilePath, false))
                {
                    foreach (var material in missingMaterials)
                    {
                        writer.WriteLine(material);
                        UnityEngine.Debug.LogError($"Material not found: '{material}'");
                    }
                }
                UnityEngine.Debug.LogError($"Missing materials list written to {failedMaterialsFilePath}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error writing to file: {ex.Message}");
            }
        }
        else
        {
            UnityEngine.Debug.Log("All materials found.");
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
        bool hasGrd = textures.Any(file => file.EndsWith("_grd.png"));

        if (baseName.Contains("decal"))
        {
            return Shader.Find("Shader Graphs/Decal");
        }
        if (ShouldUseCustomShader(baseName))
        {
            return Shader.Find("Shader Graphs/Skin");
        }
        else if (ShouldUseHairShader(baseName))
        {
            return Shader.Find("Shader Graphs/Hair");
        }
        else if (hasGra || hasIdx || hasGrd)
        {
            return Shader.Find("Shader Graphs/Clothing");
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

    private void FixWhiteMaterials()
    {
        string materialsPath = "Assets/Resources/Materials";
        var existingMaterials = LoadMaterialsFromFolder(materialsPath);
        var textureFiles = LoadTexturesFromFolder("Assets/Resources/Textures");

        int processedMaterialsCount = 0;

        foreach (var materialEntry in existingMaterials)
        {
            if (processedMaterialsCount >= materialProcessingLimit)
            {
                Debug.Log($"Processing limit of {materialProcessingLimit} materials reached. Stopping.");
                break;
            }

            Material material = materialEntry.Value;

            if (MaterialHasMultipleTextures(material, minTexturesRequired))
            {
                string baseName = materialEntry.Key;
                List<string> matchingTextures = FindTexturesForMaterial(textureFiles, baseName);

                // If no textures found, try modifying the base name
                if (matchingTextures.Count == 0)
                {
                    // Try removing the start of the base name up to the first '_'
                    string modifiedBaseName = RemoveStartOfBaseName(baseName);
                    matchingTextures = FindTexturesForMaterial(textureFiles, modifiedBaseName);

                    // If still no textures found, try removing the end of the base name back to the last '_'
                    if (matchingTextures.Count == 0)
                    {
                        modifiedBaseName = RemoveEndOfBaseName(baseName);
                        matchingTextures = FindTexturesForMaterial(textureFiles, modifiedBaseName);
                    }

                    // If textures are found with the modified base name, use them
                    if (matchingTextures.Count > 0)
                    {
                        baseName = modifiedBaseName;
                    }
                }

                if (matchingTextures.Count > 0)
                {
                    AssignTexturesToMaterial(material, matchingTextures, baseName);
                    EditorUtility.SetDirty(material);
                    processedMaterialsCount++;
                }
                else
                {
                    Debug.LogError($"No matching textures found for material: {baseName}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Processed {processedMaterialsCount} materials.");
    }

    private string RemoveStartOfBaseName(string baseName)
    {
        int index = baseName.IndexOf('_');
        return index >= 0 ? baseName.Substring(index + 1) : baseName;
    }

    private string RemoveEndOfBaseName(string baseName)
    {
        int lastIndex = baseName.LastIndexOf('_');
        return lastIndex >= 0 ? baseName.Substring(0, lastIndex) : baseName;
    }

    private bool MaterialHasMultipleTextures(Material material, int minTextures)
    {
        string[] textureProperties = {
        "_nrm", "_rgh", "_dif", "_ocl", "_opc", "_spc", "_gra", "_grd", "_clp", "_ems"
    };

        int textureCount = textureProperties.Count(property => material.HasProperty(property) && material.GetTexture(property) != null);

        // If minTextures is 0, return true only if textureCount is 0, i.e., no textures are assigned.
        // Otherwise, return true if textureCount is equal to or greater than minTextures.
        return minTextures == 0 ? textureCount == 0 : textureCount >= minTextures;
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
                material.shader.name == "Shader Graphs/Hair" ||
                material.shader.name == "Shader Graphs/Decal" ||
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
        "Shader Graphs/Hair",
        "Shader Graphs/Decal",
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
                if (useCustomShader)
                {
                    material.SetTexture("_ocl", texture);
                }
                else
                {
                    //material.SetTexture("_NormalMap", texture);
                }
            }
            else if (textureFile.Contains(baseName + "_opc") && useCustomShader)
            {
                if (useCustomShader)
                {
                    material.SetTexture("_opc", texture);
                }
                else
                {
                    //material.SetTexture("_NormalMap", texture);
                }
            }
            else if (textureFile.Contains(baseName + "_spc") && useCustomShader)
            {
                if (useCustomShader)
                {
                    material.SetTexture("_spc", texture);
                }
                else
                {
                    //material.SetTexture("_NormalMap", texture);
                }
            }
            else if (textureFile.Contains(baseName + "_clp") && useCustomShader)
            {
                if (useCustomShader)
                {
                    material.SetTexture("_clp", texture);
                }
                else
                {
                    //material.SetTexture("_NormalMap", texture);
                }
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
                material.SetTexture("_msk", texture);
            }
            else if (textureFile.Contains(baseName + "_rgh"))
            {
                if (useCustomShader)
                {
                    material.SetTexture("_rgh", texture);
                }
                else
                {
                    material.SetTexture("_MaskMap", texture);
                }
            }
            else if (textureFile.Contains(baseName + "_ems"))
            {
                if (useCustomShader)
                {
                    material.SetTexture("_ems", texture);
                    Debug.Log($"_ems set for custom shader: {texture}");
                }
                else
                {
                    material.SetTexture("_EmissiveColorMap", texture);
                    Color emissiveHDRColor = new Color(8f, 8f, 8f, 8f); // High intensity color for HDR
                    material.SetColor("_EmissiveColor", emissiveHDRColor);
                    Color emissiveLDRColor = Color.white; // Standard white color for LDR
                    material.SetColor("_EmissiveColorLDR", emissiveLDRColor);
                    material.SetInt("_UseEmissiveIntensity", 1);
                    float emissionIntensity = Mathf.Pow(2f, 3f); // Convert 3 EV100 to HDRP's internal unit
                    material.SetFloat("_EmissiveIntensity", emissionIntensity);
                    Debug.Log($"_ems set for standard shader: {texture}");
                }
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

            if (!hasSpecularTexture)
            {
                string frsTexturePath = textureFiles.FirstOrDefault(file => file.Contains(baseName + "_frs"));
                if (frsTexturePath != null)
                {
                    Texture2D frsTexture = LoadTexture(frsTexturePath);
                    if (useCustomShader)
                    {
                        material.SetTexture("_spc", frsTexture);
                    }
                    else
                    {
                        //material.SetTexture("_SpecMap", frsTexture);
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
#endif