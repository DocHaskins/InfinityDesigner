using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class ModelMaterialMapper : EditorWindow
{
    private string modelPath = "Assets/Resources/Models";
    private string materialPath = "Assets/Resources/Materials";
    private string mappingJsonPath = "Assets/Resources/mesh_material_mapping.json";
    private string updatedMaterialsJsonPath = "Assets/Resources/UpdatedMaterials.json";
    private int modelLimit = 10;

    [MenuItem("Tools/Model Material Mapper")]
    public static void ShowWindow()
    {
        GetWindow(typeof(ModelMaterialMapper));
    }

    private void OnGUI()
    {
        modelLimit = EditorGUILayout.IntField("Model Limit", modelLimit);

        if (GUILayout.Button("Map Materials to Models"))
        {
            MapMaterialsToModels();
        }
    }

    private void MapMaterialsToModels()
    {
        // Load JSON data
        JObject meshMaterialMapping = JObject.Parse(File.ReadAllText(mappingJsonPath));
        int totalModels = meshMaterialMapping.Count;
        Debug.Log($"Total models found in mapping JSON: {totalModels}");

        JObject updatedMaterials = JObject.Parse(File.ReadAllText(updatedMaterialsJsonPath));
        int totalMaterials = updatedMaterials.Count;
        Debug.Log($"Total materials found in updated materials JSON: {totalMaterials}");

        // Dictionary to track processed materials
        Dictionary<string, Material> processedMaterials = new Dictionary<string, Material>();

        // Get all model files
        string[] modelFiles = Directory.GetFiles(modelPath, "*.fbx", SearchOption.AllDirectories);
        int processedModels = 0;

        foreach (string modelFile in modelFiles)
        {
            if (processedModels >= modelLimit)
            {
                Debug.Log($"Processing halted after reaching the limit of {modelLimit} models.");
                break;
            }

            string modelName = Path.GetFileNameWithoutExtension(modelFile);

            // Find materials in mesh_material_mapping.json
            if (meshMaterialMapping.ContainsKey(modelName))
            {
                JArray materialArray = (JArray)meshMaterialMapping[modelName];

                foreach (string materialName in materialArray)
                {
                    string materialNameWithoutExtension = Path.GetFileNameWithoutExtension(materialName);

                    // Check if the material exists in updatedMaterials.json
                    if (updatedMaterials.ContainsKey(materialNameWithoutExtension))
                    {
                        string materialFileName = materialName + ".mat";
                        string materialFullPath = Path.Combine(materialPath, materialFileName);

                        // Check if material has already been processed
                        if (!processedMaterials.ContainsKey(materialFullPath))
                        {
                            // Attempt to load the material
                            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialFullPath);
                            if (material == null)
                            {
                                material = new Material(Shader.Find("Shader Graphs/Skin"));
                                AssetDatabase.CreateAsset(material, materialFullPath.Replace(".mat", ""));
                                Debug.Log($"Created new material: {materialName}");
                            }
                            else
                            {
                                Debug.Log($"Found existing material: {materialName}");
                            }

                            // Set Shader based on material name
                            SetMaterialShader(material, materialName);

                            // Set material properties from UpdatedMaterials.json
                            SetMaterialProperties(material, (JObject)updatedMaterials[materialNameWithoutExtension]);

                            // Add material to processed list
                            processedMaterials[materialFullPath] = material;
                        }
                        else
                        {
                            Debug.Log($"Material {materialName} has already been processed, skipping reprocessing.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"No material properties found for {materialNameWithoutExtension} in the updated materials JSON.");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"No material mapping found for model: {modelName} in mesh_material_mapping.json.");
            }

            processedModels++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void SetMaterialShader(Material material, string materialName)
    {
        if (materialName.Contains("decal") || materialName.Contains("tattoo") || materialName.Contains("tatoo") ||
    materialName.Contains("patch") || materialName.Contains("logo"))
        {
            material.shader = Shader.Find("Shader Graphs/Decal");
            //Debug.Log($"Assigned Decal shader to material: {material.name}");
        }
        else if (materialName.Contains("hat") || materialName.Contains("cap") || materialName.Contains("headwear") ||
         materialName.Contains("bandana") || materialName.Contains("beret") || materialName.Contains("bracken_bandage") ||
         materialName.Contains("beanie") || materialName.Contains("hood") || materialName.Contains("sh_man_pk_headcover_c") ||
         materialName.Contains("headband") || materialName.Contains("man_bdt_headcover_i") || materialName.Contains("coverlet") ||
         materialName.Contains("mask") || materialName.Contains("balaclava") || materialName.Contains("scarf") ||
         materialName.Contains("glasses") || materialName.Contains("player_tank_headwear_a_tpp") || materialName.Contains("jewelry") ||
         materialName.Contains("necklace") || materialName.Contains("earing") || materialName.Contains("earring") ||
         materialName.Contains("npc_man_ring") || materialName.Contains("cape") || materialName.Contains("chainmail") ||
         materialName.Contains("sweater") || materialName.Contains("shawl") || materialName.Contains("choker") ||
         materialName.Contains("cloak") || materialName.Contains("torso") || materialName.Contains("addon") ||
         materialName.Contains("centre") || materialName.Contains("vest") || materialName.Contains("stethoscope") ||
         materialName.Contains("top") || materialName.Contains("dress") || materialName.Contains("coat") ||
         materialName.Contains("plates") || materialName.Contains("jacket") || materialName.Contains("belt") ||
         materialName.Contains("bumbag") || materialName.Contains("spikes") || materialName.Contains("knife") ||
         materialName.Contains("wrap") || materialName.Contains("pouch") || materialName.Contains("collar") ||
         materialName.Contains("neck") || materialName.Contains("pocket") || materialName.Contains("waist_shirt") ||
         materialName.Contains("chain_armour") || materialName.Contains("skull_") || materialName.Contains("sport_bag") ||
         materialName.Contains("walkietalkie") || materialName.Contains("chain") || materialName.Contains("wrench") ||
         materialName.Contains("suspenders") || materialName.Contains("turtleneck") || materialName.Contains("apron") ||
         materialName.Contains("gloves") || materialName.Contains("glove") || materialName.Contains("arm") ||
         materialName.Contains("sleeve") || materialName.Contains("backpack") || materialName.Contains("bag") ||
         materialName.Contains("parachute") || materialName.Contains("leg") || materialName.Contains("pants") ||
         materialName.Contains("trousers") || materialName.Contains("socks") || materialName.Contains("bottom") ||
         materialName.Contains("skirt") || materialName.Contains("holster") || materialName.Contains("equipment") ||
         materialName.Contains("patch_") || materialName.Contains("bandage") || materialName.Contains("shoes") ||
         materialName.Contains("shoe") || materialName.Contains("feet") || materialName.Contains("boots") ||
         materialName.Contains("helmet") || materialName.Contains("armor"))
        {
            material.shader = Shader.Find("Shader Graphs/Clothing");
            //Debug.Log($"Assigned Clothing shader to material: {material.name}");
        }
        else if (materialName.Contains("hair") || materialName.Contains("extension") || materialName.Contains("base") ||
    materialName.Contains("sides") || materialName.Contains("braids") || materialName.Contains("fringe") ||
    materialName.Contains("facial_hair") || materialName.Contains("beard") || materialName.Contains("jaw_"))
        {
            material.shader = Shader.Find("Shader Graphs/Hair");
            //Debug.Log($"Assigned Hair shader to material: {material.name}");
        }
        else
        {
            material.shader = Shader.Find("Shader Graphs/Skin");
            //Debug.Log($"Assigned Skin shader to material: {material.name}");
        }
    }

    private void SetMaterialProperties(Material material, JObject materialProperties)
    {
        List<string> texturesToAssign = new List<string>();

        foreach (var property in materialProperties)
        {
            string rttiValueName = property.Key;
            string textureName = property.Value.ToString();

            string texturePath = Path.Combine("Assets/Resources/Textures", textureName);

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            if (texture != null)
            {
                texturesToAssign.Add(textureName);

                switch (rttiValueName)
                {
                    case "msk_0_tex":
                        material.SetTexture("_msk", texture);
                        break;
                    case "msk_1_tex":
                    case "msk_1_add_tex":
                        material.SetTexture("_msk_1", texture);
                        //Debug.Log($"Assigned texture {textureName} to _msk on material: {material.name}");
                        break;
                    case "idx_0_tex":
                    case "idx_1_tex":
                        material.SetTexture("_idx", texture);
                        //Debug.Log($"Assigned texture {textureName} to _idx on material: {material.name}");
                        break;
                    case "grd_0_tex":
                    case "grd_1_tex":
                        material.SetTexture("_gra", texture);
                        //Debug.Log($"Assigned texture {textureName} to _gra on material: {material.name}");
                        break;
                    case "spc_0_tex":
                    case "spc_1_tex":
                        material.SetTexture("_spc", texture);
                        //Debug.Log($"Assigned texture {textureName} to _spc on material: {material.name}");
                        break;
                    case "clp_0_tex":
                    case "clp_1_tex":
                        material.SetTexture("_clp", texture);
                        //Debug.Log($"Assigned texture {textureName} to _clp on material: {material.name}");
                        break;
                    case "rgh_0_tex":
                    case "rgh_1_tex":
                        material.SetTexture("_rgh", texture);
                        //Debug.Log($"Assigned texture {textureName} to _rgh on material: {material.name}");
                        break;
                    case "ocl_0_tex":
                    case "ocl_1_tex":
                        material.SetTexture("_ocl", texture);
                        //Debug.Log($"Assigned texture {textureName} to _ocl on material: {material.name}");
                        break;
                    case "ems_0_tex":
                    case "ems_1_tex":
                        material.SetTexture("_ems", texture);
                        //Debug.Log($"Assigned texture {textureName} to _ems on material: {material.name}");
                        break;
                    case "dif_1_tex":
                    case "dif_0_tex":
                        material.SetTexture("_dif", texture);
                        //Debug.Log($"Assigned texture {textureName} to _dif on material: {material.name}");
                        break;
                    case "nrm_1_tex":
                    case "nrm_0_tex":
                        material.SetTexture("_nrm", texture);
                        //Debug.Log($"Assigned texture {textureName} to _nrm on material: {material.name}");
                        break;
                }
            }
            else
            {
                Debug.LogWarning($"Texture {textureName} not found at path: {texturePath}");
            }
        }

        if (texturesToAssign.Count > 0)
        {
            Debug.Log($"Textures to be assigned to material {material.name}: {string.Join(", ", texturesToAssign)}");
        }
    }
}
