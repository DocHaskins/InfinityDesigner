using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using Cinemachine;
using UnityEngine.UI;
using static ModelData;
using System.Linq;
using System.Text;

/// <summary>
/// This script utilizes a combination of JSON data for model properties and dynamically generated UI elements to allow users to build and customize characters in real-time.
/// </summary>

namespace doppelganger
{
    public class CharacterBuilder : MonoBehaviour
    {
        [Header("Managers")]
        public Platform platform;
        public CharacterBuilder_InterfaceManager interfaceManager;
        public ScreenshotManager screenshotManager;
        public SkeletonLookup skeletonLookup;
        public FilterMapping filterMapping;
        public VariationBuilder variationBuilder;
        public AutoTargetCinemachineCamera autoTargetCinemachineCamera;

        public bool HasMaterialIndicesChanged { get; private set; }
        private Dictionary<GameObject, List<int>> disabledRenderers = new Dictionary<GameObject, List<int>>();
        private Dictionary<GameObject, bool[]> initialRendererStates = new Dictionary<GameObject, bool[]>();
        public Dictionary<string, List<Material>> originalMaterials = new Dictionary<string, List<Material>>();
        public Dictionary<string, GameObject> currentlyLoadedModels = new Dictionary<string, GameObject>();
        public Dictionary<int, SkinnedMeshRenderer> materialIndexToRendererMap = new Dictionary<int, SkinnedMeshRenderer>();
        public Dictionary<int, SkinnedMeshRenderer> correctedMaterialIndexToRendererMap = new Dictionary<int, SkinnedMeshRenderer>();
        Dictionary<int, SkinnedMeshRenderer> finalSortedMaterialIndexToRendererMap = new Dictionary<int, SkinnedMeshRenderer>();
        List<ModelData.MaterialResource> unappliedMaterials = new List<ModelData.MaterialResource>();
        public Dictionary<string, Dictionary<int, int>> ModelIndexChanges { get; private set; } = new Dictionary<string, Dictionary<int, int>>();

        void Update()
        {
            if (Input.GetKey(KeyCode.F12))
            {
                CreateCrashReport();
            }
        }

        public void CreateCrashReport()
        {
            string crashReportsDirectory = Path.Combine(Application.dataPath, "Crash Reports");
            Directory.CreateDirectory(crashReportsDirectory);

            string dateTimeFormat = "yyyyMMdd_HHmmss";
            string reportFileName = $"CrashReport_{DateTime.Now.ToString(dateTimeFormat)}.txt";
            string reportFilePath = Path.Combine(crashReportsDirectory, reportFileName);

            StringBuilder reportContent = new StringBuilder();

            foreach (var pair in currentlyLoadedModels)
            {
                GameObject model = pair.Value;
                reportContent.AppendLine($"Model Key: {pair.Key}, GameObject Name: {model.name}");

                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        reportContent.AppendLine($"\tMaterial: {material.name}");
                    }
                }
                reportContent.AppendLine();
            }

            File.WriteAllText(reportFilePath, reportContent.ToString());

