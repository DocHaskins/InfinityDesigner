#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using static ModelData;
using System;
using System.Linq;

public class JsonCreatorWindow : EditorWindow
{
    [MenuItem("Tools/Json Creator")]
    public static void ShowWindow()
    {
        GetWindow<JsonCreatorWindow>("Json Creator");
    }

    string selectedFolder = "";
    private Dictionary<string, List<string>> filters;
    private Dictionary<string, string[]> categoryPrefixes;
    private Dictionary<string, List<string>> exclude_filters;

    void OnEnable()
    {
        InitializeDictionaries();
    }

    private void InitializeDictionaries()
    {
        filters = new Dictionary<string, List<string>>()
        {
            {"head", new List<string> {"sh_scan_", "sh_npc_", "sh_wmn_", "aiden_young", "sh_", "sh_man_", "sh_biter_", "sh_man_viral_a", "head", "sh_biter_deg_b", "zmb_volatile_a"}},
            {"hat", new List<string> {"hat", "cap", "headwear", "bandana", "beret", "bracken_bandage", "beanie", "hood", "sh_man_pk_headcover_c", "headband", "man_bdt_headcover_i", "coverlet"}},
            {"hat_access", new List<string> {"hat", "cap", "headwear", "bandana", "beret", "bracken_bandage", "beanie", "hood", "sh_man_pk_headcover_c", "headband", "man_bdt_headcover_i", "coverlet"}},
            {"mask", new List<string> {"mask", "balaclava", "scarf_a_part_d"}},
            {"mask_access", new List<string> {"mask", "balaclava", "scarf_a_part_d"}},
            {"glasses", new List<string> {"glasses", "man_ren_scarf_a_part_c", "player_tank_headwear_a_tpp"}},
            {"necklace", new List<string> {"jewelry", "necklace", "man_bdt_chain_i", "man_bdt_chain_g", "man_bdt_chain_h", "man_bdt_chain_f"}},
            {"earrings", new List<string> {"earing", "earring"}},
            {"rings", new List<string> {"npc_man_ring"}},
            {"hair", new List<string> {"hair", "npc_aiden_hair_headwear"}},
            {"hair_base", new List<string> {"base"}},
            {"hair_2", new List<string> {"sides"}},
            {"hair_3", new List<string> {"fringe"}},
            {"facial_hair", new List<string> {"facial_hair", "beard", "jaw_"}},
            {"cape", new List<string> {"cape", "scarf", "sweater", "shawl", "shalw", "choker", "cloak", "npc_man_torso_g_hood_b"}},
            {"torso", new List<string> {"torso", "npc_anderson", "player_reload_outfit", "singer_top", "npc_frank", "bolter", "goon", "charger_body", "corruptor", "screamer", "spitter", "viral", "volatile", "zmb_suicider_corpse"}},
            {"torso_extra", new List<string> {"torso", "addon", "dress", "machete_tpp", "coat", "plates", "vest", "jacket", "bubbles", "horns", "guts", "gastank"}},
            {"torso_access", new List<string> {"belt", "bumbag", "torso_b_cape_spikes", "spikes", "knife", "add_", "wrap", "npc_colonel_feathers_a", "axe", "npc_wmn_pants_b_torso_g_tank_top_bumbag_a", "zipper", "rag", "pouch", "collar", "neck", "pocket", "waist_shirt", "chain_armour", "skull_", "plates", "sport_bag", "gastank", "walkietalkie", "man_bdt_belt_c_addon_a", "chain", "battery", "torso_a_pk_top", "man_bdt_torso_d_shirt_c", "wrench", "suspenders", "turtleneck", "npc_waltz_torso_a_glasses_addon", "apron"}},
            {"tattoo", new List<string> {"tatoo", "tattoo"}},
            {"hands", new List<string> {"hands", "arms"}},
            {"lhand", new List<string> {"arm", "_left", "hand"}},
            {"rhand", new List<string> {"arm", "_right", "hand"}},
            {"gloves", new List<string> {"gloves", "arms_rag", "glove"}},
            {"arm_access", new List<string> {"biomarker", "bracelet", "npc_barney_band","gloves_a_addon"}},
            {"sleeve", new List<string> {"sleeve", "forearm", "arm", "arms"}},
            {"backpack", new List<string> {"backpack", "bag", "parachute", "backback"}},
            {"decals", new List<string> {"decal", "patch"}},
            {"legs", new List<string> {"leg", "legs", "pants", "bottom", "trousers"}},
            {"leg_access", new List<string> {"pocket", "socks", "pouch", "holster", "skirt", "chain", "equipment", "npc_jack_legs_adds", "man_srv_legs_b_bottom_a", "belt", "pad_", "legs_c_add_", "legs_a_addon", "pants_b_rag", "bumbag", "bag", "patch_", "bandage", "element", "tapes"}},
            {"shoes", new List<string> {"shoes", "shoe", "feet", "boots"}},
            {"armor_helmet", new List<string> {"helmet", "tank_headwear", "skullface_helmet", "headcover", "head_armor", "headgear"}},
            {"armor_helmet_access", new List<string> {"helmet", "skullface_helmet", "headcover", "head_armor", "headgear"}},
            {"armor_torso", new List<string> {"armor"}},
            {"armor_torso_upperright", new List<string> {"upperright"}},
            {"armor_torso_upperleft", new List<string> {"upperleft", "man_bandit_shoulders_armor_left_a", "shoulderpad"}},
            {"armor_torso_lowerright", new List<string> {"lowerright", "bracers", "bracer", "hand_tapes_a_right", "pad_a_r", "elbow_pad_a_normal_r", "elbow_pad_a_muscular_r"}},
            {"armor_torso_lowerleft", new List<string> {"lowerleft", "bracers", "npc_skullface_shield", "pad_a_l", "hand_tapes_a_left", "elbow_pad_a_muscular_l", "elbow_pad_a_normal_l"}},
            {"armor_legs", new List<string> {"armor_upperright", "armor_upperleft", "armor_lowerright", "armor_lowerleft"}},
            {"armor_legs_upperright", new List<string> {"armor_upperright"}},
            {"armor_legs_upperleft", new List<string> {"armor_upperleft"}},
            {"armor_legs_lowerright", new List<string> {"armor_lowerright"}},
            {"armor_legs_lowerleft", new List<string> {"armor_lowerleft"}}
        };

        categoryPrefixes = new Dictionary<string, string[]>
        {
            {"Biter", new[] {"biter", "man_zmb_", "wmn_zmb_", "_zmb"}},
            {"Viral", new[] {"viral", "wmn_viral"}},
            {"Infected", new[] { "volatile", "suicider", "spitter", "goon", "demolisher", "screamer", "bolter", "corruptor", "banshee", "charger" }},
            {"Player", new[] { "sh_npc_aiden", "player", "player_outfit_lubu_tpp", "player_outfit_gunslinger_tpp"}},
            {"Man", new[] {"man", "man_srv_craftmaster", "npc_carl", "npc_alberto_paganini", "npc_callum", "npc_feliks", "npc_marcus", "npc_mq_stan", "npc_hank", "npc_jack", "npc_colonel", "npc_pilgrim", "sh_baker", "npc_simon", "npc_juan", "npc_rowe", "npc_vincente", "sh_bruce",  "sh_frank", "sh_dlc_opera_npc_tetsuo", "sh_dlc_opera_npc_ogar", "sh_dlc_opera_npc_ciro", "sh_dlc_opera_npc_andrew", "npc_dylan", "npc_waltz", "dlc_opera_man", "npc_skullface", "sh_johnson", "npc_hakon", "npc_steve", "npc_barney", "multihead007_npc_carl_"}},
            {"Wmn", new[] {"wmn", "dlc_opera_wmn", "npc_anderson", "sh_mother", "npc_thalia", "npc_hilda", "npc_lola", "npc_mq_singer", "npc_astrid", "npc_dr_veronika", "npc_lawan", "npc_meredith", "npc_mia", "npc_nuwa", "npc_plaguewitch", "npc_sophie"}},
            {"Child", new[] {"child", "kid", "girl", "boy", "young", "chld"}}
        };

        exclude_filters = new Dictionary<string, List<string>>()
        {
            {"head", new List<string> {"hat", "cap", "headwear", "mask", "bdt_balaclava", "sh_benchmark_npc_hakon_cc", "beret", "glasses", "hair", "bandana", "facial_hair", "beard", "headcover", "emblem", "hat", "cap", "headwear", "bandana", "beanie", "hood"}},
            {"hat", new List<string> {"part", "player_camo_headwear_a_tpp", "player_headwear_crane_b_tpp", "player_tank_headwear_a_tpp", "player_camo_hood_a_tpp", "npc_man_torso_g_hood_b", "glasses", "hair", "decal_logo", "cape", "mask", "_addon", "decal", "facial_hair"}},
            {"hat_access", new List<string> {"player_camo_headwear_a_tpp", "player_headwear_crane_b_tpp", "player_tank_headwear_a_tpp", "player_camo_hood_a_tpp", "npc_man_torso_g_hood_b", "glasses", "hair", "cape", "mask", "facial_hair"}},
            {"mask", new List<string> {"glasses", "hair", "decal_logo", "cape", "_addon", "chr_player_healer_mask", "facial_hair"}},
            {"mask_access", new List<string> {"glasses", "hair", "decal_logo", "cape", "chr_player_healer_mask", "facial_hair"}},
            {"glasses", new List<string> {"hat", "cap", "npc_waltz_torso_a_glasses_addon", "headwear", "mask", "hair", "facial_hair"}},
            {"necklace", new List<string> {"bandana"}},
            {"earrings", new List<string> {"bandana", "torso"}},
            {"rings", new List<string> {"bandana", "torso", "earing", "earring"}},
            {"hair", new List<string> {"cap", "headwear", "mask", "glasses", "facial_hair"}},
            {"hair_base", new List<string> {"cap", "headwear", "mask", "glasses", "facial_hair", "young_hair", "fringe", "sides"}},
            {"hair_2", new List<string> {"cap", "headwear", "mask", "glasses", "facial_hair", "young_hair", "fringe", "base"}},
            {"hair_3", new List<string> {"cap", "headwear", "mask", "glasses", "facial_hair", "young_hair", "sides", "base"}},
            {"facial_hair", new List<string> {"hat", "cap", "headwear", "mask", "glasses"}},
            {"cape", new List<string> {"mask", "spikes", "belts", "scarf_a_part_d", "bags", "scarf_a_part_c"}},
            {"torso", new List<string> {"armor", "sleeve", "plates", "equipment", "head", "hair", "player_camo_torso_a_tpp", "hat", "mask", "player_inquisitor_torso_a_tpp", "player_torso_tpp_a", "addon", "machete_tpp", "skull", "chain_armour", "hands", "battery", "arms", "cape", "turtleneck", "apron", "pants", "backpack", "bag", "parachute", "suspenders", "legs", "hood", "spikes", "chain", "decal", "collar", "scarf", "pouch", "zipper", "belt", "part", "patch", "bag", "pocket", "jewelry", "necklace", "ring"}},
            {"torso_extra", new List<string> {"armor", "mask", "gloves", "sleeve", "hands", "battery", "arms", "cape", "pants", "backpack", "bag", "parachute", "legs", "hood", "spikes", "chain", "decal", "collar", "scarf", "pouch", "zipper", "belt", "bag", "pocket", "jewelry", "necklace", "ring"}},
            {"torso_access", new List<string> {"pants", "leg", "legs", "shoes", "man_bdt_chain_i", "bracken_bandage", "man_bdt_chain_g", "man_bdt_chain_h", "man_bdt_chain_f"}},
            {"hands", new List<string> {"sleeve", "_left", "_right", "armor", "glove", "decal_tattoo", "decal"}},
            {"lhand", new List<string> {"_right", "sleeve", "armor", "glove", "decal_tattoo", "tattoo", "torso", "leg", "legs", "pants", "hand", "decal"}},
            {"rhand", new List<string> {"_left", "sleeve", "armor", "glove", "decal_tattoo", "tattoo", "torso", "leg", "legs", "pants", "hand", "decal"}},
            {"tattoo", new List<string> {"mask"}},
            {"gloves", new List<string> {"arm","hand", "decal", "addon", "player_camo_gloves_a_tpp"}},
            {"arm_access", new List<string> {"torso"}},
            {"sleeve", new List<string> {"decal", "logo"}},
            {"backpack", new List<string> {"pants"}},
            {"decals", new List<string> {"mask"}},
            {"legs", new List<string> {"mask", "shoes", "player_legs_a", "player_camo_pants_a_tpp", "arm", "chain", "elbow", "_right", "npc_jack_legs_adds", "bandage", "sleeve", "patch", "add_", "armor", "glove", "addon", "bag", "tapes", "man_srv_legs_b_bottom_a", "element", "hand", "decal", "equipment", "pocket", "pouch", "element", "decal_logo", "belt", "pad", "pants_b_rag", "bumbag", "bag", "patch_"}},
            {"leg_access", new List<string> {"armor", "man_bdt_belt_d_pouches_a", "man_bdt_belt_g", "man_bdt_belt_c_addon_a", "man_bdt_belt_c", "man_bzr_belt_c", "man_bdt_belt_d", "man_bzr_belt_a", "man_srv_belt_bags_a", "torso", "elbow", "decal_logo", "shoes", "arm", "sleeve", "armor", "glove", "torso", "hand", "hat"}},
            {"shoes", new List<string> {"mask"}},
            {"armor_helmet", new List<string> {"hat", "cap", "part", "sh_man_pk_headcover_c", "emblem", "man_bdt_headcover_i", "bandana", "beanie", "hood", "headband", "torso", "glasses", "hair", "decal", "cape"}},
            {"armor_helmet_access", new List<string> {"hat", "cap", "sh_man_pk_headcover_c", "man_bdt_headcover_i", "beanie", "hood", "torso", "glasses", "hair", "cape"}},
            {"armor_torso", new List<string> {"legs", "pants", "bottom", "pouches", "walkietalkie", "pin", "leg", "upper", "element", "bottom_a_armor", "cape", "addon", "upperleft", "lowerright", "lowerleft", "upperright"}},
            {"armor_torso_upperright", new List<string> {"legs", "pants", "leg"}},
            {"armor_torso_upperleft", new List<string> {"legs", "pants", "leg"}},
            {"armor_torso_lowerright", new List<string> {"legs", "pants", "leg"}},
            {"armor_torso_lowerleft", new List<string> {"legs", "pants", "leg"}},
            {"armor_legs", new List<string> {"torso_armor", "torso", "arm", "arms", "upperleft", "lowerright", "lowerleft", "upperright"}},
            {"armor_legs_upperright", new List<string> {"torso", "arm", "arms"}},
            {"armor_legs_upperleft", new List<string> {"torso", "arm", "arms"}},
            {"armor_legs_lowerright", new List<string> {"torso", "arm", "arms"}},
            {"armor_legs_lowerleft", new List<string> {"torso", "arm", "arms"}}
        };
    }

