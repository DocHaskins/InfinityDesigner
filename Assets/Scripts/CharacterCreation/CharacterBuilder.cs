using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using Cinemachine;
using TMPro;
using UnityEngine.UI;
using static ModelData;
using System.Linq;

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
        private string currentType = "Player";
        private string currentPath;
        public TMP_Dropdown typeDropdown;
        public TMP_Dropdown categoryDropdown;
        public TMP_Dropdown classDropdown;
        public GameObject slidersPanel;
        public GameObject sliderPrefab;
        public GameObject variationSliderPrefab;
        public GameObject loadedSkeleton;
        public GameObject buttonPrefab;
        public Transform subButtonsPanel;
        public Button bodyButton;
        public Button armorButton;
        public Button clothesButton;

        private string lastFilterCategoryKey = "";
        public Dictionary<string, float> sliderValues = new Dictionary<string, float>();
        private Dictionary<string, bool> sliderInitialized = new Dictionary<string, bool>();
        private List<string> modelNamesToFind = new List<string>();
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
            UpdateCameraTarget(loadedSkeleton.transform);

            PopulateDropdown(typeDropdown, Application.streamingAssetsPath + "/SlotData", "Human");
            PopulateDropdown(categoryDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", "Human"), "ALL", true);
            PopulateDropdown(classDropdown, Path.Combine(Application.streamingAssetsPath, "SlotData", "Human", "Player"), "ALL");

            // Add listeners to dropdowns
            typeDropdown.onValueChanged.AddListener(OnTypeChanged);
            categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
            classDropdown.onValueChanged.AddListener(OnClassChanged);

            // Set up button listeners
            if (bodyButton != null) bodyButton.onClick.AddListener(() => FilterCategory("BodyButton"));
            if (armorButton != null) armorButton.onClick.AddListener(() => FilterCategory("ArmorButton"));
            if (clothesButton != null) clothesButton.onClick.AddListener(() => FilterCategory("ClothesButton"));
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

        public string FindSlotForModel(string modelName)
        {
            Debug.Log($"FindSlotForModel for {modelName}");
            string genderProperty = GetCurrentType();

            // Construct the correct path using the gender property
            string genderFolderPath = Path.Combine(Application.streamingAssetsPath, "SlotData", genderProperty);

            // Check if the gender folder path exists
            if (!Directory.Exists(genderFolderPath))
            {
                Debug.LogError($"Gender folder not found: {genderFolderPath}");
                return null;
            }

            // Iterate through all slot JSON files within the gender folder
            foreach (var slotFile in Directory.GetFiles(genderFolderPath, "*.json"))
            {
                string slotJsonData = File.ReadAllText(slotFile);
                //Debug.Log($"searching {slotFile} for {modelName}");

                try
                {
                    SlotModelData slotModelData = JsonUtility.FromJson<SlotModelData>(slotJsonData);

                    foreach (string meshName in slotModelData.meshes)
                    {
                        // Case-insensitive comparison
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
            Debug.Log($"GetModelIndex for slot {slotName} and model {modelName}");
            string genderProperty = GetCurrentType();
            string slotJsonFilePath = Path.Combine(Application.streamingAssetsPath, "SlotData", genderProperty, slotName + ".json");
            modelName = modelName.Trim(); // Trim any leading or trailing whitespace

            if (File.Exists(slotJsonFilePath))
            {
                string slotJsonData = File.ReadAllText(slotJsonFilePath);
                SlotModelData slotModelData = JsonUtility.FromJson<SlotModelData>(slotJsonData);

                for (int i = 0; i < slotModelData.meshes.Count; i++)
                {
                    // Case-insensitive comparison
                    if (string.Equals(slotModelData.meshes[i].Trim(), modelName, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"Model {modelName} found at index {i} in slot {slotName}");
                        return i;
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
            Debug.Log($"SetSliderValue for {sliderName} with model index {modelIndex}");
            int sliderIndex = FindSliderIndex(sliderName);
            if (sliderIndex != -1)
            {
                Transform sliderTransform = slidersPanel.transform.GetChild(sliderIndex);
                Slider slider = sliderTransform.GetComponentInChildren<Slider>();
                if (slider != null)
                {
                    slider.value = modelIndex;
                    Debug.Log($"Set slider value for {sliderName} to {modelIndex}");
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

        public string GetSelectedType()
        {
            return typeDropdown.options[typeDropdown.value].text;
        }

        public string GetSelectedCategory()
        {
            return categoryDropdown.options[categoryDropdown.value].text;
        }

        public string GetSelectedClass()
        {
            return classDropdown.options[classDropdown.value].text;
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
            // Check if modelInstance and modelInfo are not null
            if (modelInstance == null || modelInfo == null || modelInfo.materialsData == null)
            {
                Debug.LogError("ApplyMaterials: modelInstance or modelInfo is null.");
                return;
            }

            var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            // Store initial state
            if (!initialRendererStates.ContainsKey(modelInstance))
            {
                initialRendererStates[modelInstance] = skinnedMeshRenderers.Select(r => r.enabled).ToArray();
            }

            foreach (var materialData in modelInfo.materialsData)
            {
                // Check if materialData is not null
                if (materialData == null)
                {
                    Debug.LogError("Material data is null.");
                    continue;
                }

                int rendererIndex = materialData.number - 1;
                if (rendererIndex >= 0 && rendererIndex < skinnedMeshRenderers.Length)
                {
                    var renderer = skinnedMeshRenderers[rendererIndex];
                    ApplyMaterialToRenderer(renderer, materialData.name, modelInstance);
                }
                else
                {
                    Debug.LogError($"Renderer index out of bounds: {rendererIndex} for material number {materialData.number} in model '{modelInfo.name}'");
                }
            }
        }

        private void ApplyMaterialToRenderer(SkinnedMeshRenderer renderer, string materialName, GameObject modelInstance)
        {
            if (materialName.Equals("null.mat", StringComparison.OrdinalIgnoreCase))
            {
                renderer.enabled = false;
                if (!disabledRenderers.ContainsKey(modelInstance))
                {
                    disabledRenderers[modelInstance] = new List<int>();
                }
                disabledRenderers[modelInstance].Add(Array.IndexOf(modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true), renderer));
                return;
            }
            else
            {
                renderer.enabled = true;
            }

            if (materialName.Equals(ShouldDisableRenderer(renderer.gameObject.name)))
            {
                renderer.enabled = false;
            }

            if (materialName.StartsWith("sm_"))
            {
                Debug.Log($"Skipped material '{materialName}' as it starts with 'sm_'");
                return;
            }

            string matPath = "materials/" + Path.GetFileNameWithoutExtension(materialName);
            Material loadedMaterial = Resources.Load<Material>(matPath);

            if (loadedMaterial != null)
            {
                Material[] rendererMaterials = renderer.sharedMaterials;
                if (rendererMaterials.Length > 0)
                {
                    rendererMaterials[0] = loadedMaterial;
                    renderer.sharedMaterials = rendererMaterials;
                }
                else
                {
                    Debug.LogError($"Renderer {renderer.gameObject.name} has no materials to apply to");
                }
            }
            else
            {
                Debug.LogError($"Material not found: '{matPath}' for renderer '{renderer.gameObject.name}'");
            }
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