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
using static PlasticGui.LaunchDiffParameters;
using System.Text;

public class JsonCreatorWindow : EditorWindow
{
    [MenuItem("Tools/Json Creator")]
    public static void ShowWindow()
    {
        GetWindow<JsonCreatorWindow>("Json Creator");
    }

    string selectedFolder = "";
    private bool storeClassData = false;
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
            {"hat_access", new List<string> {"hat", "cap", "headwear", "bandana", "horns", "beret", "bracken_bandage", "beanie", "hood", "sh_man_pk_headcover_c", "headband", "man_bdt_headcover_i", "coverlet"}},
            {"mask", new List<string> {"mask", "balaclava", "scarf_a_part_d"}},
            {"mask_access", new List<string> {"mask", "balaclava", "scarf_a_part_d"}},
            {"glasses", new List<string> {"glasses", "man_ren_scarf_a_part_c", "player_tank_headwear_a_tpp"}},
            {"necklace", new List<string> {"jewelry", "necklace", "man_bdt_chain_i", "man_bdt_chain_g", "man_bdt_chain_h", "man_bdt_chain_f"}},
            {"earrings", new List<string> {"earing", "earring"}},
            {"rings", new List<string> {"npc_man_ring"}},
            {"hair", new List<string> {"hair", "npc_aiden_hair_headwear", "extension"}},
            {"hair_base", new List<string> {"base"}},
            {"hair_2", new List<string> {"sides", "braids", "extension"}},
            {"hair_3", new List<string> {"fringe", "extension"}},
            {"facial_hair", new List<string> {"facial_hair", "beard", "jaw_"}},
            {"cape", new List<string> {"cape", "scarf", "chainmail", "sweater", "shawl", "shalw", "choker", "cloak", "npc_man_torso_g_hood_b"}},
            {"torso", new List<string> {"torso", "npc_anderson", "player_reload_outfit", "singer_top", "npc_frank", "bolter", "goon", "charger_body", "corruptor", "screamer", "spitter", "viral", "volatile", "zmb_suicider_corpse"}},
            {"torso_2", new List<string> { "torso", "centre", "vest", "wmn_torso_e", "wmn_torso_i", "center", "top", "fronttop", "front", "dress", "coat", "jacket", "bubbles", "horns", "guts", "gastank"}},
            {"torso_extra", new List<string> {"torso", "addon", "wpntmp", "centre", "detail", "vest", "wmn_torso_e", "wmn_torso_i", "stethoscope", "center", "top", "fronttop", "front", "dress", "machete_tpp", "coat", "plates", "vest", "jacket", "bubbles", "horns", "guts", "gastank"}},
            {"torso_access", new List<string> {"belt", "bumbag", "wpntmp", "centre", "detail", "vest", "wmn_torso_e", "wmn_torso_i", "stethoscope", "center", "top", "fronttop", "front", "torso_b_cape_spikes", "spikes", "knife", "add_", "wrap", "npc_colonel_feathers_a", "axe", "npc_wmn_pants_b_torso_g_tank_top_bumbag_a", "zipper", "rag", "pouch", "collar", "neck", "pocket", "waist_shirt", "chain_armour", "skull_", "plates", "sport_bag", "gastank", "walkietalkie", "man_bdt_belt_c_addon_a", "chain", "battery", "torso_a_pk_top", "man_bdt_torso_d_shirt_c", "wrench", "suspenders", "turtleneck", "npc_waltz_torso_a_glasses_addon", "apron"}},
            {"tattoo", new List<string> {"tatoo", "tattoo"}},
            {"hands", new List<string> {"hands", "arms"}},
            {"lhand", new List<string> {"arm", "_left", "hand"}},
            {"rhand", new List<string> {"arm", "_right", "hand"}},
            {"gloves", new List<string> {"gloves", "arms_rag", "glove", "army_gloves"}},
            {"arm_access", new List<string> {"biomarker", "basic_watch", "bracelet", "npc_barney_band","gloves_a_addon"}},
            {"sleeve", new List<string> {"sleeve", "sleeves", "forearm", "arm", "arms"}},
            {"backpack", new List<string> {"backpack", "bag", "parachute", "backback"}},
            {"decals", new List<string> {"decal", "patch"}},
            {"decals_extra", new List<string> {"decal", "patch"}},
            {"decals_logo", new List<string> {"logo"}},
            {"legs", new List<string> {"leg", "legs", "pants", "trousers", "army_pants"}},
            {"legs_extra", new List<string> {"pocket", "socks", "bottom", "sc_ov_legs", "legs_a_part", "child_torso_a_bottom", "skirt", "man_srv_legs_b_bottom_a", "belt", "legs_c_add_", "legs_a_addon", "pants_b_rag"}},
            {"legs_access", new List<string> {"pocket", "socks", "bottom", "sc_ov_legs", "legs_a_part", "pouch", "child_torso_a_bottom", "holster", "skirt", "chain", "equipment", "npc_jack_legs_adds", "man_srv_legs_b_bottom_a", "belt", "pad_", "legs_c_add_", "legs_a_addon", "pants_b_rag", "bumbag", "bag", "patch_", "bandage", "element", "tapes"}},
            {"shoes", new List<string> {"shoes", "shoe", "feet", "boots", "child_pants_b"}},
            {"armor_helmet", new List<string> {"helmet", "tank_headwear", "armor_b_head", "skullface_helmet", "headcover", "head_armor", "headgear"}},
            {"armor_helmet_access", new List<string> {"helmet", "skullface_helmet", "headcover", "head_armor", "headgear"}},
            {"armor_torso", new List<string> {"armor", "nuwa_torso_a"}},
            {"armor_torso_access", new List<string> {"armor", "addon"}},
            {"armor_torso_upperright", new List<string> {"upperright"}},
            {"armor_torso_upperleft", new List<string> {"upperleft", "man_bandit_shoulders_armor_left_a", "shoulderpad"}},
            {"armor_torso_lowerright", new List<string> {"lowerright", "bracers", "bracer", "hand_tapes_a_right", "pad_a_r", "elbow_pad_a_normal_r", "elbow_pad_a_muscular_r"}},
            {"armor_torso_lowerleft", new List<string> {"lowerleft", "bracers", "npc_skullface_shield", "pad_a_l", "hand_tapes_a_left", "elbow_pad_a_muscular_l", "elbow_pad_a_normal_l"}},
            {"armor_legs", new List<string> { "legs_armor", "legs_b_armor", "leg_armor", "pants_armor", "legs_a_armor", "pants_b_armor", "leg_armor", "pants_a_armor"}},
            {"armor_legs_upperright", new List<string> { "armor"}},
            {"armor_legs_upperleft", new List<string> { "armor"}},
            {"armor_legs_lowerright", new List<string> { "armor"}},
            {"armor_legs_lowerleft", new List<string> { "armor"}}
        };

        categoryPrefixes = new Dictionary<string, string[]>
        {
            {"Biter", new[] {"biter", "man_zmb_", "wmn_zmb_", "_zmb"}},
            {"Viral", new[] {"viral", "wmn_viral"}},
            {"Special Infected", new[] { "volatile", "suicider", "spitter", "goon", "demolisher", "screamer", "bolter", "corruptor", "banshee", "charger" }},
            {"Player", new[] { "sh_npc_aiden", "player", "player_outfit_lubu_tpp", "player_outfit_carrier_leader_tpp", "player_outfit_brecken", "player_outfit_gunslinger_tpp"}},
            {"Man", new[] {"man", "man_srv_craftmaster", "dlc_opera_man_shopkeeper_special", "npc_pipsqueak", "dlc_opera_man_npc_ciro", "npc_mc_dispatcher", "dlc_opera_man_npc_ferka", "dlc_opera_man_npc_hideo", "dlc_opera_man_npc_ogar", "npc_carl", "npc_alberto_paganini", "npc_outpost_guard", "npc_callum", "npc_abandon_srv_emmett", "npc_abandon_pk_master_brewer", "npc_feliks", "npc_marcus", "npc_mq_stan", "npc_hank", "npc_jack", "npc_colonel", "npc_pilgrim", "sh_baker", "npc_simon", "npc_juan", "npc_rowe", "npc_vincente", "sh_bruce",  "sh_frank", "sh_dlc_opera_npc_tetsuo", "sh_dlc_opera_npc_ogar", "sh_dlc_opera_npc_ciro", "sh_dlc_opera_npc_andrew", "npc_dylan", "npc_waltz", "dlc_opera_man", "npc_skullface", "sh_johnson", "npc_hakon", "npc_steve", "npc_barney", "multihead007_npc_carl_"}},
            {"Wmn", new[] {"wmn", "dlc_opera_wmn", "npc_anderson", "dlc_opera_man_wmn_brienne", "sh_mother", "npc_thalia", "npc_hilda", "npc_lola", "npc_mq_singer", "npc_astrid", "npc_dr_veronika", "npc_lawan", "npc_meredith", "npc_mia", "npc_nuwa", "npc_plaguewitch", "npc_sophie"}},
            {"Child", new[] {"child", "kid", "girl", "boy", "young", "chld"}}
        };

        exclude_filters = new Dictionary<string, List<string>>()
        {
            {"head", new List<string> {"hat", "cap", "headwear", "headgear", "armor", "mask", "bdt_balaclava", "horns", "sh_benchmark_npc_hakon_cc", "beret", "glasses", "hair", "bandana", "facial_hair", "beard", "headcover", "emblem", "hat", "cap", "headwear", "bandana", "beanie", "hood"}},
            {"hat", new List<string> {"part", "player_camo_headwear_a_tpp", "fringe", "base", "braids", "player_headwear_crane_b_tpp", "player_tank_headwear_a_tpp", "player_camo_hood_a_tpp", "npc_man_torso_g_hood_b", "glasses", "hair", "decal_logo", "cape", "mask", "_addon", "decal", "facial_hair"}},
            {"hat_access", new List<string> {"player_camo_headwear_a_tpp", "player_headwear_crane_b_tpp", "player_tank_headwear_a_tpp", "player_camo_hood_a_tpp", "npc_man_torso_g_hood_b", "glasses", "hair", "cape", "mask", "facial_hair"}},
            {"mask", new List<string> {"glasses", "hair", "decal_logo", "cape", "_addon", "chr_player_healer_mask", "facial_hair"}},
            {"mask_access", new List<string> {"glasses", "hair", "decal_logo", "cape", "chr_player_healer_mask", "facial_hair"}},
            {"glasses", new List<string> {"hat", "cap", "npc_waltz_torso_a_glasses_addon", "headwear", "mask", "hair", "facial_hair"}},
            {"necklace", new List<string> {"bandana"}},
            {"earrings", new List<string> {"bandana", "torso"}},
            {"rings", new List<string> {"bandana", "torso", "earing", "earring"}},
            {"hair", new List<string> {"cap", "mask", "glasses", "facial_hair", "decal"}},
            {"hair_base", new List<string> {"cap", "headwear", "mask", "torso", "glasses", "decal", "facial_hair", "young_hair", "fringe", "sides"}},
            {"hair_2", new List<string> {"cap", "headwear", "mask", "glasses", "decal", "facial_hair", "young_hair", "fringe", "base"}},
            {"hair_3", new List<string> {"cap", "headwear", "mask", "glasses", "decal", "facial_hair", "young_hair", "sides", "base"}},
            {"facial_hair", new List<string> {"hat", "cap", "headwear", "mask", "decal", "glasses" }},
            {"cape", new List<string> {"mask", "spikes", "belts", "scarf_a_part_d", "bags", "scarf_a_part_c"}},
            {"torso", new List<string> {"armor", "sleeve", "plates", "wrench", "detail", "vest", "skirt", "center",  "wmn_torso_e", "bracer", "bracers", "sh_npc_anderson", "wmn_torso_i", "bottom", "base", "front_", "fronttop",  "torso_c_sweater", "rag", "stethoscope", "plaguewitch", "centre", "nuwa_torso_a", "horns", "wpntmp", "equipment", "head", "hair", "shoulderpad", "player_camo_torso_a_tpp", "hat", "mask", "player_inquisitor_torso_a_tpp", "player_torso_tpp_a", "addon", "machete_tpp", "skull", "chain_armour", "hands", "battery", "arms", "cape", "turtleneck", "apron", "pants", "backpack", "bag", "parachute", "suspenders", "legs", "hood", "spikes", "chain", "decal", "collar", "scarf", "pouch", "zipper", "belt", "part", "patch", "bag", "pocket", "jewelry", "necklace", "ring"}},
            {"torso_2", new List<string> {"armor", "sleeve", "plates", "wrench", "detail", "bracer", "bracers", "sh_npc_anderson", "bottom", "base", "torso_c_sweater", "stethoscope", "plaguewitch", "horns", "wpntmp", "equipment", "head", "hair", "shoulderpad", "player_camo_torso_a_tpp", "hat", "mask", "player_inquisitor_torso_a_tpp", "player_torso_tpp_a", "addon", "machete_tpp", "skull", "chain_armour", "hands", "battery", "arms", "cape", "turtleneck", "apron", "pants", "backpack", "bag", "parachute", "suspenders", "legs", "hood", "spikes", "chain", "decal", "collar", "scarf", "pouch", "zipper", "belt", "part", "patch", "bag", "pocket", "jewelry", "necklace", "ring"}},
            {"torso_extra", new List<string> {"armor", "mask", "gloves", "sleeve", "hands", "battery", "arms", "cape", "pants", "backpack", "bag", "parachute", "legs", "hood", "spikes", "chain", "decal", "collar", "scarf", "pouch", "zipper", "belt", "bag", "pocket", "jewelry", "necklace", "ring"}},
            {"torso_access", new List<string> {"pants", "leg", "legs", "shoes", "man_bdt_chain_i", "bracken_bandage", "man_bdt_chain_g", "man_bdt_chain_h", "man_bdt_chain_f"}},
            {"hands", new List<string> {"sleeve", "upper_arms", "pk", "man_srv_arms_a", "man_srv_torso_b_arms", "_left", "belts", "chains", "elbow", "_right", "armor", "decal_tattoo", "decal"}},
            {"lhand", new List<string> {"_right", "upper", "headwear", "belts", "wrapper", "bracer", "bracers", "chains", "elbow", "balaclava", "shoes", "sleeve", "armor", "glove", "decal_tattoo", "tattoo", "torso", "leg", "legs", "pants", "hand", "decal"}},
            {"rhand", new List<string> {"_left", "upper", "headwear", "belts", "wrapper", "bracer", "bracers", "chains", "elbow", "balaclava", "shoes", "sleeve", "armor", "glove", "decal_tattoo", "tattoo", "torso", "leg", "legs", "pants", "hand", "decal"}},
            {"tattoo", new List<string> {"mask"}},
            {"gloves", new List<string> {"arm","hand", "decal", "addon", "player_camo_gloves_a_tpp"}},
            {"arm_access", new List<string> {"torso"}},
            {"sleeve", new List<string> {"decal", "logo", "headwear", "gloves", "pants", "shoes", "leg", "addon", "torso_armor"}},
            {"backpack", new List<string> {"pants"}},
            {"decals", new List<string> {"mask"}},
            {"decals_extra", new List<string> {"mask"}},
            {"decals_graphic", new List<string> {"mask"}},
            {"legs", new List<string> {"mask", "shoes", "player_legs_a", "sc_ov_legs", "feet", "part", "child_torso_a_bottom", "child_pants_b", "player_camo_pants_a_tpp", "arm", "chain", "elbow", "_right", "npc_jack_legs_adds", "bandage", "sleeve", "patch", "add_", "armor", "glove", "addon", "bag", "tapes", "man_srv_legs_b_bottom_a", "element", "hand", "decal", "equipment", "pocket", "pouch", "element", "decal_logo", "belt", "pad", "pants_b_rag", "bumbag", "bag", "patch_"}},
            {"legs_extra", new List<string> {"armor", "man_bdt_belt_d_pouches_a", "hair", "chainmail", "bracken_bandage", "man_bdt_belt_g", "man_bdt_belt_c_addon_a", "man_bdt_belt_c", "man_bzr_belt_c", "man_bdt_belt_d", "man_bzr_belt_a", "man_srv_belt_bags_a", "torso", "elbow", "decal_logo", "shoes", "arm", "sleeve", "armor", "glove", "torso", "hand", "hat"}},
            {"legs_access", new List<string> {"armor", "man_bdt_belt_d_pouches_a", "hair", "chainmail", "bracken_bandage", "man_bdt_belt_g", "man_bdt_belt_c_addon_a", "man_bdt_belt_c", "man_bzr_belt_c", "man_bdt_belt_d", "man_bzr_belt_a", "man_srv_belt_bags_a", "torso", "elbow", "decal_logo", "shoes", "arm", "sleeve", "armor", "glove", "torso", "hand", "hat"}},
            {"shoes", new List<string> {"mask"}},
            {"armor_helmet", new List<string> {"hat", "cap", "part", "sh_man_pk_headcover_c", "emblem", "man_bdt_headcover_i", "bandana", "beanie", "hood", "headband", "torso", "glasses", "hair", "decal", "cape"}},
            {"armor_helmet_access", new List<string> {"hat", "cap", "sh_man_pk_headcover_c", "man_bdt_headcover_i", "beanie", "hood", "torso", "glasses", "hair", "cape"}},
            {"armor_torso", new List<string> {"legs", "pants", "bottom", "armor_b_head", "pouches", "walkietalkie", "pin", "leg", "upper", "element", "bottom_a_armor", "cape", "addon", "upperleft", "lowerright", "lowerleft", "upperright"}},
            {"armor_torso_access", new List<string> {"legs", "pants", "mask", "scarf", "hair", "bottom", "pouches", "walkietalkie", "pin", "leg", "upper", "element", "bottom_a_armor", "cape", "upperleft", "lowerright", "lowerleft", "upperright"}},
            {"armor_torso_upperright", new List<string> {"legs", "pants", "leg"}},
            {"armor_torso_upperleft", new List<string> {"legs", "pants", "leg"}},
            {"armor_torso_lowerright", new List<string> {"legs", "pants", "leg"}},
            {"armor_torso_lowerleft", new List<string> {"legs", "pants", "leg"}},
            {"armor_legs", new List<string> {"mask"}},
            {"armor_legs_upperright", new List<string> { "lowerleft", "lowerright", "upperleft", "arm", "arms"}},
            {"armor_legs_upperleft", new List<string> { "lowerleft", "lowerright", "upperright", "mask", "arm", "arms" }},
            {"armor_legs_lowerright", new List<string> { "lowerleft", "upperright", "upperleft", "mask", "arm", "arms" }},
            {"armor_legs_lowerleft", new List<string> { "upperright", "lowerright", "upperleft", "mask", "arm", "arms" }}
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

        storeClassData = EditorGUILayout.Toggle("Store Class Data", storeClassData);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create ALL jsons"))
        {
            ReorganizeAndCombineJsonFiles();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Build Unique Skeleton Jsons"))
        {
            BuildUniqueSkeletonJsons();
        }
    }

    void OrganizeJsonFiles()
    {
        storeClassData = EditorGUILayout.Toggle("Store Class Data", storeClassData);
        string jsonsDir = Path.Combine(Application.dataPath, "StreamingAssets/Jsons");
        List<string> jsonFiles = new List<string>();
        foreach (var typeDir in Directory.GetDirectories(jsonsDir))
        {
            foreach (var categoryDir in Directory.GetDirectories(typeDir))
            {
                jsonFiles.AddRange(Directory.GetFiles(categoryDir, "*.json"));
            }
        }
        Dictionary<string, Dictionary<string, List<string>>> modelsSortedByCategory = new Dictionary<string, Dictionary<string, List<string>>>();
        HashSet<string> unsortedModels = new HashSet<string>();
        HashSet<string> ignoreList = new HashSet<string> { "player_legs_a.msh", "player_camo_bracers_a_tpp.msh",  "man_bdt_torso_c_shawl_b.msh", "chr_player_healer_mask.msh", "reporter_woman_old_skeleton.msh", "player_camo_gloves_a_tpp.msh", "player_camo_headwear_a_tpp.msh", "player_camo_hood_a_tpp.msh", "player_camo_pants_a_tpp.msh", "npc_colonel_coat_b.msh" };
        Dictionary<string, List<string>> modelToFilterLookup = new Dictionary<string, List<string>>();
        Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> modelsByClassAndFilter = new Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>();

        Dictionary<string, string> specificTermsToCategory = new Dictionary<string, string>
{
    { "young", "Child" },
    { "destroyed", "Biter" },
    { "waltz_young", "Man" },
    { "npc_mq_kiddie", "Man" }
    // Add more terms as needed
};

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

                string modelClass = modelData.modelProperties.@class;

                foreach (var slotPair in modelData.slotPairs)
                {
                    foreach (var model in slotPair.slotData.models)
                    {
                        string modelName = model.name.ToLower();
                        SortModel(modelName, modelsSortedByCategory, unsortedModels, modelToFilterLookup, ignoreList, specificTermsToCategory, modelData, modelsByClassAndFilter);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing file {file}: {ex.Message}");
            }
        }

        SaveSortedModels(modelsSortedByCategory, modelsByClassAndFilter, storeClassData);
        SaveUnsortedModels(unsortedModels);
        SaveModelFilterLookup(modelToFilterLookup);
        ReorganizeAndCombineJsonFiles();
        Debug.Log("JSON files organized and saved.");
    }

    void SortModel(string modelName, Dictionary<string, Dictionary<string, List<string>>> modelsSortedByCategory, HashSet<string> unsortedModels, Dictionary<string, List<string>> modelToFilterLookup, HashSet<string> ignoreList, Dictionary<string, string> specificTermsToCategory, ModelData modelData, Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> modelsByClassAndFilter)
    {

        if (ignoreList.Contains(modelName))
        {
            Debug.Log($"Model '{modelName}' is in the ignore list and will be skipped.");
            return;
        }

        string category = specificTermsToCategory.FirstOrDefault(pair => modelName.Contains(pair.Key)).Value ?? DetermineModelCategory(modelName, categoryPrefixes);
        if (!modelsSortedByCategory.ContainsKey(category))
        {
            modelsSortedByCategory[category] = new Dictionary<string, List<string>>();
        }

        bool matchedAnyFilter = false;

        string modelClass = string.Empty;
        if (storeClassData && !string.IsNullOrEmpty(modelData.modelProperties.@class))
        {
            modelClass = modelData.modelProperties.@class.ToLower();
            if (!modelsByClassAndFilter.ContainsKey(category))
            {
                modelsByClassAndFilter[category] = new Dictionary<string, Dictionary<string, List<string>>>();
            }
            if (!modelsByClassAndFilter[category].ContainsKey(modelClass))
            {
                modelsByClassAndFilter[category][modelClass] = new Dictionary<string, List<string>>();
            }
        }

        foreach (var filterPair in filters)
        {
            string filterName = filterPair.Key;
            List<string> filterTerms = filterPair.Value;
            foreach (var filterTerm in filterTerms)
            {
                if (modelName.Contains(filterTerm) && !(exclude_filters.ContainsKey(filterName) && exclude_filters[filterName].Any(excludeTerm => modelName.Contains(excludeTerm))))
                {
                    if (!modelsSortedByCategory[category].ContainsKey(filterName))
                    {
                        modelsSortedByCategory[category][filterName] = new List<string>();
                    }
                    modelsSortedByCategory[category][filterName].Add(modelName);
                    if (!modelToFilterLookup.ContainsKey(modelName))
                    {
                        modelToFilterLookup[modelName] = new List<string>();
                    }
                    if (!modelToFilterLookup[modelName].Contains(filterName))
                    {
                        modelToFilterLookup[modelName].Add(filterName);
                    }

                    if (storeClassData)
                    {
                        if (!modelsByClassAndFilter[category][modelClass].ContainsKey(filterName))
                        {
                            modelsByClassAndFilter[category][modelClass][filterName] = new List<string>();
                        }
                        modelsByClassAndFilter[category][modelClass][filterName].Add(modelName);
                    }

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

    void SaveSortedModels(Dictionary<string, Dictionary<string, List<string>>> modelsSortedByCategory,
                      Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> modelsByClassAndFilter,
                      bool storeClassData)
    {
        HashSet<string> classSkipList = new HashSet<string> { "none", "player", "npc", "viral", "biter" };
        Dictionary<string, string> classIdentifiers = new Dictionary<string, string>
{
    { "peacekeeper", "_pk_" },
    { "bandit", "_bdt_" },
    { "renegade", "_ren_" },
    { "scavenger", "_sc_" },
    { "survivor", "_srv_" }
};

        // Iterate through each category
        foreach (var category in modelsSortedByCategory.Keys)
        {
            string type = DetermineType(category);
            string categoryDir = Path.Combine(Application.dataPath, $"StreamingAssets/SlotData/{type}/{category}");
            Directory.CreateDirectory(categoryDir);

            // Save general category models
            foreach (var filter in modelsSortedByCategory[category].Keys)
            {
                List<string> sortedModels = modelsSortedByCategory[category][filter].Distinct().ToList();
                sortedModels.Sort();

                string filePath = Path.Combine(categoryDir, $"ALL_{filter}.json");
                string jsonContent = JsonConvert.SerializeObject(new { meshes = sortedModels }, Formatting.Indented);
                File.WriteAllText(filePath, jsonContent);
            }

            // Save class-specific models if storeClassData is true
            if (storeClassData && modelsByClassAndFilter.ContainsKey(category))
            {
                foreach (var classEntry in modelsByClassAndFilter[category])
                {
                    if (classSkipList.Contains(classEntry.Key))
                    {
                        continue; // Skip this class
                    }

                    string classDir = Path.Combine(categoryDir, classEntry.Key);
                    Directory.CreateDirectory(classDir);

                    foreach (var filterEntry in classEntry.Value)
                    {
                        // Filter models based on the class-specific identifier
                        string classIdentifier = classIdentifiers.ContainsKey(classEntry.Key) ? classIdentifiers[classEntry.Key] : "";
                        List<string> classSortedModels = filterEntry.Value
                            .Where(model => model.Contains(classIdentifier))
                            .Distinct().ToList();
                        classSortedModels.Sort();

                        // Only write file if classSortedModels is not empty
                        if (classSortedModels.Any())
                        {
                            string classFilePath = Path.Combine(classDir, $"ALL_{filterEntry.Key}.json");
                            string classJsonContent = JsonConvert.SerializeObject(new { meshes = classSortedModels }, Formatting.Indented);
                            File.WriteAllText(classFilePath, classJsonContent);
                        }
                    }
                }
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

    private void BuildUniqueSkeletonJsons()
    {
        Debug.Log($"Started Building Unique Skeleton Jsons");
        // Initialize storeClassData based on your needs
        storeClassData = EditorGUILayout.Toggle("Store Class Data", storeClassData);

        // Define the directory and gather JSON files
        string jsonsDir = Path.Combine(Application.dataPath, "StreamingAssets/Jsons");
        List<string> jsonFiles = new List<string>();
        Dictionary<string, HashSet<string>> allSkeletonMeshes = new Dictionary<string, HashSet<string>>();
        Dictionary<string, List<string>> meshOccurrences = new Dictionary<string, List<string>>();
        foreach (var typeDir in Directory.GetDirectories(jsonsDir))
        {
            foreach (var categoryDir in Directory.GetDirectories(typeDir))
            {
                jsonFiles.AddRange(Directory.GetFiles(categoryDir, "*.json"));
            }
        }

        Debug.Log($"jsonsDir {jsonsDir}");

        string outputDir = Path.Combine(Application.dataPath, "StreamingAssets/Skeleton Data/");
        // Ensure the output directory exists
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        // List of target skeleton names
        HashSet<string> targetSkeletonNames = new HashSet<string>
    {
        "man_bdt_heavy_coat_skeleton.msh",
        "man_bdt_heavy_skeleton.msh",
        "man_bdt_heavy_torso_d_skeleton.msh",
        "man_bdt_medium_skeleton.msh",
        "man_pk_heavy_skeleton.msh",
        "man_pk_medium_skeleton.msh",
        "man_sc_heavy_skeleton.msh",
        "man_sc_medium_skeleton.msh",
        "man_srv_heavy_skeleton.msh",
        "man_srv_medium_skeleton.msh",
        "man_srv_skinybiter_skeleton.msh",
        "man_zmb_heavy_skeleton.msh",
        "man_zmb_medium_skeleton.msh",
        "woman_npc_meredith_skeleton.msh",
        "woman_npc_singer_skeleton.msh"
    };

        // Dictionary to store meshes for each of the target skeletons
        Dictionary<string, HashSet<string>> skeletonMeshes = new Dictionary<string, HashSet<string>>();

        // Process each JSON file
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

                string skeletonName = modelData.skeletonName;

                foreach (var slotPair in modelData.slotPairs)
                {
                    foreach (var model in slotPair.slotData.models)
                    {
                        string modelName = model.name.ToLower();

                        // Update mesh occurrences without filtering by targetSkeletonNames
                        if (!meshOccurrences.ContainsKey(modelName))
                        {
                            meshOccurrences[modelName] = new List<string>();
                        }
                        if (!meshOccurrences[modelName].Contains(skeletonName))
                        {
                            meshOccurrences[modelName].Add(skeletonName);
                        }

                        // Only add to allSkeletonMeshes if skeleton is a target
                        if (targetSkeletonNames.Contains(skeletonName))
                        {
                            if (!allSkeletonMeshes.ContainsKey(skeletonName))
                            {
                                allSkeletonMeshes[skeletonName] = new HashSet<string>();
                            }
                            allSkeletonMeshes[skeletonName].Add(modelName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing file {file}: {ex.Message}");
            }
        }

        // Dictionary to store unique meshes for each target skeleton
        Dictionary<string, HashSet<string>> uniqueSkeletonMeshes = new Dictionary<string, HashSet<string>>();

        // Determine unique meshes for each target skeleton
        foreach (var targetSkeleton in targetSkeletonNames)
        {
            uniqueSkeletonMeshes[targetSkeleton] = new HashSet<string>();

            if (allSkeletonMeshes.ContainsKey(targetSkeleton))
            {
                foreach (var mesh in allSkeletonMeshes[targetSkeleton])
                {
                    // A mesh is unique if it is only associated with the current target skeleton
                    if (meshOccurrences.ContainsKey(mesh) && meshOccurrences[mesh].Count == 1 && meshOccurrences[mesh][0] == targetSkeleton)
                    {
                        uniqueSkeletonMeshes[targetSkeleton].Add(mesh);
                    }
                }
            }
        }

        // Write unique meshes for each target skeleton to files
        foreach (var skeleton in uniqueSkeletonMeshes.Keys)
        {
            var sortedMeshes = uniqueSkeletonMeshes[skeleton].OrderBy(mesh => mesh).ToList();
            string skeletonFileName = Path.GetFileNameWithoutExtension(skeleton);
            string outputFile = Path.Combine(outputDir, $"{skeletonFileName}.json");

            var meshData = new { mesh = sortedMeshes };

            string jsonContent = JsonConvert.SerializeObject(meshData, Formatting.Indented);

            File.WriteAllText(outputFile, jsonContent);

            Debug.Log($"Unique meshes for skeleton '{skeleton}' written to: {outputFile}");
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

    void ReorganizeAndCombineJsonFiles()
    {
        string slotDataPath = Path.Combine(Application.dataPath, "StreamingAssets/SlotData");

        // Create Human and Infected folders
        string humanPath = Path.Combine(slotDataPath, "Human");
        string infectedPath = Path.Combine(slotDataPath, "Infected");

        // Combine ALL_{filters}.json files
        CombineJsonFiles(humanPath);
        CombineJsonFiles(infectedPath);
    }

    void CreateDirectoryIfNotExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    void MergeDirectories(string sourceDir, string destDir)
    {
        // Create the destination directory if it doesn't exist
        CreateDirectoryIfNotExists(destDir);

        // Move each file and subdirectory from source to destination
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            if (File.Exists(destFile))
            {
                File.Delete(destFile);
            }
            File.Move(file, destFile);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(directory));
            MergeDirectories(directory, destSubDir);
        }

        // Optionally, delete the source directory if now empty
        if (Directory.GetFileSystemEntries(sourceDir).Length == 0)
        {
            Directory.Delete(sourceDir);
        }
    }

    void CombineJsonFiles(string parentFolderPath)
    {
        // Retrieve all subfolder paths
        var subFolders = Directory.GetDirectories(parentFolderPath);

        // Dictionary to store combined data
        var combinedData = new Dictionary<string, List<string>>();

        foreach (var folder in subFolders)
        {
            // Skip the "Child" folder
            if (Path.GetFileName(folder).Equals("Child", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var jsonFiles = Directory.GetFiles(folder, "ALL_*.json");
            foreach (var file in jsonFiles)
            {
                string fileName = Path.GetFileName(file);
                if (!combinedData.ContainsKey(fileName))
                {
                    combinedData[fileName] = new List<string>();
                }

                var jsonData = File.ReadAllText(file);
                var allFiltersModelData = JsonConvert.DeserializeObject<AllFiltersModelData>(jsonData);
                combinedData[fileName].AddRange(allFiltersModelData.meshes);
            }
        }


        // Write combined data to new JSON files
        foreach (var entry in combinedData)
        {
            string combinedFilePath = Path.Combine(parentFolderPath, entry.Key);
            string jsonContent = JsonConvert.SerializeObject(new { meshes = entry.Value.Distinct().ToList() }, Formatting.Indented);
            File.WriteAllText(combinedFilePath, jsonContent);
        }
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

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(modelFile);
            var processedData = ProcessModelData(modelObject);
            string outputJson = JsonConvert.SerializeObject(processedData, Newtonsoft.Json.Formatting.Indented);

            // Determine category using file name, class, and sex
            string category = DetermineCategory(processedData.modelProperties.@class, processedData.modelProperties.sex, fileNameWithoutExtension);
            string type = DetermineType(category);

            // Create directories if they don't exist
            string outputDir = Path.Combine(Application.dataPath, "StreamingAssets/Jsons", type, category);
            Directory.CreateDirectory(outputDir);

            string outputFilePath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(modelFile) + ".json");
            File.WriteAllText(outputFilePath, outputJson);
            Debug.Log($"Processed data saved to: {outputFilePath}");
        }
    }

    string DetermineCategory(string modelClass, string sex, string fileName)
    {
        fileName = fileName.ToLower();

        // First, try to determine the category based on the file name
        foreach (var prefix in categoryPrefixes)
        {
            if (categoryPrefixes[prefix.Key].Any(p => fileName.Contains(p.ToLower())))
            {
                return prefix.Key;
            }
        }

        // If the file name check fails, fallback to modelClass
        if (!string.IsNullOrEmpty(modelClass))
        {
            modelClass = modelClass.ToLower();
            foreach (var prefix in categoryPrefixes)
            {
                if (categoryPrefixes[prefix.Key].Any(p => modelClass.Contains(p.ToLower())))
                {
                    return prefix.Key;
                }
            }
        }

        // Lastly, use sex as a secondary fallback
        return DetermineCategoryBySex(sex);
    }

    string DetermineCategoryBySex(string sex)
    {
        if (!string.IsNullOrEmpty(sex))
        {
            switch (sex.ToLower())
            {
                case "male":
                    return "Man";
                case "female":
                    return "Wmn";
            }
        }
        return "Unknown"; // Default category if no match is found
    }

    string DetermineType(string category)
    {
        string humanPath = Path.Combine(Application.dataPath, "StreamingAssets/Jsons", "Human");
        string infectedPath = Path.Combine(Application.dataPath, "StreamingAssets/Jsons", "Infected");
        var folderMappings = new Dictionary<string, string>
    {
        {"Player", humanPath},
        {"Man", humanPath},
        {"Wmn", humanPath},
        {"Child", humanPath},
        {"Biter", infectedPath},
        {"Viral", infectedPath},
        {"Special Infected", infectedPath}
    };
        return folderMappings.TryGetValue(category, out string typePath) ? Path.GetFileName(typePath) : "Unknown";
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