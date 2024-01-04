using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ModelLoader : MonoBehaviour
{
    public string jsonFileName;

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
        Debug.Log("JSON Data: " + jsonData); // Log the raw JSON data
        ModelData modelData = JsonUtility.FromJson<ModelData>(jsonData);

        if (modelData != null)
        {
            Debug.Log("Successfully deserialized JSON data.");
            Debug.Log("Skeleton Name: " + modelData.skeletonName); // Check if the skeleton name is correctly deserialized

            var slots = modelData.GetSlots();
            if (slots != null && slots.Count > 0)
            {
                Debug.Log("Slots count: " + slots.Count); // Check the number of slots
                foreach (var slot in slots)
                {
                    Debug.Log("Slot: " + slot.Key + ", Models Count: " + slot.Value.models.Count); // Log each slot's details
                }

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
        // Remove .msh and .fbx extension for Resources.Load
        string resourcePath = "Models/" + Path.GetFileNameWithoutExtension(skeletonName.Replace(".msh", ".fbx"));
        Debug.Log("Loading skeleton from: " + resourcePath);
        GameObject skeletonPrefab = Resources.Load<GameObject>(resourcePath);
        if (skeletonPrefab != null)
        {
            Instantiate(skeletonPrefab, Vector3.zero, Quaternion.identity);
            Debug.Log("Successfully instantiated skeleton: " + skeletonName);
        }
        else
        {
            Debug.LogError("Skeleton FBX not found in Resources: " + resourcePath);
        }
    }

    private void LoadModels(Dictionary<string, ModelData.SlotData> slotDictionary)
    {
        foreach (var slotPair in slotDictionary)
        {
            var slot = slotPair.Value;
            Debug.Log("Processing slot: " + slot.name + " with " + slot.models.Count + " models.");
            foreach (var modelInfo in slot.models)
            {
                string modelPath = "Models/" + Path.GetFileNameWithoutExtension(modelInfo.name.Replace(".msh", ".fbx"));
                Debug.Log("Attempting to load model: " + modelInfo.name + " from path: " + modelPath);

                GameObject modelPrefab = Resources.Load<GameObject>(modelPath);
                if (modelPrefab != null)
                {
                    GameObject modelInstance = Instantiate(modelPrefab, Vector3.zero, Quaternion.identity);
                    Debug.Log("Successfully instantiated model: " + modelInfo.name);
                    ApplyMaterials(modelInstance, modelInfo);
                }
                else
                {
                    Debug.LogError("Model FBX not found in Resources: " + modelPath);
                }
            }
        }
    }

    private void ApplyMaterials(GameObject modelInstance, ModelData.ModelInfo modelInfo)
    {
        // Find all SkinnedMeshRenderer components in the children of the modelInstance
        SkinnedMeshRenderer[] skinnedRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>();

        if (skinnedRenderers.Length == 0)
        {
            Debug.LogError("No SkinnedMeshRenderer components found for model: " + modelInfo.name);
            return;
        }

        if (modelInfo.materialsData == null || modelInfo.materialsData.Count == 0)
        {
            Debug.LogError("Materials data is null or empty for model: " + modelInfo.name);
            return;
        }

        Debug.Log("Applying materials to model: " + modelInfo.name);

        // Iterate through all the materials data and apply them to the corresponding SkinnedMeshRenderer
        for (int i = 0; i < modelInfo.materialsData.Count; i++)
        {
            var materialData = modelInfo.materialsData[i];
            string matPath = "Models/" + materialData.name.Replace(".mat", "");

            Material mat = Resources.Load<Material>(matPath);
            if (mat == null)
            {
                Debug.LogError("Material not found in Resources: " + matPath);
                continue;
            }

            if (materialData.number - 1 < skinnedRenderers.Length)
            {
                var renderer = skinnedRenderers[materialData.number - 1];
                Material[] materialsArray = renderer.sharedMaterials; // Use sharedMaterials instead of materials
                if (materialsArray.Length > 0)
                {
                    materialsArray[0] = mat; // Replace the material at the primary index (0) with the new one
                    renderer.sharedMaterials = materialsArray; // Assign the updated materials array back to the renderer using sharedMaterials
                    Debug.Log("Applied material: " + mat.name + " to " + renderer.gameObject.name);
                }
                else
                {
                    Debug.LogError("No materials found on renderer for model: " + modelInfo.name);
                }
            }
            else
            {
                Debug.LogError("Material number " + materialData.number + " exceeds the number of SkinnedMeshRenderers for model: " + modelInfo.name);
            }
        }
    }
}