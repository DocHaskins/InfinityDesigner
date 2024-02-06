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
using Michsky.UI.Heat;

/// <summary>
/// Facilitates the loading and application of different character components based on selections made through a UI, managing categories such as type, class, and race. 
/// The script also handles the instantiation of sliders for fine-tuned character customization, supports preset configurations for quick loading, and integrates with Cinemachine for camera adjustments. 
/// It utilizes a combination of JSON data for model properties and dynamically generated UI elements to allow users to build and customize characters in real-time.
/// </summary>

namespace doppelganger
{
    public class CharacterBuilder : MonoBehaviour
    {
        [Header("Managers")]
        public SkeletonLookup skeletonLookup;
        public FilterMapping filterMapping;
        public CinemachineCameraZoomTool cameraTool;
        
        private string currentType;
        private string currentPath;

        [Header("Interface")]
        [SerializeField] 
        private HorizontalSelector typeSelector;
        public TMP_Dropdown categoryDropdown;
        public TMP_Dropdown classDropdown;
        public TMP_InputField saveName;
        public TMP_Dropdown saveTypeDropdown;
        public TMP_Dropdown saveCategoryDropdown;
        public TMP_Dropdown saveClassDropdown;
        public TMP_Dropdown presetDropdown;
        public GameObject slidersPanel;
        public GameObject sliderPrefab;
        public GameObject variationSliderPrefab;
        public GameObject loadedSkeleton;
        public GameObject buttonPrefab;
        public Button presetLoadButton;

        private string lastFilterCategoryKey = "";
        private Dictionary<string, float> slotWeights;
        private string selectedCategory;
        private string selectedClass;

        public Dictionary<string, float> sliderValues = new Dictionary<string, float>();
        private Dictionary<string, bool> sliderSetStatus = new Dictionary<string, bool>();
        private Dictionary<GameObject, List<int>> disabledRenderers = new Dictionary<GameObject, List<int>>();
        private Dictionary<GameObject, bool[]> initialRendererStates = new Dictionary<GameObject, bool[]>();
        private Dictionary<string, List<Material>> originalMaterials = new Dictionary<string, List<Material>>();
        private Dictionary<string, List<string>> slotData = new Dictionary<string, List<string>>();
        public Dictionary<string, GameObject> currentlyLoadedModels = new Dictionary<string, GameObject>();

        Dictionary<string, string> classConversions = new Dictionary<string, string>
{
    { "peacekeeper", "pk" },
    { "scavenger", "sc" },
    { "survivor", "sv" },
    { "bandit", "bdt" },
    { "volatile", "volatile" }
    // Add other class conversions as needed
};

