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
                // Path should start from the folder directly under Resources and exclude the file extension
                string modelPath = "Models/" + Path.GetFileNameWithoutExtension(modelInfo.name);
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
        Renderer renderer = modelInstance.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("Renderer component is missing for model: " + modelInfo.name);
            return;
        }

        if (modelInfo.materialsData == null)
        {
            Debug.LogError("Materials data is null for model: " + modelInfo.name);
            return;
        }

        Debug.Log("Applying " + modelInfo.materialsData.Count + " materials to model: " + modelInfo.name);
        Material[] materials = new Material[modelInfo.materialsData.Count];

        for (int i = 0; i < modelInfo.materialsData.Count; i++)
        {
            string matPath = "Models/" + Path.GetFileNameWithoutExtension(modelInfo.materialsData[i].name);
            Debug.Log("Attempting to apply material: " + modelInfo.materialsData[i].name + " from path: " + matPath);

            Material mat = Resources.Load<Material>(matPath);
            if (mat != null)
            {
                materials[i] = mat;
                Debug.Log("Applied material: " + matPath);
            }
            else
            {
                Debug.LogError("Material not found in Resources: " + matPath);
            }
        }
        renderer.materials = materials;
    }
}