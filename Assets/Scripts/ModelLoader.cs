using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class ModelLoader : MonoBehaviour
{
    public string jsonFileName;
    private List<GameObject> loadedModels = new List<GameObject>();
    private GameObject loadedSkeleton;

    void Start()
    {
        LoadModelFromJson();
    }

    public void LoadModelFromJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Jsons", jsonFileName);
        Debug.Log("Loading JSON from path: " + path);

        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return;
        }

        string jsonData = File.ReadAllText(path);
        ModelData modelData = JsonUtility.FromJson<ModelData>(jsonData);

        if (modelData != null)
        {
            var slots = modelData.GetSlots();
            if (slots != null && slots.Count > 0)
            {
                LoadSkeleton(modelData.skeletonName);
                LoadModels(slots);
            }
            else
            {
                Debug.LogError("Slots dictionary is null or empty.");
            }
        }
        else
        {
            Debug.LogError("Failed to deserialize JSON data");
        }
    }

    private void LoadSkeleton(string skeletonName)
    {
        string resourcePath = "Models/" + skeletonName.Replace(".msh", "");
        GameObject skeletonPrefab = Resources.Load<GameObject>(resourcePath);
        if (skeletonPrefab != null)
        {
            loadedSkeleton = Instantiate(skeletonPrefab, Vector3.zero, Quaternion.Euler(-90, 0, 0));

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
        }
        else
        {
            Debug.LogError("Skeleton prefab not found in Resources: " + resourcePath);
        }
    }

    private void LoadModels(Dictionary<string, ModelData.SlotData> slotDictionary)
    {
        foreach (var slotPair in slotDictionary)
        {
            var slot = slotPair.Value;
            foreach (var modelInfo in slot.models)
            {
                // Correctly formatting the prefab path for Resources.Load
                string prefabPath = modelInfo.name.Replace(".msh", "");
                GameObject modelPrefab = Resources.Load<GameObject>("Prefabs/" + prefabPath);

                Debug.Log($"Attempting to load prefab from Resources path: Prefabs/{prefabPath}");

                if (modelPrefab != null)
                {
                    GameObject modelInstance = Instantiate(modelPrefab, Vector3.zero, Quaternion.identity);
                    ApplyMaterials(modelInstance, modelInfo);
                    loadedModels.Add(modelInstance);

                    // Check if the prefab name contains "sh_man_facial_hair_" and adjust Z position
                    if (prefabPath.Contains("sh_man_facial_hair_"))
                    {
                        Vector3 localPosition = modelInstance.transform.localPosition;
                        localPosition.z += 0.01f;
                        modelInstance.transform.localPosition = localPosition;

                        Debug.Log($"Adjusted position for facial hair prefab: {prefabPath}");
                    }

                    if (prefabPath.Contains("sh_man_hair_system_"))
                    {
                        Vector3 localPosition = modelInstance.transform.localPosition;
                        //localPosition.y += 0.009f;
                        localPosition.z += 0.009f;
                        modelInstance.transform.localPosition = localPosition;

                        Debug.Log($"Adjusted position for hair prefab: {prefabPath}");
                    }

                    Debug.Log($"Prefab loaded and instantiated: {prefabPath}");
                }
                else
                {
                    Debug.LogError($"Model prefab not found in Resources: Prefabs/{prefabPath}");
                }
            }
        }
    }

    private void ApplyMaterials(GameObject modelInstance, ModelData.ModelInfo modelInfo)
    {
        Debug.Log($"Applying materials to model '{modelInfo.name}'.");

        var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var materialResource in modelInfo.materialsResources)
        {
            if (materialResource.number - 1 < skinnedMeshRenderers.Length)
            {
                var renderer = skinnedMeshRenderers[materialResource.number - 1];
                ApplyMaterialToRenderer(renderer, materialResource.resources[0]);
            }
            else
            {
                Debug.LogError($"Renderer index out of bounds for material number {materialResource.number} in model '{modelInfo.name}'");
            }
        }
    }

    private void ApplyMaterialToRenderer(SkinnedMeshRenderer renderer, ModelData.Resource resource)
    {
        // Skip materials starting with "sm_"
        if (resource.name.StartsWith("sm_"))
        {
            Debug.Log($"Skipped material '{resource.name}' as it starts with 'sm_'");
            return;
        }

        // Updated path to load materials from "resources/materials" folder
        string matPath = "materials/" + Path.GetFileNameWithoutExtension(resource.name);
        Material originalMat = Resources.Load<Material>(matPath);

        if (originalMat != null)
        {
            Material clonedMaterial = new Material(originalMat);
            Material[] rendererMaterials = renderer.sharedMaterials;
            rendererMaterials[0] = clonedMaterial; // Assuming you want to replace the first material
            renderer.sharedMaterials = rendererMaterials;

            foreach (var rttiValue in resource.rttiValues)
            {
                ApplyTextureToMaterial(clonedMaterial, rttiValue.name, rttiValue.val_str);
            }

            Debug.Log($"Material '{resource.name}' applied to '{renderer.gameObject.name}'");

            // Check if the renderer should be disabled based on its name
            if (ShouldDisableRenderer(renderer.gameObject.name))
            {
                renderer.enabled = false;
                Debug.Log($"SkinnedMeshRenderer disabled on '{renderer.gameObject.name}'");
            }
        }
        else
        {
            Debug.LogError($"Material not found: '{matPath}' for renderer '{renderer.gameObject.name}'");
        }
    }

    private void ApplyTextureToMaterial(Material material, string rttiValueName, string textureName)
    {
        string texturePath = "textures/" + Path.GetFileNameWithoutExtension(textureName);
        Texture2D texture = Resources.Load<Texture2D>(texturePath);

        if (texture != null)
        {
            switch (rttiValueName)
            {
                case "msk_1_tex":
                    material.SetTexture("_CoatMaskMap", texture);
                    material.SetFloat("_CoatMask", 1.0f);
                    Debug.Log($"Assigned texture '{texture.name}' to '_CoatMaskMap'");
                    break;
                case "dif_1_tex":
                case "dif_0_tex": // Add this case for dif_0_tex
                    material.SetTexture("_BaseColorMap", texture);
                    Debug.Log($"Assigned texture '{texture.name}' to '_BaseColorMap'");
                    break;
                case "nrm_1_tex":
                case "nrm_0_tex": // Add this case for nrm_0_tex
                    material.SetTexture("_NormalMap", texture);
                    Debug.Log($"Assigned texture '{texture.name}' to '_NormalMap'");
                    break;
                case "ems_0_tex":
                case "ems_1_tex": // Add this case for ems_1_tex
                    material.SetTexture("_EmissiveColorMap", texture);
                    material.SetColor("_EmissiveColor", Color.white * 2); // You might want to adjust this value
                    material.EnableKeyword("_EMISSION");
                    Debug.Log($"Assigned texture '{texture.name}' to '_EmissiveColorMap' and enabled emission");
                    break;
                    // Add other cases as needed
            }
        }
        else
        {
            Debug.LogError($"Texture '{texturePath}' not found in Resources for material '{rttiValueName}'");
        }
    }

    private bool ShouldDisableRenderer(string gameObjectName)
    {
        return gameObjectName.Contains("sh_eye_shadow") ||
               gameObjectName.Contains("sh_wet_eye") ||
               gameObjectName.Contains("_null");
    }

    public void UnloadModel()
    {
        if (loadedSkeleton != null)
        {
            DestroyObject(loadedSkeleton);
            loadedSkeleton = null;
        }

        foreach (var model in loadedModels)
        {
            if (model != null)
            {
                DestroyObject(model);
            }
        }
        loadedModels.Clear();

        DestroyObject(this.gameObject);
    }

    private void DestroyObject(GameObject obj)
    {
#if UNITY_EDITOR
        DestroyImmediate(obj);
#else
        Destroy(obj);
#endif
    }
}