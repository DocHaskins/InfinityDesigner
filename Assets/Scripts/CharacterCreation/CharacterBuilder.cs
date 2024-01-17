using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using Cinemachine;
using TMPro;
using UnityEngine.UI;
using static ModelData;

public class CharacterBuilder : MonoBehaviour
{
    public CinemachineCameraZoomTool cameraTool;
    public Slider genderSlider;
    public GameObject slidersPanel;
    public GameObject sliderPrefab;
    public GameObject loadedSkeleton;
    private Dictionary<string, List<string>> slotData = new Dictionary<string, List<string>>();
    private Dictionary<string, GameObject> currentlyLoadedModels = new Dictionary<string, GameObject>();

    void Start()
    {
        LoadSlotData();
        genderSlider.value = 0;
        PopulateSliders();
        UpdateCameraTarget(loadedSkeleton.transform);
        genderSlider.onValueChanged.AddListener(delegate { PopulateSliders(); });
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

    void PopulateSliders()
    {
        string selectedGender = genderSlider.value == 0 ? "Man" : "Wmn";
        List<string> slots = slotData[selectedGender];

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
            labelText.text = slotName;
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