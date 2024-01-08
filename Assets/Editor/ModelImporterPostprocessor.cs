using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class ModelImporterPostprocessor : AssetPostprocessor
{
    //private void OnPreprocessModel()
    //{
    //    ModelImporter modelImporter = assetImporter as ModelImporter;
    //    if (modelImporter == null) return;

    //    string jsonPath = Path.Combine(meshReferencesDirectory, Path.GetFileNameWithoutExtension(assetPath) + ".json");
    //    if (File.Exists(jsonPath))
    //    {
    //        string jsonContent = File.ReadAllText(jsonPath);
    //        var modelData = JsonUtility.FromJson<ModelData>(jsonContent);

    //        var allMaterialsData = modelData.slotPairs
    //            .SelectMany(slotPair => slotPair.slotData.models)
    //            .SelectMany(model => model.materialsData)
    //            .ToList();

    //        foreach (var materialData in allMaterialsData)
    //        {
    //            string materialPath = Path.Combine(modelsDirectory, materialData.name);
    //            Material newMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
    //            if (newMaterial != null)
    //            {
    //                // The material name should match the material name used in the model
    //                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), materialData.name), newMaterial);
    //            }
    //        }
    //    }
    //}

    //private static string meshReferencesDirectory = "Assets/StreamingAssets/Mesh references";
    //private static string modelsDirectory = "Assets/Resources/Models";
}