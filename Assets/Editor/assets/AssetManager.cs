#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using static PrefabUtilityScript;
using static doppelganger.AssetManager;
using static ModelData;

/// <summary>
/// Automates the management of models, materials, and prefabs within the project. 
/// It provides functionalities for creating and updating prefabs, creating materials from textures, fixing material assignments, and managing texture assignments. 
/// It supports batch processing and integrates directly into the Unity Editor for efficient asset pipeline management.
/// </summary>

namespace doppelganger
{
    public class AssetManager : EditorWindow
    {
        private static string modelsDirectory = "Assets/Resources/Models";
        private static string materialsDirectory = "Assets/Resources/Materials";
        private static string prefabsDirectory = "Assets/Resources/Prefabs";
        private static string meshReferencesDirectory = "Assets/StreamingAssets/Mesh references";
        private static string newShaderName = "Shader Graphs/Clothing";
        private const string MaterialSlot = "_gra";
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
            GUILayout.Space(20);
            GUILayout.Label("Custom Material Tools", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Max .Mat to Create:", GUILayout.Width(150));
            materialCreationLimit = EditorGUILayout.IntField(materialCreationLimit);
            GUILayout.EndHorizontal();

            // Button for creating and binding materials with the specified limit
            if (GUILayout.Button("Create and Bind Materials"))
            {
                CreateAndBindMaterials(materialCreationLimit);
            }
            
            GUILayout.Space(4);
            if (GUILayout.Button("Convert HDRP Materials"))
            {
                ConvertMaterials();
            }
            GUILayout.Space(4);
            if (GUILayout.Button("Create Materials Index"))
            {
                CreateMaterialsIndex();
            }
            GUILayout.Space(4);
            materialProcessingLimit = EditorGUILayout.IntField("Max .Mat to Process:", materialProcessingLimit);

            if (GUILayout.Button("Fix FPP/TPP Materials"))
            {
                FixTPPFPPMaterials();
            }

            if (GUILayout.Button("Fix Variant Materials"))
            {
                FixVariantMaterials();
            }

            if (GUILayout.Button("Check Missing Materials"))
            {
                CheckAllMaterials();
            }

            GUILayout.Space(20);

            GUILayout.Label("Texture Management", EditorStyles.boldLabel);
            GUILayout.Space(4);
            if (GUILayout.Button("Create Textures Index"))
            {
                CreateTexturesIndex();
            }
            GUILayout.Space(4);
            minTexturesRequired = EditorGUILayout.IntField("Max textures for Assignment", minTexturesRequired);
            if (GUILayout.Button("Assign Custom Shader Textures"))
            {
                AssignCustomShaderTextures();
            }

            if (GUILayout.Button("Check Missing Textures"))
            {
                CheckAllTextures();
            }

            if (GUILayout.Button("Remove Specific Texture"))
            {
                RemoveTexture();
            }

        }

        private List<string> variationSuffixes = new List<string>
{
    "_b", "_tpp_dirt", "_monster_fpp", "_02", "_03", "_tpp_02", "_tpp_03", "_02_fpp", "_02_tpp", "_03_fpp", "_03_tpp", "_03_r2_fpp", "_03_r2_tpp", "_03_r3_fpp", "_03_r3_tpp", "_03_r4_fpp", "_03_r4_tpp", "_03_r5_fpp", "_03_r5_tpp"
};

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

        public static void ConvertMaterials()
        {
            // Load all materials in the specified folder
            string[] materialFiles = Directory.GetFiles(Path.Combine(Application.dataPath, "Resources/Materials"), "*.mat", SearchOption.AllDirectories);

            Shader newShader = Shader.Find(newShaderName);
            if (newShader == null)
            {
                Debug.LogError("Custom shader not found. Please ensure the shader name and path are correct.");
                return;
            }

            foreach (string materialFile in materialFiles)
            {
                string assetPath = "Assets" + materialFile.Replace(Application.dataPath, "").Replace('\\', '/');
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if (mat != null)
                {
                    // Check if the material uses an HDRP/Lit or HDRP/Hair shader
                    if (mat.shader.name == "HDRP/Lit" || mat.shader.name == "HDRP/Hair" || mat.shader.name == "Standard")
                    {
                        // Change the shader to the custom one
                        mat.shader = newShader;
                        Debug.Log($"Converted {mat.name} to use {newShaderName}");
                    }
                }
            }

            // Save changes
            AssetDatabase.SaveAssets();
            Debug.Log("All suitable materials have been converted.");
        }

