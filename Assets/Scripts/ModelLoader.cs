using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class ModelLoader : MonoBehaviour
{
    public string jsonFileName;

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
            LoadSkeleton(modelData.skeletonName);
            LoadModels(slots);
        }
        else
        {
            Debug.LogError("Failed to deserialize JSON data");
        }
    }

    private void LoadSkeleton(string skeletonName)
    {
        string resourcePath = "Models/" + skeletonName.Replace(".msh", "");
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
            Debug.Log("Processing slot: " + slot.name);
            foreach (var modelInfo in slot.models)
            {
                string modelPath = "Models/" + modelInfo.name.Replace(".msh", "");
                Debug.Log("Loading model from: " + modelPath);
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
        if (renderer != null && modelInfo.materialsData != null)
        {
            Debug.Log("Applying materials to model: " + modelInfo.name);
            Material[] materials = new Material[modelInfo.materialsData.Count];
            for (int i = 0; i < modelInfo.materialsData.Count; i++)
            {
                string matPath = "Materials/" + modelInfo.materialsData[i].name;
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
}