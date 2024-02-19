using Michsky.UI.Heat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ModelData;

/// <summary>
/// Facilitates the loading and application of different character components based on selections made through a UI, managing categories such as type, class, and race. 
/// The script also handles the instantiation of sliders for fine-tuned character customization, supports preset configurations for quick loading, and integrates with Cinemachine for camera adjustments. 
/// </summary>

namespace doppelganger
{
    public class CharacterBuilder_InterfaceManager : MonoBehaviour
    {
        [Header("Managers")]
        public CharacterBuilder characterBuilder;
        public AutoTargetCinemachineCamera autoTargetCinemachineCamera;
        public VariationBuilder variationBuilder;
        public SliderKeyboardControl sliderKeyboardControl;
        public FilterMapping filterMapping;

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
        public GameObject buttonPrefab;
        public Button presetLoadButton;
        public Button infoPanelButton;

        private string currentType;
        private string currentPath;

        private string lastFilterCategoryKey = "";
        private string selectedCategory;
        private string selectedClass;
        public string currentSlider = "";

        public Dictionary<string, float> sliderValues = new Dictionary<string, float>();
        private Dictionary<string, bool> sliderSetStatus = new Dictionary<string, bool>();
        private Dictionary<string, float> slotWeights;
        private Dictionary<string, List<string>> slotData = new Dictionary<string, List<string>>();
        public Dictionary<string, int> selectedVariationIndexes = new Dictionary<string, int>();

        Dictionary<string, string> classConversions = new Dictionary<string, string>

{
    { "peacekeeper", "pk" },
    { "scavenger", "sc" },
    { "survivor", "sv" },
    { "bandit", "bdt" },
    { "volatile", "volatile" }
};

        public class DropdownValueMapper : MonoBehaviour
        {
            public Dictionary<string, string> ValueMap { get; set; } = new Dictionary<string, string>();

            // Method to update the mapping
            public void UpdateValueMap(Dictionary<string, string> newValueMap)
            {
                ValueMap = newValueMap;
            }
        }

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
                Debug.LogWarning("Player type not found in dropdown options.");
            }

            // Manually trigger the interface update as if the dropdown values were changed
            UpdateInterfaceBasedOnDropdownSelection();

            typeSelector.onValueChanged.AddListener((index) => OnTypeChanged(index));

