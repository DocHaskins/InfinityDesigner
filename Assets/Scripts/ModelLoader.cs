using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using UnityEngine.Animations.Rigging;

public class ModelLoader : MonoBehaviour
{
    public string jsonFileName;
    private List<GameObject> loadedModels = new List<GameObject>();
    private GameObject loadedSkeleton;
    private bool isModelLoaded = false; // Property to track if the model is loaded

    void Start()
    {
        LoadModelFromJson();
    }

    public void LoadModelFromJson()
    {
        isModelLoaded = false;
        Debug.Log($"isModelLoaded: {isModelLoaded}");
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
        isModelLoaded = true;
        Debug.Log($"isModelLoaded: {isModelLoaded}");
    }

    private void LoadSkeleton(string skeletonName)
    {
        string resourcePath = "Prefabs/" + skeletonName.Replace(".msh", "");
        GameObject skeletonPrefab = Resources.Load<GameObject>(resourcePath);
        if (skeletonPrefab != null)
        {
            loadedSkeleton = Instantiate(skeletonPrefab, Vector3.zero, Quaternion.Euler(-90, 0, 0));

            // Find the 'pelvis' child in the loaded skeleton
            Transform pelvis = loadedSkeleton.transform.Find("pelvis");
            if (pelvis != null)
            {
                // Create a new GameObject named 'Legs'
                GameObject legs = new GameObject("legs");

                // Set 'Legs' as a child of 'pelvis'
                legs.transform.SetParent(pelvis);

                // Set the local position of 'Legs' with the specified offset
                legs.transform.localPosition = new Vector3(0, 0, -0.005f);
            }
            else
            {
                Debug.LogError("Pelvis not found in the skeleton prefab: " + skeletonName);
            }
        }
        else
        {
            Debug.LogError("Skeleton prefab not found in Resources: " + resourcePath);
        }
    }

    private void LoadModels(Dictionary<string, ModelData.SlotData> slotDictionary)
    {
        //SkeletonAttachmentHelper helper = new SkeletonAttachmentHelper(loadedSkeleton);

        foreach (var slotPair in slotDictionary)
        {
            var slot = slotPair.Value;
            foreach (var modelInfo in slot.models)
            {
                string prefabPath = modelInfo.name.Replace(".msh", "");
                GameObject modelPrefab = Resources.Load<GameObject>("Prefabs/" + prefabPath);

                if (modelPrefab != null)
                {
                    GameObject modelInstance = Instantiate(modelPrefab, Vector3.zero, Quaternion.identity);
                    PrefabSkeletonMapper mapper = modelInstance.GetComponent<PrefabSkeletonMapper>();

                    if (mapper != null)
                    {
                        ApplyMaterials(modelInstance, modelInfo);
                        loadedModels.Add(modelInstance);

                        //SetupModelRigConstraints(modelInstance, mapper.BoneDictionary);
                        //AttachToRiggedSkeleton(modelInstance);
                        //helper.SetupModelRigConstraints(modelInstance, mapper.BoneDictionary);
                    }
                }
                else
                {
                    Debug.LogError($"Model prefab not found in Resources: Prefabs/{prefabPath}");
                }
            }
        }
    }

    //private void AttachToRiggedSkeleton(GameObject modelInstance)
    //{
    //    SkinnedMeshRenderer modelRenderer = modelInstance.GetComponent<SkinnedMeshRenderer>();
    //    if (modelRenderer != null)
    //    {
    //        Transform[] boneTransforms = new Transform[modelRenderer.bones.Length];
    //        for (int i = 0; i < modelRenderer.bones.Length; i++)
    //        {
    //            string boneName = modelRenderer.bones[i].name;
    //            Transform boneInSkeleton = loadedSkeleton.transform.FindDeepChild(boneName);
    //            if (boneInSkeleton != null)
    //            {
    //                boneTransforms[i] = boneInSkeleton;
    //            }
    //            else
    //            {
    //                Debug.LogWarning($"Bone '{boneName}' not found in rigged skeleton.");
    //            }
    //        }
    //        modelRenderer.bones = boneTransforms;
    //        modelRenderer.rootBone = loadedSkeleton.transform; // Set the root bone if needed
    //    }
    //}

