using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using Cinemachine;
using TMPro;
using UnityEngine.UI;
using static ModelData;
using System.Linq;
using System.Collections;

[Serializable]
public class CharacterConfig
{
    public string gender_property;
    public List<ModelConfiguration> models;
}

[Serializable]
public class SlotModelData
{
    public List<string> meshes;
}

[Serializable]
public class ModelConfiguration
{
    public string modelName;
    public List<string> materialsResources;
}

namespace doppelganger
{
    public class CharacterBuilder : MonoBehaviour
    {
        public CinemachineCameraZoomTool cameraTool;
        public Button TypeManButton, TypeWmnButton, TypePlayerButton, TypeInfectedButton, TypeChildButton;
        private string currentType;
        private string currentPath;
        public TMP_Dropdown typeDropdown;
        public TMP_Dropdown categoryDropdown;
        public TMP_Dropdown classDropdown;
        public TMP_Dropdown presetDropdown;
        public GameObject slidersPanel;
        public GameObject sliderPrefab;
        public GameObject variationSliderPrefab;
        public GameObject loadedSkeleton;
        public GameObject buttonPrefab;
        public Transform subButtonsPanel;
        public Button bodyButton;
        public Button armorButton;
        public Button clothesButton;
        public Button presetLoadButton;

        private string lastFilterCategoryKey = "";
        private Dictionary<string, float> slotWeights;
        public Dictionary<string, float> sliderValues = new Dictionary<string, float>();
        private Dictionary<string, bool> sliderSetStatus = new Dictionary<string, bool>();
        private Dictionary<GameObject, List<int>> disabledRenderers = new Dictionary<GameObject, List<int>>();
        private Dictionary<GameObject, bool[]> initialRendererStates = new Dictionary<GameObject, bool[]>();
        private Dictionary<string, List<Material>> originalMaterials = new Dictionary<string, List<Material>>();
        private Dictionary<string, List<string>> slotData = new Dictionary<string, List<string>>();
        public Dictionary<string, GameObject> currentlyLoadedModels = new Dictionary<string, GameObject>();
        private Dictionary<string, List<string>> filterSets = new Dictionary<string, List<string>>
{
    { "BodyButton", new List<string> { "ALL_head", "ALL_facial_hair", "ALL_hair", "ALL_hair_base", "ALL_hair_2", "ALL_hair_3", "ALL_hands", "ALL_tattoo" } },
    { "ClothesButton", new List<string> { "ALL_backpack", "ALL_cape", "ALL_decals", "ALL_earrings", "ALL_glasses", "ALL_gloves", "ALL_hat", "ALL_leg_access", "ALL_legs", "ALL_mask", "ALL_necklace", "ALL_rings", "ALL_shoes", "ALL_sleeve", "ALL_torso", "ALL_torso_extra", "ALL_torso_access" } },
    { "ArmorButton", new List<string> { "ALL_armor_helmet", "ALL_armor_torso", "ALL_armor_torso_lowerleft", "ALL_armor_torso_lowerright", "ALL_armor_torso_upperleft", "ALL_armor_torso_upperright", "armor_legs", "armor_legs_upperright", "armor_legs_upperleft", "armor_legs_lowerright", "armor_legs_lowerleft" } },
    { "HeadButton", new List<string> { "ALL_head"} },
    { "HairButton", new List<string> { "ALL_hair", "ALL_hair_base", "ALL_hair_2", "ALL_hair_3" } },
    { "HairBaseButton", new List<string> { "ALL_hair_base"} },
    { "Hair2Button", new List<string> { "ALL_hair_2" } },
    { "Hair3Button", new List<string> { "ALL_hair_3" } },
    { "HatButton", new List<string> { "ALL_hat" } },
    { "MaskButton", new List<string> { "ALL_mask" } },
    { "GlassesButton", new List<string> { "ALL_glasses" } },
    { "NecklaceButton", new List<string> { "ALL_necklace" } },
    { "EarringsButton", new List<string> { "ALL_earrings" } },
    { "RingsButton", new List<string> { "ALL_rings" } },
    { "FacialHairButton", new List<string> { "ALL_facial_hair" } },
    { "CapeButton", new List<string> { "ALL_cape" } },
    { "TorsoButton", new List<string> { "ALL_torso", "ALL_torso_extra", "ALL_torso_access" } },
    { "TorsoExtraButton", new List<string> { "ALL_torso_extra" } },
    { "TorsoAccessButton", new List<string> { "ALL_torso_access" } },
    { "TattooButton", new List<string> { "ALL_tattoo" } },
    { "HandsButton", new List<string> { "ALL_hands" } },
    { "LhandButton", new List<string> { "ALL_lhand" } },
    { "RhandButton", new List<string> { "ALL_rhand" } },
    { "GlovesButton", new List<string> { "ALL_gloves" } },
    { "SleeveButton", new List<string> { "ALL_sleeve" } },
    { "BackpackButton", new List<string> { "ALL_backpack" } },
    { "DecalsButton", new List<string> { "ALL_decals" } },
    { "LegsButton", new List<string> { "ALL_legs" } },
    { "LegAccessButton", new List<string> { "ALL_leg_access" } },
    { "ShoesButton", new List<string> { "ALL_shoes" } },
    { "ArmorHelmetButton", new List<string> { "ALL_armor_helmet" } },
    { "ArmorTorsoButton", new List<string> { "ALL_armor_torso" } },
    { "ArmorTorsoUpperRightButton", new List<string> { "ALL_armor_torso_upperright" } },
    { "ArmorTorsoUpperLeftButton", new List<string> { "ALL_armor_torso_upperleft" } },
    { "ArmorTorsoLowerRightButton", new List<string> { "ALL_armor_torso_lowerright" } },
    { "ArmorTorsoLowerLeftButton", new List<string> { "ALL_armor_torso_lowerleft" } },
    { "ArmorLegsButton", new List<string> { "ALL_armor_legs" } },
    { "ArmorLegsUpperRightButton", new List<string> { "ALL_armor_legs_upperright" } },
    { "ArmorLegsUpperLeftButton", new List<string> { "ALL_armor_legs_upperleft" } },
    { "ArmorLegsLowerRightButton", new List<string> { "ALL_armor_legs_lowerright" } },
    { "ArmorLegsLowerLeftButton", new List<string> { "ALL_armor_legs_lowerleft" } }
};