            // Manually update preset dropdown based on initial dropdown selections
            UpdatePresetDropdown();
            slotWeights = characterBuilder.LoadSlotWeights();
        }

        IEnumerator SetInitialDropdownValues()
        {
            // Wait for the end of the frame to ensure dropdowns are populated
            yield return new WaitForEndOfFrame();
            string defaultJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Jsons", "Human", "Player", "player_tpp_skeleton.json");
            characterBuilder.LoadJsonAndSetSliders(defaultJsonFilePath);
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

        public void PopulateDropdown(TMPro.TMP_Dropdown dropdown, string path, string defaultValue, bool includeAllOption = false)
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

        public List<string> GetSubFolders(string path)
        {
            if (Directory.Exists(path))
            {
                return Directory.GetDirectories(path).Select(Path.GetFileName).ToList();
            }
            return new List<string>();
        }

        public void OnSaveTypeChanged(int index)
        {
            // Assuming you have a way to map the index to a type
            string selectedType = GetTypeBasedOnIndex(index);
            //PopulateDropdown(saveCategoryDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType), "ALL", false);
            string selectedCategory = saveCategoryDropdown.options[index].text;
            //PopulateDropdown(saveClassDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType, selectedCategory), "ALL", true);
        }
        public void OnTypeChanged(int index)
        {
            // Assuming you have a way to map the index to a type
            string selectedType = GetTypeBasedOnIndex(index);
            PopulateDropdown(categoryDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType), "ALL", true);
            PopulateDropdown(saveCategoryDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType), "ALL", true);
            UpdateInterfaceBasedOnDropdownSelection();
        }

        public string GetTypeFromSelector()
        {
            return GetTypeBasedOnIndex(typeSelector.index);
        }

        public string GetTypeBasedOnIndex(int index)
        {
            string[] types = { "Human", "Infected" };
            if (index >= 0 && index < types.Length)
            {
                return types[index];
            }
            else
            {
                Debug.LogWarning("Index out of range: " + index);
                return string.Empty;
            }
        }

        public void OnSaveCategoryChanged(int index)
        {
            string selectedType = GetTypeFromSelector();
            string selectedCategory = saveCategoryDropdown.options[index].text;

            // Use the selectedType to populate the classDropdown based on the selected category
            PopulateDropdown(saveClassDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType, selectedCategory), "ALL");
        }

        public void OnCategoryChanged(int index)
        {
            string selectedType = GetTypeFromSelector();
            string selectedCategory = categoryDropdown.options[index].text;

            // Use the selectedType to populate the classDropdown based on the selected category
            PopulateDropdown(classDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType, selectedCategory), "ALL");
            PopulateDropdown(saveClassDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", selectedType, selectedCategory), "ALL");
            UpdateInterfaceBasedOnDropdownSelection();
        }

        public void OnClassChanged(int index)
        {
            UpdateInterfaceBasedOnDropdownSelection();
        }

        public void UpdatePresetDropdown()
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

        public void PopulatePresetDropdown(TMPro.TMP_Dropdown dropdown, string path, string classAbbreviation)
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

        public string ConvertToReadableName(string fileName)
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

        
        public void SetDropdownByValue(TMP_Dropdown dropdown, string value)
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

        
        public string GetActualValueFromDropdown(TMPro.TMP_Dropdown dropdown)
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

        public List<string> GetJsonFiles(string path)
        {
            if (Directory.Exists(path))
            {
                return Directory.GetFiles(path, "*.json", SearchOption.AllDirectories)
                                .Select(file => Path.GetFileNameWithoutExtension(file))
                                .ToList();
            }
            return new List<string>();
        }

        public void UpdateInterfaceBasedOnDropdownSelection()
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
            sliderKeyboardControl.RefreshSliders();
            characterBuilder.LoadSkeletonBasedOnSelection();
        }

        public void UpdateSlidersBasedOnSelection()
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
                sliderKeyboardControl.RefreshSliders();
            }
            else
            {
                Debug.LogError("Filter set not found for category: " + categoryKey);
            }
        }

        public void PopulateSliders(string path)
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

        public void PopulateSlidersWithFilters(string basePath, List<string> filters)
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
            sliderKeyboardControl.RefreshSliders();
        }

       

        public void ClearExistingSliders()
        {
            foreach (Transform child in slidersPanel.transform)
            {
                Destroy(child.gameObject);
            }
        }

        public void CreateSliderForSlot(string slotName, string path)
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

        public void InitializePrimarySlider(string slotName, int meshesCount)
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

            // Set label text only for the slider
            TextMeshProUGUI labelText = sliderObject.GetComponentInChildren<TextMeshProUGUI>();
            if (labelText != null && sliderObject.name.Contains("Slider"))
            {
                labelText.text = slotName.Replace("ALL_", "");
            }
        }


        public void OnSliderValueChanged(string slotName, float value, bool userChanged)
        {
            if (userChanged)
            {
                sliderValues[slotName] = value;
                currentSlider = slotName;
            }

            if (value == 0)
            {
                characterBuilder.RemoveModelAndVariationSlider(slotName);
                selectedVariationIndexes.Remove(slotName);
                Debug.Log($"OnSliderValueChanged: Slot '{slotName}' removed. Updated selectedVariationIndexes: {string.Join(", ", selectedVariationIndexes.Keys)}");
                variationBuilder.UpdateModelInfoPanel(null);
            }
            else
            {
                int modelIndex = Mathf.Clamp((int)(value - 1), 0, int.MaxValue);
                characterBuilder.LoadModelAndCreateVariationSlider(slotName, modelIndex);
                variationBuilder.UpdateModelInfoPanel(currentSlider);
            }
        }

        public void CreateOrUpdateVariationSlider(string slotName, string modelName)
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

        public void CreateVariationSlider(string slotName, int variationCount)
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
        }

        public void OnVariationSliderValueChanged(string slotName, float value)
        {
            //Debug.Log($"OnVariationSliderValueChanged for slot: {slotName} with value: {value}");
            if (!characterBuilder.currentlyLoadedModels.TryGetValue(slotName, out GameObject currentModel))
            {
                Debug.LogError($"No model currently loaded for slot: {slotName}");
                return;
            }

            if (value == 0 && characterBuilder.originalMaterials.TryGetValue(slotName, out List<Material> mats))
            {
                characterBuilder.ApplyOriginalMaterials(currentModel, mats);
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
                        characterBuilder.ApplyVariationMaterials(currentModel, variationResources);
                    }
                }
            }
            else
            {
                Debug.LogError("Material JSON file not found: " + materialJsonFilePath);
            }
            string currentSlider = slotName;
            Debug.Log($"OnVariationSliderValueChanged: currentSlider {currentSlider}");
            variationBuilder.UpdateModelInfoPanel(currentSlider);
            selectedVariationIndexes[slotName] = Mathf.Clamp((int)value - 1, 0, int.MaxValue);

            // Debug log to show the actual stored index for each slot
            Debug.Log("OnVariationSliderValueChanged:");
            foreach (var kvp in selectedVariationIndexes)
            {
                Debug.Log($"Slot: {kvp.Key}, Stored Index: {kvp.Value}");
            }
        }

        public void RemoveVariationSlider(string slotName)
        {
            Transform existingVariationSlider = slidersPanel.transform.Find(slotName + "VariationSlider");
            if (existingVariationSlider != null) Destroy(existingVariationSlider.gameObject);
        }

        public int FindSliderIndex(string sliderName)
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

               

        public string GetModelNameFromIndex(string slotName, int modelIndex)
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
                    return characterBuilder.GetModelName(meshName);
                }
            }

            Debug.LogError("Slot JSON file not found: " + slotJsonFilePath);
            return null;
        }

        public Dictionary<string, float> GetSliderValues()
        {
            return sliderValues;
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

        public void OnPresetLoadButtonPressed()
        {
            string selectedPreset = GetActualValueFromDropdown(presetDropdown);
            saveName.text = selectedPreset;
            if (!string.IsNullOrEmpty(selectedPreset))
            {
                // Assuming GetTypeFromSelector and categoryDropdown provide correct values
                string type = GetTypeFromSelector();
                string category = categoryDropdown.options[categoryDropdown.value].text;

                // Build the correct path without appending the class as a directory
                string presetsPath = Path.Combine(Application.streamingAssetsPath, "Jsons", type.Equals("ALL") ? "" : type, category.Equals("ALL") ? "" : category, $"{selectedPreset}.json");

                Debug.Log("Loading JSON from path: " + presetsPath);
                characterBuilder.LoadJsonAndSetSliders(presetsPath);
            }
            else
            {
                Debug.LogError("Selected preset is invalid or not found");
            }
        }
    }
}