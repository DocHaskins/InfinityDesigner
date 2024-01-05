using UnityEditor;
using UnityEngine;
using System.IO;

public class ModelImporterPostprocessor : AssetPostprocessor
{
    // This method is called by Unity when it's assigning materials during the import of a model (FBX).
    public Material OnAssignMaterialModel(Material material, Renderer renderer)
    {
        string jsonPath = Path.Combine("Assets/StreamingAssets/Mesh references", Path.GetFileNameWithoutExtension(assetPath) + ".json");
        if (File.Exists(jsonPath))
        {
            string jsonContent = File.ReadAllText(jsonPath);
            var modelData = JsonUtility.FromJson<ModelData>(jsonContent);

            foreach (var slotPair in modelData.slotPairs)
            {
                foreach (var modelInfo in slotPair.slotData.models)
                {
                    foreach (var materialResource in modelInfo.materialsResources)
                    {
                        int materialIndex = materialResource.number - 1;
                        if (materialIndex >= 0 && materialIndex < renderer.sharedMaterials.Length)
                        {
                            if (renderer.sharedMaterials[materialIndex].name == material.name)
                            {
                                string materialPath = Path.Combine("Assets/Resources/Models", materialResource.resources[0].name);
                                Material newMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                                if (newMaterial != null)
                                {
                                    return newMaterial;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Return the original material if no custom material is found
        return material;
    }
}