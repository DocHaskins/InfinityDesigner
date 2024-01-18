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
    public Slider genderSlider;
    public GameObject slidersPanel;
    public GameObject sliderPrefab;
    public GameObject loadedSkeleton;
    public GameObject buttonPrefab;
    public Transform subButtonsPanel;
    public Button bodyButton;
    public Button armorButton;
    public Button clothesButton;

    private Dictionary<string, List<string>> slotData = new Dictionary<string, List<string>>();
    private Dictionary<string, GameObject> currentlyLoadedModels = new Dictionary<string, GameObject>();
    private Dictionary<string, List<string>> filterSets = new Dictionary<string, List<string>>
{
    { "BodyButton", new List<string> { "ALL_head", "ALL_facial_hair", "ALL_hair", "ALL_hands" } },
    { "ArmorButton", new List<string> { "ALL_armor_helmet", "ALL_armor_torso", "ALL_armor_torso_lowerleft", "ALL_armor_torso_lowerright", "ALL_armor_torso_upperleft", "ALL_armor_torso_upperright" } },
    { "ClothesButton", new List<string> { "ALL_backpack", "ALL_cape", "ALL_decals", "ALL_earrings", "ALL_glasses", "ALL_gloves", "ALL_hands", "ALL_hat", "ALL_leg_access", "ALL_legs", "ALL_mask", "ALL_necklace", "ALL_rings", "ALL_shoes", "ALL_sleeve", "ALL_torso", "ALL_torso_access" } },
    { "HeadButton", new List<string> { "ALL_head"} },
    { "HairButton", new List<string> { "ALL_hair"} },
    { "HatButton", new List<string> { "ALL_hat" } },
    { "MaskButton", new List<string> { "ALL_mask" } },
    { "GlassesButton", new List<string> { "ALL_glasses" } },
    { "NecklaceButton", new List<string> { "ALL_necklace" } },
    { "EarringsButton", new List<string> { "ALL_earrings" } },
    { "RingsButton", new List<string> { "ALL_rings" } },
    { "FacialHairButton", new List<string> { "ALL_facial_hair" } },
    { "CapeButton", new List<string> { "ALL_cape" } },
    { "TorsoButton", new List<string> { "ALL_torso" } },
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
};

    void Start()
    {
        LoadSlotData();
        genderSlider.value = 0;
        PopulateSliders();
        UpdateInterfaceBasedOnGender();
        UpdateCameraTarget(loadedSkeleton.transform);
        genderSlider.onValueChanged.AddListener(delegate { PopulateSliders(); });

        CreateDynamicButtons();
        if (bodyButton != null) bodyButton.onClick.AddListener(() => FilterCategory("BodyButton"));
        if (armorButton != null) armorButton.onClick.AddListener(() => FilterCategory("ArmorButton"));
        if (clothesButton != null) clothesButton.onClick.AddListener(() => FilterCategory("ClothesButton"));
    }

    void CreateDynamicButtons(List<string> filters = null)
    {
        if (subButtonsPanel == null || buttonPrefab == null)
        {
            return; // Early exit if essential components are missing
        }

        // Clear existing buttons in the panel
        foreach (Transform child in subButtonsPanel)
        {
            Destroy(child.gameObject);
        }

        string currentGender = GetCurrentGender();

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
                    Debug.LogWarning($"Image not found for  '{imageName}'");
                }

                Button buttonComponent = newButton.GetComponent<Button>();
                string slotName = slot; // Capture slot in local variable
                buttonComponent.onClick.AddListener(() => FilterSlidersForSlot(slotName));
            }
        }
    }

    void FilterCategory(string categoryKey)
    {
        if (filterSets.TryGetValue(categoryKey, out List<string> filters))
        {
            // Filter sliders
            PopulateSliders(filters);

            // Filter sub-buttons
            CreateDynamicButtons(filters);
        }
    }

    void UpdateInterfaceBasedOnGender()
    {
        string currentGender = GetCurrentGender();
        List<string> currentSlots = slotData[currentGender];

        PopulateSliders(currentSlots);
        CreateDynamicButtons(currentSlots);
    }

    string GetCurrentGender()
    {
        // Your logic to determine the current gender
        return genderSlider.value == 0 ? "Man" : "Wmn";
    }

    public void OnHeadButtonClick()
    {
        FilterSliders("HeadButton");
    }

    public void OnArmorButtonClick()
    {
        FilterSliders("ArmorButton");
    }

    void FilterSliders(string filterSetKey)
    {
        if (filterSets.TryGetValue(filterSetKey, out List<string> filters))
        {
            PopulateSliders(filters);
        }
    }

    void FilterSlidersForSlot(string slotName)
    {
        ClearExistingSliders();

        // Check if the slotName is in the current gender's slot data
        string currentGender = GetCurrentGender();
        if (slotData[currentGender].Contains(slotName))
        {
            CreateSliderForSlot(slotName);
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

    void LoadSlotData()
    {
        LoadSlotDataForGender("Man");
        LoadSlotDataForGender("Wmn");
    }

    void LoadSlotDataForGender(string gender)
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Slotdata", gender);
        string[] files = Directory.GetFiles(path, "*.json");
        List<string> slots = new List<string>();

        foreach (var file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            slots.Add(fileName);
        }

        slotData[gender] = slots;
    }

    void PopulateSliders(List<string> filter = null)
    {
        string selectedGender = genderSlider.value == 0 ? "Man" : "Wmn";
        List<string> slots = slotData[selectedGender];

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
        string selectedGender = genderSlider.value == 0 ? "Man" : "Wmn";
        string jsonFilePath = Path.Combine(Application.streamingAssetsPath, "Slotdata", selectedGender, slotName + ".json");

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
            slider.minValue = 0; // 0 for 'off'
            slider.maxValue = meshesCount; // Start from 1, not 0
            slider.wholeNumbers = true;
            slider.onValueChanged.AddListener(delegate { OnSliderValueChanged(slotName, slider.value); });
        }

        TextMeshProUGUI labelText = sliderObject.GetComponentInChildren<TextMeshProUGUI>();
        if (labelText != null)
        {
            // Filter out "ALL_" from the slotName for display
            labelText.text = slotName.Replace("ALL_", "");
        }
    }

    void OnSliderValueChanged(string slotName, float value)
    {
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
        string selectedGender = genderSlider.value == 0 ? "Man" : "Wmn";
        string jsonFilePath = Path.Combine(Application.streamingAssetsPath, "Slotdata", selectedGender, slotName + ".json");

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