            Debug.Log($"Crash report created at: {reportFilePath}");
        }

        public void Reroll()
        {
            foreach (Transform sliderContainer in interfaceManager.slidersPanel.transform)
            {
                ChildLockToggle lockToggleScript = sliderContainer.Find("LockToggle")?.GetComponent<ChildLockToggle>();

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
                    interfaceManager.OnSliderValueChanged(slotName, randomValue, true);
                }
                else
                {
                    //Debug.LogWarning($"No slider component found in primarySlider of: {sliderContainer.name}");
                }
            }
        }

        public Dictionary<string, float> LoadSlotWeights()
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
            foreach (Transform sliderContainer in interfaceManager.slidersPanel.transform)
            {
                Transform primarySliderTransform = sliderContainer.Find("primarySlider");
                if (primarySliderTransform != null)
                {
                    Slider slider = primarySliderTransform.GetComponent<Slider>();
                    if (slider != null)
                    {
                        slider.value = 0;
                        string slotName = sliderContainer.name.Replace("Slider", "");
                        interfaceManager.OnSliderValueChanged(slotName, 0, true);
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

        public string GetJsonFilePath(string presetName)
        {
            string type = interfaceManager.GetTypeFromSelector();
            string category = interfaceManager.categoryDropdown.options[interfaceManager.categoryDropdown.value].text;
            string classSelection = interfaceManager.classDropdown.options[interfaceManager.classDropdown.value].text;
            string jsonsBasePath = Path.Combine(Application.streamingAssetsPath, "/Jsons");

            string path = jsonsBasePath;
            if (type != "ALL") path = Path.Combine(path, type);
            if (category != "ALL") path = Path.Combine(path, category);
            if (classSelection != "ALL") path = Path.Combine(path, classSelection);

            string jsonFilePath = Path.Combine(path, presetName + ".json");
            return jsonFilePath;
        }

        public void LoadJsonAndSetSliders(string jsonPath)
        {
            if (File.Exists(jsonPath))
            {
                //Debug.Log("JSON file found: " + jsonPath);
                string jsonData = File.ReadAllText(jsonPath);
                ModelData modelData = JsonUtility.FromJson<ModelData>(jsonData);

                if (modelData != null)
                {
                    string skeletonName = modelData.skeletonName;
                    if (!string.IsNullOrEmpty(skeletonName))
                    {
                        GameObject currentSkeleton = GameObject.FindGameObjectWithTag("Skeleton");
                        bool shouldLoadSkeleton = true;

                        if (currentSkeleton != null)
                        {
                            if (currentSkeleton.name == skeletonName || currentSkeleton.name == skeletonName + "(Clone)")
                            {
                                shouldLoadSkeleton = false;
                            }
                            else
                            {
                                Destroy(currentSkeleton);
                            }
                        }

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
                                string modelNameWithClone = Path.GetFileNameWithoutExtension(modelInfo.name) + "(Clone)";

                                //Debug.Log($"Processing model: {modelInfo.name}, looking for slot.");

                                string slotName = interfaceManager.FindSlotForModel(modelInfo.name);
                                if (!string.IsNullOrEmpty(slotName))
                                {
                                    //Debug.Log($"Found slot '{slotName}' for model '{modelInfo.name}'.");

                                    int modelIndex = interfaceManager.GetModelIndex(slotName, modelInfo.name);
                                    if (modelIndex != -1)
                                    {
                                        //Debug.Log($"Setting slider value for '{slotName}' at index {modelIndex}.");
                                        interfaceManager.SetSliderValue(slotName, modelIndex);

                                        string modelName = Path.GetFileNameWithoutExtension(modelInfo.name);
                                        GameObject modelInstance = GameObject.Find(modelNameWithClone);
                                        if (modelInstance != null)
                                        {
                                            ApplyPresetMaterialsDirectly(modelInstance, modelInfo, slotName, modelIndex);
                                        }
                                        else
                                        {
                                            Debug.LogWarning($"Model instance not found for {modelNameWithClone}");
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
                    Debug.LogWarning("Failed to deserialize JSON data");
                }
                string presetName = Path.GetFileNameWithoutExtension(jsonPath);
                interfaceManager.usedSliders.Clear();
                interfaceManager.currentPreset = presetName;
                interfaceManager.currentPresetPath = jsonPath;
                interfaceManager.currentPresetLabel.text = presetName;
                screenshotManager.SetCurrentScreenshot();

                if (interfaceManager.slidersPanel.transform.childCount > 0)
                {
                    GameObject firstSlider = interfaceManager.slidersPanel.transform.GetChild(0).gameObject;
                    string firstSliderName = firstSlider.name.Replace("Slider", "");
                    interfaceManager.currentSlider = firstSliderName;
                    variationBuilder.UpdateModelInfoPanel(interfaceManager.currentSlider);
                }
            }
            else
            {
                Debug.LogWarning("Preset JSON file not found: " + jsonPath);
            }
        }

        private void ApplyPresetMaterialsDirectly(GameObject modelInstance, ModelData.ModelInfo modelInfo, string slotName, int modelIndex)
        {
            //Debug.Log($"[ApplyPresetMaterialsDirectly] Model Name: {modelInfo.name}");
            //Debug.Log($"[ApplyPresetMaterialsDirectly] Materials Data Count: {modelInfo.materialsData.Count}");
            //foreach (var matData in modelInfo.materialsData)
            //{
            //    Debug.Log($"[ApplyPresetMaterialsDirectly] Material Data - Number: {matData.number}, Name: {matData.name}");
            //}
            //foreach (var matRes in modelInfo.materialsResources)
            //{
            //    Debug.Log($"[ApplyPresetMaterialsDirectly] Material Resource - Number: {matRes.number}, Resources Count: {matRes.resources.Count}");
            //    foreach (var res in matRes.resources)
            //    {
            //        Debug.Log($"[ApplyPresetMaterialsDirectly] Resource - Name: {res.name}, Selected: {res.selected}, LayoutId: {res.layoutId}, LoadFlags: {res.loadFlags}");
            //        foreach (var rtti in res.rttiValues)
            //        {
            //            Debug.Log($"[ApplyPresetMaterialsDirectly] RTTI Value - Name: {rtti.name}, Type: {rtti.type}, Value: {rtti.val_str}");
            //        }
            //    }
            //}

            if (modelInstance == null || modelInfo == null || modelInfo.materialsData == null || modelInfo.materialsResources == null)
            {
                Debug.LogWarning("[ApplyPresetMaterialsDirectly] Model instance, modelInfo, or its materials are null.");
                return;
            }

            var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            //Debug.Log($"[ApplyPresetMaterialsDirectly] Found {skinnedMeshRenderers.Length} SkinnedMeshRenderer components in the model.");

            var rendererNameToIndexMap = new Dictionary<string, int>();
            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                string formattedName = FormatRendererName(skinnedMeshRenderers[i].name, modelInstance.name.Replace("sh_", "").Replace("(Clone)", ""));
                rendererNameToIndexMap[formattedName] = i;
                //Debug.Log($"[ApplyPresetMaterialsDirectly] Mapping renderer '{skinnedMeshRenderers[i].name}' to formatted name '{formattedName}' at index {i}.");
            }

            for (int i = 0; i < skinnedMeshRenderers.Length && i < modelInfo.materialsData.Count; i++)
            {
                var materialData = modelInfo.materialsData[i];
                if (materialData == null)
                {
                    Debug.LogWarning($"[ApplyPresetMaterialsDirectly] Material data at index {i} is null.");
                    continue;
                }

                ModelData.MaterialResource materialResource = modelInfo.materialsResources.FirstOrDefault(mr => mr.number == i + 1);
                if (materialResource == null)
                {
                    Debug.LogWarning($"[ApplyPresetMaterialsDirectly] No material resource found for data index {i + 1}.");
                    continue;
                }

                //Debug.Log($"[ApplyPresetMaterialsDirectly] Applying material '{materialData.name}' to renderer at index {i} based on index matching.");
                foreach (var resource in materialResource.resources)
                {
                    ApplyMaterialToRenderer(skinnedMeshRenderers[i], resource.name, modelInstance, resource.rttiValues, slotName, modelIndex);
                }
            }

            foreach (var kvp in rendererNameToIndexMap)
            {
                string rendererNameWithoutSuffix = kvp.Key.Replace("_fpp", "").Replace("_tpp", "");

                foreach (var materialResource in modelInfo.materialsResources)
                {
                    var materialData = modelInfo.materialsData.FirstOrDefault(md => md.number == materialResource.number);
                    if (materialData == null) continue;

                    string formattedMaterialName = materialData.name.Replace(".mat", "").Replace("_fpp", "").Replace("_tpp", "");

                    if (rendererNameWithoutSuffix.Equals(formattedMaterialName))
                    {
                        var renderer = skinnedMeshRenderers[kvp.Value];
                        //Debug.Log($"[ApplyPresetMaterialsDirectly] Found specific matching renderer '{renderer.gameObject.name}' for material '{formattedMaterialName}'. Reapplying this material.");
                        foreach (var resource in materialResource.resources)
                        {
                            ApplyMaterialToRenderer(skinnedMeshRenderers[kvp.Value], resource.name, modelInstance, resource.rttiValues, slotName, kvp.Value);
                        }
                    }
                }
            }
        }

        public void PrintCurrentlyLoadedModels()
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
                GameObject loadedSkeleton = Instantiate(skeletonPrefab, Vector3.zero, Quaternion.Euler(-90, 0, 0), platform.transform);
                loadedSkeleton.tag = "Skeleton";
                autoTargetCinemachineCamera.FocusOnSkeleton(loadedSkeleton);
                Debug.Log("LoadSkeleton: Setting Camera to focus on:" + skeletonName);

                Transform pelvis = loadedSkeleton.transform.Find("pelvis");
                if (pelvis != null)
                {
                    GameObject legs = new GameObject("legs");
                    legs.transform.SetParent(pelvis);
                    legs.transform.localPosition = new Vector3(0, 0, -0.005f);
                }
                else
                {
                    Debug.LogWarning("Pelvis not found in the skeleton prefab: " + skeletonName);
                }
            }
            else
            {
                Debug.LogWarning("Skeleton prefab not found in Resources: " + resourcePath);
            }
        }

        public void LoadSkeletonBasedOnSelection()
        {
            // Get the selected skeleton name based on category and class
            string selectedSkeleton = skeletonLookup.GetSelectedSkeleton();
            string resourcePath = "Prefabs/" + selectedSkeleton.Replace(".msh", "");
            
            GameObject currentSkeleton = GameObject.FindGameObjectWithTag("Skeleton");
            bool shouldLoadSkeleton = true;

            if (currentSkeleton != null && currentSkeleton.name.StartsWith(selectedSkeleton))
            {
                shouldLoadSkeleton = false;
            }
            else if (currentSkeleton != null)
            {
                Destroy(currentSkeleton);
            }

            if (shouldLoadSkeleton)
            {
                //Debug.Log("Loading skeleton prefab from resource path: " + resourcePath);
                GameObject skeletonPrefab = Resources.Load<GameObject>(resourcePath);
                if (skeletonPrefab != null)
                {
                    GameObject loadedSkeleton = Instantiate(skeletonPrefab, Vector3.zero, Quaternion.Euler(-90, 0, 0), platform.transform.transform);
                    loadedSkeleton.tag = "Skeleton";
                    loadedSkeleton.name = selectedSkeleton; // Optionally set the name to manage future checks
                    //Debug.Log("LoadSkeleton: Setting Camera to focus on:" + loadedSkeleton);
                    autoTargetCinemachineCamera.FocusOnSkeleton(loadedSkeleton);

                    Transform pelvis = loadedSkeleton.transform.Find("pelvis");
                    if (pelvis != null)
                    {
                        GameObject legs = new GameObject("legs");
                        legs.transform.SetParent(pelvis);
                        legs.transform.localPosition = new Vector3(0, 0, -0.005f);
                    }
                    else
                    {
                        Debug.LogWarning("Pelvis not found in the skeleton prefab: " + selectedSkeleton);
                    }
                }
                else
                {
                    Debug.LogWarning("Skeleton prefab not found in Resources: " + resourcePath);
                }
            }
            else
            {
                //Debug.Log("Skeleton with the name " + selectedSkeleton + " is already loaded. Skipping.");
            }
        }

        public void RemoveModelAndVariationSlider(string slotName)
        {
            if (currentlyLoadedModels.TryGetValue(slotName, out GameObject currentModel))
            {
                string modelName = currentModel.name.Replace("(Clone)", "");
                if (variationBuilder.modelSpecificChanges.ContainsKey(modelName))
                {
                    variationBuilder.modelSpecificChanges.Remove(modelName);
                    //Debug.Log($"All changes cleared for model: {modelName}.");
                }
                Destroy(currentModel);
                currentlyLoadedModels.Remove(slotName);
                interfaceManager.UpdatePersistentSlotToModelMap(slotName, null);

                Transform existingVariationSlider = interfaceManager.slidersPanel.transform.Find(slotName + "Slider" + "_VariationSlider" + "_" + modelName);
                if (existingVariationSlider != null)
                {
                    //Debug.Log($"Existing variation slider found {existingVariationSlider}. Destroying...");
                    Destroy(existingVariationSlider.gameObject);
                }
            }
        }

        public void ApplyOriginalMaterials(GameObject modelInstance, List<Material> originalMats)
        {
            var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                if (i < originalMats.Count)
                {
                    skinnedMeshRenderers[i].sharedMaterial = originalMats[i];
                }
            }
        }

        public void LoadModelAndCreateVariationSlider(string slotName, int modelIndex)
        {
            GameObject modelInstance = null;

            string type = interfaceManager.GetTypeFromSelector();
            string category = interfaceManager.categoryDropdown.options[interfaceManager.categoryDropdown.value].text;
            string classSelection = interfaceManager.classDropdown.options[interfaceManager.classDropdown.value].text;

            string path = Path.Combine(Application.streamingAssetsPath, "SlotData", type);

            if (category != "ALL")
            {
                path = Path.Combine(path, category);

                if (classSelection != "ALL")
                {
                    path = Path.Combine(path, classSelection);
                }
            }

            if (modelInstance != null)
            {
                if (currentlyLoadedModels.ContainsKey(slotName))
                {
                    currentlyLoadedModels[slotName] = modelInstance;
                }
                else
                {
                    currentlyLoadedModels.Add(slotName, modelInstance);
                }

                // Update the sliderToLoadedModelMap in the InterfaceManager
                interfaceManager.UpdateSliderLoadedModel(slotName, modelInstance);
                interfaceManager.UpdatePersistentSlotToModelMap(slotName, modelInstance);
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
                    interfaceManager.CreateOrUpdateVariationSlider(slotName, modelName);

                    if (currentlyLoadedModels.ContainsKey(slotName))
                    {
                        currentlyLoadedModels[slotName] = modelInstance;
                    }
                    else
                    {
                        currentlyLoadedModels.Add(slotName, modelInstance);
                    }
                }
                else
                {
                    Debug.LogWarning("Mesh data is null or empty for slot: " + slotName);
                }
            }
            else
            {
                Debug.LogWarning("Slot JSON file not found: " + slotJsonFilePath);
            }
        }

        public string GetModelName(string meshName)
        {
            return meshName.EndsWith(".msh") ? meshName.Substring(0, meshName.Length - 4) : meshName;
        }

        public void LoadAndApplyMaterials(string modelName, GameObject modelInstance, string slotName)
        {
            string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", modelName + ".json");
            //Debug.Log($"[LoadAndApplyMaterials] Attempting to load material JSON from path: {materialJsonFilePath}");

            if (File.Exists(materialJsonFilePath))
            {
                string materialJsonData = File.ReadAllText(materialJsonFilePath);
                ModelData.ModelInfo modelInfo = JsonUtility.FromJson<ModelData.ModelInfo>(materialJsonData);

                if (modelInfo != null)
                {
                    //Debug.Log($"[LoadAndApplyMaterials] Model Name: {modelInfo.name}");
                    //Debug.Log($"[LoadAndApplyMaterials] Materials Data Count: {modelInfo.materialsData.Count}");
                    //foreach (var matData in modelInfo.materialsData)
                    //{
                    //    Debug.Log($"[LoadAndApplyMaterials] Material Data - Number: {matData.number}, Name: {matData.name}");
                    //}
                    //foreach (var matRes in modelInfo.materialsResources)
                    //{
                    //    Debug.Log($"[LoadAndApplyMaterials] Material Resource - Number: {matRes.number}, Resources Count: {matRes.resources.Count}");
                    //    foreach (var res in matRes.resources)
                    //    {
                    //        Debug.Log($"[LoadAndApplyMaterials] Resource - Name: {res.name}, Selected: {res.selected}, LayoutId: {res.layoutId}, LoadFlags: {res.loadFlags}");
                    //        foreach (var rtti in res.rttiValues)
                    //        {
                    //            Debug.Log($"[LoadAndApplyMaterials] RTTI Value - Name: {rtti.name}, Type: {rtti.type}, Value: {rtti.val_str}");
                    //        }
                    //    }
                    //}

                    ApplyMaterials(modelInstance, modelInfo);

                    var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    List<Material> mats = new List<Material>();
                    foreach (var renderer in skinnedMeshRenderers)
                    {
                        mats.AddRange(renderer.sharedMaterials);
                    }
                    originalMaterials[slotName] = mats;
                    //Debug.Log($"[LoadAndApplyMaterials] Stored original materials for {modelName} in slot {slotName}.");
                }
                else
                {
                    Debug.LogWarning($"[LoadAndApplyMaterials] Failed to deserialize material data for {modelName}.");
                }
            }
            else
            {
                Debug.LogWarning($"[LoadAndApplyMaterials] Material JSON file not found for {modelName} at path: {materialJsonFilePath}");
            }
        }

        private void ApplyMaterials(GameObject modelInstance, ModelData.ModelInfo modelInfo)
        {
            if (modelInstance == null || modelInfo == null || modelInfo.materialsData == null)
            {
                Debug.LogWarning("[ApplyMaterials] modelInstance, modelInfo, or modelInfo.materialsData is null.");
                return;
            }

            correctedMaterialIndexToRendererMap.Clear();
            materialIndexToRendererMap.Clear();
            var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            var rendererNameToIndexMap = new Dictionary<string, int>();
            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                string formattedName = FormatRendererName(skinnedMeshRenderers[i].name, modelInstance.name.Replace("sh_", "").Replace("(Clone)", ""));
                rendererNameToIndexMap[formattedName] = i;
            }

            //Debug.Log("[ApplyMaterials] All formatted renderer names mapped: " + string.Join(", ", rendererNameToIndexMap.Keys));
            //Debug.Log("[ApplyMaterials] All material names from JSON: " + string.Join(", ", modelInfo.materialsData.Select(md => md.name.Replace(".mat", ""))));

            for (int matIndex = 0; matIndex < modelInfo.materialsData.Count; matIndex++) // Fixed from .Length to .Count
            {
                var materialData = modelInfo.materialsData[matIndex];
                if (materialData == null)
                {
                    Debug.LogWarning("[ApplyMaterials] Material data at index " + matIndex + " is null or empty.");
                    continue;
                }

                string formattedMaterialName = materialData.name.Replace(".mat", "");
                bool matched = false;

                foreach (var kvp in rendererNameToIndexMap)
                {
                    if (kvp.Key.EndsWith(formattedMaterialName))
                    {
                        Material material = Resources.Load<Material>($"Materials/{formattedMaterialName}");
                        if (material != null)
                        {
                            var renderer = skinnedMeshRenderers[kvp.Value];
                            string originalMaterialName = renderer.material.name;

                            renderer.material = material;
                            //Debug.Log($"[ApplyMaterials] Successfully applied material '{formattedMaterialName}' to renderer {renderer.name}.");

                            materialIndexToRendererMap[matIndex] = renderer;
                            correctedMaterialIndexToRendererMap[kvp.Value] = renderer;

                            variationBuilder.RecordMaterialChange(modelInstance.name.Replace("(Clone)", ""), originalMaterialName, materialData.name, matIndex);
                            matched = true;
                            break;
                        }
                        else
                        {
                            Debug.LogWarning($"[ApplyMaterials] Failed to load material '{formattedMaterialName}' from Resources.");
                        }
                    }
                }


                if (!matched)
                {
                    // Try removing 'tpp' or 'fpp' from the material name if no match was found.
                    string alternativeMaterialName = formattedMaterialName.Replace("_tpp", "").Replace("_fpp", "");
                    foreach (var kvp in rendererNameToIndexMap)
                    {
                        if (kvp.Key.EndsWith(alternativeMaterialName))
                        {
                            Material material = Resources.Load<Material>($"Materials/{formattedMaterialName}");
                            if (material != null)
                            {
                                var renderer = skinnedMeshRenderers[kvp.Value];
                                string originalMaterialName = renderer.material.name;

                                renderer.material = material;

                                materialIndexToRendererMap[matIndex] = renderer;
                                correctedMaterialIndexToRendererMap[kvp.Value] = renderer;

                                variationBuilder.RecordMaterialChange(modelInstance.name.Replace("(Clone)", ""), originalMaterialName, materialData.name, matIndex);
                                matched = true;
                                break;
                            }
                            else
                            {
                                Debug.LogWarning($"[ApplyMaterials] Failed to load alternative material '{alternativeMaterialName}' from Resources.");
                            }
                        }
                    }

                    if (!matched)
                    {
                        // Attempt to apply material using the original method based on JSON data
                        if (matIndex >= 0 && matIndex < skinnedMeshRenderers.Length)
                        {
                            var renderer = skinnedMeshRenderers[matIndex];
                            string originalMaterialName = renderer.material.name.Replace(" (Instance)", "");
                            string resourcePath = $"Materials/{materialData.name.Replace(".mat", "")}";
                            Material material = Resources.Load<Material>(resourcePath);
                            if (material != null)
                            {
                                renderer.material = material;
                                variationBuilder.RecordMaterialChange(modelInstance.name.Replace("(Clone)", ""), originalMaterialName, materialData.name, matIndex);
                            }
                            else
                            {
                                Debug.LogWarning($"[ApplyMaterials] Failed to load material '{materialData.name}' from path '{resourcePath}'. Ensure the material exists in the Resources/Materials folder without the .mat extension and that the name is correct.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[ApplyMaterials] Renderer index {matIndex} out of bounds for the model '{modelInfo.name}'. Total renderers: {skinnedMeshRenderers.Length}.");
                        }
                    }
                }
            }
            var sortedMaterialToRendererMap = correctedMaterialIndexToRendererMap.OrderBy(entry => entry.Key);

            finalSortedMaterialIndexToRendererMap.Clear();

            foreach (var entry in sortedMaterialToRendererMap)
            {
                finalSortedMaterialIndexToRendererMap[entry.Key] = entry.Value;
            }

            // Convert the sorted result to a string if you want to use it elsewhere
            //string sortedMapToString = string.Join("; ", sortedMaterialToRendererMap.Select(entry => $"MaterialIndex: {entry.Key}, RendererName: {entry.Value.name}"));
            //Debug.Log($"[ApplyMaterials] Final Sorted materialIndexToRendererMap: {sortedMapToString}");
        }

        private string FormatRendererName(string rendererName, string modelName)
        {
            string prefixPattern = @"sm_\d+_";
            string lodPattern = @"lod_\d+_";
            string modelNamePattern = modelName + @"_\d+";
            string cleanedName = System.Text.RegularExpressions.Regex.Replace(rendererName, prefixPattern, "");
            cleanedName = System.Text.RegularExpressions.Regex.Replace(cleanedName, lodPattern, "");
            cleanedName = System.Text.RegularExpressions.Regex.Replace(cleanedName, modelNamePattern, "");
            cleanedName = System.Text.RegularExpressions.Regex.Replace(cleanedName, "_+", "_");
            cleanedName = cleanedName.TrimStart('_');
            cleanedName = cleanedName.Replace(".mat", "");

            //Debug.Log($"[FormatRendererName] modelName {modelName}, Child Original: {rendererName}, Cleaned: {cleanedName}");

            return cleanedName;
        }

        public void ApplyVariationMaterials(GameObject modelInstance, Variation variation, string slotName, int modelIndex)
        {
            //Debug.Log($"[ApplyVariationMaterials] Start. Model: {modelInstance.name}, Slot: {slotName}, ModelIndex: {modelIndex}");

            var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            ResetRenderersToInitialState(modelInstance, skinnedMeshRenderers);
            var rendererNameToIndexMap = new Dictionary<string, int>();
            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                string formattedName = FormatRendererName(skinnedMeshRenderers[i].name, modelInstance.name.Replace("sh_", "").Replace("(Clone)", ""));
                rendererNameToIndexMap[formattedName] = i;
            }

            foreach (var materialData in variation.materialsData)
            {
                string materialDataName = materialData.name.Replace(".mat", "");
                int materialDataIndex = materialData.number - 1; // Convert to 0-based index
                //Debug.Log($"[ApplyVariationMaterials] Processing MaterialData: {materialDataName}");

                foreach (var materialResource in variation.materialsResources)
                {
                    if (materialResource.number == materialData.number)
                    {
                        bool matched = false;
                        //Debug.Log($"[ApplyVariationMaterials] Matching MaterialResource: {materialResource.resources.FirstOrDefault()?.name ?? "N/A"} to MaterialData: {materialDataName}");

                        foreach (var kvp in rendererNameToIndexMap)
                        {
                            if (kvp.Key.Contains(materialDataName))
                            {
                                SkinnedMeshRenderer matchedRenderer = skinnedMeshRenderers[kvp.Value];
                                foreach (var resource in materialResource.resources)
                                {
                                    string originalMaterialName = matchedRenderer.material.name;
                                    ApplyMaterialToRenderer(matchedRenderer, resource.name, modelInstance, resource.rttiValues, slotName, materialDataIndex);
                                    //Debug.Log($"[ApplyVariationMaterials] Corrected material '{resource.name}' applied to matched renderer '{matchedRenderer.name}' based on name matching from Variation.");
                                    variationBuilder.RecordMaterialChange(modelInstance.name.Replace("(Clone)", ""), originalMaterialName.Replace(" (Instance)", ""), resource.name, materialDataIndex);
                                    matched = true;
                                    break;
                                }
                                if (matched) break;
                            }
                        }

                        if (!matched)
                        {
                            if (materialDataIndex >= 0 && materialDataIndex < skinnedMeshRenderers.Length)
                            {
                                SkinnedMeshRenderer fallbackRenderer = skinnedMeshRenderers[materialDataIndex];
                                foreach (var resource in materialResource.resources)
                                {
                                    string originalMaterialName = fallbackRenderer.material.name;
                                    ApplyMaterialToRenderer(fallbackRenderer, resource.name, modelInstance, resource.rttiValues, slotName, materialDataIndex);
                                    variationBuilder.RecordMaterialChange(modelInstance.name.Replace("(Clone)", ""), originalMaterialName.Replace(" (Instance)", ""), resource.name, materialDataIndex);
                                    //Debug.Log($"[ApplyVariationMaterials] Material '{resource.name}' applied to renderer '{fallbackRenderer.name}' by index as a fallback at {materialDataIndex}.");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"[ApplyVariationMaterials] Index {materialDataIndex} out of bounds for model '{modelInstance.name}' and no matching renderer found by name in Variation.");
                            }
                        }
                    }
                }
            }
        }

        private string intMapToString(Dictionary<int, int> map)
        {
            return "{" + string.Join(", ", map.Select(kvp => $"{kvp.Key}: {kvp.Value}")) + "}";
        }

        private string MapToString(Dictionary<int, SkinnedMeshRenderer> map)
        {
            var entries = map.Select(entry => $"MaterialIndex: {entry.Key}, RendererName: {entry.Value.name}");
            return "{" + string.Join("; ", entries) + "}";
        }


        public GameObject LoadModelPrefab(string modelName, string slotName)
        {
            string prefabPath = Path.Combine("Prefabs", modelName);
            GameObject prefab = Resources.Load<GameObject>(prefabPath);

            if (prefab != null)
            {
                // Instantiate the model as a child of 'CurrentModels'
                GameObject modelInstance = Instantiate(prefab, Vector3.zero, Quaternion.Euler(0,0,0), platform.transform);
                platform.ResetChildRotations();
                currentlyLoadedModels[slotName] = modelInstance;
                variationBuilder.currentModel = modelInstance;
                return modelInstance;
            }
            else
            {
                Debug.LogWarning("Prefab not found: " + prefabPath);
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

        private void ApplyMaterialToRenderer(SkinnedMeshRenderer renderer, string materialName, GameObject modelInstance, List<RttiValue> rttiValues, string slotName, int materialIndex)
        {
            renderer.enabled = true;
            //Debug.Log($"[ApplyMaterialToRenderer] Attempting to apply material {materialName} to renderer {renderer.gameObject.name}, at materialIndex {materialIndex}");
            string originalMaterialName = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "Unknown";

            Material loadedMaterial = LoadMaterial(materialName);
            if (loadedMaterial != null)
            {
                //Debug.Log($"[ApplyMaterialToRenderer] Loaded material '{materialName}' for renderer '{renderer.gameObject.name}'. Preparing to apply RTTI values.");
                Material clonedMaterial = new Material(loadedMaterial);
                renderer.sharedMaterials = new Material[] { clonedMaterial };
                variationBuilder.RecordMaterialChange(modelInstance.name.Replace("(Clone)", ""), originalMaterialName, materialName, materialIndex);

                if (rttiValues != null && rttiValues.Count > 0)
                {
                    foreach (var rttiValue in rttiValues)
                    {
                        //Debug.Log($"[ApplyMaterialToRenderer] Detected RTTI value for '{materialName}': '{rttiValue.name}' with value '{rttiValue.val_str}' on materialIndex {materialIndex}.");
                        ApplyTextureToMaterial(modelInstance, clonedMaterial, rttiValue.name, rttiValue.val_str, slotName, materialIndex);
                        
                    }
                }
                else
                {
                    //Debug.Log($"No RTTI values found for material '{materialName}'.");
                }
            }
            else
            {
                Debug.LogWarning($"Material '{materialName}' not found for renderer '{renderer.gameObject.name}'");
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

        private void ApplyTextureToMaterial(GameObject modelInstance, Material material, string rttiValueName, string textureName, string slotName, int modelIndex)
        {
            //Debug.Log($"[ApplyTextureToMaterial] modelInstance '{modelInstance}', material '{material.name}', rttiValueName '{rttiValueName}', textureName '{textureName}', slotName '{slotName}', modelIndex '{modelIndex}'");
            string shaderProperty = GetShaderProperty(rttiValueName);

            if (shaderProperty == null)
            {
                Debug.LogWarning($"Unsupported RTTI Value Name: {rttiValueName}. Unable to determine shader property.");
                return;
            }

            string newTextureName = Path.GetFileNameWithoutExtension(textureName);

            if ("null".Equals(textureName, StringComparison.OrdinalIgnoreCase))
            {
                //Debug.Log($"Removing texture from shader property: {shaderProperty} for RTTI: {rttiValueName}.");
                variationBuilder.RecordTextureChange(modelInstance.name, material.name.Replace(" (Instance)", ""), rttiValueName, newTextureName, modelIndex);
                material.SetTexture(shaderProperty, null);
            }
            else if (!string.IsNullOrEmpty(textureName))
            {
                string texturePath = $"textures/{newTextureName}";
                Texture2D texture = Resources.Load<Texture2D>(texturePath);

                if (texture != null)
                {
                    //Debug.Log($"Applying texture to shader property: {shaderProperty}. RTTI Value Name: {rttiValueName}, Texture Name: {textureName}.");
                    material.SetTexture(shaderProperty, texture);
                    variationBuilder.RecordTextureChange(modelInstance.name, material.name.Replace(" (Instance)", ""), rttiValueName, newTextureName, modelIndex);
                    ApplyAdditionalSettings(material, rttiValueName, texture);
                }
                else
                {
                    Debug.LogWarning($"Texture '{texturePath}' not found in Resources for RTTI: {rttiValueName}.");
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
                { "msk_1_tex", "_msk_1" },
                { "msk_1_add_tex", "_msk" },
                { "idx_0_tex", "_idx" },
                { "idx_1_tex", "_idx" },
                { "gra_0_tex", "_gra" },
                { "gra_1_tex", "_gra" },
                { "grd_0_tex", "_gra" },
                { "grd_1_tex", "_gra" },
                { "grf_dif_tex", "_gra" },
                { "grf_tex", "_gra" },
                { "spc_0_tex", "_spc" },
                { "spc_1_tex", "_spc" },
                { "clp_0_tex", "_clp" },
                { "clp_1_tex", "_clp" },
                { "rgh_0_tex", "_rgh" },
                { "rgh_1_tex", "_rgh" },
                { "ocl_0_tex", "_ocl" },
                { "ocl_1_tex", "_ocl" },
                { "ocl_tex", "_ocl" },
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


        // Example of a public getter method for currentlyLoadedModels
        public Dictionary<string, GameObject> GetCurrentlyLoadedModels()
        {
            return currentlyLoadedModels;
        }

    }
}