    private void ApplyMaterials(GameObject modelInstance, ModelData.ModelInfo modelInfo)
    {
        var skinnedMeshRenderers = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var materialResource in modelInfo.materialsResources)
        {
            // Calculate the renderer index based on the material number
            int rendererIndex = materialResource.number - 1;

            // Debugging: Log the material name and its intended index
            //Debug.Log($"Applying material '{materialResource.resources[0].name}' to renderer at index {rendererIndex} for model '{modelInfo.name}'");

            if (rendererIndex >= 0 && rendererIndex < skinnedMeshRenderers.Length)
            {
                var renderer = skinnedMeshRenderers[rendererIndex];
                ApplyMaterialToRenderer(renderer, materialResource.resources[0]);
            }
            else
            {
                Debug.LogError($"Renderer index out of bounds: {rendererIndex} for material number {materialResource.number} in model '{modelInfo.name}'");
            }
        }
    }

    private void ApplyMaterialToRenderer(SkinnedMeshRenderer renderer, ModelData.Resource resource)
    {
        if (resource.name.StartsWith("sm_"))
        {
            Debug.Log($"Skipped material '{resource.name}' as it starts with 'sm_'");
            return;
        }

        if (resource.name.Equals("null.mat", StringComparison.OrdinalIgnoreCase))
        {
            renderer.enabled = false;
            Debug.Log($"Disabled SkinnedMeshRenderer on '{renderer.gameObject.name}' due to null.mat");
            return;
        }

        string matPath = "materials/" + Path.GetFileNameWithoutExtension(resource.name);
        Material originalMat = Resources.Load<Material>(matPath);

        if (originalMat != null)
        {
            Material clonedMaterial = new Material(originalMat);

            // Determine which shader to use
            if (resource.name.Equals("sh_man_bdt_balaclava", StringComparison.OrdinalIgnoreCase))
            {
                clonedMaterial.shader = Shader.Find("HDRP/Lit");
            }
            else
            {
                // Check if the original material uses one of the custom shaders
                string[] customShaders = new string[] {
                "Shader Graphs/Clothing",
                "Shader Graphs/Clothing_dif",
                "Shader Graphs/Decal",
                "Shader Graphs/Skin"
            };
                bool useCustomShader = customShaders.Contains(originalMat.shader.name) || ShouldUseCustomShader(resource.name);
                bool useHairShader = ShouldUseHairShader(resource.name);

                // Set the shader based on conditions
                if (useCustomShader)
                {
                    clonedMaterial.shader = originalMat.shader; // Use the original shader
                }
                else if (useHairShader)
                {
                    clonedMaterial.shader = Shader.Find("HDRP/Hair");
                }
                else
                {
                    clonedMaterial.shader = Shader.Find("HDRP/Lit");
                }

                // Apply textures
                foreach (var rttiValue in resource.rttiValues)
                {
                    if (rttiValue.name != "ems_scale")
                    {
                        ApplyTextureToMaterial(clonedMaterial, rttiValue.name, rttiValue.val_str, useCustomShader);
                    }
                }
            }

            // Check if the renderer should be disabled
            if (ShouldDisableRenderer(renderer.gameObject.name))
            {
                renderer.enabled = false;
            }
            else
            {
                // Apply the cloned material to the renderer
                Material[] rendererMaterials = renderer.sharedMaterials;
                rendererMaterials[0] = clonedMaterial;
                renderer.sharedMaterials = rendererMaterials;
            }
        }
        else
        {
            Debug.LogError($"Material not found: '{matPath}' for renderer '{renderer.gameObject.name}'");
        }
    }

    private bool ShouldUseCustomShader(string resourceName)
    {
        // Define names that should use the custom shader
        string[] specialNames = {
        "sh_biter_", "sh_man_", "sh_scan_man_", "multihead007_npc_carl_",
        "sh_wmn_", "sh_scan_wmn_", "sh_dlc_opera_wmn_", "nnpc_wmn_worker",
        "sh_scan_kid_", "sh_scan_girl_", "sh_scan_boy_", "sh_chld_"
    };

        if (resourceName.Contains("hair"))
        {
            return false;
        }

        return specialNames.Any(name => resourceName.StartsWith(name));
    }
    private bool ShouldUseHairShader(string resourceName)
    {
        string[] hairNames = {
        "sh_man_hair_", "chr_npc_hair_", "chr_hair_", "man_facial_hair_",
        "man_hair_", "npc_aiden_hair", "npc_hair_", "sh_wmn_hair_",
        "sh_wmn_zmb_hair", "wmn_hair_", "viral_hair_", "wmn_viral_hair_",
        "zmb_bolter_a_hair_", "zmb_banshee_hairs_"
    };
        return hairNames.Any(name => resourceName.StartsWith(name));
    }

    private void ApplyTextureToMaterial(Material material, string rttiValueName, string textureName, bool useCustomShader)
    {
        if (!string.IsNullOrEmpty(textureName))
        {
            Debug.Log($"Applying texture. RTTI Value Name: {rttiValueName}, Texture Name: {textureName}");

            if (rttiValueName == "ems_scale")
            {
                return;
            }

            string texturePath = "textures/" + Path.GetFileNameWithoutExtension(textureName);
            Texture2D texture = Resources.Load<Texture2D>(texturePath);
            string difTextureName = null;
            string modifierTextureName = null;

            if (texture != null)
            {
                if (useCustomShader)
                {
                    bool difTextureApplied = false;
                    switch (rttiValueName)
                    {
                        case "msk_0_tex":
                        case "msk_1_tex":
                        case "msk_1_add_tex":
                            material.SetTexture("_msk", texture);
                            break;
                        case "idx_0_tex":
                        case "idx_1_tex":
                            material.SetTexture("_idx", texture);
                            break;
                        case "grd_0_tex":
                        case "grd_1_tex":
                            material.SetTexture("_gra", texture);
                            break;
                        case "spc_0_tex":
                        case "spc_1_tex":
                            material.SetTexture("_spc", texture);
                            break;
                        case "clp_0_tex":
                        case "clp_1_tex":
                            material.SetTexture("_clp", texture);
                            break;
                        case "rgh_0_tex":
                        case "rgh_1_tex":
                            material.SetTexture("_rgh", texture);
                            break;
                        case "ocl_0_tex":
                        case "ocl_1_tex":
                            material.SetTexture("_ocl", texture);
                            break;
                        case "ems_0_tex":
                        case "ems_1_tex":
                            material.SetTexture("_ems", texture);
                            break;
                        case "dif_1_tex":
                        case "dif_0_tex":
                            material.SetTexture("_dif", texture);
                            difTextureName = textureName;
                            difTextureApplied = true;
                            break;
                        case "nrm_1_tex":
                        case "nrm_0_tex":
                            material.SetTexture("_nrm", texture);
                            break;
                    }

                    if (difTextureApplied && textureName.StartsWith("chr_"))
                    {
                        material.SetTexture("_modifier", texture);
                        modifierTextureName = textureName;
                    }

                    // Check if _dif and _modifier textures have the same name
                    if (difTextureName != null && modifierTextureName != null && difTextureName == modifierTextureName)
                    {
                        material.SetFloat("_Modifier", 0);
                    }
                    else if (difTextureApplied)
                    {
                        material.SetFloat("_Modifier", 1);
                    }
                }
                else
                {
                    // HDRP/Lit shader texture assignments
                    switch (rttiValueName)
                    {
                        case "dif_1_tex":
                        case "dif_0_tex":
                            material.SetTexture("_BaseColorMap", texture);
                            break;
                        case "nrm_1_tex":
                        case "nrm_0_tex":
                            material.SetTexture("_NormalMap", texture);
                            break;
                        case "msk_1_tex":
                            // Assuming msk_1_tex corresponds to the mask map in HDRP/Lit shader
                            material.SetTexture("_MaskMap", texture);
                            break;
                        case "ems_0_tex":
                        case "ems_1_tex":
                            material.SetTexture("_EmissiveColorMap", texture);
                            material.SetColor("_EmissiveColor", Color.white * 2);
                            material.EnableKeyword("_EMISSION");
                            break;
                            // Add other cases for HDRP/Lit shader
                    }
                }
            }
            else
            {
                Debug.LogError($"Texture '{texturePath}' not found in Resources for material '{rttiValueName}'");
            }
        }
        else
        {
            //Debug.LogWarning($"Texture name is empty for RTTI Value Name: {rttiValueName}");
        }
    }

    private bool ShouldDisableRenderer(string gameObjectName)
    {
        return gameObjectName.Contains("sh_eye_shadow") ||
               gameObjectName.Contains("sh_wet_eye") ||
               gameObjectName.Contains("_null");
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
        isModelLoaded = false; // Reset the flag when unloading
    }

    public bool IsModelLoaded => isModelLoaded;

    private void DestroyObject(GameObject obj)
    {
#if UNITY_EDITOR
        DestroyImmediate(obj);
#else
        Destroy(obj);
#endif
    }
}