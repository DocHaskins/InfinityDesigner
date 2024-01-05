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
                string prefabPath = "Models/" + modelInfo.name.Replace(".msh", "");
                GameObject modelPrefab = Resources.Load<GameObject>(prefabPath);
                if (modelPrefab != null)
                {
                    GameObject modelInstance = Instantiate(modelPrefab, Vector3.zero, Quaternion.identity);
                    ApplyMaterials(modelInstance, modelInfo);
                    loadedModels.Add(modelInstance);
                }
                else
                {
                    Debug.LogError("Model prefab not found in Resources: " + prefabPath);
                }
            }
        }
    }

    private void ApplyMaterials(GameObject modelInstance, ModelData.ModelInfo modelInfo)
    {
        SkinnedMeshRenderer[] skinnedRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>();

        if (skinnedRenderers.Length == 0)
        {
            Debug.LogError($"No SkinnedMeshRenderer components found for model: {modelInfo.name}");
            return;
        }

        foreach (var materialResource in modelInfo.materialsResources)
        {
            foreach (var resource in materialResource.resources)
            {
                string matPath = "Models/" + Path.GetFileNameWithoutExtension(resource.name);
                Material mat = Resources.Load<Material>(matPath);

                if (mat != null)
                {
                    int rendererIndex = materialResource.number - 1;
                    if (rendererIndex >= 0 && rendererIndex < skinnedRenderers.Length)
                    {
                        var skinnedRenderer = skinnedRenderers[rendererIndex];
                        var rendererMaterials = skinnedRenderer.sharedMaterials;

                        if (rendererMaterials.Length > 0)
                        {
                            // Clone the material to avoid changing the shared material
                            Material clonedMaterial = new Material(mat);
                            rendererMaterials[0] = clonedMaterial;
                            skinnedRenderer.sharedMaterials = rendererMaterials;

                            foreach (var rttiValue in resource.rttiValues)
                            {
                                string texturePath = "Models/" + rttiValue.val_str;
                                Texture2D texture = Resources.Load<Texture2D>(texturePath);

                                if (texture != null)
                                {
                                    ApplyTextureToMaterial(clonedMaterial, rttiValue.name, texture);
                                }
                                else
                                {
                                    Debug.LogError($"Texture '{texturePath}' not found in Resources for material '{resource.name}'");
                                }
                            }

                            // Check if SkinnedMeshRenderer needs to be disabled
                            if (ShouldDisableRenderer(skinnedRenderer.gameObject.name))
                            {
                                skinnedRenderer.enabled = false;
                                Debug.Log($"Disabled SkinnedMeshRenderer on '{skinnedRenderer.gameObject.name}' due to name match.");
                            }

                            Debug.Log($"Successfully applied material '{clonedMaterial.name}' to renderer '{skinnedRenderer.name}'");
                        }
                        else
                        {
                            Debug.LogWarning($"Renderer '{skinnedRenderer.name}' does not have any materials to update.");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Renderer index {rendererIndex} is out of bounds for model: {modelInfo.name}");
                    }
                }
                else
                {
                    Debug.LogError($"Material '{matPath}' not found in Resources for model: {modelInfo.name}");
                }
            }
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