        void Start()
        {
            string initialType = "Human";
            string initialCategory = "Player";
            PopulateDropdown(categoryDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", "Human"), "ALL", true);
            PopulateDropdown(saveTypeDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData"), "Human", false);
            PopulateDropdown(saveCategoryDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", "Human"), "Player", false);
            PopulateDropdown(saveClassDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", initialType, initialCategory), "ALL", true);

            StartCoroutine(SetInitialDropdownValues());

            // Set up button listeners
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

            // Manually trigger the interface update as if the dropdown values were changed
            UpdateInterfaceBasedOnDropdownSelection();

            typeSelector.onValueChanged.AddListener((index) => OnTypeChanged(index));

            // Manually update preset dropdown based on initial dropdown selections
            UpdatePresetDropdown();

            // Load default JSON file and set sliders
            string defaultJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Jsons", "Human", "Player", "player_tpp_skeleton.json");
            LoadJsonAndSetSliders(defaultJsonFilePath);
            slotWeights = LoadSlotWeights();
        }

        IEnumerator SetInitialDropdownValues()
        {
            // Wait for the end of the frame to ensure dropdowns are populated
            yield return new WaitForEndOfFrame();

            // Set initial values for Type, Category, and Class dropdowns

            categoryDropdown.value = categoryDropdown.options.FindIndex(option => option.text == "Player");
            classDropdown.value = classDropdown.options.FindIndex(option => option.text == "ALL");
            SetDropdownByValue(saveTypeDropdown, "Human");
            SetDropdownByValue(saveCategoryDropdown, "Player");
            SetDropdownByValue(saveClassDropdown, "ALL");

            OnCategoryChanged(categoryDropdown.value);

            categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
            classDropdown.onValueChanged.AddListener(OnClassChanged);
            saveTypeDropdown.onValueChanged.AddListener(OnSaveTypeChanged);
            saveCategoryDropdown.onValueChanged.AddListener(OnSaveCategoryChanged);

            UpdateInterfaceBasedOnDropdownSelection();
        }

        void SetDropdownByValue(TMP_Dropdown dropdown, string value)
        {
            int index = dropdown.options.FindIndex(option => string.Equals(option.text, value, StringComparison.OrdinalIgnoreCase));
            if (index != -1)
            {
                dropdown.value = index;
                dropdown.RefreshShownValue();
            }
            else
            {
                Debug.LogError($"Option '{value}' not found in dropdown.");
            }
        }

        public void Reroll()
        {
            foreach (Transform sliderContainer in slidersPanel.transform)
            {
                ChildLockToggle lockToggleScript = sliderContainer.Find("LockToggle")?.GetComponent<ChildLockToggle>();

                // Proceed only if either the lock toggle doesn't exist or it exists and is off
                if (lockToggleScript != null && lockToggleScript.childToggle.isOn)
                {
                    Debug.Log("Skipping locked slider: " + sliderContainer.name);
                    continue;
                }

                Slider slider = sliderContainer.Find("primarySlider")?.GetComponent<Slider>();
                if (slider != null)
                {
                    float randomValue = UnityEngine.Random.Range(slider.minValue, slider.maxValue + 1);
                    //Debug.Log($"Random value for {sliderContainer.name}: {randomValue}");
                    slider.value = randomValue;

                    string slotName = sliderContainer.name.Replace("Slider", "");
                    // Assuming OnSliderValueChanged is a method that handles the slider's value change.
                    OnSliderValueChanged(slotName, randomValue, true);
                }
                else
                {
                    //Debug.LogWarning($"No slider component found in primarySlider of: {sliderContainer.name}");
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
                //Debug.Log("Reading ini file: " + iniPath);
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
                                //Debug.Log($"Loaded weight for {slotName}: {weight}");
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

        public class DropdownValueMapper : MonoBehaviour
        {
            public Dictionary<string, string> ValueMap { get; set; } = new Dictionary<string, string>();

            // Method to update the mapping
            public void UpdateValueMap(Dictionary<string, string> newValueMap)
            {
                ValueMap = newValueMap;
            }
        }

        string GetActualValueFromDropdown(TMPro.TMP_Dropdown dropdown)
        {
            DropdownValueMapper mapper = dropdown.gameObject.GetComponent<DropdownValueMapper>();
            if (mapper != null && mapper.ValueMap.TryGetValue(dropdown.options[dropdown.value].text, out string actualValue))
            {
                return actualValue;
            }
            else
            {
                Debug.LogError("No mapping found for selected dropdown value");
                return null;
            }
        }

        void OnPresetLoadButtonPressed()
        {
            string selectedPreset = GetActualValueFromDropdown(presetDropdown);
            saveName.text = selectedPreset; // Assuming you want to display the actual preset name
            if (!string.IsNullOrEmpty(selectedPreset))
            {
                // Assuming GetTypeFromSelector and categoryDropdown provide correct values
                string type = GetTypeFromSelector();
                string category = categoryDropdown.options[categoryDropdown.value].text;

                // Build the correct path without appending the class as a directory
                string presetsPath = Path.Combine(Application.streamingAssetsPath, "Jsons", type.Equals("ALL") ? "" : type, category.Equals("ALL") ? "" : category, $"{selectedPreset}.json");

                Debug.Log("Loading JSON from path: " + presetsPath);
                LoadJsonAndSetSliders(presetsPath);
            }
            else
            {
                Debug.LogError("Selected preset is invalid or not found");
            }
        }

        string GetJsonFilePath(string presetName)
        {
            string type = GetTypeFromSelector();
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
                        // Find any existing skeleton
                        GameObject currentSkeleton = GameObject.FindGameObjectWithTag("Skeleton");
                        bool shouldLoadSkeleton = true;

                        // Check if the found skeleton's name matches the required skeletonName
                        if (currentSkeleton != null)
                        {
                            if (currentSkeleton.name == skeletonName || currentSkeleton.name == skeletonName + "(Clone)")
                            {
                                // If names match, no need to load a new skeleton
                                shouldLoadSkeleton = false;
                            }
                            else
                            {
                                // If names don't match, destroy the existing skeleton
                                Destroy(currentSkeleton);
                            }
                        }

                        // Load the skeleton only if needed
                        if (shouldLoadSkeleton)
                        {
                            LoadSkeleton(skeletonName);
                        }
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
                                        //if (currentlyLoadedModels.TryGetValue(modelNameWithClone, out GameObject modelInstance))
                                        //{
                                        //    ApplyMaterials(modelInstance, modelInfo);
                                        //}
                                        //else
                                        //{
                                        //    Debug.LogError($"Model instance not found for {modelNameWithClone}");
                                        //    //PrintCurrentlyLoadedModels();
                                        //}
                                    }
                                    else
                                    {
                                        Debug.LogError($"Model index not found for {modelInfo.name} in slot {slotName}");
                                    }
                                }
                                else
                                {
                                    Debug.LogError($"Slot not found for model {modelInfo.name}");
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
            string type = GetTypeFromSelector();
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
                            //Debug.Log($"Found slot {slotName} for model {modelName}");
                            return slotName;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing JSON file: {slotFile}. Error: {e.Message}");
                }
            }

            Debug.LogError($"Model {modelName} not found in any slot");
            return null;
        }

        public int GetModelIndex(string slotName, string modelName)
        {
            //Debug.Log($"GetModelIndex for slot {slotName} and model {modelName}");
            string type = GetTypeFromSelector();
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
                        //Debug.Log($"Model {modelName} found at index {i}, adjusted index: {adjustedIndex}, in slot {slotName}");
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
            //ResetCameraToDefaultView();
        }

        private void UpdateCameraTarget(Transform loadedModelTransform)
        {
            if (cameraTool != null && loadedModelTransform != null)
            {
                cameraTool.targets.Clear();
                string[] pointNames = { "pelvis", "spine1", "spine3", "neck", "legs", "r_hand", "l_hand", "l_foot", "r_foot" };
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

            if (!includeAllOption && options.Any(o => o.text == "ALL"))
            {
                // Remove the "ALL" option if it's not supposed to be included
                options.RemoveAll(o => o.text == "ALL");
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

        void OnSaveTypeChanged(int index)
        {
            // Assuming you have a way to map the index to a type
            string selectedType = GetTypeBasedOnIndex(index);
            //PopulateDropdown(saveCategoryDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType), "ALL", false);
            string selectedCategory = saveCategoryDropdown.options[index].text;
            //PopulateDropdown(saveClassDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType, selectedCategory), "ALL", true);
        }
        void OnTypeChanged(int index)
        {
            // Assuming you have a way to map the index to a type
            string selectedType = GetTypeBasedOnIndex(index);
            PopulateDropdown(categoryDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType), "ALL", true);
            PopulateDropdown(saveCategoryDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType), "ALL", true);
            UpdateInterfaceBasedOnDropdownSelection();
        }

        string GetTypeFromSelector()
        {
            return GetTypeBasedOnIndex(typeSelector.index);
        }

        string GetTypeBasedOnIndex(int index)
        {
            string[] types = { "Human", "Infected" };
            if (index >= 0 && index < types.Length)
            {
                return types[index];
            }
            else
            {
                Debug.LogError("Index out of range: " + index);
                return string.Empty;
            }
        }

        void OnSaveCategoryChanged(int index)
        {
            string selectedType = GetTypeFromSelector();
            string selectedCategory = saveCategoryDropdown.options[index].text;

            // Use the selectedType to populate the classDropdown based on the selected category
            PopulateDropdown(saveClassDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType, selectedCategory), "ALL");
        }

        void OnCategoryChanged(int index)
        {
            string selectedType = GetTypeFromSelector();
            string selectedCategory = categoryDropdown.options[index].text;

            // Use the selectedType to populate the classDropdown based on the selected category
            PopulateDropdown(classDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType, selectedCategory), "ALL");
            PopulateDropdown(saveClassDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType, selectedCategory), "ALL");

            UpdateInterfaceBasedOnDropdownSelection();
        }

        void OnClassChanged(int index)
        {
            UpdateInterfaceBasedOnDropdownSelection();
        }

        void UpdatePresetDropdown()
        {
            string type = GetTypeFromSelector(); // Assuming this method retrieves the selected type
            string category = categoryDropdown.options[categoryDropdown.value].text;
            string classSelection = classDropdown.options[classDropdown.value].text;

            // Normalize case for comparison, but use original case for path and filtering
            string normalizedClassSelection = classSelection.ToLower();

            string presetsPath = Path.Combine(Application.streamingAssetsPath, "Jsons");

            // The path is now only based on Type and Category selections
            string searchPath = Path.Combine(presetsPath, type.Equals("ALL") ? "" : type, category.Equals("ALL") ? "" : category);

            // Class abbreviation is used solely for file filtering, not path construction
            string classAbbreviation = normalizedClassSelection != "all" && classConversions.ContainsKey(normalizedClassSelection) ? classConversions[normalizedClassSelection] : "";

            PopulatePresetDropdown(presetDropdown, searchPath, classAbbreviation);
        }

        void PopulatePresetDropdown(TMPro.TMP_Dropdown dropdown, string path, string classAbbreviation)
        {
            // Ensure the directory exists
            if (!Directory.Exists(path))
            {
                Debug.LogError($"Directory does not exist: {path}");
                dropdown.ClearOptions();
                return;
            }

            var jsonFiles = Directory.GetFiles(path, "*.json").Select(Path.GetFileNameWithoutExtension);

            // Filter files by class abbreviation if not empty
            var filteredFiles = string.IsNullOrEmpty(classAbbreviation)
                ? jsonFiles
                : jsonFiles.Where(file => file.ToLower().Contains(classAbbreviation)).ToList();

            // Further exclude specific files based on naming conventions
            filteredFiles = filteredFiles.Where(file => !file.StartsWith("db_") && !file.EndsWith("_fpp")).ToList();

            List<TMPro.TMP_Dropdown.OptionData> options = new List<TMPro.TMP_Dropdown.OptionData>();
            Dictionary<string, string> fileDisplayNames = new Dictionary<string, string>();

            foreach (var file in filteredFiles)
            {
                string displayName = ConvertToReadableName(file); // Convert file names to a readable format
                options.Add(new TMPro.TMP_Dropdown.OptionData(displayName));
                fileDisplayNames[displayName] = file; // Map the display name to the original file name
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(options);

            // Update existing DropdownValueMapper or add a new one if it doesn't exist
            DropdownValueMapper mapper = dropdown.gameObject.GetComponent<DropdownValueMapper>();
            if (mapper == null)
            {
                mapper = dropdown.gameObject.AddComponent<DropdownValueMapper>();
            }
            mapper.UpdateValueMap(fileDisplayNames);

            dropdown.RefreshShownValue();
        }

        public List<TMP_Dropdown.OptionData> GetCurrentPresetOptions()
        {
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            return options;
        }

        string ConvertToReadableName(string fileName)
        {
            // Remove specific terms
            string[] termsToRemove = { "_test_", "_outfit_opera", "dlc_opera_", "zmb_", "player_", "outfit_", "enemy", "fh_", "tpp" };
            foreach (var term in termsToRemove)
            {
                fileName = fileName.Replace(term, "");
            }

            // Replace underscores with spaces
            fileName = fileName.Replace("_", " ");
            return fileName;
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
            string category = categoryDropdown.options[categoryDropdown.value].text;

            // New approach to determine filters based on the selected category
            List<string> filters = new List<string>();
            if (category != "ALL")
            {
                // Assume 'buttonMappings' contains keys that match dropdown options
                if (filterMapping.buttonMappings.TryGetValue("Button_" + category, out List<string> categoryFilters))
                {
                    filters.AddRange(categoryFilters);
                }
            }
            else
            {
                // If category is "ALL", add all filters from buttonMappings
                filters.AddRange(filterMapping.buttonMappings.SelectMany(pair => pair.Value).Distinct());
            }

            // Assuming PopulateSlidersWithFilters does the actual update
            PopulateSlidersWithFilters(currentPath, filters);

            // Presumed existing functionality
            UpdateInterfaceBasedOnType();
            UpdateSlidersBasedOnSelection();
            UpdatePresetDropdown();
            LoadSkeletonBasedOnSelection();
        }

        private void LoadSkeletonBasedOnSelection()
        {
            // Get the selected skeleton name based on category and class
            string selectedSkeleton = skeletonLookup.GetSelectedSkeleton();
            string resourcePath = "Prefabs/" + selectedSkeleton.Replace(".msh", "");

            // Check for the current skeleton in the scene
            GameObject currentSkeleton = GameObject.FindGameObjectWithTag("Skeleton");
            bool shouldLoadSkeleton = true;

            // If there is a current skeleton, check its prefab name against the selected skeleton
            if (currentSkeleton != null && currentSkeleton.name.StartsWith(selectedSkeleton))
            {
                // If names match (considering "(Clone)" suffix Unity adds to instantiated objects), skip loading a new one
                shouldLoadSkeleton = false;
            }
            else if (currentSkeleton != null)
            {
                // If there is a skeleton but with a wrong name, destroy it
                Destroy(currentSkeleton);
            }

            // Load and instantiate the new skeleton if needed
            if (shouldLoadSkeleton)
            {
                Debug.Log("Loading skeleton prefab from resource path: " + resourcePath);
                GameObject skeletonPrefab = Resources.Load<GameObject>(resourcePath);
                if (skeletonPrefab != null)
                {
                    GameObject loadedSkeleton = Instantiate(skeletonPrefab, Vector3.zero, Quaternion.Euler(-90, 0, 0));
                    loadedSkeleton.tag = "Skeleton";
                    loadedSkeleton.name = selectedSkeleton; // Optionally set the name to manage future checks

                    // Find the 'pelvis' child in the loaded skeleton and handle 'legs' creation
                    Transform pelvis = loadedSkeleton.transform.Find("pelvis");
                    if (pelvis != null)
                    {
                        GameObject legs = new GameObject("legs");
                        legs.transform.SetParent(pelvis);
                        legs.transform.localPosition = new Vector3(0, 0, -0.005f);
                    }
                    else
                    {
                        Debug.LogError("Pelvis not found in the skeleton prefab: " + selectedSkeleton);
                    }
                    UpdateCameraTarget(loadedSkeleton.transform);
                }
                else
                {
                    Debug.LogError("Skeleton prefab not found in Resources: " + resourcePath);
                }
            }
            else
            {
                Debug.Log("Skeleton with the name " + selectedSkeleton + " is already loaded. Skipping.");
            }
        }


        void UpdateSlidersBasedOnSelection()
        {
            string type = GetTypeFromSelector();
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

        public void FilterCategory(string categoryKey)
        {
            // Assuming 'buttonMappings' is now part of this script or accessible through a reference
            if (filterMapping.buttonMappings.TryGetValue(categoryKey, out List<string> filters))
            {
                PopulateSlidersWithFilters(currentPath, filters);
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
            //CreateDynamicButtons(filters);
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
            string currentType = GetTypeFromSelector();

            string path = Path.Combine(Application.streamingAssetsPath, "SlotData", currentType);
            PopulateSliders(path);

            // Create a list to store all filters
            List<string> allFilters = filterMapping.buttonMappings.SelectMany(pair => pair.Value).Distinct().ToList();
            //CreateDynamicButtons(allFilters);
        }

        void FilterSlidersForSlot(string slotName)
        {
            ClearExistingSliders();

            string type = GetTypeFromSelector();
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
                ApplyOriginalMaterials(currentModel, mats);
                return;
            }

            // Retrieve the model index from the primary slider
            int modelIndex = Mathf.Clamp((int)sliderValues[slotName] - 1, 0, int.MaxValue);
            string modelName = GetModelNameFromIndex(slotName, modelIndex);
            string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", modelName + ".json");

            if (File.Exists(materialJsonFilePath))
            {
                string materialJsonData = File.ReadAllText(materialJsonFilePath);
                ModelData.ModelInfo modelInfo = JsonUtility.FromJson<ModelData.ModelInfo>(materialJsonData);
                if (modelInfo != null && modelInfo.variations != null && modelInfo.variations.Count > 0)
                {
                    int variationIndex = Mathf.Clamp((int)value - 1, 0, modelInfo.variations.Count - 1);
                    var variationResources = modelInfo.variations[variationIndex].materialsResources;

                    if (variationResources != null)
                    {
                        ApplyVariationMaterials(currentModel, variationResources);
                    }
                }
            }
            else
            {
                Debug.LogError("Material JSON file not found: " + materialJsonFilePath);
            }
        }

        private void ApplyOriginalMaterials(GameObject modelInstance, List<Material> originalMats)
        {
            var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                // Ensure there's an original material for this renderer before applying
                if (i < originalMats.Count)
                {
                    skinnedMeshRenderers[i].sharedMaterial = originalMats[i];
                }
            }
        }

        void RemoveVariationSlider(string slotName)
        {
            Transform existingVariationSlider = slidersPanel.transform.Find(slotName + "VariationSlider");
            if (existingVariationSlider != null) Destroy(existingVariationSlider.gameObject);
        }

        string GetModelNameFromIndex(string slotName, int modelIndex)
        {
            string type = GetTypeFromSelector();
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

            string type = GetTypeFromSelector();
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

        private void ApplyVariationMaterials(GameObject modelInstance, List<ModelData.MaterialResource> materialsResources)
        {
            var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            // Reset to initial state before applying new variation
            ResetRenderersToInitialState(modelInstance, skinnedMeshRenderers);

            // Apply each materialResource to the appropriate renderer
            foreach (var materialResource in materialsResources)
            {
                foreach (var resource in materialResource.resources)
                {
                    int rendererIndex = materialResource.number - 1; // Assuming 'number' indicates the renderer's index
                    if (rendererIndex >= 0 && rendererIndex < skinnedMeshRenderers.Length)
                    {
                        var renderer = skinnedMeshRenderers[rendererIndex];
                        // Here, you may decide to apply the first resource, or a specific one based on conditions
                        ApplyMaterialToRenderer(renderer, resource.name, modelInstance, resource.rttiValues);
                    }
                    else
                    {
                        Debug.LogError($"Renderer index out of bounds: {rendererIndex} for material resource number {materialResource.number} in model '{modelInstance.name}'");
                    }
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
                                ApplyTextureToMaterial(clonedMaterial, rttiValue.name, rttiValue.val_str);
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

        private void ResetRenderersToInitialState(GameObject modelInstance, SkinnedMeshRenderer[] skinnedMeshRenderers)
        {
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
        }

        private void AddToDisabledRenderers(GameObject modelInstance, SkinnedMeshRenderer renderer)
        {
            if (!disabledRenderers.ContainsKey(modelInstance))
            {
                disabledRenderers[modelInstance] = new List<int>();
            }
            disabledRenderers[modelInstance].Add(Array.IndexOf(modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true), renderer));
        }

        private void ApplyTextureToMaterial(Material material, string rttiValueName, string textureName)
        {
            Debug.Log($"Processing RTTI Value Name: {rttiValueName}, Texture Name: '{textureName}'.");

            // Attempt to find the shader property from custom shader mapping, then HDRP mapping as fallback
            string shaderProperty = GetShaderProperty(rttiValueName);

            if (shaderProperty == null)
            {
                Debug.LogError($"Unsupported RTTI Value Name: {rttiValueName}. Unable to determine shader property.");
                return;
            }

            if ("null".Equals(textureName, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"Removing texture from shader property: {shaderProperty} for RTTI: {rttiValueName}.");
                material.SetTexture(shaderProperty, null);
            }
            else if (!string.IsNullOrEmpty(textureName))
            {
                string texturePath = $"textures/{Path.GetFileNameWithoutExtension(textureName)}";
                Texture2D texture = Resources.Load<Texture2D>(texturePath);

                if (texture != null)
                {
                    Debug.Log($"Applying texture to shader property: {shaderProperty}. RTTI Value Name: {rttiValueName}, Texture Name: {textureName}.");
                    material.SetTexture(shaderProperty, texture);
                    ApplyAdditionalSettings(material, rttiValueName, texture);
                }
                else
                {
                    Debug.LogError($"Texture '{texturePath}' not found in Resources for RTTI: {rttiValueName}.");
                }
            }
            else
            {
                Debug.LogWarning($"No texture name provided for RTTI: {rttiValueName}, skipping texture application.");
            }
        }

        private string GetShaderProperty(string rttiValueName)
        {
            // First try to get the property directly from the custom shader mapping
            var customMapping = GetCustomShaderMapping();
            if (customMapping.TryGetValue(rttiValueName, out var shaderProp))
            {
                return shaderProp;
            }

            // Try a fallback to a more generic property if the specific one is not found
            string basePropertyName = GetBasePropertyName(rttiValueName);
            if (basePropertyName != null && customMapping.TryGetValue(basePropertyName, out shaderProp))
            {
                return shaderProp;
            }

            // Then try the HDRP mapping as a last resort
            var hdrpMapping = GetHDRPMapping();
            if (hdrpMapping.TryGetValue(rttiValueName, out shaderProp))
            {
                return shaderProp;
            }
            else if (basePropertyName != null && hdrpMapping.TryGetValue(basePropertyName, out shaderProp))
            {
                return shaderProp;
            }

            // No mapping found
            return null;
        }

        private string GetBasePropertyName(string rttiValueName)
        {
            // Example of stripping a suffix to fallback to a more generic name
            // This can be customized based on your specific naming conventions
            if (rttiValueName.EndsWith("_1_tex") || rttiValueName.EndsWith("_0_tex"))
            {
                return rttiValueName.Substring(0, rttiValueName.LastIndexOf('_')) + "_tex";
            }
            else if (rttiValueName.Contains("_1_") || rttiValueName.Contains("_0_"))
            {
                return rttiValueName.Replace("_1_", "_").Replace("_0_", "_");
            }

            // Add other fallback rules as needed
            return null;
        }

        private Dictionary<string, string> GetCustomShaderMapping()
        {
            return new Dictionary<string, string>
            {
                { "msk_0_tex", "_msk" },
                { "msk_1_tex", "_msk" },
                { "msk_1_add_tex", "_msk" },
                { "idx_0_tex", "_idx" },
                { "idx_1_tex", "_idx" },
                { "grd_0_tex", "_gra" },
                { "grd_1_tex", "_gra" },
                { "spc_0_tex", "_spc" },
                { "spc_1_tex", "_spc" },
                { "clp_0_tex", "_clp" },
                { "clp_1_tex", "_clp" },
                { "rgh_0_tex", "_rgh" },
                { "rgh_1_tex", "_rgh" },
                { "ocl_0_tex", "_ocl" },
                { "ocl_1_tex", "_ocl" },
                { "ems_0_tex", "_ems" },
                { "ems_1_tex", "_ems" },
                { "dif_1_tex", "_dif_1" },
                { "dif_0_tex", "_dif" },
                { "det_0_b_dtm_dif", "_dif" },
                { "nrm_1_tex", "_nrm" },
                { "nrm_0_tex", "_nrm" },
                { "det_0_b_dtm_tex", "_nrm" }
            };
        }


        private Dictionary<string, string> GetHDRPMapping()
        {
            return new Dictionary<string, string>
        {
                {"dif_1_tex", "_BaseColorMap"},
                {"dif_0_tex", "_BaseColorMap"},
                {"det_0_b_dtm_dif", "_BaseColorMap" },
                {"nrm_1_tex", "_NormalMap"},
                {"nrm_0_tex", "_NormalMap"},
                {"det_0_b_dtm_tex", "_nrm" },
                {"msk_1_tex", "_MaskMap"},
                {"ems_0_tex", "_EmissiveColorMap"},
                {"ems_1_tex", "_EmissiveColorMap"}
            };
        }

        private void ApplyAdditionalSettings(Material material, string rttiValueName, Texture2D texture)
        {
            // Example: Apply emissive color if the RTTI value name suggests an emissive texture
            if (rttiValueName.StartsWith("ems"))
            {
                material.SetColor("_EmissiveColor", Color.white * 2);
                material.EnableKeyword("_EMISSION");
            }
            // Additional settings can be applied here based on RTTI value names
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