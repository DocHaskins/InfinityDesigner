using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEditor;
using Cinemachine;

public class RuntimeJsonLoader : MonoBehaviour
{
    private string selectedJson;
    private List<string> jsonFiles = new List<string>();
    private List<string> filteredJsonFiles = new List<string>();
    private Vector2 scrollPosition = Vector2.zero;
    private string searchTerm = "";
    private string selectedClass = "All";
    private string selectedSex = "All";
    private string selectedRace = "All";
    private HashSet<string> classes = new HashSet<string> { "All" };
    private HashSet<string> sexes = new HashSet<string> { "All" };
    private HashSet<string> races = new HashSet<string> { "All" };
    private int selectedIndex = -1; // Initial selection index
    private Vector2 scrollPos;
    private bool foldout = false;
    private string jsonFileName;
    private GameObject loadedSkeleton; 
    private List<GameObject> loadedObjects = new List<GameObject>();
    private CinemachineCameraZoomTool cameraTool;

    void Start()
    {
        LoadJsonFiles();
        cameraTool = FindObjectOfType<CinemachineCameraZoomTool>();
    }

    void LoadJsonFiles()
    {
        jsonFiles.Clear();
        string jsonsFolderPath = Path.Combine(Application.streamingAssetsPath, "Jsons");
        if (Directory.Exists(jsonsFolderPath))
        {
            foreach (var file in Directory.GetFiles(jsonsFolderPath, "*.json"))
            {
                string jsonPath = Path.Combine(jsonsFolderPath, file);
                string jsonData = File.ReadAllText(jsonPath);
                ModelData modelData = JsonUtility.FromJson<ModelData>(jsonData);
                jsonFiles.Add(Path.GetFileName(file));

                if (modelData.modelProperties != null)
                {
                    classes.Add(modelData.modelProperties.@class ?? "Unknown");
                    sexes.Add(modelData.modelProperties.sex ?? "Unknown");
                    races.Add(modelData.modelProperties.race ?? "Unknown");
                }
            }
        }
        else
        {
            Debug.LogError("Jsons folder not found: " + jsonsFolderPath);
        }

        UpdateFilteredJsonFiles();
    }

    void UpdateFilteredJsonFiles()
    {
        filteredJsonFiles = jsonFiles
            .Where(f => (selectedClass == "All" || f.Contains(selectedClass)) &&
                        (selectedSex == "All" || f.Contains(selectedSex)) &&
                        (selectedRace == "All" || f.Contains(selectedRace)) &&
                        (string.IsNullOrEmpty(searchTerm) || f.ToLower().Contains(searchTerm.ToLower())))
            .ToList();
    }

    void OnGUI()
    {
        // Filters
        GUILayout.BeginHorizontal();
        selectedClass = DropdownField("Filter by Class", selectedClass, classes);
        selectedSex = DropdownField("Filter by Sex", selectedSex, sexes);
        selectedRace = DropdownField("Filter by Race", selectedRace, races);
        GUILayout.EndHorizontal();

        searchTerm = GUILayout.TextField(searchTerm);
        if (GUILayout.Button("Search"))
        {
            UpdateFilteredJsonFiles();
        }

        // Scrollable selection grid for JSON files
        GUILayout.Label("Select JSON File:");
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(100));
        selectedIndex = GUILayout.SelectionGrid(selectedIndex, filteredJsonFiles.ToArray(), 1);
        GUILayout.EndScrollView();

        if (selectedIndex >= 0 && selectedIndex < filteredJsonFiles.Count)
        {
            selectedJson = filteredJsonFiles[selectedIndex];
        }

        if (GUILayout.Button("Load"))
        {
            LoadModelFromJson();
        }