        private static void UpdateMaterials(GameObject prefab, List<MaterialData> materialsList)
        {
            var prefabPath = AssetDatabase.GetAssetPath(prefab);
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var skinnedMeshRenderers = prefabAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (var materialData in materialsList)
            {
                int rendererIndex = materialData.number - 1; // Assuming 'number' starts from 1
                if (rendererIndex >= 0 && rendererIndex < skinnedMeshRenderers.Length)
                {
                    var renderer = skinnedMeshRenderers[rendererIndex];
                    string materialPath = Path.Combine(materialsDirectory, materialData.name);
                    materialPath = materialPath.Replace("\\", "/");
                    Material newMat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                    if (newMat != null)
                    {
                        Material[] rendererMaterials = renderer.sharedMaterials;
                        if (rendererMaterials.Length > 0)
                        {
                            rendererMaterials[0] = newMat; // Assign new material to the first slot
                            renderer.sharedMaterials = rendererMaterials;
                            Debug.Log($"Assigned material '{materialData.name}' to '{renderer.gameObject.name}' in prefab '{prefab.name}' at index {rendererIndex}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Material '{materialData.name}' not found at '{materialPath}' for renderer '{renderer.gameObject.name}' in prefab '{prefab.name}'.");
                    }
                }
                else
                {
                    Debug.LogError($"Renderer index out of bounds: {rendererIndex} for material number {materialData.number} in prefab '{prefab.name}'.");
                }
            }

            EditorUtility.SetDirty(prefabAsset);
            Debug.Log($"Prefab '{prefabAsset.name}' marked as dirty for material updates.");
        }

        public class MaterialsIndex
        {
            public List<string> materials;
        }

        private void CreateMaterialsIndex()
        {
            string jsonsFolderPath = Application.streamingAssetsPath + "/Jsons";
            string materialsFolderPath = Application.dataPath + "/Resources/Materials";
            string meshReferencesFolderPath = Application.streamingAssetsPath + "/Mesh References";
            string materialsIndexPath = Path.Combine(meshReferencesFolderPath, "materials_index.json");

            HashSet<string> materialNamesFromJson = new HashSet<string>(); // Store names from JSON

            Debug.Log("Loading materials from JSON files...");
            if (Directory.Exists(jsonsFolderPath))
            {
                Debug.Log($"Searching JSON files in {jsonsFolderPath}");
                foreach (var jsonFile in Directory.GetFiles(jsonsFolderPath, "*.json", SearchOption.AllDirectories))
                {
                    Debug.Log($"Reading {jsonFile}");
                    string jsonData = File.ReadAllText(jsonFile);
                    try
                    {
                        ModelData modelData = JsonConvert.DeserializeObject<ModelData>(jsonData);
                        if (modelData != null && modelData.slotPairs != null)
                        {
                            foreach (var slotPair in modelData.slotPairs)
                            {
                                foreach (var modelInfo in slotPair.slotData.models)
                                {
                                    foreach (var materialData in modelInfo.materialsData)
                                    {
                                        if (materialData.name.EndsWith(".mat"))
                                        {
                                            Debug.Log($"Found material in JSON: {materialData.name}");
                                            materialNamesFromJson.Add(materialData.name);
                                        }
                                    }

                                    foreach (var materialResource in modelInfo.materialsResources)
                                    {
                                        foreach (var resource in materialResource.resources)
                                        {
                                            if (resource.name.EndsWith(".mat"))
                                            {
                                                Debug.Log($"Found material resource in JSON: {resource.name}");
                                                materialNamesFromJson.Add(resource.name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing file {jsonFile}: {ex.Message}");
                    }
                }
            }
            else
            {
                Debug.LogError($"Jsons folder not found at {jsonsFolderPath}");
            }

            // Additional logic to verify materials exist in the folder before finalizing the list
            HashSet<string> validatedMaterialNames = new HashSet<string>();
            Debug.Log("Verifying materials exist in the Materials folder...");
            if (Directory.Exists(materialsFolderPath))
            {
                foreach (string materialName in materialNamesFromJson)
                {
                    string materialPath = Path.Combine(materialsFolderPath, materialName);
                    if (File.Exists(materialPath))
                    {
                        validatedMaterialNames.Add(materialName);
                        Debug.Log($"Validated material exists: {materialName}");
                    }
                    else
                    {
                        Debug.LogWarning($"Material listed in JSON not found in Materials folder: {materialName}");
                    }
                }
            }
            else
            {
                Debug.LogError($"Materials folder not found at {materialsFolderPath}");
            }

            // Convert the HashSet to a list, sort it alphabetically, and wrap it in an object
            List<string> sortedValidatedMaterialNames = validatedMaterialNames.ToList();
            sortedValidatedMaterialNames.Sort(); // This will sort the list alphabetically
            MaterialsIndex materialsIndex = new MaterialsIndex { materials = sortedValidatedMaterialNames };

            // Convert the object to JSON and save it
            string materialsIndexJson = JsonConvert.SerializeObject(materialsIndex, Formatting.Indented);
            if (!Directory.Exists(meshReferencesFolderPath))
            {
                Directory.CreateDirectory(meshReferencesFolderPath);
            }
            File.WriteAllText(materialsIndexPath, materialsIndexJson);
            Debug.Log($"Materials index created successfully at: {materialsIndexPath}. Total materials indexed: {sortedValidatedMaterialNames.Count}");
        }

        private void CheckAllMaterials()
        {
            string jsonsFolderPath = Application.streamingAssetsPath + "/Jsons";
            HashSet<string> materialNamesFromJson = new HashSet<string>(); // Store names from JSON
            Dictionary<string, int> missingMaterials = new Dictionary<string, int>(); // Track missing materials

            if (Directory.Exists(jsonsFolderPath))
            {
                Debug.Log($"Searching JSON files in {jsonsFolderPath}");
                foreach (var jsonFile in Directory.GetFiles(jsonsFolderPath, "*.json", SearchOption.AllDirectories))
                {
                    Debug.Log($"Reading {jsonFile}");
                    string jsonData = File.ReadAllText(jsonFile);
                    try
                    {
                        ModelData modelData = JsonConvert.DeserializeObject<ModelData>(jsonData);
                        if (modelData != null && modelData.slotPairs != null)
                        {
                            foreach (var slotPair in modelData.slotPairs)
                            {
                                foreach (var modelInfo in slotPair.slotData.models)
                                {
                                    foreach (var materialData in modelInfo.materialsData)
                                    {
                                        if (materialData.name.EndsWith(".mat"))
                                        {
                                            //Debug.Log($"Found material in JSON: {materialData.name}");
                                            materialNamesFromJson.Add(materialData.name);
                                        }
                                    }

                                    foreach (var materialResource in modelInfo.materialsResources)
                                    {
                                        foreach (var resource in materialResource.resources)
                                        {
                                            if (resource.name.EndsWith(".mat"))
                                            {
                                                //Debug.Log($"Found material resource in JSON: {resource.name}");
                                                materialNamesFromJson.Add(resource.name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing file {jsonFile}: {ex.Message}");
                    }
                }
            }
            else
            {
                Debug.LogError($"Jsons folder not found at {jsonsFolderPath}");
            }

            Debug.Log("[CheckAllMaterials] Verifying materials exist in the Unity Resources folder...");
            foreach (string materialName in materialNamesFromJson)
            {
                string materialPath = $"Materials/{Path.GetFileNameWithoutExtension(materialName)}";
                Material material = Resources.Load<Material>(materialPath);
                if (material == null)
                {
                    if (!missingMaterials.ContainsKey(materialName))
                    {
                        missingMaterials[materialName] = 1;
                    }
                    else
                    {
                        missingMaterials[materialName]++;
                    }

                    // Create the missing material
                    Debug.LogWarning($"[CheckAllMaterials] Creating missing material: {materialName}");
                    Material newMaterial = new Material(Shader.Find("HDRP/Lit"));
                    if (!Directory.Exists(Path.Combine(Application.dataPath, "Resources/Materials")))
                    {
                        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources/Materials"));
                    }
                    string assetPath = $"Assets/Resources/{materialPath}.mat";
                    UnityEditor.AssetDatabase.CreateAsset(newMaterial, assetPath);
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();
                }
            }

            if (missingMaterials.Count > 0)
            {
                Debug.LogWarning("[CheckAllMaterials] Some materials listed in JSON were missing and have been created:");
                foreach (var missingMaterial in missingMaterials)
                {
                    Debug.LogWarning($"[CheckAllMaterials] Created missing material: {missingMaterial.Key} - Referenced {missingMaterial.Value} times");
                }
            }
            else
            {
                Debug.Log("[CheckAllMaterials] All materials referenced in JSON files are found in Resources folder.");
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

        private void FixTPPFPPMaterials()
        {
            string materialsPath = "Assets/Resources/Materials";
            var existingMaterials = LoadMaterialsFromFolder(materialsPath);

            // List of custom texture slots to check
            string[] textureSlots = new string[] { "_dif", "_ems", "_gra", "_nrm", "_spc", "_rgh", "_msk", "_idx", "_clp", "_ocl" };

            int processedMaterialsCount = 0;

            foreach (var materialEntry in existingMaterials)
            {
                Material material = materialEntry.Value;

                // Initially assume the material is empty
                bool materialIsEmpty = true;
                foreach (string slot in textureSlots)
                {
                    if (material.HasProperty(slot) && material.GetTexture(slot) != null)
                    {
                        materialIsEmpty = false; // Found a texture, mark as not empty
                        break;
                    }
                }

                // Continue to the next material if this one is not empty
                if (!materialIsEmpty) continue;

                // Strip off any suffixes and determine what type of material this is (FPP or TPP)
                string baseName = Path.GetFileNameWithoutExtension(materialEntry.Key);
                string subjectName = ExtractSubjectName(baseName);
                bool isFPP = baseName.EndsWith("_fpp");
                bool isTPP = baseName.EndsWith("_tpp");

                // Skip materials that are neither FPP nor TPP as they have no clear counterpart
                if (!isFPP && !isTPP) continue;

                // Determine the counterpart's name
                string counterpartBaseName = isFPP ? $"{subjectName}_tpp" : $"{subjectName}_fpp";

                // Try to find the counterpart material
                Material counterpartMaterial = existingMaterials.Values.FirstOrDefault(m => Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(m)) == counterpartBaseName);

                if (counterpartMaterial != null)
                {
                    // Check if counterpart material has textures in the relevant slots and assign them to the current material
                    bool texturesAssigned = false;
                    foreach (string slot in textureSlots)
                    {
                        if (material.HasProperty(slot) && counterpartMaterial.HasProperty(slot) && counterpartMaterial.GetTexture(slot) != null)
                        {
                            material.SetTexture(slot, counterpartMaterial.GetTexture(slot));
                            texturesAssigned = true; // Mark that we've made an assignment for logging
                        }
                    }

                    if (texturesAssigned)
                    {
                        EditorUtility.SetDirty(material);
                        processedMaterialsCount++;
                    }
                }
                else
                {
                    Debug.LogWarning($"No matching counterpart found for material: {baseName}. Expected counterpart: {counterpartBaseName}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Processed {processedMaterialsCount} materials with missing textures.");
        }

        private void FixVariantMaterials()
        {
            string materialsPath = "Assets/Resources/Materials";
            var existingMaterials = LoadMaterialsFromFolder(materialsPath);

            string[] textureSlots = new string[] { "_dif", "_ems", "_gra", "_nrm", "_spc", "_rgh", "_msk", "_idx", "_clp", "_ocl" };

            int processedMaterialsCount = 0;
            Dictionary<string, List<Material>> allMaterials = new Dictionary<string, List<Material>>();

            // Group materials by their base names
            foreach (var materialEntry in existingMaterials)
            {
                string materialName = Path.GetFileNameWithoutExtension(materialEntry.Key);
                string baseName = ExtractBaseName(materialName);

                if (!allMaterials.ContainsKey(baseName))
                {
                    allMaterials[baseName] = new List<Material>();
                }
                allMaterials[baseName].Add(materialEntry.Value);
            }

            // Process each group of materials
            foreach (var baseName in allMaterials.Keys)
            {
                var materialsGroup = allMaterials[baseName];

                // Identify the material with the most textures
                Material referenceMaterial = null;
                int maxTexturesCount = 0;
                foreach (var material in materialsGroup)
                {
                    int textureCount = textureSlots.Count(slot => material.HasProperty(slot) && material.GetTexture(slot) != null);
                    if (textureCount > maxTexturesCount)
                    {
                        maxTexturesCount = textureCount;
                        referenceMaterial = material;
                    }
                }

                // Copy missing textures to other materials in the group
                if (referenceMaterial != null)
                {
                    foreach (var material in materialsGroup)
                    {
                        if (material == referenceMaterial) continue; // Skip the reference material itself

                        int texturesAdded = 0;
                        foreach (var slot in textureSlots)
                        {
                            if (!material.HasProperty(slot) || material.GetTexture(slot) != null) continue; // Skip if property does not exist or texture already assigned

                            if (referenceMaterial.HasProperty(slot) && referenceMaterial.GetTexture(slot) != null)
                            {
                                material.SetTexture(slot, referenceMaterial.GetTexture(slot));
                                texturesAdded++;
                                EditorUtility.SetDirty(material);
                            }
                        }

                        if (texturesAdded > 0)
                        {
                            processedMaterialsCount++;
                            Debug.Log($"[FixVariantMaterials] Copied {texturesAdded} textures from '{referenceMaterial.name}' to '{material.name}'");
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FixVariantMaterials] Processed {processedMaterialsCount} materials by adding missing textures.");
        }

        private string ExtractBaseName(string materialName)
        {
            var sortedVariationSuffixes = variationSuffixes.OrderByDescending(suffix => suffix.Length);

            foreach (var suffix in sortedVariationSuffixes)
            {
                if (materialName.EndsWith(suffix))
                {
                    int index = materialName.LastIndexOf(suffix);
                    if (index > 0)
                    {
                        // Remove the suffix to find the base name
                        return materialName.Substring(0, index);
                    }
                }
            }
            // Return the original name if no variation suffix is found
            return materialName;
        }

        private string ExtractSubjectName(string baseName)
        {
            // Remove the known suffixes that indicate perspective or type
            return baseName.Replace("_fpp", "").Replace("_tpp", "");
        }

        private string RemoveSuffix(string name)
        {
            return name.Replace("_tpp", "").Replace("_fpp", "");
        }

        private string SequentiallyShortenName(string name)
        {
            int lastIndex = name.LastIndexOf('_');
            if (lastIndex > -1)
            {
                return name.Substring(0, lastIndex);
            }
            return name; // No more modifications possible
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

        public class TexturesIndex
        {
            public List<string> textures;
        }

        private void CreateTexturesIndex()
        {
            string texturesFolderPath = "Assets/Resources/Textures";
            string meshReferencesFolderPath = "Assets/StreamingAssets/Mesh References";
            string texturesIndexPath = Path.Combine(meshReferencesFolderPath, "textures_index.json");

            if (!Directory.Exists(texturesFolderPath))
            {
                Debug.LogError("Textures folder not found at: " + texturesFolderPath);
                return;
            }

            List<string> textureNames = new List<string>();
            string[] textureFiles = Directory.GetFiles(texturesFolderPath, "*.png", SearchOption.AllDirectories);
            foreach (string textureFile in textureFiles)
            {
                string textureName = Path.GetFileNameWithoutExtension(textureFile);
                textureNames.Add(textureName);
            }

            TexturesIndex texturesIndex = new TexturesIndex { textures = textureNames };
            string texturesIndexJson = JsonUtility.ToJson(texturesIndex, true);

            if (!Directory.Exists(meshReferencesFolderPath))
            {
                Directory.CreateDirectory(meshReferencesFolderPath);
            }

            File.WriteAllText(texturesIndexPath, texturesIndexJson);

            Debug.Log("Textures index created successfully at: " + texturesIndexPath);
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
            //Debug.Log($"FindTexturesForMaterial");
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

        public static void RemoveTexture()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { materialsDirectory });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

                if (material != null && material.HasProperty(MaterialSlot))
                {
                    // Check if there is a texture applied to the slot
                    Texture texture = material.GetTexture(MaterialSlot);

                    if (texture != null)  // This line ensures we only clear slots that have a texture
                    {
                        // Clear the texture from the slot
                        material.SetTexture(MaterialSlot, null);
                        Debug.Log($"Cleared texture from {MaterialSlot} in material {material.name}");

                        // Save changes
                        EditorUtility.SetDirty(material);
                        AssetDatabase.SaveAssets();
                    }
                }
            }

            Debug.Log("Finished searching and clearing textures in specified materials.");
        }
    }
}
#endif