    void OnGUI()
    {
        if (GUILayout.Button("Select Folder"))
        {
            selectedFolder = EditorUtility.OpenFolderPanel("Select Folder", "", "");
        }

        EditorGUILayout.Space();

        if (!string.IsNullOrEmpty(selectedFolder) && GUILayout.Button("Process Models"))
        {
            ProcessModelsInFolder(selectedFolder);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Organize JSONs"))
        {
            OrganizeJsonFiles();
        }
    }

    void OrganizeJsonFiles()
    {
        string jsonsDir = Path.Combine(Application.dataPath, "StreamingAssets/Jsons");
        string[] jsonFiles = Directory.GetFiles(jsonsDir, "*.json");
        Dictionary<string, Dictionary<string, List<string>>> modelsSortedByCategory = new Dictionary<string, Dictionary<string, List<string>>>();
        HashSet<string> unsortedModels = new HashSet<string>();
        Dictionary<string, List<string>> modelToFilterLookup = new Dictionary<string, List<string>>();

        foreach (var file in jsonFiles)
        {
            try
            {
                string jsonData = File.ReadAllText(file);
                ModelData modelData = JsonConvert.DeserializeObject<ModelData>(jsonData);

                if (modelData.modelProperties == null || modelData.slotPairs == null)
                {
                    Debug.LogWarning($"Skipped file {file} due to missing properties or slotPairs.");
                    continue;
                }

                foreach (var slotPair in modelData.slotPairs)
                {
                    foreach (var model in slotPair.slotData.models)
                    {
                        string modelName = model.name.ToLower();
                        SortModel(modelName, modelsSortedByCategory, unsortedModels, modelToFilterLookup);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing file {file}: {ex.Message}");
            }
        }

        SaveSortedModels(modelsSortedByCategory);
        SaveUnsortedModels(unsortedModels);
        SaveModelFilterLookup(modelToFilterLookup);
        Debug.Log("JSON files organized and saved.");
    }

    void SortModel(string modelName, Dictionary<string, Dictionary<string, List<string>>> modelsSortedByCategory, HashSet<string> unsortedModels, Dictionary<string, List<string>> modelToFilterLookup)
    {

        // Determine the category of the model
        string category = DetermineModelCategory(modelName, categoryPrefixes);
        //Debug.Log($"Model '{modelName}' determined category: {category}");

        // Initialize category in modelsSortedByCategory if not exists
        if (!modelsSortedByCategory.ContainsKey(category))
        {
            modelsSortedByCategory[category] = new Dictionary<string, List<string>>();
        }

        bool matchedAnyFilter = false;

        // Check against all filters
        foreach (var filterPair in filters)
        {
            string filterName = filterPair.Key;
            List<string> filterTerms = filterPair.Value;

            foreach (var filterTerm in filterTerms)
            {
                if (modelName.Contains(filterTerm))
                {
                    // Check if model is excluded by any term in exclude_filters
                    if (exclude_filters.ContainsKey(filterName) && exclude_filters[filterName].Any(excludeTerm => modelName.Contains(excludeTerm)))
                    {
                        //Debug.Log($"Model '{modelName}' is excluded by term in exclude_filters for filter '{filterName}'");
                        continue; // Skip this filter as model is excluded
                    }

                    //Debug.Log($"Model '{modelName}' matches filter term '{filterTerm}' in filter category '{filterName}'");

                    // Initialize filter list in category if not exists
                    if (!modelsSortedByCategory[category].ContainsKey(filterName))
                    {
                        modelsSortedByCategory[category][filterName] = new List<string>();
                    }

                    if (!modelToFilterLookup.ContainsKey(modelName))
                    {
                        modelToFilterLookup[modelName] = new List<string>();
                    }
                    if (!modelToFilterLookup[modelName].Contains(filterName))
                    {
                        modelToFilterLookup[modelName].Add(filterName);
                    }

                    // Add model to the corresponding filter list
                    modelsSortedByCategory[category][filterName].Add(modelName);
                    matchedAnyFilter = true;
                }
            }
        }

        if (!matchedAnyFilter)
        {
            Debug.LogWarning($"Model '{modelName}' did not match any filters or was excluded in category '{category}'");
            unsortedModels.Add(modelName);
        }
    }

    void SaveModelFilterLookup(Dictionary<string, List<string>> modelToFilterLookup)
    {
        string outputDir = Path.Combine(Application.dataPath, "StreamingAssets/SlotData");
        Directory.CreateDirectory(outputDir);
        string filePath = Path.Combine(outputDir, "ModelFilterLookup.json");
        string jsonContent = JsonConvert.SerializeObject(modelToFilterLookup, Formatting.Indented);
        File.WriteAllText(filePath, jsonContent);
    }

    void SaveSortedModels(Dictionary<string, Dictionary<string, List<string>>> modelsSortedByCategory)
    {
        // Iterate through each category
        foreach (var category in modelsSortedByCategory.Keys)
        {
            string categoryDir = Path.Combine(Application.dataPath, $"StreamingAssets/SlotData/{category}");
            Directory.CreateDirectory(categoryDir);

            // Iterate through each filter in the category
            foreach (var filter in modelsSortedByCategory[category].Keys)
            {
                // Remove duplicates and sort the list alphabetically
                List<string> sortedModels = modelsSortedByCategory[category][filter].Distinct().ToList();
                sortedModels.Sort();

                string filePath = Path.Combine(categoryDir, $"ALL_{filter}.json");
                string jsonContent = JsonConvert.SerializeObject(new { meshes = sortedModels }, Formatting.Indented);
                File.WriteAllText(filePath, jsonContent);
            }
        }
    }

    void SaveUnsortedModels(HashSet<string> unsortedModels)
    {
        if (unsortedModels.Any())
        {
            string outputDir = Path.Combine(Application.dataPath, "StreamingAssets/SlotData");
            Directory.CreateDirectory(outputDir);
            string filePath = Path.Combine(outputDir, "None_found.json");
            string jsonContent = JsonConvert.SerializeObject(new { unsortedMeshes = unsortedModels }, Formatting.Indented);
            File.WriteAllText(filePath, jsonContent);
        }
    }

    string DetermineModelCategory(string modelName, Dictionary<string, string[]> categoryTerms)
    {
        foreach (var category in categoryTerms)
        {
            if (category.Value.Any(term => modelName.Contains(term)))
                return category.Key;
        }
        return "Other";
    }

    void SortAndRemoveDuplicates(List<string> models)
    {
        models.Sort();
    }

    void ProcessModelsInFolder(string folderPath)
    {
        string[] files = Directory.GetFiles(folderPath, "*.model", SearchOption.AllDirectories);

        // Define the ignore list
        var ignoreList = new HashSet<string> { "heron.model", "sh_debug_face_anim.model", "hen.model", "goat.model", "gazelle_fem.model", "empty_model.model", "horse.model", "wolf.model", "rat.model", "roedeer.model", "polito_01.model", "dog_prototype.model", "goat.model" };

        foreach (string modelFile in files)
        {
            // Skip files in the ignore list
            if (ignoreList.Contains(Path.GetFileName(modelFile).ToLower()))
            {
                Debug.Log($"Skipped ignored model file: {modelFile}");
                continue;
            }

            Debug.Log($"Processing model file: {modelFile}");
            string jsonData = File.ReadAllText(modelFile);
            JObject modelObject = JObject.Parse(jsonData);

            var processedData = ProcessModelData(modelObject);
            string outputJson = JsonConvert.SerializeObject(processedData, Newtonsoft.Json.Formatting.Indented);

            string outputDir = Path.Combine(Application.dataPath, "StreamingAssets/Jsons");
            Directory.CreateDirectory(outputDir);

            string outputFilePath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(modelFile) + ".json");
            File.WriteAllText(outputFilePath, outputJson);
            Debug.Log($"Processed data saved to: {outputFilePath}");
        }
    }

    ModelData ProcessModelData(JObject modelObject)
    {
        // Extract skeletonName from the preset object
        string skeletonName = modelObject["preset"]["skeletonName"]?.ToString();

        // Extract modelProperties from the data object
        JObject dataObject = modelObject["data"] as JObject;
        ModelProperties modelProperties = ExtractModelProperties(dataObject["properties"] as JArray);

        // Extract slotPairs directly from the root object
        List<SlotDataPair> slotPairs = ExtractSlotPairs(modelObject["slots"] as JArray);

        // Construct and return the ModelData object
        return new ModelData
        {
            skeletonName = skeletonName,
            modelProperties = modelProperties,
            slotPairs = slotPairs
        };
    }

    ModelProperties ExtractModelProperties(JArray propertiesArray)
    {
        ModelProperties properties = new ModelProperties();
        foreach (JObject prop in propertiesArray)
        {
            string propName = prop["name"]?.ToString();
            switch (propName)
            {
                case "class":
                    properties.@class = prop["value"]?.ToString();
                    break;
                case "race":
                    properties.race = prop["value"]?.ToString();
                    break;
                case "sex":
                    properties.sex = prop["value"]?.ToString();
                    break;
                    // Add other properties as needed
            }
        }
        return properties;
    }


    List<SlotDataPair> ExtractSlotPairs(JArray slotsArray)
    {
        var slotPairs = new List<SlotDataPair>();
        foreach (JObject slot in slotsArray)
        {
            SlotDataPair slotPair = new SlotDataPair
            {
                key = slot["name"]?.ToString(),
                slotData = new SlotData
                {
                    slotUid = (int)slot["slotUid"],
                    name = slot["name"]?.ToString(),
                    filterText = slot["filterText"]?.ToString(),
                    models = ExtractModels(slot["meshResources"]["resources"] as JArray)
                }
            };
            slotPairs.Add(slotPair);
        }
        return slotPairs;
    }

    List<ModelInfo> ExtractModels(JArray modelsArray)
    {
        var models = new List<ModelInfo>();
        foreach (JObject model in modelsArray)
        {
            ModelInfo modelInfo = new ModelInfo
            {
                name = model["name"]?.ToString(),
                materialsData = ExtractMaterialsData(model["materialsData"] as JArray),
                materialsResources = ExtractMaterialsResources(model["materialsResources"] as JArray)
            };
            models.Add(modelInfo);
        }
        return models;
    }

    List<MaterialData> ExtractMaterialsData(JArray materialsDataArray)
    {
        var materialsData = new List<MaterialData>();
        foreach (JObject materialDataObject in materialsDataArray)
        {
            var materialData = new MaterialData
            {
                number = (int)materialDataObject["number"],
                name = materialDataObject["name"]?.ToString()
            };
            materialsData.Add(materialData);
        }
        return materialsData;
    }

    List<MaterialResource> ExtractMaterialsResources(JArray materialsResourcesArray)
    {
        var materialsResources = new List<MaterialResource>();
        foreach (JObject materialResourceObject in materialsResourcesArray)
        {
            var materialResource = new MaterialResource
            {
                number = (int)materialResourceObject["number"],
                resources = ExtractResources(materialResourceObject["resources"] as JArray)
            };
            materialsResources.Add(materialResource);
        }
        return materialsResources;
    }

    List<Resource> ExtractResources(JArray resourcesArray)
    {
        var resources = new List<Resource>();
        foreach (JObject resourceObject in resourcesArray)
        {
            var resource = new Resource
            {
                name = resourceObject["name"]?.ToString(),
                selected = (bool)resourceObject["selected"],
                layoutId = (int)resourceObject["layoutId"],
                loadFlags = resourceObject["loadFlags"]?.ToString(),
                rttiValues = ExtractRttiValues(resourceObject["rttiValues"] as JArray)
            };
            resources.Add(resource);
        }
        return resources;
    }

    List<RttiValue> ExtractRttiValues(JArray rttiValuesArray)
    {
        var rttiValues = new List<RttiValue>();
        if (rttiValuesArray == null)
        {
            Debug.LogWarning("rttiValuesArray is null.");
            return rttiValues; // Returning an empty list as there's nothing to process.
        }

        foreach (JObject rttiValueObject in rttiValuesArray)
        {
            if (rttiValueObject == null)
            {
                Debug.LogWarning("rttiValueObject is null, skipping this iteration.");
                continue;
            }

            var rttiValue = new RttiValue
            {
                name = rttiValueObject["name"]?.ToString(),
                type = (int)(rttiValueObject["type"] ?? 0),
                val_str = rttiValueObject["val_str"]?.ToString()
            };
            rttiValues.Add(rttiValue);
        }
        return rttiValues;
    }
}
#endif