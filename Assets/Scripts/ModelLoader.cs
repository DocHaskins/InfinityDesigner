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
            loadedSkeleton = Instantiate(skeletonPrefab, Vector3.zero, Quaternion.identity);
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

        foreach (var materialResource in modelInfo.materialsResources)
        {
            foreach (var resource in materialResource.resources)
            {
                string matPath = "Models/" + Path.GetFileNameWithoutExtension(resource.name);
                Material originalMat = Resources.Load<Material>(matPath);

                if (originalMat != null)
                {
                    var childRenderers = new List<SkinnedMeshRenderer>();
                    FindChildRenderersByName(modelInstance.transform, resource.name, childRenderers);

                    if (childRenderers.Count > 0)
                    {
                        foreach (var childRenderer in childRenderers)
                        {
                            Material clonedMaterial = new Material(originalMat);
                            Material[] rendererMaterials = new Material[childRenderer.sharedMaterials.Length];
                            rendererMaterials[0] = clonedMaterial;
                            for (int i = 1; i < rendererMaterials.Length; i++)
                            {
                                rendererMaterials[i] = childRenderer.sharedMaterials[i]; // Copy other materials if any
                            }
                            childRenderer.sharedMaterials = rendererMaterials;

                            //foreach (var rttiValue in resource.rttiValues)
                            //{
                            //    string texturePath = "Models/" + rttiValue.val_str;
                            //    Texture2D texture = Resources.Load<Texture2D>(texturePath);
                            //    if (texture != null)
                            //    {
                            //        ApplyTextureToMaterial(clonedMaterial, rttiValue.name, texture);
                            //    }
                            //    else
                            //    {
                            //        Debug.LogError($"Texture '{texturePath}' not found in Resources for material '{resource.name}'");
                            //    }
                            //}

                            if (ShouldDisableRenderer(childRenderer.gameObject.name))
                            {
                                childRenderer.enabled = false;
                                Debug.Log($"Disabled SkinnedMeshRenderer on '{childRenderer.gameObject.name}' due to name match.");
                            }
                            Debug.Log($"Material '{resource.name}' applied to '{childRenderer.gameObject.name}'");
                        }
                    }
                    else
                    {
                        Debug.LogError($"No child SkinnedMeshRenderer with the name '{resource.name}' found in '{modelInstance.name}'");
                    }
                }
                else
                {
                    Debug.LogError($"Material not found: '{matPath}' for model '{modelInfo.name}'");
                }
            }
        }
    }

    private void FindChildRenderersByName(Transform parent, string name, List<SkinnedMeshRenderer> renderers)
    {
        foreach (Transform child in parent)
        {
            var renderer = child.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null && child.gameObject.name.Contains(name))
            {
                renderers.Add(renderer);
            }
            FindChildRenderersByName(child, name, renderers); // Recursively find in all children
        }
    }

    private void ApplyTextureToMaterial(Material material, string rttiValueName, Texture2D texture)
    {
        switch (rttiValueName)
        {
            case "msk_1_tex":
                material.SetTexture("_CoatMaskMap", texture);
                material.SetFloat("_CoatMask", 1.0f);
                Debug.Log($"Assigned texture '{texture.name}' to '_CoatMaskMap'");
                break;
            case "dif_1_tex":
                material.SetTexture("_BaseColorMap", texture);
                Debug.Log($"Assigned texture '{texture.name}' to '_BaseColorMap'");
                break;
            case "nrm_1_tex":
                material.SetTexture("_NormalMap", texture);
                Debug.Log($"Assigned texture '{texture.name}' to '_NormalMap'");
                break;
                // Add other cases as needed
        }
    }

    private bool ShouldDisableRenderer(string gameObjectName)
    {
        return gameObjectName.Contains("sh_eye_shadow") || gameObjectName.Contains("sh_wet_eye");
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