        void Start()
        {
            currentType = "Human";

            PopulateDropdown(typeDropdown, Application.streamingAssetsPath + "/SlotData", "Human");
            PopulateDropdown(categoryDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", "Human"), "ALL", true);

            StartCoroutine(SetInitialDropdownValues());

            // Set up button listeners
            if (bodyButton != null) bodyButton.onClick.AddListener(() => FilterCategory("BodyButton"));
            if (armorButton != null) armorButton.onClick.AddListener(() => FilterCategory("ArmorButton"));
            if (clothesButton != null) clothesButton.onClick.AddListener(() => FilterCategory("ClothesButton"));
            if (presetLoadButton != null)
            {
                presetLoadButton.onClick.AddListener(OnPresetLoadButtonPressed);
            }

            foreach (Transform child in slidersPanel.transform)
            {
                string sliderName = child.name.Replace("Slider", "");
                sliderSetStatus[sliderName] = false;
            }

            int playerTypeIndex = categoryDropdown.options.FindIndex(option => option.text == "Player");
            if (playerTypeIndex != -1)
            {
                categoryDropdown.value = playerTypeIndex;
            }
            else
            {
                Debug.LogError("Player type not found in dropdown options.");
            }

            // Update dropdowns based on the selected 'Player' type
            OnTypeChanged(typeDropdown.value);

            // Load default JSON file and set sliders
            string defaultJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Jsons", "Human", "Player", "player_tpp_skeleton.json");
            LoadJsonAndSetSliders(defaultJsonFilePath);
            slotWeights = LoadSlotWeights();

            // Manually trigger the interface update as if the dropdown values were changed
            UpdateInterfaceBasedOnDropdownSelection();

            // Manually update preset dropdown based on initial dropdown selections
            UpdatePresetDropdown();
        }

        IEnumerator SetInitialDropdownValues()
        {
            // Wait for the end of the frame to ensure dropdowns are populated
            yield return new WaitForEndOfFrame();

            // Set initial values for Type, Category, and Class dropdowns
            typeDropdown.value = typeDropdown.options.FindIndex(option => option.text == "Human");
            categoryDropdown.value = categoryDropdown.options.FindIndex(option => option.text == "Player");
            classDropdown.value = classDropdown.options.FindIndex(option => option.text == "ALL");

            // Trigger updates
            OnTypeChanged(typeDropdown.value);
            OnCategoryChanged(categoryDropdown.value);

            // Add listeners to dropdowns after setting the initial values
            typeDropdown.onValueChanged.AddListener(OnTypeChanged);
            categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
            classDropdown.onValueChanged.AddListener(OnClassChanged);
        }

        public void Reroll()
        {
            // Iterate through each child of slidersPanel which is expected to be a slider container
            foreach (Transform sliderContainer in slidersPanel.transform)
            {
                // Find the child object named "LockToggle" in the sliderContainer
                Transform lockToggleTransform = sliderContainer.Find("LockToggle");
                Toggle lockToggle = lockToggleTransform ? lockToggleTransform.GetComponent<Toggle>() : null;

                // Skip this slider if the toggle is set to true
                if (lockToggle != null && lockToggle.isOn)
                {
                    Debug.Log("Skipping locked slider: " + sliderContainer.name);
                    continue;
                }

                // Find the child object named "primarySlider" in the sliderContainer
                Transform primarySliderTransform = sliderContainer.Find("primarySlider");
                if (primarySliderTransform != null)
                {
                    Slider slider = primarySliderTransform.GetComponent<Slider>();
                    if (slider != null)
                    {
                        float randomValue = UnityEngine.Random.Range(slider.minValue, slider.maxValue + 1);
                        Debug.Log("Random value for " + sliderContainer.name + ": " + randomValue);

                        slider.value = randomValue;

                        string slotName = sliderContainer.name.Replace("Slider", "");
                        OnSliderValueChanged(slotName, randomValue, true);
                    }
                    else
                    {
                        Debug.LogWarning("No slider component found in primarySlider of: " + sliderContainer.name);
                    }
                }
            }
        }

        private float SelectAndWeighValue(float minRange, float maxRange, float weight)
        {
            Debug.Log($"Value: {minRange}, {maxRange}, {weight}");
            float randomValue = UnityEngine.Random.Range(0f, 1f);

            if (randomValue <= weight)
            {
                // Generate a random value within the range and ensure it does not exceed maxRange
                float selectedValue = UnityEngine.Random.Range(minRange, maxRange);
                return Mathf.Clamp(selectedValue, minRange, maxRange);
            }
            else
            {
                // Return a value outside the range as a marker
                return maxRange + 1f;
            }
        }

        private Dictionary<string, float> LoadSlotWeights()
        {
            string iniPath = Path.Combine(Application.streamingAssetsPath, "config.ini");
            Dictionary<string, float> slotWeights = new Dictionary<string, float>();

            if (File.Exists(iniPath))
            {
                Debug.Log("Reading ini file: " + iniPath);
                string[] lines = File.ReadAllLines(iniPath);
                bool readingWeightsSection = false;
                foreach (string line in lines)
                {
                    if (line.StartsWith(";") || string.IsNullOrWhiteSpace(line))
                        continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        if (line.Equals("[Weights]", StringComparison.OrdinalIgnoreCase))
                        {
                            readingWeightsSection = true;
                        }
                        else
                        {
                            readingWeightsSection = false;
                        }
                        continue;
                    }

                    if (readingWeightsSection)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string slotName = parts[0].Trim();
                            if (float.TryParse(parts[1], out float weight))
                            {
                                slotWeights[slotName] = weight;
                                Debug.Log($"Loaded weight for {slotName}: {weight}");
                            }
                            else
                            {
                                Debug.LogWarning($"Could not parse weight for slot '{slotName}' in ini file.");
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Slot weights ini file not found: " + iniPath);
            }

            return slotWeights;
        }

        public void Reset()
        {
            foreach (Transform sliderContainer in slidersPanel.transform)
            {
                Debug.Log("Checking child: " + sliderContainer.name);

                // Find the child object named "primarySlider" in the sliderContainer
                Transform primarySliderTransform = sliderContainer.Find("primarySlider");
                if (primarySliderTransform != null)
                {
                    Slider slider = primarySliderTransform.GetComponent<Slider>();
                    if (slider != null)
                    {
                        slider.value = 0;
                        string slotName = sliderContainer.name.Replace("Slider", "");
                        OnSliderValueChanged(slotName, 0, true);
                    }
                    else
                    {
                        Debug.LogWarning("No slider component found in primarySlider of: " + sliderContainer.name);
                    }
                }
                else
                {
                    //Debug.LogWarning("primarySlider not found in: " + sliderContainer.name);
                }
            }
        }

        void OnPresetLoadButtonPressed()
        {
            string selectedPreset = presetDropdown.options[presetDropdown.value].text;
            string jsonPath = GetJsonFilePath(selectedPreset);
            Debug.Log("Loading JSON from path: " + jsonPath);
            LoadJsonAndSetSliders(jsonPath);
        }

        string GetJsonFilePath(string presetName)
        {
            string type = typeDropdown.options[typeDropdown.value].text;
            string category = categoryDropdown.options[categoryDropdown.value].text;
            string classSelection = classDropdown.options[classDropdown.value].text;
            string jsonsBasePath = Path.Combine(Application.streamingAssetsPath, "Jsons");

            string path = jsonsBasePath;
            if (type != "ALL") path = Path.Combine(path, type);
            if (category != "ALL") path = Path.Combine(path, category);
            if (classSelection != "ALL") path = Path.Combine(path, classSelection);

            string jsonFilePath = Path.Combine(path, presetName + ".json");
            return jsonFilePath;
        }

        void LoadJsonAndSetSliders(string jsonPath)
        {
            if (File.Exists(jsonPath))
            {
                Debug.Log("JSON file found: " + jsonPath);
                string jsonData = File.ReadAllText(jsonPath);
                ModelData modelData = JsonUtility.FromJson<ModelData>(jsonData);

                if (modelData != null)
                {
                    string skeletonName = modelData.skeletonName;
                    if (!string.IsNullOrEmpty(skeletonName))
                    {
                        // Find and destroy the currently loaded skeleton
                        GameObject currentSkeleton = GameObject.FindGameObjectWithTag("Skeleton");
                        if (currentSkeleton != null)
                        {
                            Destroy(currentSkeleton);
                        }

                        LoadSkeleton(skeletonName);
                    }
                    else
                    {
                        Debug.LogWarning("Skeleton name not found in JSON.");
                    }

                    var slots = modelData.GetSlots();
                    if (slots != null && slots.Count > 0)
                    {
                        foreach (var slotPair in slots)
                        {
                            SlotData slot = slotPair.Value;
                            foreach (var modelInfo in slot.models)
                            {
                                // Remove the '.msh' extension from the model name
                                string modelNameWithClone = Path.GetFileNameWithoutExtension(modelInfo.name) + "(Clone)";

                                string slotName = FindSlotForModel(modelInfo.name);
                                if (!string.IsNullOrEmpty(slotName))
                                {
                                    int modelIndex = GetModelIndex(slotName, modelInfo.name);
                                    if (modelIndex != -1)
                                    {
                                        SetSliderValue(slotName, modelIndex);
                                        if (currentlyLoadedModels.TryGetValue(modelNameWithClone, out GameObject modelInstance))
                                        {
                                            ApplyMaterials(modelInstance, modelInfo);
                                        }
                                        else
                                        {
                                            Debug.LogWarning($"Model instance not found for {modelNameWithClone}");
                                            PrintCurrentlyLoadedModels();
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"Model index not found for {modelInfo.name} in slot {slotName}");
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning($"Slot not found for model {modelInfo.name}");
                                }
                                
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No slots found in the JSON file.");
                    }
                }
                else
                {
                    Debug.LogError("Failed to deserialize JSON data");
                }
            }
            else
            {
                Debug.LogError("Preset JSON file not found: " + jsonPath);
            }
        }

        void PrintCurrentlyLoadedModels()
        {
            Debug.Log("Currently Loaded Models:");
            foreach (var pair in currentlyLoadedModels)
            {
                Debug.Log($"Key: {pair.Key}, GameObject Name: {pair.Value.name}");
            }
        }

        private void LoadSkeleton(string skeletonName)
        {
            string resourcePath = "Prefabs/" + skeletonName.Replace(".msh", "");
            GameObject skeletonPrefab = Resources.Load<GameObject>(resourcePath);
            if (skeletonPrefab != null)
            {
                GameObject loadedSkeleton = Instantiate(skeletonPrefab, Vector3.zero, Quaternion.Euler(-90, 0, 0));
                loadedSkeleton.tag = "Skeleton";

                // Find the 'pelvis' child in the loaded skeleton
                Transform pelvis = loadedSkeleton.transform.Find("pelvis");
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
                UpdateCameraTarget(loadedSkeleton.transform);
            }
            else
            {
                Debug.LogError("Skeleton prefab not found in Resources: " + resourcePath);
            }
        }

        public string FindSlotForModel(string modelName)
        {
            //Debug.Log($"FindSlotForModel for {modelName}");
            string type = typeDropdown.options[typeDropdown.value].text;
            string category = categoryDropdown.options[categoryDropdown.value].text;
            string classSelection = classDropdown.options[classDropdown.value].text;

            string slotDataPath = Path.Combine(Application.streamingAssetsPath, "SlotData", type);
            if (category != "ALL")
            {
                slotDataPath = Path.Combine(slotDataPath, category);
            }
            if (classSelection != "ALL")
            {
                slotDataPath = Path.Combine(slotDataPath, classSelection);
            }

            if (!Directory.Exists(slotDataPath))
            {
                Debug.LogError($"Slot data folder not found: {slotDataPath}");
                return null;
            }

            foreach (var slotFile in Directory.GetFiles(slotDataPath, "*.json"))
            {
                string slotJsonData = File.ReadAllText(slotFile);
                try
                {
                    SlotModelData slotModelData = JsonUtility.FromJson<SlotModelData>(slotJsonData);
                    foreach (string meshName in slotModelData.meshes)
                    {
                        if (string.Equals(meshName.Trim(), modelName, StringComparison.OrdinalIgnoreCase))
                        {
                            string slotName = Path.GetFileNameWithoutExtension(slotFile);
                            Debug.Log($"Found slot {slotName} for model {modelName}");
                            return slotName;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing JSON file: {slotFile}. Error: {e.Message}");
                }
            }

            Debug.Log($"Model {modelName} not found in any slot");
            return null;
        }

        public int GetModelIndex(string slotName, string modelName)
        {
            //Debug.Log($"GetModelIndex for slot {slotName} and model {modelName}");
            string type = typeDropdown.options[typeDropdown.value].text;
            string category = categoryDropdown.options[categoryDropdown.value].text;
            string classSelection = classDropdown.options[classDropdown.value].text;

            string slotJsonFilePath = Path.Combine(Application.streamingAssetsPath, "SlotData", type);
            if (category != "ALL")
            {
                slotJsonFilePath = Path.Combine(slotJsonFilePath, category);
            }
            if (classSelection != "ALL")
            {
                slotJsonFilePath = Path.Combine(slotJsonFilePath, classSelection);
            }
            slotJsonFilePath = Path.Combine(slotJsonFilePath, slotName + ".json");

            if (File.Exists(slotJsonFilePath))
            {
                string slotJsonData = File.ReadAllText(slotJsonFilePath);
                SlotModelData slotModelData = JsonUtility.FromJson<SlotModelData>(slotJsonData);

                for (int i = 0; i < slotModelData.meshes.Count; i++)
                {
                    if (string.Equals(slotModelData.meshes[i].Trim(), modelName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Increment index by 1 so 0 can represent "off"
                        int adjustedIndex = i + 1;
                        Debug.Log($"Model {modelName} found at index {i}, adjusted index: {adjustedIndex}, in slot {slotName}");
                        return adjustedIndex;
                    }
                }
                Debug.Log($"Model {modelName} not found in slot {slotName}");
            }
            else
            {
                Debug.Log($"Slot JSON file not found for {slotName}");
            }
            return -1;
        }


        public void SetSliderValue(string slotName, int modelIndex)
        {
            string sliderName = slotName + "Slider";
            //Debug.Log($"SetSliderValue for {sliderName} with model index {modelIndex}");
            int sliderIndex = FindSliderIndex(sliderName);
            if (sliderIndex != -1)
            {
                Transform sliderTransform = slidersPanel.transform.GetChild(sliderIndex);
                Slider slider = sliderTransform.GetComponentInChildren<Slider>();
                if (slider != null)
                {
                    slider.value = modelIndex;
                    //Debug.Log($"Set slider value for {sliderName} to {modelIndex}");
                }
                else
                {
                    Debug.Log($"Slider component not found for {sliderName}");
                }
            }
            else
            {
                Debug.Log($"Slider index not found for {sliderName}");
            }
        }

        private void UpdateCameraTarget(Transform loadedModelTransform)
        {
            if (cameraTool != null && loadedModelTransform != null)
            {
                cameraTool.targets.Clear();
                string[] pointNames = { "spine3", "neck", "legs", "r_hand", "l_hand", "l_foot", "r_foot" };
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

        void PopulateDropdown(TMPro.TMP_Dropdown dropdown, string path, string defaultValue, bool includeAllOption = false)
        {
            var options = GetSubFolders(path).Select(option => new TMPro.TMP_Dropdown.OptionData(option)).ToList();

            if (includeAllOption)
            {
                options.Insert(0, new TMPro.TMP_Dropdown.OptionData("ALL"));
            }

            if (defaultValue != null && !options.Any(o => o.text == defaultValue))
            {
                options.Insert(0, new TMPro.TMP_Dropdown.OptionData(defaultValue));
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.value = options.FindIndex(option => option.text == defaultValue);
            dropdown.RefreshShownValue();
        }


        List<string> GetSubFolders(string path)
        {
            if (Directory.Exists(path))
            {
                return Directory.GetDirectories(path).Select(Path.GetFileName).ToList();
            }
            return new List<string>();
        }

        void OnTypeChanged(int index)
        {
            string selectedType = typeDropdown.options[index].text;
            PopulateDropdown(categoryDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType), "ALL", true);
            UpdateInterfaceBasedOnDropdownSelection();
        }

        void OnCategoryChanged(int index)
        {
            string selectedCategory = categoryDropdown.options[index].text;
            PopulateDropdown(classDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", typeDropdown.options[typeDropdown.value].text, selectedCategory), "ALL");
            UpdateInterfaceBasedOnDropdownSelection();
        }

        void OnClassChanged(int index)
        {
            UpdateInterfaceBasedOnDropdownSelection();
        }

        void UpdatePresetDropdown()
        {
            string type = typeDropdown.options[typeDropdown.value].text;
            string category = categoryDropdown.options[categoryDropdown.value].text;
            string classSelection = classDropdown.options[classDropdown.value].text;

            string presetsPath = Path.Combine(Application.streamingAssetsPath, "Jsons");

            // Generate the path based on dropdown selections
            string searchPath = presetsPath;
            if (type != "ALL")
            {
                searchPath = Path.Combine(searchPath, type);
                if (category != "ALL")
                {
                    searchPath = Path.Combine(searchPath, category);
                    if (classSelection != "ALL")
                    {
                        searchPath = Path.Combine(searchPath, classSelection);
                    }
                }
            }

            PopulatePresetDropdown(presetDropdown, searchPath);
        }

        void PopulatePresetDropdown(TMPro.TMP_Dropdown dropdown, string path)
        {
            var jsonFiles = GetJsonFiles(path);
            var filteredFiles = jsonFiles.Where(file => !file.StartsWith("db_")).ToList();
            var options = filteredFiles.Select(fileName => new TMPro.TMP_Dropdown.OptionData(fileName)).ToList();

            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.RefreshShownValue();
        }

        List<string> GetJsonFiles(string path)
        {
            if (Directory.Exists(path))
            {
                return Directory.GetFiles(path, "*.json", SearchOption.AllDirectories)
                                .Select(file => Path.GetFileNameWithoutExtension(file))
                                .ToList();
            }
            return new List<string>();
        }

        void UpdateInterfaceBasedOnDropdownSelection()
        {
            UpdateSlidersBasedOnSelection();

            string type = typeDropdown.options[typeDropdown.value].text;
            string category = categoryDropdown.options[categoryDropdown.value].text;
            string classSelection = classDropdown.options[classDropdown.value].text;

            // Create a list to store filters based on dropdown selections
            List<string> filters = new List<string>();

            // Populate filters based on dropdown selections
            if (category != "ALL")
            {
                filters.Add(category);
                if (classSelection != "ALL")
                {
                    filters.Add(classSelection);
                }
            }
            else
            {
                // If category is "ALL", add all filters
                filters.AddRange(filterSets.SelectMany(pair => pair.Value).Distinct());
            }

            CreateDynamicButtons(filters);
            UpdateInterfaceBasedOnType();
            UpdateSlidersBasedOnSelection();
            UpdatePresetDropdown();
        }


        void UpdateSlidersBasedOnSelection()
        {
            string type = typeDropdown.options[typeDropdown.value].text;
            string category = categoryDropdown.options[categoryDropdown.value].text;
            string classSelection = classDropdown.options[classDropdown.value].text;

            currentPath = Path.Combine(Application.streamingAssetsPath, "SlotData", type);

            if (category != "ALL")
            {
                currentPath = Path.Combine(currentPath, category);
                if (classSelection != "ALL")
                {
                    currentPath = Path.Combine(currentPath, classSelection);
                }
            }

            PopulateSliders(currentPath);
        }

        void FilterCategory(string categoryKey)
        {
            lastFilterCategoryKey = categoryKey;

            if (filterSets.TryGetValue(categoryKey, out List<string> filters))
            {
                PopulateSlidersWithFilters(currentPath, filters);
                //CreateDynamicButtons(filters);
            }
            else
            {
                Debug.LogError("Filter set not found for category: " + categoryKey);
            }
        }

        void PopulateSlidersWithFilters(string basePath, List<string> filters)
        {
            ClearExistingSliders();

            foreach (var filter in filters)
            {
                string fullPath = Path.Combine(basePath, filter + ".json");
                if (File.Exists(fullPath))
                {
                    CreateSliderForSlot(filter, basePath);
                }
                else
                {
                    Debug.LogWarning("JSON file not found for filter: " + fullPath);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(slidersPanel.GetComponent<RectTransform>());
            CreateDynamicButtons(filters);
        }

        public void SetCurrentType(string type)
        {
            if (slotData.ContainsKey(type))
            {
                currentType = type;
                lastFilterCategoryKey = string.Empty;
                UpdateInterfaceBasedOnType();
            }
            else
            {
                Debug.LogError($"Invalid type specified: {type}. This type does not exist in slotData.");
            }
        }

        public void UpdateInterfaceBasedOnType()
        {
            string currentType = GetCurrentType();
            if (string.IsNullOrEmpty(currentType) || !slotData.ContainsKey(currentType))
            {
                Debug.LogError($"Invalid or missing type: {currentType}");
                return;
            }

            string path = Path.Combine(Application.streamingAssetsPath, "SlotData", currentType);
            PopulateSliders(path);

            // Create a list to store all filters
            List<string> allFilters = filterSets.SelectMany(pair => pair.Value).Distinct().ToList();
            CreateDynamicButtons(allFilters);
        }

        public string GetCurrentType()
        {
            return currentType;
        }

        void FilterSlidersForSlot(string slotName)
        {
            ClearExistingSliders();

            string type = typeDropdown.options[typeDropdown.value].text;
            string category = categoryDropdown.options[categoryDropdown.value].text;
            string classSelection = classDropdown.options[classDropdown.value].text;

            string path = Path.Combine(Application.streamingAssetsPath, "SlotData", type);

            if (category != "ALL")
            {
                path = Path.Combine(path, category);

                if (classSelection != "ALL")
                {
                    path = Path.Combine(path, classSelection);
                }
            }

            string slotPath = Path.Combine(path, slotName + ".json");
            if (File.Exists(slotPath))
            {
                CreateSliderForSlot(slotName, path);
            }
            else
            {
                Debug.LogWarning($"FilterSlidersForSlot: Slot file '{slotPath}' not found");
            }
        }

        void CreateDynamicButtons(List<string> filters)
        {
            if (subButtonsPanel == null || buttonPrefab == null)
            {
                Debug.LogError("CreateDynamicButtons: subButtonsPanel or buttonPrefab is null");
                return; // Early exit if essential components are missing
            }

            // Clear existing buttons in the panel
            foreach (Transform child in subButtonsPanel)
            {
                Destroy(child.gameObject);
            }

            string type = typeDropdown.options[typeDropdown.value].text;
            string category = categoryDropdown.options[categoryDropdown.value].text;
            string classSelection = classDropdown.options[classDropdown.value].text;

            // Construct the path based on the dropdown selections
            string path = Path.Combine(Application.streamingAssetsPath, "SlotData", type);

            if (category != "ALL")
            {
                path = Path.Combine(path, category);
                if (classSelection != "ALL")
                {
                    path = Path.Combine(path, classSelection);
                }
            }

            // Ensure the path exists before trying to access it
            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path, "*.json");
                foreach (var file in files)
                {
                    string slotName = Path.GetFileNameWithoutExtension(file);
                    GameObject newButton = Instantiate(buttonPrefab, subButtonsPanel);

                    string imageName = "Character_Builder_" + slotName.Replace("ALL_", "");
                    Sprite buttonImage = Resources.Load<Sprite>("UI/" + imageName);

                    if (buttonImage != null)
                    {
                        Image buttonImageComponent = newButton.GetComponent<Image>();
                        buttonImageComponent.sprite = buttonImage;
                    }
                    else
                    {
                        Debug.LogWarning($"CreateDynamicButtons: Image not found for '{imageName}'");
                    }

                    Button buttonComponent = newButton.GetComponent<Button>();
                    buttonComponent.onClick.AddListener(() => FilterSlidersForSlot(slotName));
                }
            }
            else
            {
                Debug.LogWarning("Path not found for creating buttons: " + path);
            }
        }

        void PopulateSliders(string path)
        {
            // Clear existing sliders first
            ClearExistingSliders();

            // Your logic to load sliders from the specified path
            string[] files = Directory.GetFiles(path, "*.json");
            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                CreateSliderForSlot(fileName, path); // Pass the path here
            }

            // Rebuild layout if necessary
            LayoutRebuilder.ForceRebuildLayoutImmediate(slidersPanel.GetComponent<RectTransform>());
        }

        void ClearExistingSliders()
        {
            foreach (Transform child in slidersPanel.transform)
            {
                Destroy(child.gameObject);
            }
        }

        void CreateSliderForSlot(string slotName, string path)
        {
            string slotJsonFilePath = Path.Combine(path, slotName + ".json");

            if (File.Exists(slotJsonFilePath))
            {
                string slotJsonData = File.ReadAllText(slotJsonFilePath);
                SlotModelData slotModelData = JsonUtility.FromJson<SlotModelData>(slotJsonData);

                if (slotModelData != null && slotModelData.meshes != null)
                {
                    // Initialize primary slider with the full range of meshes
                    InitializePrimarySlider(slotName, slotModelData.meshes.Count);
                }
            }
            else
            {
                Debug.LogError("Slot JSON file not found: " + slotJsonFilePath);
            }
        }

        void InitializePrimarySlider(string slotName, int meshesCount)
        {
            GameObject sliderObject = Instantiate(sliderPrefab, slidersPanel.transform, false);
            sliderObject.name = slotName + "Slider";

            Slider slider = sliderObject.GetComponentInChildren<Slider>();
            if (slider != null)
            {
                slider.minValue = 0; // 0 for 'off'
                slider.maxValue = meshesCount;
                slider.wholeNumbers = true;
                slider.value = sliderValues.ContainsKey(slotName) ? sliderValues[slotName] : 0;
                slider.onValueChanged.AddListener(delegate { OnSliderValueChanged(slotName, slider.value, true); });
            }

            // Set label text
            TextMeshProUGUI labelText = sliderObject.GetComponentInChildren<TextMeshProUGUI>();
            if (labelText != null) labelText.text = slotName.Replace("ALL_", "");
        }


        void CreateVariationSlider(string slotName, int variationCount)
        {
            GameObject variationSliderObject = Instantiate(variationSliderPrefab, slidersPanel.transform, false);
            variationSliderObject.name = slotName + "VariationSlider";

            // Find the index of the primary slider in the slidersPanel
            int primarySliderIndex = FindSliderIndex(slotName + "Slider");

            // Position the variation slider directly below the primary slider
            if (primarySliderIndex != -1 && primarySliderIndex < slidersPanel.transform.childCount - 1)
            {
                variationSliderObject.transform.SetSiblingIndex(primarySliderIndex + 1);
            }

            Slider variationSlider = variationSliderObject.GetComponentInChildren<Slider>();
            if (variationSlider != null)
            {
                // Slider setup
                variationSlider.minValue = 0;
                variationSlider.maxValue = variationCount;
                variationSlider.wholeNumbers = true;
                variationSlider.onValueChanged.AddListener(delegate { OnVariationSliderValueChanged(slotName, variationSlider.value); });
            }

            TextMeshProUGUI variationLabel = variationSliderObject.GetComponentInChildren<TextMeshProUGUI>();
            if (variationLabel != null)
            {
                variationLabel.text = "Variation";
            }
        }

        int FindSliderIndex(string sliderName)
        {
            for (int i = 0; i < slidersPanel.transform.childCount; i++)
            {
                if (slidersPanel.transform.GetChild(i).name == sliderName)
                {
                    return i;
                }
            }
            return -1; // Return -1 if the slider is not found
        }

        void OnSliderValueChanged(string slotName, float value, bool userChanged)
        {
            if (userChanged)
            {
                sliderValues[slotName] = value;
            }

            if (value == 0)
            {
                RemoveModelAndVariationSlider(slotName);
            }
            else
            {
                int modelIndex = Mathf.Clamp((int)(value - 1), 0, int.MaxValue);
                LoadModelAndCreateVariationSlider(slotName, modelIndex);
            }
        }

        void RemoveModelAndVariationSlider(string slotName)
        {
            // Remove model
            if (currentlyLoadedModels.TryGetValue(slotName, out GameObject currentModel))
            {
                Destroy(currentModel);
                currentlyLoadedModels.Remove(slotName);
            }

            // Remove variation slider
            Transform existingVariationSlider = slidersPanel.transform.Find(slotName + "VariationSlider");
            if (existingVariationSlider != null) Destroy(existingVariationSlider.gameObject);
        }


        void OnVariationSliderValueChanged(string slotName, float value)
        {
            //Debug.Log($"OnVariationSliderValueChanged for slot: {slotName} with value: {value}");
            if (!currentlyLoadedModels.TryGetValue(slotName, out GameObject currentModel))
            {
                Debug.LogError($"No model currently loaded for slot: {slotName}");
                return;
            }

            if (value == 0 && originalMaterials.TryGetValue(slotName, out List<Material> mats))
            {
                // Apply original materials
                var skinnedMeshRenderers = currentModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                for (int i = 0; i < skinnedMeshRenderers.Length; i++)
                {
                    if (i < mats.Count)
                    {
                        skinnedMeshRenderers[i].sharedMaterial = mats[i];
                    }
                }
                return;
            }

            // Retrieve the model index from the primary slider
            int modelIndex = Mathf.Clamp((int)sliderValues[slotName] - 1, 0, int.MaxValue);

            // Get the modelName using the modelIndex
            string modelName = GetModelNameFromIndex(slotName, modelIndex);

            string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", modelName + ".json");

            if (File.Exists(materialJsonFilePath))
            {
                string materialJsonData = File.ReadAllText(materialJsonFilePath);
                ModelData.ModelInfo modelInfo = JsonUtility.FromJson<ModelData.ModelInfo>(materialJsonData);
                if (modelInfo != null && modelInfo.variations != null)
                {
                    // Correctly determine the variation index
                    int variationIndex = Mathf.Clamp((int)value - 1, 0, modelInfo.variations.Count - 1);
                    var variationMaterials = modelInfo.variations[variationIndex].materials;
                    if (variationMaterials != null)
                    {
                        ApplyVariationMaterials(currentModel, variationMaterials);
                    }
                }
            }
            else
            {
                Debug.LogError("Material JSON file not found: " + materialJsonFilePath);
            }
        }

        void RemoveVariationSlider(string slotName)
        {
            Transform existingVariationSlider = slidersPanel.transform.Find(slotName + "VariationSlider");
            if (existingVariationSlider != null) Destroy(existingVariationSlider.gameObject);
        }

        string GetModelNameFromIndex(string slotName, int modelIndex)
        {
            string type = typeDropdown.options[typeDropdown.value].text;
            string category = categoryDropdown.options[categoryDropdown.value].text;
            string classSelection = classDropdown.options[classDropdown.value].text;

            // Construct the path based on the dropdown selections
            string path = Path.Combine(Application.streamingAssetsPath, "SlotData", type);

            if (category != "ALL")
            {
                path = Path.Combine(path, category);

                if (classSelection != "ALL")
                {
                    path = Path.Combine(path, classSelection);
                }
            }

            string slotJsonFilePath = Path.Combine(path, slotName + ".json");

            if (File.Exists(slotJsonFilePath))
            {
                string slotJsonData = File.ReadAllText(slotJsonFilePath);
                SlotModelData slotModelData = JsonUtility.FromJson<SlotModelData>(slotJsonData);
                if (slotModelData != null && slotModelData.meshes != null && slotModelData.meshes.Count > modelIndex)
                {
                    string meshName = slotModelData.meshes[modelIndex];
                    return GetModelName(meshName);
                }
            }

            Debug.LogError("Slot JSON file not found: " + slotJsonFilePath);
            return null;
        }

        void LoadModelAndCreateVariationSlider(string slotName, int modelIndex)
        {
            // Declare modelInstance at the start of the method
            GameObject modelInstance = null;

            string type = typeDropdown.options[typeDropdown.value].text;
            string category = categoryDropdown.options[categoryDropdown.value].text;
            string classSelection = classDropdown.options[classDropdown.value].text;

            string path = Path.Combine(Application.streamingAssetsPath, "SlotData", type);

            if (category != "ALL")
            {
                path = Path.Combine(path, category);

                if (classSelection != "ALL")
                {
                    path = Path.Combine(path, classSelection);
                }
            }

            string slotJsonFilePath = Path.Combine(path, slotName + ".json");

            if (File.Exists(slotJsonFilePath))
            {
                string slotJsonData = File.ReadAllText(slotJsonFilePath);
                SlotModelData slotModelData = JsonUtility.FromJson<SlotModelData>(slotJsonData);

                if (slotModelData != null && slotModelData.meshes != null && slotModelData.meshes.Count > modelIndex)
                {
                    string meshName = slotModelData.meshes[modelIndex];
                    string modelName = GetModelName(meshName);

                    RemoveModelAndVariationSlider(slotName);
                    modelInstance = LoadModelPrefab(modelName, slotName);
                    LoadAndApplyMaterials(modelName, modelInstance, slotName);
                    CreateOrUpdateVariationSlider(slotName, modelName);

                    // Update the currentlyLoadedModels dictionary
                    if (currentlyLoadedModels.ContainsKey(slotName))
                    {
                        // Replace the existing model
                        currentlyLoadedModels[slotName] = modelInstance;
                    }
                    else
                    {
                        // Add new model
                        currentlyLoadedModels.Add(slotName, modelInstance);
                    }
                }
                else
                {
                    Debug.LogError("Mesh data is null or empty for slot: " + slotName);
                }
            }
            else
            {
                Debug.LogError("Slot JSON file not found: " + slotJsonFilePath);
            }
        }

        string GetModelName(string meshName)
        {
            return meshName.EndsWith(".msh") ? meshName.Substring(0, meshName.Length - 4) : meshName;
        }

        public void LoadAndApplyMaterials(string modelName, GameObject modelInstance, string slotName)
        {
            string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", modelName + ".json");
            if (File.Exists(materialJsonFilePath))
            {
                string materialJsonData = File.ReadAllText(materialJsonFilePath);
                ModelData.ModelInfo modelInfo = JsonUtility.FromJson<ModelData.ModelInfo>(materialJsonData);
                ApplyMaterials(modelInstance, modelInfo);

                // Store original materials
                var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                List<Material> mats = new List<Material>();
                foreach (var renderer in skinnedMeshRenderers)
                {
                    mats.AddRange(renderer.sharedMaterials);
                }
                originalMaterials[slotName] = mats;
            }
            else
            {
                Debug.LogError("Material JSON file not found: " + materialJsonFilePath);
            }
        }

        void CreateOrUpdateVariationSlider(string slotName, string modelName)
        {
            // Check if a variation slider already exists and remove it
            RemoveVariationSlider(slotName);

            string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", modelName + ".json");
            if (File.Exists(materialJsonFilePath))
            {
                string materialJsonData = File.ReadAllText(materialJsonFilePath);
                ModelData.ModelInfo modelInfo = JsonUtility.FromJson<ModelData.ModelInfo>(materialJsonData);

                if (modelInfo != null && modelInfo.variations != null && modelInfo.variations.Count > 0)
                {
                    CreateVariationSlider(slotName, modelInfo.variations.Count);
                }
                // No else block needed, as RemoveVariationSlider has already been called
            }
        }

        private void ApplyVariationMaterials(GameObject modelInstance, List<MaterialData> variationMaterials)
        {
            var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            // Reset to initial state before applying new variation
            if (initialRendererStates.TryGetValue(modelInstance, out var initialState))
            {
                for (int i = 0; i < skinnedMeshRenderers.Length; i++)
                {
                    if (i < initialState.Length)
                    {
                        skinnedMeshRenderers[i].enabled = initialState[i];
                    }
                }
            }

            // Clear the list of disabled renderers for this modelInstance
            if (disabledRenderers.ContainsKey(modelInstance))
            {
                disabledRenderers[modelInstance].Clear();
            }

            foreach (var materialData in variationMaterials)
            {
                int rendererIndex = materialData.number - 1;
                if (rendererIndex >= 0 && rendererIndex < skinnedMeshRenderers.Length)
                {
                    var renderer = skinnedMeshRenderers[rendererIndex];
                    ApplyMaterialToRenderer(renderer, materialData.name, modelInstance);
                }
                else
                {
                    Debug.LogError($"Renderer index out of bounds: {rendererIndex} for material number {materialData.number} in model '{modelInstance.name}'");
                }
            }
        }

        public GameObject LoadModelPrefab(string modelName, string slotName) // Add slotName as parameter
        {
            string prefabPath = Path.Combine("Prefabs", modelName);
            GameObject prefab = Resources.Load<GameObject>(prefabPath);

            if (prefab != null)
            {
                GameObject modelInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                currentlyLoadedModels[slotName] = modelInstance; // Now slotName is in the correct context
                return modelInstance;
            }
            else
            {
                Debug.LogError("Prefab not found: " + prefabPath);
                return null;
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

        private void ApplyMaterials(GameObject modelInstance, ModelData.ModelInfo modelInfo)
        {
            if (modelInstance == null || modelInfo == null || modelInfo.materialsResources == null)
            {
                Debug.LogError("ApplyMaterials: modelInstance or modelInfo is null.");
                return;
            }

            var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (!initialRendererStates.ContainsKey(modelInstance))
            {
                initialRendererStates[modelInstance] = skinnedMeshRenderers.Select(r => r.enabled).ToArray();
            }

            foreach (var materialResource in modelInfo.materialsResources)
            {
                if (materialResource == null || materialResource.resources == null || materialResource.resources.Count == 0)
                {
                    Debug.LogError("Material resource data is null or empty.");
                    continue;
                }

                int rendererIndex = materialResource.number - 1;
                if (rendererIndex >= 0 && rendererIndex < skinnedMeshRenderers.Length)
                {
                    var renderer = skinnedMeshRenderers[rendererIndex];

                    // Example logic to select the first resource or based on some condition
                    var resource = materialResource.resources.First();
                    string materialName = resource.name;
                    List<RttiValue> rttiValues = resource.rttiValues;
                    ApplyMaterialToRenderer(renderer, materialName, modelInstance, rttiValues);
                }
                else
                {
                    Debug.LogError($"Renderer index out of bounds: {rendererIndex} for material number {materialResource.number} in model '{modelInfo.name}'");
                }
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

        private void ApplyMaterialToRenderer(SkinnedMeshRenderer renderer, string materialName, GameObject modelInstance, List<RttiValue> rttiValues = null)
        {
            if (materialName.Equals("null.mat", StringComparison.OrdinalIgnoreCase))
            {
                renderer.enabled = false;
                AddToDisabledRenderers(modelInstance, renderer);
                return;
            }
            else
            {
                renderer.enabled = true;
            }

            Material loadedMaterial = LoadMaterial(materialName);

            if (loadedMaterial != null)
            {
                Material[] rendererMaterials = renderer.sharedMaterials;
                if (rendererMaterials.Length > 0)
                {
                    Material clonedMaterial = new Material(loadedMaterial);
                    rendererMaterials[0] = clonedMaterial;
                    renderer.sharedMaterials = rendererMaterials;

                    if (rttiValues != null)
                    {
                        foreach (var rttiValue in rttiValues)
                        {
                            if (rttiValue.name != "ems_scale")
                            {
                                bool useCustomShader = ShouldUseCustomShader(materialName);
                                ApplyTextureToMaterial(clonedMaterial, rttiValue.name, rttiValue.val_str, useCustomShader);
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Renderer {renderer.gameObject.name} has no materials to apply to");
                }
            }
            else
            {
                Debug.LogError($"Material not found: '{materialName}' for renderer '{renderer.gameObject.name}'");
            }
        }

        private void AddToDisabledRenderers(GameObject modelInstance, SkinnedMeshRenderer renderer)
        {
            if (!disabledRenderers.ContainsKey(modelInstance))
            {
                disabledRenderers[modelInstance] = new List<int>();
            }
            disabledRenderers[modelInstance].Add(Array.IndexOf(modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true), renderer));
        }

        private void ApplyTextureToMaterial(Material material, string rttiValueName, string textureName, bool useCustomShader)
        {
            if (!string.IsNullOrEmpty(textureName))
            {
                Debug.Log($"Applying texture. RTTI Value Name: {rttiValueName}, Texture Name: {textureName}");

                if (rttiValueName == "ems_scale")
                {
                    return;
                }

                string texturePath = "textures/" + Path.GetFileNameWithoutExtension(textureName);
                Texture2D texture = Resources.Load<Texture2D>(texturePath);
                string difTextureName = null;
                string modifierTextureName = null;

                if (texture != null)
                {
                    if (useCustomShader)
                    {
                        bool difTextureApplied = false;
                        switch (rttiValueName)
                        {
                            case "msk_0_tex":
                            case "msk_1_tex":
                            case "msk_1_add_tex":
                                material.SetTexture("_msk", texture);
                                break;
                            case "idx_0_tex":
                            case "idx_1_tex":
                                material.SetTexture("_idx", texture);
                                break;
                            case "grd_0_tex":
                            case "grd_1_tex":
                                material.SetTexture("_gra", texture);
                                break;
                            case "spc_0_tex":
                            case "spc_1_tex":
                                material.SetTexture("_spc", texture);
                                break;
                            case "clp_0_tex":
                            case "clp_1_tex":
                                material.SetTexture("_clp", texture);
                                break;
                            case "rgh_0_tex":
                            case "rgh_1_tex":
                                material.SetTexture("_rgh", texture);
                                break;
                            case "ocl_0_tex":
                            case "ocl_1_tex":
                                material.SetTexture("_ocl", texture);
                                break;
                            case "ems_0_tex":
                            case "ems_1_tex":
                                material.SetTexture("_ems", texture);
                                break;
                            case "dif_1_tex":
                            case "dif_0_tex":
                                material.SetTexture("_dif", texture);
                                difTextureName = textureName;
                                difTextureApplied = true;
                                break;
                            case "nrm_1_tex":
                            case "nrm_0_tex":
                                material.SetTexture("_nrm", texture);
                                break;
                        }

                        if (difTextureApplied && textureName.StartsWith("chr_"))
                        {
                            material.SetTexture("_modifier", texture);
                            modifierTextureName = textureName;
                        }

                        // Check if _dif and _modifier textures have the same name
                        if (difTextureName != null && modifierTextureName != null && difTextureName == modifierTextureName)
                        {
                            material.SetFloat("_Modifier", 0);
                        }
                        else if (difTextureApplied)
                        {
                            material.SetFloat("_Modifier", 1);
                        }
                    }
                    else
                    {
                        // HDRP/Lit shader texture assignments
                        switch (rttiValueName)
                        {
                            case "dif_1_tex":
                            case "dif_0_tex":
                                material.SetTexture("_BaseColorMap", texture);
                                break;
                            case "nrm_1_tex":
                            case "nrm_0_tex":
                                material.SetTexture("_NormalMap", texture);
                                break;
                            case "msk_1_tex":
                                // Assuming msk_1_tex corresponds to the mask map in HDRP/Lit shader
                                material.SetTexture("_MaskMap", texture);
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
            else
            {
                //Debug.LogWarning($"Texture name is empty for RTTI Value Name: {rttiValueName}");
            }
        }

        private Material LoadMaterial(string materialName)
        {
            string matPath = "materials/" + Path.GetFileNameWithoutExtension(materialName);
            Material loadedMaterial = Resources.Load<Material>(matPath);

            if (loadedMaterial == null && (materialName.EndsWith("_tpp") || materialName.EndsWith("_fpp")))
            {
                // Check alternative suffix first (_fpp or _tpp)
                string alternativeSuffix = materialName.EndsWith("_tpp") ? "_fpp" : "_tpp";
                string alternativeMaterialName = materialName.Replace(materialName.EndsWith("_tpp") ? "_tpp" : "_fpp", alternativeSuffix);
                string alternativeMatPath = "materials/" + Path.GetFileNameWithoutExtension(alternativeMaterialName);
                loadedMaterial = Resources.Load<Material>(alternativeMatPath);

                if (loadedMaterial == null)
                {
                    // Try loading the base material (without _tpp or _fpp)
                    string baseMaterialName = materialName.Replace("_tpp", "").Replace("_fpp", "");
                    string baseMatPath = "materials/" + Path.GetFileNameWithoutExtension(baseMaterialName);
                    loadedMaterial = Resources.Load<Material>(baseMatPath);

                    if (loadedMaterial != null)
                    {
                        // Duplicate the material and rename it to the original requested name (with _tpp or _fpp)
                        loadedMaterial = new Material(loadedMaterial);
                        loadedMaterial.name = materialName;
                        // Save the duplicated material for future use
                        SaveDuplicatedMaterial(loadedMaterial);
                    }
                }
            }

            return loadedMaterial;
        }

        private void SaveDuplicatedMaterial(Material material)
        {
            // Check if we are running in the Unity Editor
#if UNITY_EDITOR
            // Construct the path where the material should be saved
            string materialsPath = "Assets/Resources/materials/"; // Update this path as per your project structure
            string materialPath = materialsPath + material.name + ".mat";

            // Check if the material already exists to avoid overwriting
            if (!System.IO.File.Exists(materialPath))
            {
                // Save the material as a new asset
                UnityEditor.AssetDatabase.CreateAsset(material, materialPath);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
                Debug.Log("Material saved: " + materialPath);
            }
            else
            {
                Debug.Log("Material already exists: " + materialPath);
            }
#else
    Debug.LogError("SaveDuplicatedMaterial can only be used in the Unity Editor.");
#endif
        }

        private bool ShouldDisableRenderer(string gameObjectName)
        {
            return gameObjectName.Contains("sh_eye_shadow") ||
                   gameObjectName.Contains("sh_wet_eye") ||
                   gameObjectName.Contains("_null");
        }

        public Dictionary<string, float> GetSliderValues()
        {
            return sliderValues;
        }

        // Example of a public getter method for currentlyLoadedModels
        public Dictionary<string, GameObject> GetCurrentlyLoadedModels()
        {
            return currentlyLoadedModels;
        }

    }
}