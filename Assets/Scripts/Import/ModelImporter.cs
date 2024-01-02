using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ModelImporterWindow : EditorWindow
{
    [MenuItem("Tools/Model Importer")]
    public static void ShowWindow()
    {
        GetWindow<ModelImporterWindow>("Model Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Import Models from AssetImport", EditorStyles.boldLabel);

        if (GUILayout.Button("Import Models"))
        {
            string targetDirectory = Path.Combine(Application.dataPath, "AssetImport");
            if (Directory.Exists(targetDirectory))
            {
                ImportModelsAndTextures(targetDirectory);
            }
            else
            {
                Debug.LogError("AssetImport directory not found.");
            }
        }
    }

    private void ImportModelsAndTextures(string dirPath)
    {
        var allFiles = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories);
        var fbxFiles = new List<string>();
        var textureFiles = new Dictionary<string, List<string>>();

        foreach (var file in allFiles)
        {
            var relativePath = file.Replace(Application.dataPath, "Assets"); // Convert to relative path
            var baseName = GetBaseName(relativePath);

            if (Path.GetExtension(relativePath).ToLower() == ".fbx")
            {
                fbxFiles.Add(relativePath);
                if (!textureFiles.ContainsKey(baseName))
                    textureFiles[baseName] = new List<string>();
            }
            else if (IsTextureFile(relativePath))
            {
                if (!textureFiles.ContainsKey(baseName))
                    textureFiles[baseName] = new List<string>();
                textureFiles[baseName].Add(relativePath);
            }
        }

        foreach (var fbxFile in fbxFiles)
        {
            string modelName = GetBaseName(fbxFile);
            string modelPath = "Assets/Models/" + modelName;
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Models", modelName)); // Create the directory

            string newFbxPath = modelPath + "/" + Path.GetFileName(fbxFile);
            AssetDatabase.MoveAsset(fbxFile, newFbxPath);
            Debug.Log($"Moved FBX: {newFbxPath}");

            if (textureFiles.TryGetValue(modelName, out List<string> modelTextures))
            {
                Material newMaterial = new Material(Shader.Find("HDRP/Lit"));
                AssignTexturesToMaterial(newMaterial, modelTextures);

                string materialPath = modelPath + "/" + modelName + ".mat";
                AssetDatabase.CreateAsset(newMaterial, materialPath);
                Debug.Log($"Created Material: {materialPath}");

                foreach (var textureFile in modelTextures)
                {
                    string newTexturePath = modelPath + "/" + Path.GetFileName(textureFile);
                    AssetDatabase.MoveAsset(textureFile, newTexturePath);
                    Debug.Log($"Moved Texture: {newTexturePath}");
                }
            }
        }

        AssetDatabase.Refresh();
    }

    private string GetBaseName(string filePath)
    {
        var baseName = Path.GetFileNameWithoutExtension(filePath);
        var lastUnderscoreIndex = baseName.LastIndexOf('_');
        return lastUnderscoreIndex > -1 ? baseName.Substring(0, lastUnderscoreIndex) : baseName;
    }

    private bool IsTextureFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return extension == ".png" || extension == ".jpg" || extension == ".dds";
    }

    private string GetMaterialNameFromFile(string fileName)
    {
        // Extract the material name from the filename
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var lastUnderscoreIndex = baseName.LastIndexOf('_');
        return lastUnderscoreIndex > -1 ? baseName.Substring(0, lastUnderscoreIndex) : baseName;
    }

    private void AssignTexturesToMaterial(Material material, List<string> textureFiles)
    {
        Shader hdrpShader = Shader.Find("HDRP/Lit");
        if (hdrpShader == null)
        {
            Debug.LogError("HDRP/Lit shader not found. Make sure HDRP is correctly set up in your project.");
            return;
        }
        material.shader = hdrpShader;

        foreach (var textureFile in textureFiles)
        {
            Texture2D texture = LoadTexture(textureFile);
            if (texture == null)
            {
                Debug.LogError($"Failed to load texture: {textureFile}");
                continue;
            }

            // Assign textures based on the type detected in the file name
            if (textureFile.Contains("_dif"))
            {
                material.SetTexture("_BaseColorMap", texture); // Base Color Map
                Debug.Log($"Assigned diffuse '{texture.name}' to material '{material.name}'");
            }
            else if (textureFile.Contains("_nrm"))
            {
                material.SetTexture("_NormalMap", texture); // Normal Map
                Debug.Log($"Assigned normal '{texture.name}' to material '{material.name}'");
            }
            else if (textureFile.Contains("_rgh"))
            {
                // Roughness is part of the MaskMap in HDRP, need additional logic if separate roughness texture is used
                Debug.LogWarning("Separate roughness texture detected. HDRP uses the MaskMap for roughness.");
            }
            else if (textureFile.Contains("_spc"))
            {
                material.SetTexture("_SpecularColorMap", texture); // Specular Color Map
                Debug.Log($"Assigned specular '{texture.name}' to material '{material.name}'");
            }
            else if (textureFile.Contains("_ocl"))
            {
                // Occlusion is also part of the MaskMap in HDRP
                Debug.LogWarning("Separate occlusion texture detected. HDRP uses the MaskMap for occlusion.");
            }
            // Add additional texture types as needed, and consider how they map to HDRP's MaskMap if separate
        }
    }

    private Texture2D LoadTexture(string path)
    {
        string relativePath = "Assets" + path.Replace(Application.dataPath, "");
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);

        if (texture == null)
        {
            Debug.LogError($"Failed to load texture at path: {relativePath}");
        }
        else
        {
            Debug.Log($"Loaded Texture: {relativePath}");
        }

        return texture;
    }
}