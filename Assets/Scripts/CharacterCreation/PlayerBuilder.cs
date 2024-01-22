﻿using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using Cinemachine;
using TMPro;
using UnityEngine.UI;
using static ModelData;
using System.Linq;
using doppelganger;

namespace doppelganger
{
    public class PlayerBuilder : MonoBehaviour
    {
        public CinemachineCameraZoomTool cameraTool;
        public RuntimeJsonLoader runtimeJsonLoader;
        private string currentType = "Player";
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
        private Dictionary<string, float> sliderValues = new Dictionary<string, float>();
        private Dictionary<string, bool> sliderInitialized = new Dictionary<string, bool>();
        private Dictionary<GameObject, List<int>> disabledRenderers = new Dictionary<GameObject, List<int>>();
        private Dictionary<GameObject, bool[]> initialRendererStates = new Dictionary<GameObject, bool[]>();
        private Dictionary<string, List<Material>> originalMaterials = new Dictionary<string, List<Material>>();
        private Dictionary<string, List<string>> slotData = new Dictionary<string, List<string>>();
        private Dictionary<string, GameObject> currentlyLoadedModels = new Dictionary<string, GameObject>();

        void Start()
        {
            if (runtimeJsonLoader == null)
            {
                runtimeJsonLoader = GameObject.FindObjectOfType<RuntimeJsonLoader>();
            }

            if (runtimeJsonLoader != null)
            {
                runtimeJsonLoader.LoadModelFromJson("player_tpp_skeleton.json");
            }
            else
            {
                Debug.LogError("RuntimeJsonLoader not found.");
            }
        }

        void CreateDynamicButtons(List<string> filters = null)
        {
            if (subButtonsPanel == null || buttonPrefab == null)
            {
                Debug.LogError("CreateDynamicButtons: subButtonsPanel or buttonPrefab is null");
                return;
            }

            foreach (Transform child in subButtonsPanel)
            {
                Destroy(child.gameObject);
            }

            string currentGender = GetCurrentType();

            if (slotData.TryGetValue(currentGender, out List<string> slots))
            {
                // Sort the slots list
                slots.Sort();

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

        void UpdateInterfaceBasedOnType()
        {
            string currentType = GetCurrentType();
            Debug.Log($"Current type: {currentType}");
            List<string> currentSlots = slotData[currentType];

            if (currentSlots == null || currentSlots.Count == 0)
            {
                Debug.LogError($"UpdateInterfaceBasedOnType: No slots found for type {currentType}");
                return;
            }

            // Always update sliders and buttons for the new type, ignoring any previous filter
            PopulateSliders(currentSlots);
            CreateDynamicButtons(currentSlots);
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

            LayoutRebuilder.ForceRebuildLayoutImmediate(slidersPanel.GetComponent<RectTransform>());
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
            string slotJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Slotdata", selectedType, slotName + ".json");

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
            string selectedType = GetCurrentType();
            string slotJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Slotdata", selectedType, slotName + ".json");

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
            string selectedType = GetCurrentType();
            string slotJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Slotdata", selectedType, slotName + ".json");

            if (File.Exists(slotJsonFilePath))
            {
                string slotJsonData = File.ReadAllText(slotJsonFilePath);
                SlotModelData slotModelData = JsonUtility.FromJson<SlotModelData>(slotJsonData);

                if (slotModelData != null && slotModelData.meshes != null && slotModelData.meshes.Count > modelIndex)
                {
                    string meshName = slotModelData.meshes[modelIndex];
                    string modelName = GetModelName(meshName);

                    RemoveModelAndVariationSlider(slotName);
                    GameObject modelInstance = LoadModelPrefab(modelName, slotName);
                    LoadAndApplyMaterials(modelName, modelInstance, slotName);
                    CreateOrUpdateVariationSlider(slotName, modelName);
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

        void LoadAndApplyMaterials(string modelName, GameObject modelInstance, string slotName)
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

        GameObject LoadModelPrefab(string modelName, string slotName) // Add slotName as parameter
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

    }
}