        if (GUILayout.Button("Unload"))
        {
            UnloadAllObjects();
        }
    }

    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    private string DropdownField(string label, string selectedValue, HashSet<string> options)
    {
        // Initialize foldout state if it doesn't exist
        if (!foldoutStates.ContainsKey(label))
        {
            foldoutStates[label] = false;
        }

        GUILayout.Label(label);
        string[] optionArray = options.ToArray();
        int index = Array.IndexOf(optionArray, selectedValue);

        // Toggle foldout state
        foldoutStates[label] = GUILayout.Toggle(foldoutStates[label], label + (foldoutStates[label] ? " ▼" : " ►"), "button");

        if (foldoutStates[label])
        {
            // Display a selection grid when the foldout is open
            index = GUILayout.SelectionGrid(index, optionArray, 1);
        }

        return index >= 0 ? optionArray[index] : selectedValue;
    }

    void UnloadAllObjects()
    {
        foreach (var obj in loadedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        loadedObjects.Clear();
    }

    public void LoadModelFromJson()
    {
        if (selectedIndex < 0 || selectedIndex >= filteredJsonFiles.Count)
        {
            Debug.LogError("No JSON file selected or index out of range");
            return;
        }

        jsonFileName = filteredJsonFiles[selectedIndex]; // Set the selected JSON file name
        string path = Path.Combine(Application.streamingAssetsPath, "Jsons", jsonFileName);

        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return;
        }

        string jsonData = File.ReadAllText(path);
        ModelData modelData = JsonUtility.FromJson<ModelData>(jsonData);

        if (modelData != null)
        {
            var slots = modelData.GetSlots();
            if (slots != null && slots.Count > 0)
            {
                GameObject loadedModel = LoadSkeleton(modelData.skeletonName);
                LoadModels(slots);
                UpdateCameraTarget(loadedModel.transform);
            }
            else
            {
                Debug.LogError("Slots dictionary is null or empty.");
            }
        }
        else
        {
            Debug.LogError("Failed to deserialize JSON data");
        }
    }

    private void UpdateCameraTarget(Transform loadedModelTransform)
    {
        if (cameraTool != null && loadedModelTransform != null)
        {
            // Clear previous targets
            cameraTool.targets.Clear();

            // Add specific points to the targets list
            string[] pointNames = { "neck", "spine3", "legs", "r_hand", "l_hand", "l_foot", "r_foot" };
            foreach (var pointName in pointNames)
            {
                Transform targetTransform = DeepFind(loadedModelTransform, pointName);
                if (targetTransform != null)
                {
                    cameraTool.targets.Add(targetTransform);
                }
                else
                {
                    Debug.LogWarning($"Target point '{pointName}' not found in loaded model.");
                }
            }

            // Update the current target index and camera target
            if (cameraTool.targets.Count > 0)
            {
                cameraTool.CurrentTargetIndex = 0; // Reset to first target
                cameraTool.UpdateCameraTarget();
            }
        }
    }

    private Transform DeepFind(Transform parent, string name)
    {
        if (parent.name.Equals(name)) return parent;
        foreach (Transform child in parent)
        {
            Transform result = DeepFind(child, name);
            if (result != null) return result;
        }
        return null;
    }


    private GameObject LoadSkeleton(string skeletonName)
    {
        string resourcePath = "Models/" + skeletonName.Replace(".msh", "");
        GameObject skeletonPrefab = Resources.Load<GameObject>(resourcePath);
        GameObject instantiatedSkeleton = null;

        if (skeletonPrefab != null)
        {
            instantiatedSkeleton = Instantiate(skeletonPrefab, Vector3.zero, Quaternion.Euler(-90, 0, 0));
            loadedSkeleton = instantiatedSkeleton; // Store the loaded skeleton

            // Find the 'pelvis' child in the loaded skeleton
            Transform pelvis = instantiatedSkeleton.transform.Find("pelvis");
            if (pelvis != null)
            {
                // Create a new GameObject named 'Legs'
                GameObject legs = new GameObject("legs");

                // Set 'Legs' as a child of 'pelvis'
                legs.transform.SetParent(pelvis);

                // Set the local position of 'Legs' with the specified offset
                legs.transform.localPosition = new Vector3(0, 0, -0.005f);
            }
            else
            {
                Debug.LogError("Pelvis not found in the skeleton prefab: " + skeletonName);
            }
        }
        else
        {
            Debug.LogError("Skeleton prefab not found in Resources: " + resourcePath);
        }

        return instantiatedSkeleton; // Return the loaded and instantiated skeleton
    }

    private void LoadModels(Dictionary<string, ModelData.SlotData> slotDictionary)
    {
        foreach (var slotPair in slotDictionary)
        {
            var slot = slotPair.Value;
            foreach (var modelInfo in slot.models)
            {
                // Correctly formatting the prefab path for Resources.Load
                string prefabPath = modelInfo.name.Replace(".msh", "");
                GameObject modelPrefab = Resources.Load<GameObject>("Prefabs/" + prefabPath);

                //Debug.Log($"Attempting to load prefab from Resources path: Prefabs/{prefabPath}");

                if (modelPrefab != null)
                {
                    GameObject modelInstance = Instantiate(modelPrefab, Vector3.zero, Quaternion.identity);
                    ApplyMaterials(modelInstance, modelInfo);
                    loadedObjects.Add(modelInstance);

                    // Check if the prefab name contains "sh_man_facial_hair_" and adjust Z position
                    if (prefabPath.Contains("sh_man_facial_hair_"))
                    {
                        Vector3 localPosition = modelInstance.transform.localPosition;
                        localPosition.z += 0.01f;
                        modelInstance.transform.localPosition = localPosition;

                        Debug.Log($"Adjusted position for facial hair prefab: {prefabPath}");
                    }

                    if (prefabPath.Contains("sh_man_hair_system_"))
                    {
                        Vector3 localPosition = modelInstance.transform.localPosition;
                        //localPosition.y += 0.009f;
                        localPosition.z += 0.009f;
                        modelInstance.transform.localPosition = localPosition;

                        Debug.Log($"Adjusted position for hair prefab: {prefabPath}");
                    }

                    //Debug.Log($"Prefab loaded and instantiated: {prefabPath}");
                }
                else
                {
                    Debug.LogError($"Model prefab not found in Resources: Prefabs/{prefabPath}");
                }
            }
        }
    }

    private void ApplyMaterials(GameObject modelInstance, ModelData.ModelInfo modelInfo)
    {
        var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var materialResource in modelInfo.materialsResources)
        {
            if (materialResource.number - 1 < skinnedMeshRenderers.Length)
            {
                var renderer = skinnedMeshRenderers[materialResource.number - 1];
                ApplyMaterialToRenderer(renderer, materialResource.resources[0]);
            }
            else
            {
                Debug.LogError($"Renderer index out of bounds for material number {materialResource.number} in model '{modelInfo.name}'");
            }
        }
    }

    private void ApplyMaterialToRenderer(SkinnedMeshRenderer renderer, ModelData.Resource resource)
    {
        if (resource.name.StartsWith("sm_"))
        {
            Debug.Log($"Skipped material '{resource.name}' as it starts with 'sm_'");
            return;
        }

        if (resource.name.Equals("null.mat", StringComparison.OrdinalIgnoreCase))
        {
            renderer.enabled = false;
            Debug.Log($"Disabled SkinnedMeshRenderer on '{renderer.gameObject.name}' due to null.mat");
            return;
        }

        string matPath = "materials/" + Path.GetFileNameWithoutExtension(resource.name);
        Material originalMat = Resources.Load<Material>(matPath);

        if (originalMat != null)
        {
            Material clonedMaterial = new Material(originalMat);
            bool useCustomShader = ShouldUseCustomShader(resource.name);
            bool useHairShader = ShouldUseHairShader(resource.name);
            if (useCustomShader)
            {
                clonedMaterial.shader = Shader.Find("Shader Graphs/Skin");
            }
            else if (useHairShader)
            {
                clonedMaterial.shader = Shader.Find("HDRP/Hair");
            }
            else
            {
                clonedMaterial.shader = Shader.Find("HDRP/Lit");
            }

            foreach (var rttiValue in resource.rttiValues)
            {
                ApplyTextureToMaterial(clonedMaterial, rttiValue.name, rttiValue.val_str, useCustomShader);
            }

            // Check if the renderer should be disabled
            if (ShouldDisableRenderer(renderer.gameObject.name))
            {
                renderer.enabled = false;
            }
            else
            {
                // Apply the cloned material to the renderer
                Material[] rendererMaterials = renderer.sharedMaterials;
                rendererMaterials[0] = clonedMaterial;
                renderer.sharedMaterials = rendererMaterials;
            }
        }
        else
        {
            Debug.LogError($"Material not found: '{matPath}' for renderer '{renderer.gameObject.name}'");
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

    private void ApplyTextureToMaterial(Material material, string rttiValueName, string textureName, bool useCustomShader)
    {
        string texturePath = "textures/" + Path.GetFileNameWithoutExtension(textureName);
        Texture2D texture = Resources.Load<Texture2D>(texturePath);

        if (texture != null)
        {
            if (useCustomShader)
            {
                switch (rttiValueName)
                {
                    case "msk_1_tex":
                        material.SetTexture("_mask", texture);
                        break;
                    case "dif_1_tex":
                    case "dif_0_tex":
                        material.SetTexture("_dif", texture);
                        if (textureName.StartsWith("chr_"))
                        {
                            // Set the _modifier texture and enable the modifier
                            material.SetTexture("_modifier", texture);
                            material.SetFloat("_Modifier", 1);
                        }
                        break;
                    case "nrm_1_tex":
                    case "nrm_0_tex":
                        material.SetTexture("_nrm", texture);
                        break;
                        // Add other cases for custom shader
                }
            }
            else
            {
                switch (rttiValueName)
                {
                    case "msk_1_tex":
                        material.SetTexture("_CoatMaskMap", texture);
                        material.SetFloat("_CoatMask", 1.0f);
                        break;
                    case "dif_1_tex":
                    case "dif_0_tex":
                        material.SetTexture("_BaseColorMap", texture);
                        break;
                    case "nrm_1_tex":
                    case "nrm_0_tex":
                        material.SetTexture("_NormalMap", texture);
                        break;
                    case "ems_0_tex":
                    case "ems_1_tex":
                        material.SetTexture("_EmissiveColorMap", texture);
                        material.SetColor("_EmissiveColor", Color.white * 2);
                        material.EnableKeyword("_EMISSION");
                        break;
                        // Add other cases for HDRP/Lit shader
                }
            }
        }
        else
        {
            Debug.LogError($"Texture '{texturePath}' not found in Resources for material '{rttiValueName}'");
        }
    }

    private bool ShouldDisableRenderer(string gameObjectName)
    {
        return gameObjectName.Contains("sh_eye_shadow") ||
               gameObjectName.Contains("sh_wet_eye") ||
               gameObjectName.Contains("_null");
    }

    public void UnloadModel()
    {
        if (loadedSkeleton != null)
        {
            DestroyObject(loadedSkeleton);
            loadedSkeleton = null;
        }

        foreach (var model in loadedObjects)
        {
            if (model != null)
            {
                DestroyObject(model);
            }
        }
        loadedObjects.Clear();

        DestroyObject(this.gameObject);
    }

    private void DestroyObject(GameObject obj)
    {
        Destroy(obj);
    }
}