using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using Cinemachine;
using TMPro;
using UnityEngine.UI;
using static ModelData;
using System.Linq;

public class CharacterBuilder : MonoBehaviour
{
    public CinemachineCameraZoomTool cameraTool;
    public Button TypeManButton, TypeWmnButton, TypePlayerButton, TypeInfectedButton, TypeChildButton;
    private string currentType = "Man";
    public GameObject slidersPanel;
    public GameObject sliderPrefab;
    public GameObject loadedSkeleton;
    public GameObject buttonPrefab;
    public Transform subButtonsPanel;
    public Button bodyButton;
    public Button armorButton;
    public Button clothesButton;

    private string lastFilterCategoryKey = "";
    private Dictionary<string, float> sliderValues = new Dictionary<string, float>();
    private Dictionary<string, bool> sliderInitialized = new Dictionary<string, bool>();
    private Dictionary<string, List<string>> slotData = new Dictionary<string, List<string>>();
    private Dictionary<string, GameObject> currentlyLoadedModels = new Dictionary<string, GameObject>();
    private Dictionary<string, List<string>> filterSets = new Dictionary<string, List<string>>
{
    { "BodyButton", new List<string> { "ALL_head", "ALL_facial_hair", "ALL_hair", "ALL_hair_base", "ALL_hair_2", "ALL_hair_3", "ALL_hands", "ALL_tattoo" } },
    { "ArmorButton", new List<string> { "ALL_armor_helmet", "ALL_armor_torso", "ALL_armor_torso_lowerleft", "ALL_armor_torso_lowerright", "ALL_armor_torso_upperleft", "ALL_armor_torso_upperright", "armor_legs", "armor_legs_upperright", "armor_legs_upperleft", "armor_legs_lowerright", "armor_legs_lowerleft" } },
    { "ClothesButton", new List<string> { "ALL_backpack", "ALL_cape", "ALL_decals", "ALL_earrings", "ALL_glasses", "ALL_gloves", "ALL_hat", "ALL_leg_access", "ALL_legs", "ALL_mask", "ALL_necklace", "ALL_rings", "ALL_shoes", "ALL_sleeve", "ALL_torso", "ALL_torso_extra", "ALL_torso_access" } },
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
        LoadSlotData();
        UpdateInterfaceBasedOnType();
        UpdateCameraTarget(loadedSkeleton.transform);

        // Set up type button listeners
        TypeManButton.onClick.AddListener(() => SetCurrentType("Man"));
        TypeWmnButton.onClick.AddListener(() => SetCurrentType("Wmn"));
        TypePlayerButton.onClick.AddListener(() => SetCurrentType("Player"));
        TypeInfectedButton.onClick.AddListener(() => SetCurrentType("Infected"));
        TypeChildButton.onClick.AddListener(() => SetCurrentType("Child"));

        // Set up button listeners
        if (bodyButton != null) bodyButton.onClick.AddListener(() => FilterCategory("BodyButton"));
        if (armorButton != null) armorButton.onClick.AddListener(() => FilterCategory("ArmorButton"));
        if (clothesButton != null) clothesButton.onClick.AddListener(() => FilterCategory("ClothesButton"));
    }

    void CreateDynamicButtons(List<string> filters = null)
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

        string currentGender = GetCurrentType();

