using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

public class ModelImportExport : EditorWindow
{
    private string materialPath = "Assets/Resources/Materials";
    private string exportPath = "Assets/Resources/MaterialExport.json";
    private string importPath = "Assets/Resources/MaterialExport.json";

    [MenuItem("Model Tools/Material Import/Export")]
    public static void ShowWindow()
    {
        GetWindow(typeof(ModelImportExport));
    }

    private void OnGUI()
    {
        GUILayout.Label("Material Export/Import Settings", EditorStyles.boldLabel);
        materialPath = EditorGUILayout.TextField("Material Path", materialPath);
        exportPath = EditorGUILayout.TextField("Export Path", exportPath);
        importPath = EditorGUILayout.TextField("Import Path", importPath);

        if (GUILayout.Button("Export Materials"))
        {
            ExportMaterials();
        }

        if (GUILayout.Button("Import Materials"))
        {
            ImportMaterials();
        }
    }

    private void ExportMaterials()
    {
        Dictionary<string, JObject> materialData = new Dictionary<string, JObject>();
        string[] materialFiles = Directory.GetFiles(materialPath, "*.mat", SearchOption.AllDirectories);

        foreach (string materialFile in materialFiles)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialFile);
            if (material != null)
            {
                JObject materialProperties = new JObject
                {
                    { "Shader", material.shader.name }
                };

                // Get all properties from the material
                Shader shader = material.shader;
                for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
                {
                    string propertyName = ShaderUtil.GetPropertyName(shader, i);
                    ShaderUtil.ShaderPropertyType propertyType = ShaderUtil.GetPropertyType(shader, i);

                    switch (propertyType)
                    {
                        case ShaderUtil.ShaderPropertyType.Color:
                            Color color = material.GetColor(propertyName);
                            materialProperties[propertyName] = new JObject
                            {
                                { "r", color.r },
                                { "g", color.g },
                                { "b", color.b },
                                { "a", color.a }
                            };
                            break;
                        case ShaderUtil.ShaderPropertyType.Vector:
                            Vector4 vector = material.GetVector(propertyName);
                            materialProperties[propertyName] = new JObject
                            {
                                { "x", vector.x },
                                { "y", vector.y },
                                { "z", vector.z },
                                { "w", vector.w }
                            };
                            break;
                        case ShaderUtil.ShaderPropertyType.Float:
                        case ShaderUtil.ShaderPropertyType.Range:
                            materialProperties[propertyName] = material.GetFloat(propertyName);
                            break;
                        case ShaderUtil.ShaderPropertyType.TexEnv:
                            Texture texture = material.GetTexture(propertyName);
                            if (texture != null)
                            {
                                string texturePath = AssetDatabase.GetAssetPath(texture);
                                materialProperties[propertyName] = texturePath;
                            }
                            break;
                    }
                }

                string materialName = Path.GetFileNameWithoutExtension(materialFile);
                materialData[materialName] = materialProperties;
            }
        }

        string jsonOutput = JsonConvert.SerializeObject(materialData, Formatting.Indented);
        File.WriteAllText(exportPath, jsonOutput);
        Debug.Log($"Exported materials to {exportPath}");
    }

    private void ImportMaterials()
    {
        string jsonInput = File.ReadAllText(importPath);
        Dictionary<string, JObject> materialData = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(jsonInput);

        foreach (var materialEntry in materialData)
        {
            string materialName = materialEntry.Key;
            JObject materialProperties = materialEntry.Value;

            string materialFilePath = Path.Combine(materialPath, materialName + ".mat");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialFilePath);

            if (material == null)
            {
                string shaderName = materialProperties["Shader"].ToString();
                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, materialFilePath);
                }
                else
                {
                    Debug.LogWarning($"Shader {shaderName} not found. Skipping material {materialName}.");
                    continue;
                }
            }

            foreach (var property in materialProperties)
            {
                if (property.Key == "Shader") continue;

                if (material.HasProperty(property.Key))
                {
                    switch (property.Value.Type)
                    {
                        case JTokenType.String:
                            if (property.Value.ToString().EndsWith(".png") || property.Value.ToString().EndsWith(".jpg"))
                            {
                                Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(property.Value.ToString());
                                if (texture != null)
                                {
                                    material.SetTexture(property.Key, texture);
                                }
                            }
                            break;
                        case JTokenType.Float:
                            material.SetFloat(property.Key, property.Value.ToObject<float>());
                            break;
                        case JTokenType.Object:
                            if (property.Value["r"] != null)
                            {
                                Color color = new Color(
                                    property.Value["r"].Value<float>(),
                                    property.Value["g"].Value<float>(),
                                    property.Value["b"].Value<float>(),
                                    property.Value["a"].Value<float>()
                                );
                                material.SetColor(property.Key, color);
                            }
                            else if (property.Value["x"] != null)
                            {
                                Vector4 vector = new Vector4(
                                    property.Value["x"].Value<float>(),
                                    property.Value["y"].Value<float>(),
                                    property.Value["z"].Value<float>(),
                                    property.Value["w"].Value<float>()
                                );
                                material.SetVector(property.Key, vector);
                            }
                            break;
                    }
                }
            }

            EditorUtility.SetDirty(material);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Imported materials from {importPath}");
    }
}