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
        public CharacterBuilder_InterfaceManager interfaceManager;
        public SkeletonLookup skeletonLookup;
        public FilterMapping filterMapping;
        public CinemachineFreeLook ingameCamera;
        public CinemachineCameraZoomTool cameraTool;
        public VariationBuilder variationBuilder;

        private Dictionary<GameObject, List<int>> disabledRenderers = new Dictionary<GameObject, List<int>>();
        private Dictionary<GameObject, bool[]> initialRendererStates = new Dictionary<GameObject, bool[]>();
        public Dictionary<string, List<Material>> originalMaterials = new Dictionary<string, List<Material>>();
        public Dictionary<string, GameObject> currentlyLoadedModels = new Dictionary<string, GameObject>();

        void Update()
        {
            if (Input.GetKey(KeyCode.F12))
            {
                CreateCrashReport();
            }
        }

        public void CreateCrashReport()
        {
            // Ensure the "Crash Reports" directory exists
            string crashReportsDirectory = Path.Combine(Application.dataPath, "Crash Reports");
            Directory.CreateDirectory(crashReportsDirectory);

            // Create the report file name with the current datetime
            string dateTimeFormat = "yyyyMMdd_HHmmss";
            string reportFileName = $"CrashReport_{DateTime.Now.ToString(dateTimeFormat)}.txt";
            string reportFilePath = Path.Combine(crashReportsDirectory, reportFileName);

            // Initialize a StringBuilder to hold the report content
            StringBuilder reportContent = new StringBuilder();

            // Iterate through currently loaded models
            foreach (var pair in currentlyLoadedModels)
            {
                GameObject model = pair.Value;
                reportContent.AppendLine($"Model Key: {pair.Key}, GameObject Name: {model.name}");

                // Retrieve materials for each model
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        reportContent.AppendLine($"\tMaterial: {material.name}");
                    }
                }
                reportContent.AppendLine(); // Add an empty line for better readability
            }

            // Write the report to the file
            File.WriteAllText(reportFilePath, reportContent.ToString());

            Debug.Log($"Crash report created at: {reportFilePath}");
        }

        public void Reroll()
        {
            foreach (Transform sliderContainer in interfaceManager.slidersPanel.transform)
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
            string jsonsBasePath = Path.Combine(Application.streamingAssetsPath, "Jsons");

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

                                string slotName = interfaceManager.FindSlotForModel(modelInfo.name);
                                if (!string.IsNullOrEmpty(slotName))
                                {
                                    int modelIndex = interfaceManager.GetModelIndex(slotName, modelInfo.name);
                                    if (modelIndex != -1)
                                    {
                                        interfaceManager.SetSliderValue(slotName, modelIndex);
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
                // Check for the current skeleton in the scene and get its focus point
                Transform currentFocusPoint = cameraTool.GetCurrentFocusPoint(); // Assuming this method exists

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

                UpdateCameraTarget(loadedSkeleton.transform, currentFocusPoint);
            }
            else
            {
                Debug.LogError("Skeleton prefab not found in Resources: " + resourcePath);
            }
        }

        private void UpdateCameraTarget(Transform loadedModelTransform, Transform currentFocusPoint = null)
        {
            if (cameraTool != null && loadedModelTransform != null)
            {
                cameraTool.targets.Clear();
                string[] pointNames = { "pelvis", "spine1", "spine3", "neck", "legs", "r_hand", "l_hand", "l_foot", "r_foot" };

                // Rebuild the targets list
                foreach (var pointName in pointNames)
                {
                    Transform targetTransform = DeepFind(loadedModelTransform, pointName);
                    if (targetTransform != null)
                    {
                        cameraTool.targets.Add(targetTransform);
                    }
                }

                // Attempt to maintain focus on the current or similar point in the new model
                if (currentFocusPoint != null)
                {
                    int newIndex = cameraTool.targets.IndexOf(currentFocusPoint);
                    if (newIndex != -1)
                    {
                        // If the current focus point exists in the new skeleton, focus there
                        cameraTool.CurrentTargetIndex = newIndex;
                    }
                    else
                    {
                        // If the exact point doesn't exist, default to the first target or a similar point
                        cameraTool.CurrentTargetIndex = 0;
                    }
                }
                else if (cameraTool.targets.Count > 0)
                {
                    // If there was no previous focus, default to the first target
                    cameraTool.CurrentTargetIndex = 0;
                }

                // Update the camera target based on the new index
                cameraTool.UpdateCameraTarget();
            }
        }

        public void LoadSkeletonBasedOnSelection()
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

        public void RemoveModelAndVariationSlider(string slotName)
        {
            // Remove model
            if (currentlyLoadedModels.TryGetValue(slotName, out GameObject currentModel))
            {
                Destroy(currentModel);
                currentlyLoadedModels.Remove(slotName);
            }

            // Remove variation slider
            Transform existingVariationSlider = interfaceManager.slidersPanel.transform.Find(slotName + "VariationSlider");
            if (existingVariationSlider != null) Destroy(existingVariationSlider.gameObject);
        }


       
        public void ApplyOriginalMaterials(GameObject modelInstance, List<Material> originalMats)
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

        public void LoadModelAndCreateVariationSlider(string slotName, int modelIndex)
        {
            // Declare modelInstance at the start of the method
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

        public string GetModelName(string meshName)
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


        public void ApplyVariationMaterials(GameObject modelInstance, List<ModelData.MaterialResource> materialsResources)
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