        if (slotData.TryGetValue(currentGender, out List<string> slots))
        {
            foreach (string slot in slots)
            {
                if (!slot.StartsWith("ALL_") || (filters != null && !filters.Contains(slot)))
                {
                    continue; // Skip slots that do not match filters
                }

                GameObject newButton = Instantiate(buttonPrefab, subButtonsPanel);

                string imageName = "Character_Builder_" + slot.Replace("ALL_", "");
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
                string slotName = slot; // Capture slot in local variable
                buttonComponent.onClick.AddListener(() => FilterSlidersForSlot(slotName));
            }
        }
        else
        {
            Debug.LogError($"CreateDynamicButtons: No slot data found for gender {currentGender}");
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

    void FilterCategory(string categoryKey)
    {
        lastFilterCategoryKey = categoryKey; // Store the selected filter category key

        if (filterSets.TryGetValue(categoryKey, out List<string> filters))
        {
            PopulateSliders(filters);
            CreateDynamicButtons(filters);
        }
    }

    void SetCurrentType(string type)
    {
        currentType = type;
        UpdateInterfaceBasedOnType();
    }

    void UpdateInterfaceBasedOnType()
    {
        string currentType = GetCurrentType();
        Debug.Log($"Current type: {currentType}");
        List<string> currentSlots = slotData[currentType];

        if (currentSlots == null || currentSlots.Count == 0)
        {
            Debug.LogError($"UpdateInterfaceBasedOnGender: No slots found for gender {currentType}");
            return;
        }

        // If no filter category has been selected yet, update sliders and buttons normally
        if (string.IsNullOrEmpty(lastFilterCategoryKey))
        {
            PopulateSliders(currentSlots);
            CreateDynamicButtons(currentSlots);
        }
    }

    string GetCurrentType()
    {
        return currentType;
    }

    void FilterSlidersForSlot(string slotName)
    {
        ClearExistingSliders();

        // Check if the slotName is in the current gender's slot data
        string currentGender = GetCurrentType();
        if (slotData[currentGender].Contains(slotName))
        {
            CreateSliderForSlot(slotName);
        }
        else
        {
            Debug.LogWarning($"FilterSlidersForSlot: Slot '{slotName}' not found in gender '{currentGender}' data");
        }
    }

    void LoadSlotData()
    {
        LoadSlotDataForType("Man");
        LoadSlotDataForType("Wmn");
        LoadSlotDataForType("Infected");
        LoadSlotDataForType("Child");
        LoadSlotDataForType("Player");
    }

    void LoadSlotDataForType(string type)
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Slotdata", type);
        string[] files = Directory.GetFiles(path, "*.json");
        List<string> slots = new List<string>();

        foreach (var file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            slots.Add(fileName);
        }

        slotData[type] = slots;
    }

    void PopulateSliders(List<string> filter = null)
    {
        string selectedType = GetCurrentType();
        if (!slotData.ContainsKey(selectedType))
        {
            Debug.LogError($"No slot data found for type: {selectedType}");
            return;
        }

        List<string> slots = slotData[selectedType];

        // Filter slots to only include those starting with "ALL_"
        slots = slots.Where(slot => slot.StartsWith("ALL_")).ToList();

        if (filter != null)
        {
            slots = slots.Where(slot => filter.Contains(slot)).ToList();
        }

        ClearExistingSliders();

        foreach (var slot in slots)
        {
            CreateSliderForSlot(slot);
        }
    }

    void ClearExistingSliders()
    {
        foreach (Transform child in slidersPanel.transform)
        {
            Destroy(child.gameObject);
        }
    }

    void CreateSliderForSlot(string slotName)
    {
        string selectedType = GetCurrentType();
        string jsonFilePath = Path.Combine(Application.streamingAssetsPath, "Slotdata", selectedType, slotName + ".json");

        int meshesCount = 0;
        if (File.Exists(jsonFilePath))
        {
            string jsonData = File.ReadAllText(jsonFilePath);
            SlotModelData slotModelData = JsonUtility.FromJson<SlotModelData>(jsonData);
            if (slotModelData != null && slotModelData.meshes != null)
            {
                meshesCount = slotModelData.meshes.Count;
            }
        }

        GameObject sliderObject = Instantiate(sliderPrefab, slidersPanel.transform, false);
        sliderObject.name = slotName + "Slider";

        Slider slider = sliderObject.GetComponentInChildren<Slider>();
        if (slider != null)
        {
            // Set the slider value to either the stored value or default to 0
            slider.minValue = 0; // 0 for 'off'
            slider.maxValue = meshesCount; // Start from 1, not 0
            slider.wholeNumbers = true;

            float sliderValue = sliderValues.ContainsKey(slotName) ? sliderValues[slotName] : 0;
            slider.value = sliderValue;
            slider.onValueChanged.AddListener(delegate { OnSliderValueChanged(slotName, slider.value, true); });

            // Mark the slider as initialized
            sliderInitialized[slotName] = true;
        }

        TextMeshProUGUI labelText = sliderObject.GetComponentInChildren<TextMeshProUGUI>();
        if (labelText != null)
        {
            // Filter out "ALL_" from the slotName for display
            labelText.text = slotName.Replace("ALL_", "");
        }
    }

