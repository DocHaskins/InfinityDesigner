#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

public class PrefabUtilityScript : EditorWindow
{
    private static string modelsDirectory = "Assets/Resources/Models";
    private static string materialsDirectory = "Assets/Resources/Materials";
    private static string prefabsDirectory = "Assets/Resources/Prefabs";
    private static string meshReferencesDirectory = "Assets/StreamingAssets/Mesh references";
    private int maxPrefabCount = 100;

    

    [MenuItem("Tools/Prefab Creator")]
    public static void ShowWindow()
    {
        GetWindow<PrefabUtilityScript>("Prefab Creator");
    }

    void OnGUI()
    {
        GUILayout.Label("Set the maximum number of prefabs to process:");
        maxPrefabCount = EditorGUILayout.IntField("Max Prefab Count", maxPrefabCount);

        if (GUILayout.Button("Create and Update Prefabs"))
        {
            CreateAndUpdatePrefabs(maxPrefabCount);
        }
    }

    private static void CreateAndUpdatePrefabs(int maxCount)
    {
        var fbxFiles = Directory.GetFiles(modelsDirectory, "*.fbx", SearchOption.AllDirectories)
                                .Take(maxCount)
                                .ToList();

        foreach (var fbxFilePath in fbxFiles)
        {
            string assetPath = fbxFilePath.Replace("\\", "/").Replace(Application.dataPath, "Assets");
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            // Modify the prefabPath to store prefabs in the prefabsDirectory
            string prefabName = Path.GetFileNameWithoutExtension(assetPath) + ".prefab";
            string prefabPath = Path.Combine(prefabsDirectory, prefabName);
            prefabPath = prefabPath.Replace("\\", "/"); // Ensure the path uses forward slashes

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefabAsset == null)
            {
                // Create the prefab in the new directory
                prefabAsset = PrefabUtility.SaveAsPrefabAsset(fbxAsset, prefabPath);
                Debug.Log($"Prefab created: {prefabPath}");
            }
            else
            {
                // Update the existing prefab
                PrefabUtility.SaveAsPrefabAsset(fbxAsset, prefabPath);
                Debug.Log($"Prefab updated: {prefabPath}");
            }

            AssignMaterialsToPrefab(prefabAsset, Path.GetFileNameWithoutExtension(fbxFilePath));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Processed and updated up to {maxCount} prefabs with materials.");
    }

    private static void AssignMaterialsToPrefab(GameObject prefab, string meshName)
    {
        string jsonPath = Path.Combine(meshReferencesDirectory, meshName + ".json");
        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"Mesh reference JSON not found: {jsonPath}");
            return;
        }

        string jsonContent = File.ReadAllText(jsonPath);
        var meshReferenceData = JsonUtility.FromJson<ModelData.MeshReferenceData>(jsonContent);
        UpdateMaterials(prefab, meshReferenceData.materialsData);
    }

    private static void UpdateMaterials(GameObject prefab, List<ModelData.MaterialData> materialsList)
    {
        var skinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var renderer in skinnedMeshRenderers)
        {
            Material[] materialsToUpdate = renderer.sharedMaterials;

            for (int i = 0; i < materialsToUpdate.Length; i++)
            {
                if (i < materialsList.Count)
                {
                    var materialData = materialsList[i];
                    string materialPath = Path.Combine(materialsDirectory, materialData.name + ".mat");
                    materialPath = materialPath.Replace("\\", "/");
                    Material newMat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                    if (newMat != null)
                    {
                        materialsToUpdate[i] = newMat;
                        Debug.Log($"Assigned material '{materialData.name}' to '{renderer.gameObject.name}' in prefab '{prefab.name}' at index {i}");
                    }
                    else
                    {
                        Debug.LogError($"Material '{materialData.name}' not found at '{materialPath}'");
                    }
                }
            }

            renderer.sharedMaterials = materialsToUpdate;
        }

        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log($"Prefab '{prefab.name}' updated with new materials.");
    }
}
#endif