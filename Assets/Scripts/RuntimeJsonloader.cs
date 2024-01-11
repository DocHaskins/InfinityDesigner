using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Cinemachine;
using TMPro;
using UnityEngine.UI;

public class RuntimeJsonLoader : MonoBehaviour
{
    public TMP_Text modelName;
    public TMP_Dropdown filterClassDropdown;
    public TMP_Dropdown filterSexDropdown;
    public TMP_Dropdown filterRaceDropdown;
    public TMP_Dropdown modelSelectionDropdown;
    public TMP_InputField searchInputField;
    public Button loadButton;
    public Button unloadButton;

    private List<MinimalModelData> minimalModelInfos = new List<MinimalModelData>();
    private List<string> filteredJsonFiles = new List<string>();
    private string selectedJson;
    private string searchTerm = "";
    private string selectedClass = "All";
    private string selectedSex = "All";
    private string selectedRace = "All";
    private HashSet<string> classes = new HashSet<string> { "All" };
    private HashSet<string> sexes = new HashSet<string> { "All" };
    private HashSet<string> races = new HashSet<string> { "All" };
    private int selectedIndex = -1;
    private Vector2 scrollPos;
    private GameObject loadedSkeleton;
    private List<GameObject> loadedObjects = new List<GameObject>();
    private CinemachineCameraZoomTool cameraTool;

    void Start()
    {
        LoadJsonData();
        PopulateDropdowns();
        AddButtonListeners();
        cameraTool = FindObjectOfType<CinemachineCameraZoomTool>();
    }

    void PopulateDropdowns()
    {
        // Sort and populate the dropdowns, ensuring "ALL" is at the top
        AddSortedOptionsWithAllAtTop(filterClassDropdown, classes);
        AddSortedOptionsWithAllAtTop(filterSexDropdown, sexes);
        AddSortedOptionsWithAllAtTop(filterRaceDropdown, races);

        // Add listeners for dropdown value changes
        filterClassDropdown.onValueChanged.AddListener(delegate { UpdateFilteredJsonFiles(); });
        filterSexDropdown.onValueChanged.AddListener(delegate { UpdateFilteredJsonFiles(); });
        filterRaceDropdown.onValueChanged.AddListener(delegate { UpdateFilteredJsonFiles(); });
        searchInputField.onValueChanged.AddListener(delegate { UpdateFilteredJsonFiles(); });

        // Initial update for model selection dropdown
        UpdateModelSelectionDropdown();
    }

    void AddSortedOptionsWithAllAtTop(TMP_Dropdown dropdown, HashSet<string> optionsSet)
    {
        List<string> options = optionsSet.ToList(); // Convert HashSet to List
        options.Remove("All");      // Remove "ALL" if it exists
        options.Sort();             // Sort the options
        options.Insert(0, "All");   // Add "ALL" back at the top

        ClearAndAddOptions(dropdown, options);
    }
    void ClearAndAddOptions(TMP_Dropdown dropdown, List<string> options)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
    }

    void UpdateModelSelectionDropdown()
    {
        modelSelectionDropdown.ClearOptions();

        // Create a new list to store filenames without the .json extension and not containing "_fpp"
        List<string> dropdownOptions = new List<string>();

        foreach (var filename in filteredJsonFiles)
        {
            // Check if filename contains "_fpp"
            if (!filename.Contains("_fpp"))
            {
                // Remove the .json extension from each filename
                string filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
                dropdownOptions.Add(filenameWithoutExtension);
            }
        }

        // Add the processed filenames to the dropdown
        modelSelectionDropdown.AddOptions(dropdownOptions);
    }

    public void LoadSelectedModel()
    {
        if (modelSelectionDropdown.value < 0 || modelSelectionDropdown.value >= filteredJsonFiles.Count)
        {
            Debug.LogError("No model selected or index out of range");
            return;
        }

        selectedJson = filteredJsonFiles[modelSelectionDropdown.value];
        // Update the modelName text, replacing underscores with spaces
        if (modelName != null)
        {
            string displayName = Path.GetFileNameWithoutExtension(selectedJson).Replace("_", " ");
            modelName.text = displayName;
        }
        LoadModelFromJson(selectedJson); // Pass the selected JSON file name
    }

    void AddButtonListeners()
    {
        loadButton.onClick.AddListener(LoadSelectedModel);
        unloadButton.onClick.AddListener(UnloadAllObjects);
    }


    void LoadJsonData()
    {
        string jsonsFolderPath = Path.Combine(Application.streamingAssetsPath, "Jsons");
        if (!Directory.Exists(jsonsFolderPath))
        {
            Debug.LogError("Jsons folder not found: " + jsonsFolderPath);
            return;
        }

        foreach (var file in Directory.GetFiles(jsonsFolderPath, "*.json"))
        {
            string jsonPath = Path.Combine(jsonsFolderPath, file);
            string jsonData = File.ReadAllText(jsonPath);
            ModelData modelData = JsonUtility.FromJson<ModelData>(jsonData);

            if (modelData.modelProperties != null)
            {
                minimalModelInfos.Add(new MinimalModelData
                {
                    FileName = Path.GetFileName(file),
                    Properties = modelData.modelProperties
                });

                classes.Add(modelData.modelProperties.@class ?? "Unknown");
                sexes.Add(modelData.modelProperties.sex ?? "Unknown");
                races.Add(modelData.modelProperties.race ?? "Unknown");
            }
        }
        
        // Add 'All' option to enable filtering for all categories
        classes.Add("All");
        sexes.Add("All");
        races.Add("All");

        // Initial update for filtered JSON files
        UpdateFilteredJsonFiles();
    }

    void UpdateFilteredJsonFiles()
    {
        // Update selectedClass, selectedSex, selectedRace based on dropdown selections
        selectedClass = GetDropdownSelectedValue(filterClassDropdown);
        selectedSex = GetDropdownSelectedValue(filterSexDropdown);
        selectedRace = GetDropdownSelectedValue(filterRaceDropdown);
        searchTerm = searchInputField.text;

        filteredJsonFiles = minimalModelInfos.Where(info =>
        {
            bool classMatch = selectedClass == "All" || info.Properties.@class == selectedClass;
            bool sexMatch = selectedSex == "All" || info.Properties.sex == selectedSex;
            bool raceMatch = selectedRace == "All" || info.Properties.race == selectedRace;
            bool searchMatch = string.IsNullOrEmpty(searchTerm) || info.FileName.ToLower().Contains(searchTerm.ToLower());

            return classMatch && sexMatch && raceMatch && searchMatch;
        })
        .Select(info => info.FileName)
        .ToList();
        UpdateModelSelectionDropdown();
    }

    string GetDropdownSelectedValue(TMP_Dropdown dropdown)
    {
        if (dropdown.options.Count > dropdown.value)
        {
            return dropdown.options[dropdown.value].text;
        }
        return "";
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
        modelName.text = "";
    }

    public void LoadModelFromJson(string jsonFileName)
    {
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
            string[] pointNames = {  "spine3", "neck", "legs", "r_hand", "l_hand", "l_foot", "r_foot" };
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