    void OnSliderValueChanged(string slotName, float value, bool userChanged)
    {
        if (userChanged)
        {
            // Update the slider value in the dictionary only if the change is made by the user
            sliderValues[slotName] = value;
        }

        if (value == 0)
        {
            // Unload the current model
            if (currentlyLoadedModels.TryGetValue(slotName, out GameObject currentModel))
            {
                Destroy(currentModel);
                currentlyLoadedModels.Remove(slotName);
            }
        }
        else
        {
            // Adjust the index since our slider now starts from 1
            LoadModelFromJson(slotName, value - 1);
        }
    }

    void LoadModelFromJson(string slotName, float value)
    {
        string selectedType = GetCurrentType();
        string jsonFilePath = Path.Combine(Application.streamingAssetsPath, "Slotdata", selectedType, slotName + ".json");

        if (File.Exists(jsonFilePath))
        {
            string jsonData = File.ReadAllText(jsonFilePath);
            SlotModelData slotModelData = JsonUtility.FromJson<SlotModelData>(jsonData);

            if (slotModelData != null && slotModelData.meshes != null && slotModelData.meshes.Count > 0)
            {
                int modelIndex = Mathf.Clamp((int)value, 0, slotModelData.meshes.Count - 1);
                string meshName = slotModelData.meshes[modelIndex];
                string modelName = meshName.EndsWith(".msh") ? meshName.Substring(0, meshName.Length - 4) : meshName;

                if (currentlyLoadedModels.TryGetValue(slotName, out GameObject currentModel))
                {
                    Destroy(currentModel);
                    currentlyLoadedModels.Remove(slotName);
                }

                string prefabPath = Path.Combine("Prefabs", modelName);
                GameObject prefab = Resources.Load<GameObject>(prefabPath);

                if (prefab != null)
                {
                    GameObject modelInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    currentlyLoadedModels[slotName] = modelInstance;

                    // Load and apply materials
                    string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", modelName + ".json");
                    if (File.Exists(materialJsonFilePath))
                    {
                        string materialJsonData = File.ReadAllText(materialJsonFilePath);
                        ModelData.ModelInfo modelInfo = JsonUtility.FromJson<ModelData.ModelInfo>(materialJsonData);
                        ApplyMaterials(modelInstance, modelInfo);
                    }
                    else
                    {
                        Debug.LogError("Material JSON file not found: " + materialJsonFilePath);
                    }
                }
                else
                {
                    Debug.LogError("Prefab not found: " + prefabPath);
                }
            }
            else
            {
                Debug.LogError("Mesh data is null or empty for slot: " + slotName);
            }
        }
        else
        {
            Debug.LogError("JSON file not found: " + jsonFilePath);
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
        var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true); // Include inactive

        foreach (var materialData in modelInfo.materialsData)
        {
            int rendererIndex = materialData.number - 1;
            if (rendererIndex >= 0 && rendererIndex < skinnedMeshRenderers.Length)
            {
                var renderer = skinnedMeshRenderers[rendererIndex];
                ApplyMaterialToRenderer(renderer, materialData.name);
            }
            else
            {
                Debug.LogError($"Renderer index out of bounds: {rendererIndex} for material number {materialData.number} in model '{modelInfo.name}'");
            }
        }
    }

    private void ApplyMaterialToRenderer(SkinnedMeshRenderer renderer, string materialName)
    {
        if (materialName.StartsWith("sm_"))
        {
            Debug.Log($"Skipped material '{materialName}' as it starts with 'sm_'");
            return;
        }

        if (materialName.Equals("null.mat", StringComparison.OrdinalIgnoreCase) || ShouldDisableRenderer(renderer.gameObject.name))
        {
            renderer.enabled = false;
            //Debug.Log($"Disabled SkinnedMeshRenderer on '{renderer.gameObject.name}' due to null.mat or renderer should be disabled");
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

    private void DestroyObject(GameObject obj)
    {
        Destroy(obj);
    }
}