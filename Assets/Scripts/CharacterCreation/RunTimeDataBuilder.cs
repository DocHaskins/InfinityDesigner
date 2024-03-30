using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static ModelData;

public class RunTimeDataBuilder : MonoBehaviour
{
    private string selectedFolder;
    private bool storeClassData = true;
    private Dictionary<string, List<string>> filters;
    private Dictionary<string, string[]> categoryPrefixes;
    private Dictionary<string, List<string>> exclude_filters;
    private static string materialsDataDir = Path.Combine(Application.dataPath, "StreamingAssets/Mesh References");
    Dictionary<string, List<string>> modelToFilterLookup = new Dictionary<string, List<string>>();
    Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> modelsByClassAndFilter = new Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>();
    private Dictionary<string, string> skeletonToCategory = new Dictionary<string, string>
{
    {"child_skeleton", "Child"},{"child_skeleton_mia", "Child"},{"man_basic_skeleton", "Man"},{"man_basic_skeleton_no_sh", "Man"},{"man_bdt_heavy_coat_skeleton", "Man"},{"man_bdt_heavy_skeleton", "Man"},{"man_bdt_heavy_torso_d_skeleton", "Man"},{"man_bdt_light_skeleton", "Man"},{"man_bdt_medium_skeleton", "Man"},{"man_bdt_skeleton", "Man"},{"man_npc_hakon_arrow_skeleton", "Man"},{"man_npc_skeleton", "Man"},{"man_pk_heavy_skeleton", "Man"},{"man_pk_light_skeleton", "Man"},{"man_pk_medium_skeleton", "Man"},{"man_pk_skeleton", "Man"},{"man_plr_skeleton", "Man"},{"man_sc_heavy_skeleton", "Man"},{"man_sc_light_skeleton", "Man"},{"man_sc_medium_skeleton", "Man"},{"man_sc_skeleton", "Man"},{"man_skeleton", "Man"},{"man_srv_heavy_skeleton", "Man"},{"man_srv_light_skeleton", "Man"},{"man_srv_medium_skeleton", "Man"},{"man_srv_skeleton", "Man"},{"npc_frank_skeleton", "Man"},{"npc_hakon_sh_skeleton", "Man"},{"player_fpp_phx_skeleton", "Player"},{"player_fpp_skeleton", "Player"},{"player_phx_skeleton", "Player"},{"player_skeleton", "Player"},{"viral_skeleton", "Viral"},{"woman_basic_skeleton", "Wmn"},{"woman_light_skeleton", "Wmn"},{"woman_npc_meredith_skeleton", "Wmn"},{"woman_npc_singer_skeleton", "Wmn"},{"woman_sc_skeleton", "Wmn"},{"woman_skeleton", "Wmn"},{"woman_srv_skeleton", "Wmn"},{"zmb_banshee_skeleton", "Special Infected"},{"zmb_bolter_skeleton", "Special Infected"},{"zmb_charger_skeleton", "Special Infected"},{"zmb_corruptor_skeleton", "Special Infected"},{"zmb_demolisher_phx_skeleton", "Special Infected"},{"zmb_demolisher_skeleton", "Special Infected"},{"zmb_goon_skeleton", "Special Infected"},{"zmb_screamer_skeleton", "Special Infected"},{"zmb_spitter_skeleton", "Special Infected"},{"zmb_spitter_tier_a_skeleton", "Special Infected"},{"zmb_suicider_skeleton", "Special Infected"},{"zmb_volataile_hive_skeleton", "Special Infected"},{"zmb_volataile_skeleton", "Special Infected"},{"man_zmb_heavy_skeleton", "Biter"},{"man_zmb_light_skeleton", "Biter"},{"man_zmb_medium_skeleton", "Biter"},{"man_zmb_skeleton", "Biter"},
{"woman_zmb_skeleton", "Biter"},{"man_srv_skinybiter_skeleton", "Biter"}
    };

    private void Start()
    {
        InitializeDictionaries();
    }

    private void InitializeDictionaries()
    {
        filters = new Dictionary<string, List<string>>()
        {
            {"head", new List<string> {"sh_scan_", "sh_npc_", "sh_wmn_", "aiden_young", "sh_", "sh_man_", "sh_biter_", "sh_man_viral_a", "head", "sh_biter_deg_b", "zmb_volatile_a"}},
            {"hat", new List<string> {"hat", "cap", "headwear", "blisters", "bandana", "beret", "bracken_bandage", "beanie", "hood", "sh_man_pk_headcover_c", "headband", "man_bdt_headcover_i", "coverlet"}},
            {"hat_access", new List<string> {"hat", "cap", "goggles", "headwear", "blisters", "bandana", "horns", "beret", "bracken_bandage", "beanie", "hood", "sh_man_pk_headcover_c", "headband", "man_bdt_headcover_i", "coverlet"}},
            {"hood", new List<string> {"hood"}},
            {"mask", new List<string> {"mask", "balaclava", "blisters", "scarf_a_part_d", "npc_skullface_helmet_basic" }},
            {"mask_access", new List<string> {"mask", "balaclava", "blisters", "scarf_a_part_d", "npc_skullface_helmet_basic" }},
            {"glasses", new List<string> {"glasses", "goggles", "man_ren_scarf_a_part_c", "player_tank_headwear_a_tpp"}},
            {"necklace", new List<string> {"jewelry", "necklace", "man_bdt_chain_i", "man_bdt_chain_g", "man_bdt_chain_h", "man_bdt_chain_f", "stethoscope"}},
            {"earrings", new List<string> {"earing", "earring"}},
            {"rings", new List<string> {"npc_man_ring"}},
            {"hair", new List<string> {"hair", "npc_aiden_hair_headwear", "extension"}},
            {"hair_base", new List<string> {"base", "ponytail", "headband", "bun"}},
            {"hair_2", new List<string> {"sides", "braids", "ponytail", "headband", "bun", "extension" }},
            {"hair_3", new List<string> {"fringe", "ponytail", "headband", "bun", "extension" }},
            {"facial_hair", new List<string> {"facial_hair", "beard", "jaw_"}},
            {"cape", new List<string> {"cape", "scarf", "chainmail", "sweater", "shawl", "shalw", "choker", "cloak", "npc_man_torso_g_hood_b"}},
            {"chest", new List<string> { "torso_body_a", "player_torso_body_a", "npc_frank_torso_wounded", "man_sc_worker_torso_body_a", "man_torso_muscular_a", "man_torso_fat_a", "player_wrestler_torso_tank_top_a_tpp", "tank_top_body", "man_bdt_torso_leader_a" }},
            {"torso", new List<string> {"torso", "npc_anderson", "singer_top", "player_reload_outfit", "singer_top", "npc_frank", "bolter", "goon", "charger_body", "corruptor", "screamer", "spitter", "viral", "volatile", "zmb_suicider_corpse"}},
            {"torso_2", new List<string> { "torso", "centre", "vest", "wmn_torso_e", "blisters", "wmn_torso_i", "center", "top", "fronttop", "front", "dress", "coat", "jacket", "bubbles", "horns", "guts", "gastank"}},
            {"torso_extra", new List<string> {"torso", "addon", "accesories", "torso_c_bottom_a_armor_c", "collar", "feathers", "pocket", "sixshooter", "rag", "waist", "element", "sleeves", "belts", "blisters", "scarf", "wpntmp", "centre", "detail", "vest", "wmn_torso_e", "wmn_torso_i", "stethoscope", "center", "top", "fronttop", "front", "dress", "machete_tpp", "coat", "plates", "vest", "jacket", "bubbles", "horns", "guts", "apron", "suspenders", "gastank" }},
            {"torso_access", new List<string> {"belt", "accesories", "torso_c_bottom_a_armor_c", "feathers", "wpntmp", "element", "waist", "blisters", "scarf", "centre", "pin", "detail", "vest", "wmn_torso_e", "wmn_torso_i", "stethoscope", "center", "top", "fronttop", "front", "torso_b_cape_spikes", "spikes", "knife", "add_", "wrap", "npc_colonel_feathers_a", "axe", "npc_wmn_pants_b_torso_g_tank_top_bumbag_a", "zipper", "rag", "pouch", "collar", "neck", "pocket", "waist_shirt", "chain_armour", "skull_", "plates", "sport_bag", "gastank", "walkietalkie", "man_bdt_belt_c_addon_a", "chain", "battery", "torso_a_pk_top", "man_bdt_torso_d_shirt_c", "wrench", "suspenders", "part", "turtleneck", "npc_waltz_torso_a_glasses_addon", "apron"}},
            {"shirt", new List<string> {"shirt", "bracken_torso_a_tp", "npc_man_torso_k", "man_sc_runner_torso_a", "man_sc_ov_torso_a", "man_pk_torso_e", "rahim_torso_a", "crane_basic_torso_a" }},
            {"jacket", new List<string> {"jacket", "man_ren_torso_a", "npc_waltz_torso_a", "npc_man_torso_n", "npc_man_torso_m", "npc_man_torso_j", "man_sc_runner_torso_b", "man_sc_ov_torso_b", "man_ren_torso_c", "man_pk_torso_f", "man_pk_craftmaster_torso_b", "pk_torso_a_empty", "man_bdt_torso_d", "man_bdt_torso_f", "rais_torso", "rick_torso_a", "vest" }},
            {"flashlight", new List<string> {"flashlight"}},
            {"waist", new List<string> { "waist"}},
            {"weapons", new List<string> { "knife", "sixshooter", "axe", "shield", "sword", "machete"}},
            {"belts", new List<string> {"belt", "belts"}},
            {"hands", new List<string> {"hands", "arms"}},
            {"lhand", new List<string> {"arm", "left", "hand"}},
            {"rhand", new List<string> {"arm", "right", "hand"}},
            {"gloves", new List<string> { "player_army_gloves", "gloves", "arms_rag", "glove"}},
            {"gloves_2", new List<string> { "player_army_gloves", "gloves", "arms_rag", "glove"}},
            {"arm_access", new List<string> {"biomarker", "basic_watch", "bracelet", "wrapper", "npc_barney_band", "glove", "gloves", "gloves_a_addon"}},
            {"arm_access_2", new List<string> {"biomarker", "basic_watch", "bracelet", "wrapper", "npc_barney_band", "glove", "gloves", "gloves_a_addon" }},
            {"sleeve", new List<string> {"sleeve", "upper_arms", "sleeves", "forearm", "arm", "arms"}},
            {"backpack", new List<string> {"backpack", "bag", "parachute", "backback"}},
            {"bumbag", new List<string> { "bumbag", "bag"}},
            {"gastank", new List<string> { "gastank_tank"}},
            {"blisters_1", new List<string> { "blister", "blisters"}},
            {"blisters_2", new List<string> { "blister", "blisters"}},
            {"blisters_3", new List<string> { "blister", "blisters"}},
            {"blisters_4", new List<string> { "blister", "blisters"}},
            {"pockets", new List<string> {"pocket", "pockets"}},
            {"decals", new List<string> {"decal", "patch"}},
            {"decals_2", new List<string> {"decal", "patch"}},
            {"decals_extra", new List<string> {"decal", "patch"}},
            {"decals_logo", new List<string> {"logo"}},
            {"tattoo", new List<string> {"tatoo", "tattoo"}},
            {"tattoo_2", new List<string> {"tatoo", "tattoo"}},
            {"legs", new List<string> { "player_army_pants", "leg", "legs", "pants", "viral_body_legs_a", "trousers"}},
            {"legs_2", new List<string> { "player_army_pants", "leg", "legs", "pants", "viral_body_legs_a", "trousers"}},
            {"legs_extra", new List<string> {"pocket", "socks", "pants_a_add", "bottom", "viral_body_legs_a", "sc_ov_legs", "legs_a_part", "child_torso_a_bottom", "skirt", "man_srv_legs_b_bottom_a", "belt", "legs_c_add_", "legs_a_addon", "pants_b_rag"}},
            {"legs_access", new List<string> {"pocket", "socks", "bottom", "sc_ov_legs", "viral_body_legs_a", "legs_a_part", "pouch", "child_torso_a_bottom", "holster", "skirt", "chain", "equipment", "npc_jack_legs_adds", "man_srv_legs_b_bottom_a", "belt", "pad_", "legs_c_add_", "legs_a_addon", "pants_b_rag", "bumbag", "bag", "patch_", "bandage", "element", "tapes"}},
            {"shoes", new List<string> {"shoes", "shoe", "feet", "boots", "child_pants_b"}},
            {"armor_helmet", new List<string> {"helmet", "tank_headwear", "blisters", "armor_b_head", "headcover", "head_armor", "headgear", "npc_skullface_helmet_basic"}},
            {"armor_helmet_access", new List<string> {"helmet", "blisters", "mask", "headcover", "head_armor", "headgear", "npc_skullface_helmet_basic"}},
            {"armor_torso", new List<string> {"armor", "nuwa_torso_a"}},
            {"armor_torso_access", new List<string> {"armor", "addon"}},
            {"armor_torso_extra", new List<string> {"armor", "addon"}},
            {"armor_torso_upperright", new List<string> {"upperright", "right_fat", "upper_armor_a_right" }},
            {"armor_torso_upperleft", new List<string> {"upperleft", "upper_armor_a_left", "man_bandit_shoulders_armor_left_a", "shoulderpad"}},
            {"armor_torso_lowerright", new List<string> {"lowerright", "bracers", "bracer", "hand_tapes_a_right", "pad_a_r", "elbow_pad_a_normal_r", "elbow_pad_a_muscular_r"}},
            {"armor_torso_lowerleft", new List<string> {"lowerleft", "bracers", "npc_skullface_shield", "pad_a_l", "hand_tapes_a_left", "elbow_pad_a_muscular_l", "elbow_pad_a_normal_l"}},
            {"armor_legs", new List<string> { "legs_armor", "legs_b_armor", "leggings_a_pad", "leg_armor", "pants_a_pad", "pants_armor", "legs_a_armor", "pants_b_armor",  "leg_armor", "pants_a_armor"}},
            {"armor_legs_access", new List<string> { "legs_armor", "legs_b_armor", "leggings_a_pad", "leg_armor", "pants_a_pad", "pants_armor", "legs_a_armor", "pants_b_armor",  "leg_armor", "pants_a_armor"}},
            {"armor_legs_upperright", new List<string> { "armor"}},
            {"armor_legs_upperleft", new List<string> { "armor"}},
            {"armor_legs_lowerright", new List<string> { "armor"}},
            {"armor_legs_lowerleft", new List<string> { "armor"}}
        };

        exclude_filters = new Dictionary<string, List<string>>()
        {
            {"head", new List<string> {"hat", "cap", "headwear", "blisters", "headgear", "armor", "mask", "bdt_balaclava", "horns", "sh_benchmark_npc_hakon_cc", "beret", "glasses", "hair", "bandana", "facial_hair", "beard", "headcover", "emblem", "hat", "cap", "headwear", "bandana", "beanie", "hood"}},
            {"hat", new List<string> {"part", "tube", "emblem", "fringe", "base", "braids", "player_headwear_crane_b_tpp", "player_tank_headwear_a_tpp", "npc_man_torso_g_hood_b", "glasses", "hair", "decal_logo", "cape", "mask", "_addon", "decal", "facial_hair"}},
            {"hat_access", new List<string> {"player_headwear_crane_b_tpp", "player_tank_headwear_a_tpp", "npc_man_torso_g_hood_b", "glasses", "hair", "cape", "mask", "facial_hair"}},
            {"hood", new List<string> { "hat"}},
            {"mask", new List<string> {"glasses", "hair", "decal_logo", "cape", "_addon", "chr_player_healer_mask", "facial_hair"}},
            {"mask_access", new List<string> {"glasses", "hair", "decal_logo", "cape", "chr_player_healer_mask", "facial_hair"}},
            {"glasses", new List<string> {"hat", "cap", "npc_waltz_torso_a_glasses_addon", "headwear", "mask", "hair", "facial_hair"}},
            {"necklace", new List<string> {"bandana"}},
            {"earrings", new List<string> {"bandana", "torso"}},
            {"rings", new List<string> {"bandana", "torso", "earing", "earring"}},
            {"hair", new List<string> {"cap", "mask", "glasses", "facial_hair", "decal"}},
            {"hair_base", new List<string> {"cap", "headwear", "mask", "torso", "glasses", "decal", "facial_hair", "young_hair", "fringe", "sides"}},
            {"hair_2", new List<string> {"cap", "headwear", "mask", "glasses", "decal", "facial_hair", "young_hair"}},
            {"hair_3", new List<string> {"cap", "headwear", "mask", "glasses", "decal", "facial_hair", "young_hair"}},
            {"facial_hair", new List<string> {"hat", "cap", "headwear", "mask", "decal", "glasses" }},
            {"cape", new List<string> {"mask", "spikes", "belts", "scarf_a_part_d", "bags", "scarf_a_part_c"}},
            {"chest", new List<string> {"worker", "hands", "decal"}},
            {"torso", new List<string> {"armor", "sleeve", "sh_man_viral_tier3_a", "man_bdt_torso_d", "top_", "man_ren_torso_a", "man_sc_runner_torso_b", "man_bdt_torso_f", "bdt_torso_d_shirt_c", "accesories", "flashlight", "plates", "wrench", "jacket", "_adds", "upperright", "upperleft", "bracelet", "back_", "pin", "beard", "shawl", "detail", "vest", "skirt", "center",  "wmn_torso_e", "bracer", "bracers", "sh_npc_anderson", "wmn_torso_i", "bottom", "base", "front_", "fronttop",  "torso_c_sweater", "rag", "stethoscope", "plaguewitch", "centre", "nuwa_torso_a", "horns", "wpntmp", "equipment", "head", "hair", "shoulderpad", "hat", "mask", "player_inquisitor_torso_a_tpp", "player_torso_tpp_a", "addon", "machete_tpp", "skull", "chain_armour", "hands", "battery", "arms", "cape", "turtleneck", "apron", "pants", "backpack", "bag", "parachute", "suspenders", "legs", "hood", "spikes", "chain", "decal", "collar", "scarf", "pouch", "zipper", "belt", "part", "patch", "bag", "pocket", "jewelry", "necklace", "ring"}},
            {"torso_2", new List<string> {"armor", "sleeve", "plates", "wrench", "detail", "bracer", "bracers", "sh_npc_anderson", "base", "torso_c_sweater", "stethoscope", "plaguewitch", "horns", "wpntmp", "equipment", "head", "hair", "shoulderpad", "hat", "mask", "player_inquisitor_torso_a_tpp", "player_torso_tpp_a", "addon", "machete_tpp", "skull", "chain_armour", "hands", "battery", "arms", "cape", "turtleneck", "apron", "pants", "backpack", "bag", "parachute", "suspenders", "legs", "hood", "spikes", "chain", "decal", "collar", "scarf", "pouch", "zipper", "belt", "part", "patch", "bag", "pocket", "jewelry", "necklace", "ring"}},
            {"torso_extra", new List<string> {"armor", "mask", "horns", "hood", "gloves", "sleeve", "hands", "battery", "arms", "cape", "pants", "backpack", "bag", "parachute", "legs", "hood", "spikes", "chain", "decal", "collar", "scarf", "pouch", "zipper", "bag", "pocket", "jewelry", "necklace", "ring"}},
            {"torso_access", new List<string> {"pants", "leg", "legs", "shoes", "man_bdt_chain_i", "bracken_bandage", "man_bdt_chain_g", "man_bdt_chain_h", "man_bdt_chain_f"}},
            {"shirt", new List<string> {"mask", "sleeves", "decal", "waist", "armor", "addons", "hood", "suspenders", "necklace", "waist", "belt", "hood", "bottom", "man_bdt_torso_d_shirt_c" }},
            {"jacket", new List<string> {"decal", "pin", "belt", "spikes", "adds", "chain", "addon", "upperright", "collar", "front", "man_bdt_torso_d_shirt_c", "armor", "cape", "hood", "pouch", "bag", "belta", "detail", "sleeve", "hoodie", "bottom", "scarf", "necklace", "sleeves", "center", "addons", "shawl"}},
            {"waist", new List<string> {"mask"}},
            {"weapons", new List<string> {"mask"}},
            {"flashlight", new List<string> {"mask"}},
            {"belts", new List<string> {"mask"}},
            {"hands", new List<string> {"sleeve", "upper_arms", "pk", "man_srv_arms_a", "man_srv_torso_b_arms", "_left", "belts", "chains", "elbow", "_right", "armor", "decal_tattoo", "decal"}},
            {"lhand", new List<string> {"right", "upper", "headwear", "belts", "man_srv_arms_a", "wrapper", "bracer", "bracers", "chains", "elbow", "balaclava", "shoes", "sleeve", "armor", "glove", "decal_tattoo", "tattoo", "torso", "leg", "legs", "pants", "decal"}},
            {"rhand", new List<string> {"left", "upper", "headwear", "belts", "man_srv_arms_a", "wrapper", "bracer", "bracers", "chains", "elbow", "balaclava", "shoes", "sleeve", "armor", "glove", "decal_tattoo", "tattoo", "torso", "leg", "legs", "pants", "decal"}},
            {"gloves", new List<string> {"arm","hand", "decal", "addon"}},
            {"gloves_2", new List<string> {"arm","hand", "decal", "addon"}},
            {"arm_access", new List<string> {"torso"}},
            {"arm_access_2", new List<string> {"torso"}},
            {"sleeve", new List<string> {"decal", "logo", "headwear", "balaclava", "belts", "army", "element", "chain", "wrapper", "pouch", "chains", "gloves", "pants", "shoes", "leg", "addon", "armor", "torso_armor" }},
            {"backpack", new List<string> {"pants"}},
            {"bumbag", new List<string> {"backpack"}},
            {"gastank", new List<string> {"backpack", "headgear", "pants"}},
            {"blisters_1", new List<string> { "mask"}},
            {"blisters_2", new List<string> { "mask"}},
            {"blisters_3", new List<string> { "mask"}},
            {"blisters_4", new List<string> { "mask"}},
            {"pockets", new List<string> {"mask"}},
            {"decals", new List<string> {"mask"}},
            {"decals_2", new List<string> {"mask"}},
            {"decals_extra", new List<string> {"mask"}},
            {"decals_graphic", new List<string> {"mask"}},
            {"tattoo", new List<string> {"mask"}},
            {"tattoo_2", new List<string> {"mask"}},
            {"legs", new List<string> {"mask", "shoes", "player_legs_a", "sc_ov_legs", "feet", "part", "child_torso_a_bottom", "child_pants_b", "arm", "chain", "elbow", "_right", "npc_jack_legs_adds", "bandage", "sleeve", "patch", "add_", "armor", "glove", "addon", "bag", "tapes", "man_srv_legs_b_bottom_a", "element", "hand", "decal", "equipment", "pocket", "pouch", "element", "decal_logo", "belt", "pad", "pants_b_rag", "bumbag", "bag", "patch_"}},
            {"legs_2", new List<string> {"mask", "shoes", "player_legs_a", "sc_ov_legs", "feet", "part", "child_torso_a_bottom", "child_pants_b", "arm", "chain", "elbow", "_right", "npc_jack_legs_adds", "bandage", "sleeve", "patch", "add_", "armor", "glove", "addon", "bag", "tapes", "man_srv_legs_b_bottom_a", "element", "hand", "decal", "equipment", "pocket", "pouch", "element", "decal_logo", "belt", "pad", "pants_b_rag", "bumbag", "bag", "patch_"}},
            {"legs_extra", new List<string> {"armor", "man_bdt_belt_d_pouches_a", "hair", "chainmail", "bracken_bandage", "man_bdt_belt_g", "man_bdt_belt_c_addon_a", "man_bdt_belt_c", "man_bzr_belt_c", "man_bdt_belt_d", "man_bzr_belt_a", "man_srv_belt_bags_a", "torso", "elbow", "decal_logo", "shoes", "arm", "sleeve", "armor", "glove", "torso", "hand", "hat"}},
            {"legs_access", new List<string> {"armor", "man_bdt_belt_d_pouches_a", "hair", "chainmail", "bracken_bandage", "man_bdt_belt_g", "man_bdt_belt_c_addon_a", "man_bdt_belt_c", "man_bzr_belt_c", "man_bdt_belt_d", "man_bzr_belt_a", "man_srv_belt_bags_a", "torso", "elbow", "decal_logo", "shoes", "arm", "sleeve", "armor", "glove", "torso", "hand", "hat"}},
            {"shoes", new List<string> {"mask"}},
            {"armor_helmet", new List<string> {"hat", "cap", "part", "sh_man_pk_headcover_c", "emblem", "man_bdt_headcover_i", "bandana", "beanie", "hood", "headband", "torso", "glasses", "hair", "decal", "cape"}},
            {"armor_helmet_access", new List<string> {"hat", "cap", "sh_man_pk_headcover_c", "man_bdt_headcover_i", "beanie", "hood", "torso", "glasses", "hair", "cape"}},
            {"armor_torso", new List<string> {"legs", "pants", "bottom", "armor_b_head", "pouches", "walkietalkie", "pin", "leg", "upper", "element", "bottom_a_armor", "cape", "addon", "upperleft", "lowerright", "lowerleft", "upperright"}},
            {"armor_torso_access", new List<string> {"legs", "pants", "mask", "scarf", "hair", "pouches", "walkietalkie", "pin", "leg", "upper", "element", "bottom_a_armor", "cape", "upperleft", "lowerright", "lowerleft", "upperright"}},
            {"armor_torso_extra", new List<string> {"legs", "pants", "mask", "scarf", "hair", "pouches", "pin", "leg", "upper", "bottom_a_armor", "cape", "upperleft", "lowerright", "lowerleft", "upperright"}},
            {"armor_torso_upperright", new List<string> {"legs", "pants", "leg"}},
            {"armor_torso_upperleft", new List<string> {"legs", "pants", "leg"}},
            {"armor_torso_lowerright", new List<string> {"legs", "pants", "leg"}},
            {"armor_torso_lowerleft", new List<string> {"legs", "pants", "leg"}},
            {"armor_legs", new List<string> {"mask"}},
            {"armor_legs_access", new List<string> {"mask"}},
            {"armor_legs_upperright", new List<string> { "lowerleft", "lowerright", "upperleft", "arm", "arms"}},
            {"armor_legs_upperleft", new List<string> { "lowerleft", "lowerright", "upperright", "mask", "arm", "arms" }},
            {"armor_legs_lowerright", new List<string> { "lowerleft", "upperright", "upperleft", "mask", "arm", "arms" }},
            {"armor_legs_lowerleft", new List<string> { "upperright", "lowerright", "upperleft", "mask", "arm", "arms" }}
        };

        
    }

    public void ProcessModelsInFolder(string folderPath)
    {
        Task.Run(() =>
        {
            var ignoreList = new HashSet<string>
        {
            "heron.model", "sh_debug_face_anim.model", "hen.model", "goat.model",
            "gazelle_fem.model", "empty_model.model", "horse.model", "wolf.model",
            "rat.model", "roedeer.model", "polito_01.model", "dog_prototype.model", "goat.model"
        };

            string[] files = Directory.GetFiles(folderPath, "*.model", SearchOption.AllDirectories);
            List<Task> processingTasks = new List<Task>();

            foreach (string modelFile in files)
            {
                //Debug.Log($"ProcessModelsInFolder: modelFile: {modelFile}");
                processingTasks.Add(Task.Run(() =>
                {
                    if (ignoreList.Contains(Path.GetFileName(modelFile).ToLower()))
                    {
                        UnityMainThreadDispatcher.Instance.Enqueue(() => Debug.Log($"Skipped ignored model file: {modelFile}"));
                        return;
                    }

                    try
                    {
                        string jsonData = File.ReadAllText(modelFile);
                        JObject modelObject = JObject.Parse(jsonData);
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(modelFile);
                        var processedData = ProcessModelData(modelObject);
                        //Debug.Log($"ProcessModelsInFolder: Skeleton name: {processedData.skeletonName}");
                        // Now passing skeletonName to DetermineCategory
                        string category = DetermineCategory(processedData.skeletonName);
                        //Debug.Log($"ProcessModelsInFolder: category: {category}");
                        string normalizedFileName = fileNameWithoutExtension.ToLower();
                        if (category == "Man" || category == "Wmn")
                        {
                            if (normalizedFileName.Contains("_test_player"))
                            {
                                category = "Player";
                                //Debug.Log($"Category changed to Player due to name: {normalizedFileName}");
                            }
                            else if (normalizedFileName.Contains("zmb"))
                            {
                                category = "Biter";
                               //Debug.Log($"Category changed to Biter due to name: {normalizedFileName}");
                            }
                            else if (normalizedFileName.Contains("viral"))
                            {
                                category = "Viral";
                                //Debug.Log($"Category changed to Viral due to name: {normalizedFileName}");
                            }
                            if (category == "Biter" || category == "Viral")
                            {
                                if (normalizedFileName.Contains("npc_waltz"))
                                {
                                    category = "Man";
                                    //Debug.Log($"Category changed to Man due to name: {normalizedFileName}");
                                }
                            }
                        }
                        string type = DetermineType(category, fileNameWithoutExtension);

                        string outputDir = Path.Combine(Application.dataPath, "StreamingAssets/Jsons", type, category);
                        Directory.CreateDirectory(outputDir);

                        string outputFilePath = Path.Combine(outputDir, fileNameWithoutExtension + ".json");
                        //Debug.Log($"outputFilePath {outputFilePath}");
                        string outputJson = CreateJsonWithSkeletonNameAtTop(processedData);
                        File.WriteAllText(outputFilePath, outputJson);

                        UnityMainThreadDispatcher.Instance.Enqueue(() => Debug.Log($"Processed data saved to: {outputFilePath}"));
                    }
                    catch (Exception ex)
                    {
                        UnityMainThreadDispatcher.Instance.Enqueue(() => Debug.LogError($"Failed to process model file {modelFile}: {ex.Message}"));
                    }
                }));
            }

            Task.WhenAll(processingTasks).ContinueWith(t => UnityMainThreadDispatcher.Instance.Enqueue(OrganizeJsonFiles));
        });
    }


    public struct ProcessedModelData
    {
        public string skeletonName;
        public ModelProperties modelProperties;
        public List<SlotDataPair> slotPairs;
    }

    private ProcessedModelData ProcessModelData(JObject modelObject)
    {
        // Extract skeletonName from the preset object
        string skeletonName = modelObject["preset"]?["skeletonName"]?.ToString() ?? modelObject["skeletonName"]?.ToString();
        //Debug.Log($"ProcessModelData: Skeleton name: {skeletonName}");
        // Extract modelProperties from the data object
        JObject dataObject = modelObject["data"] as JObject;
        ModelProperties modelProperties = ExtractModelProperties(dataObject["properties"] as JArray);

        // Extract slotPairs directly from the root object
        List<SlotDataPair> slotPairs = ExtractSlotPairs(modelObject["slots"] as JArray);

        // Construct and return the ModelData object
        return new ProcessedModelData
        {
            skeletonName = skeletonName,
            modelProperties = modelProperties,
            slotPairs = slotPairs
        };
    }


    private string DetermineCategory(string skeletonName)
    {
        //Debug.Log($"DetermineCategory: Processing skeleton name: {skeletonName}");

        // Normalize skeletonName for comparison
        string normalizedSkeletonName = skeletonName.ToLower().Replace(".msh", "");

        foreach (var entry in skeletonToCategory)
        {
            string normalizedKey = entry.Key.ToLower();
            // Use exact match instead of Contains
            if (normalizedSkeletonName.Equals(normalizedKey))
            {
                //Debug.Log($"Exact match found: {normalizedSkeletonName} -> {entry.Value}");
                return entry.Value;
            }
        }

        Debug.Log($"No exact category match found for skeleton '{normalizedSkeletonName}'. Defaulting to 'Other'");
        return "Other";
    }

    private string DetermineType(string category, string fileName)
    {
        string normalizedFileName = fileName.ToLower();

        // Adjust category for specific cases based on filename content
        if (category == "Man")
        {
            if (normalizedFileName.Contains("wmn"))
            {
                category = "Wmn";
                //Debug.Log($"Category changed to Wmn due to name: {normalizedFileName}");
            }
        }

        if (category.Equals("Man") || category.Equals("Wmn"))
        {
            if (normalizedFileName.Contains("player_"))
            {
                category = "Player";
            }
            if (normalizedFileName.Contains("zmb"))
            {
                category = "Biter";
            }
            else if (normalizedFileName.Contains("viral"))
            {
                category = "Viral";
            }
        }

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

    private string CreateJsonWithSkeletonNameAtTop(ProcessedModelData data)
    {
        var jsonObj = new JObject
        {
            ["skeletonName"] = data.skeletonName,
            ["modelProperties"] = JToken.FromObject(data.modelProperties),
            ["slotPairs"] = JToken.FromObject(data.slotPairs)
        };

        return jsonObj.ToString(Formatting.Indented);
    }

    public static void CreateVariationJsons()
    {
        string jsonsDir = Path.Combine(Application.dataPath, "StreamingAssets/Jsons");
        string outputDir = Path.Combine(Application.dataPath, "StreamingAssets/Mesh References");

        // Use ConcurrentDictionary for thread-safe operations
        ConcurrentDictionary<string, VariationOutput> existingVariations = new ConcurrentDictionary<string, VariationOutput>();

        var jsonFiles = Directory.GetFiles(jsonsDir, "*.json", SearchOption.AllDirectories);

        // Use Parallel.ForEach for concurrent processing
        Parallel.ForEach(jsonFiles, (file) =>
        {
            ProcessJsonFile(file, outputDir, existingVariations);
        });

        // Once all tasks are complete, you might want to perform some finalization work here
        Debug.Log("All variation JSONs created.");
    }

    private static void ProcessJsonFile(string file, string outputDir, ConcurrentDictionary<string, VariationOutput> existingVariations)
    {
        string jsonContent = File.ReadAllText(file);
        ModelData modelData = JsonConvert.DeserializeObject<ModelData>(jsonContent);

        foreach (var slotPair in modelData.slotPairs)
        {
            foreach (var model in slotPair.slotData.models)
            {
                if (string.IsNullOrWhiteSpace(model.name)) continue;

                string baseMeshName = Path.GetFileNameWithoutExtension(model.name);
                string sanitizedMeshName = SanitizeFilename(baseMeshName);
                string outputFilePath = Path.Combine(outputDir, $"{sanitizedMeshName}.json");

                VariationOutput variationOutput = existingVariations.GetOrAdd(outputFilePath, new VariationOutput());

                // Thread-safe update or add new variations
                UpdateVariations(variationOutput, model);

                // Serialize and write the updated VariationOutput to the file
                string outputJson = JsonConvert.SerializeObject(variationOutput, Formatting.Indented);
                File.WriteAllText(outputFilePath, outputJson);
            }
        }
    }

    private static string SanitizeFilename(string filename)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            filename = filename.Replace(c, '_');
        }
        return filename;
    }

    private static void UpdateVariations(VariationOutput variationOutput, ModelInfo model)
    {
        if (variationOutput.materialsData == null || variationOutput.materialsData.Count == 0)
        {
            variationOutput.materialsData = model.materialsData;
        }

        // HashSet to keep track of unique variation signatures to prevent adding duplicates
        HashSet<string> existingVariationSignatures = new HashSet<string>();

        // Populate existingVariationSignatures with signatures of already processed variations
        foreach (var existingVariation in variationOutput.variations)
        {
            string existingSignature = GenerateVariationSignature(existingVariation);
            existingVariationSignatures.Add(existingSignature);
        }

        Variation newVariation = CreateNewVariationWithAllMaterials(variationOutput, model);

        if (DoesVariationIntroduceChanges(newVariation, model, existingVariationSignatures))
        {
            variationOutput.variations.Add(newVariation);
        }
    }

    private static string GenerateVariationSignature(Variation variation)
    {
        // Concatenate material names and RTTI values to form a unique signature for the variation
        return string.Join("|", variation.materialsResources.SelectMany(mr => mr.resources.Select(r =>
            $"{r.name}:{string.Join(",", r.rttiValues.Select(rtti => $"{rtti.name}={rtti.val_str}"))}")).OrderBy(name => name));
    }

    private static bool DoesVariationIntroduceChanges(Variation newVariation, ModelInfo model, HashSet<string> existingVariationSignatures)
    {
        if (DoesVariationMatchOriginalMaterialsWithoutRTTI(newVariation, model))
        {
            return false;
        }

        string variationSignature = GenerateVariationSignature(newVariation);

        if (existingVariationSignatures.Contains(variationSignature))
        {
            return false;
        }

        existingVariationSignatures.Add(variationSignature);
        return true;
    }

    private static bool DoesVariationMatchOriginalMaterialsWithoutRTTI(Variation variation, ModelInfo model)
    {
        bool allNamesMatch = model.materialsData.All(md =>
            variation.materialsResources.SelectMany(mr => mr.resources).Any(r => r.name == md.name));

        bool noRTTIValues = variation.materialsResources.SelectMany(mr => mr.resources).All(r =>
            !r.rttiValues.Any() && model.materialsData.Any(md => md.name == r.name));

        return allNamesMatch && noRTTIValues;
    }

    private static Variation CreateNewVariationWithAllMaterials(VariationOutput variationOutput, ModelInfo model)
    {
        int nextVariationId = GetNextVariationId(variationOutput);

        return new Variation
        {
            id = nextVariationId.ToString(),
            materialsData = new List<MaterialData>(model.materialsData),
            materialsResources = new List<MaterialResource>(model.materialsResources)
        };
    }

    private static int GetNextVariationId(VariationOutput variationOutput)
    {
        return variationOutput.variations.Any() ? variationOutput.variations.Max(v => int.TryParse(v.id, out int id) ? id : 0) + 1 : 1;
    }
    public void OrganizeJsonFiles()
    {
        string jsonsDir = Path.Combine(Application.dataPath, "StreamingAssets/Jsons");
        List<string> jsonFiles = new List<string>();
        Dictionary<string, Dictionary<string, List<string>>> modelsSortedByCategory = new Dictionary<string, Dictionary<string, List<string>>>();
        HashSet<string> unsortedModels = new HashSet<string>();
        HashSet<string> ignoreList = new HashSet<string> { "player_legs_a.msh", "player_shoes_a.msh", "player_torso_tpp_a_hood.msh", "player_inquisitor_torso_a_tpp.msh", "player_inquisitor_headwear_a_tpp.msh", "player_inquisitor_bracers_a_tpp.msh", "man_bdt_torso_c_shawl_b.msh", "chr_player_healer_mask.msh", "reporter_woman_old_skeleton.msh", "player_camo_gloves_a_tpp.msh", "player_camo_headwear_a_tpp.msh", "player_camo_pants_a_tpp.msh", "npc_colonel_coat_b.msh" };
        Dictionary<string, List<string>> modelToFilterLookup = new Dictionary<string, List<string>>();
        Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> modelsByClassAndFilter = new Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>();
        Dictionary<string, string> specificTermsToCategory = new Dictionary<string, string>
{
    { "young", "Human/Child" },
    { "destroyed", "Infected/Biter" },
    { "waltz_young", "Human/Man" },
    { "npc_mq_kiddie", "Human/Man" }
    // Add more terms as needed
};
        Directory.CreateDirectory(materialsDataDir);
        foreach (var typeDir in Directory.GetDirectories(jsonsDir))
        {
            foreach (var categoryDir in Directory.GetDirectories(typeDir))
            {
                jsonFiles.AddRange(Directory.GetFiles(categoryDir, "*.json"));
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

                        // Extracting category from the file path
                        string relativePath = file.Substring(jsonsDir.Length).Replace("\\", "/").TrimStart('/');
                        string[] pathParts = relativePath.Split('/');
                        string initialCategory = String.Join("/", pathParts.Take(pathParts.Length - 1));

                        foreach (var slotPair in modelData.slotPairs)
                        {
                            foreach (var model in slotPair.slotData.models)
                            {
                                string modelName = model.name.ToLower();
                                SortModel(modelName, modelsSortedByCategory, unsortedModels, modelToFilterLookup, ignoreList, specificTermsToCategory, initialCategory, modelData, modelsByClassAndFilter);
                            }
                        }

                        //foreach (var slotPair in modelData.slotPairs)
                        //{
                        //    foreach (var modelInfo in slotPair.slotData.models)
                        //    {
                        //        // Check if model name is valid
                        //        if (!string.IsNullOrWhiteSpace(modelInfo.name))
                        //        {
                        //            // Create or update JSON file for the model's material data
                        //            string modelName = modelInfo.name.ToLower();
                        //            UpdateMaterialDataJson(modelName, modelInfo.materialsData);
                        //        }
                        //    }
                        //}
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing file {file}: {ex.Message}");
                    }
                }
            }
        }

        SaveSortedModels(modelsSortedByCategory, modelsByClassAndFilter, storeClassData);
        SaveUnsortedModels(unsortedModels);
        SaveModelFilterLookup(modelToFilterLookup);
        ReorganizeAndCombineJsonFiles();
        Debug.Log("JSON files organized and saved.");
    }

    void SortModel(string modelName, Dictionary<string, Dictionary<string, List<string>>> modelsSortedByCategory, HashSet<string> unsortedModels, Dictionary<string, List<string>> modelToFilterLookup, HashSet<string> ignoreList, Dictionary<string, string> specificTermsToCategory, string initialCategory, ModelData modelData, Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> modelsByClassAndFilter)
    {
        if (ignoreList.Contains(modelName))
        {
            Debug.Log($"Model '{modelName}' is in the ignore list and will be skipped.");
            return;
        }

        // Determine if the model's name matches specific terms to override the category
        var overrideCategory = specificTermsToCategory.FirstOrDefault(pair => modelName.Contains(pair.Key)).Value;
        string finalCategory = overrideCategory ?? initialCategory;

        if (!modelsSortedByCategory.ContainsKey(finalCategory))
        {
            modelsSortedByCategory[finalCategory] = new Dictionary<string, List<string>>();
        }

        bool matchedAnyFilter = false;
        string modelClass = string.Empty;
        if (storeClassData && !string.IsNullOrEmpty(modelData.modelProperties.@class))
        {
            modelClass = modelData.modelProperties.@class.ToLower();
            if (!modelsByClassAndFilter.ContainsKey(finalCategory))
            {
                modelsByClassAndFilter[finalCategory] = new Dictionary<string, Dictionary<string, List<string>>>();
            }
            if (!modelsByClassAndFilter[finalCategory].ContainsKey(modelClass))
            {
                modelsByClassAndFilter[finalCategory][modelClass] = new Dictionary<string, List<string>>();
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
                    if (!modelsSortedByCategory[finalCategory].ContainsKey(filterName))
                    {
                        modelsSortedByCategory[finalCategory][filterName] = new List<string>();
                    }
                    modelsSortedByCategory[finalCategory][filterName].Add(modelName);
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
                        if (!modelsByClassAndFilter[finalCategory][modelClass].ContainsKey(filterName))
                        {
                            modelsByClassAndFilter[finalCategory][modelClass][filterName] = new List<string>();
                        }
                        modelsByClassAndFilter[finalCategory][modelClass][filterName].Add(modelName);
                    }

                    matchedAnyFilter = true;
                }
            }
        }

        if (!matchedAnyFilter)
        {
            Debug.LogWarning($"Model '{modelName}' did not match any filters or was excluded in category '{finalCategory}'");
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

        foreach (var categoryKey in modelsSortedByCategory.Keys)
        {
            string categoryDir = Path.Combine(Application.dataPath, $"StreamingAssets/SlotData/{categoryKey}");
            Directory.CreateDirectory(categoryDir);

            foreach (var filter in modelsSortedByCategory[categoryKey].Keys)
            {
                List<string> sortedModels = modelsSortedByCategory[categoryKey][filter].Distinct().ToList();
                sortedModels.Sort();

                string filePath = Path.Combine(categoryDir, $"ALL_{filter}.json");
                string jsonContent = JsonConvert.SerializeObject(new { meshes = sortedModels }, Formatting.Indented);
                File.WriteAllText(filePath, jsonContent);
            }

            if (storeClassData && modelsByClassAndFilter.ContainsKey(categoryKey))
            {
                foreach (var classEntry in modelsByClassAndFilter[categoryKey])
                {
                    if (classSkipList.Contains(classEntry.Key))
                    {
                        continue;
                    }

                    string classDir = Path.Combine(categoryDir, classEntry.Key);
                    Directory.CreateDirectory(classDir);

                    foreach (var filterEntry in classEntry.Value)
                    {
                        string classIdentifier = classIdentifiers.ContainsKey(classEntry.Key) ? classIdentifiers[classEntry.Key] : "";
                        List<string> classSortedModels = filterEntry.Value
                            .Where(model => model.Contains(classIdentifier))
                            .Distinct().ToList();
                        classSortedModels.Sort();

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


    void ReorganizeAndCombineJsonFiles()
    {
        string slotDataPath = Path.Combine(Application.dataPath, "StreamingAssets/SlotData");

        string allPath = Path.Combine(slotDataPath, "ALL");
        string humanPath = Path.Combine(slotDataPath, "Human");
        string infectedPath = Path.Combine(slotDataPath, "Infected");

        CombineJsonFiles(allPath);
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
        CreateDirectoryIfNotExists(destDir);

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
    }

    void CombineJsonFiles(string parentFolderPath)
    {
        var subFolders = Directory.GetDirectories(parentFolderPath);
        var combinedData = new Dictionary<string, List<string>>();

        foreach (var folder in subFolders)
        {
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

        foreach (var entry in combinedData)
        {
            string combinedFilePath = Path.Combine(parentFolderPath, entry.Key);
            string jsonContent = JsonConvert.SerializeObject(new { meshes = entry.Value.Distinct().ToList() }, Formatting.Indented);
            File.WriteAllText(combinedFilePath, jsonContent);
        }
    }


    private void GenerateModelSlotLookup()
    {
        string jsonDirectoryPath = Path.Combine(Application.dataPath, "StreamingAssets/Jsons");
        if (!Directory.Exists(jsonDirectoryPath))
        {
            Debug.LogError("JSON directory does not exist.");
            return;
        }

        ModelData.ModelSlotLookup modelSlotLookup = new ModelData.ModelSlotLookup();

        foreach (var typeDir in Directory.GetDirectories(jsonDirectoryPath))
        {
            foreach (var categoryDir in Directory.GetDirectories(typeDir))
            {
                var jsonFiles = Directory.GetFiles(categoryDir, "*.json");
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

                        foreach (var slotPair in modelData.GetSlots())
                        {
                            SlotData slotData = slotPair.Value;
                            foreach (var model in slotData.models)
                            {
                                string modelName = model.name.ToLower();
                                ModelData.SlotInfo slotInfo = new ModelData.SlotInfo
                                {
                                    slotUid = slotData.slotUid,
                                    name = slotData.name,
                                    filterText = slotData.filterText
                                };

                                if (!modelSlotLookup.modelSlots.ContainsKey(modelName))
                                {
                                    modelSlotLookup.modelSlots[modelName] = new List<ModelData.SlotInfo>();
                                }

                                // Check for uniqueness before adding
                                if (!ContainsSlotInfo(modelSlotLookup.modelSlots[modelName], slotInfo))
                                {
                                    modelSlotLookup.modelSlots[modelName].Add(slotInfo);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing file {file}: {ex.Message}");
                    }
                }
            }
        }

        SaveModelSlotLookup(modelSlotLookup);
    }

    private bool ContainsSlotInfo(List<ModelData.SlotInfo> slotInfos, ModelData.SlotInfo newSlotInfo)
    {
        foreach (var slotInfo in slotInfos)
        {
            if (slotInfo.slotUid == newSlotInfo.slotUid && slotInfo.name == newSlotInfo.name && slotInfo.filterText == newSlotInfo.filterText)
            {
                return true;
            }
        }
        return false;
    }

    private void SaveModelSlotLookup(ModelData.ModelSlotLookup modelSlotLookup)
    {
        // Sort each list of SlotInfo by slotUid before saving
        foreach (var modelName in modelSlotLookup.modelSlots.Keys.ToList()) // ToList() to avoid collection modified exception
        {
            modelSlotLookup.modelSlots[modelName] = modelSlotLookup.modelSlots[modelName]
                .OrderBy(slotInfo => slotInfo.slotUid)
                .ToList();
        }

        string outputPath = Path.Combine(Application.dataPath, "StreamingAssets/SlotData/SlotUIDLookup.json");
        string jsonOutput = JsonConvert.SerializeObject(modelSlotLookup, Formatting.Indented);
        File.WriteAllText(outputPath, jsonOutput);
        Debug.Log($"Model Slot Lookup saved to: {outputPath}");
    }

    private void GenerateNameSlotUidFrequencyLookup()
    {
        string jsonDirectoryPath = Path.Combine(Application.dataPath, "StreamingAssets/Jsons");
        if (!Directory.Exists(jsonDirectoryPath))
        {
            Debug.LogError("JSON directory does not exist.");
            return;
        }

        // Dictionary to hold name and all associated slotUids
        var nameToSlotUids = new Dictionary<string, List<int>>();

        foreach (var typeDir in Directory.GetDirectories(jsonDirectoryPath))
        {
            foreach (var categoryDir in Directory.GetDirectories(typeDir))
            {
                var jsonFiles = Directory.GetFiles(categoryDir, "*.json");
                foreach (var file in jsonFiles)
                {
                    try
                    {
                        string jsonData = File.ReadAllText(file);
                        ModelData modelData = JsonConvert.DeserializeObject<ModelData>(jsonData);

                        if (modelData.modelProperties == null || modelData.slotPairs == null) continue;

                        foreach (var slotPair in modelData.GetSlots())
                        {
                            SlotData slotData = slotPair.Value;
                            foreach (var model in slotData.models)
                            {
                                string nameKey = slotData.name.ToUpper(); // Using slotData.name for mapping

                                if (!nameToSlotUids.ContainsKey(nameKey))
                                {
                                    nameToSlotUids[nameKey] = new List<int>();
                                }
                                nameToSlotUids[nameKey].Add(slotData.slotUid);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing file {file}: {ex.Message}");
                    }
                }
            }
        }

        // Process the mapping to count frequencies and sort by them
        var sortedNameToSlotUids = nameToSlotUids.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.GroupBy(uid => uid)
                                .OrderByDescending(group => group.Count()) // Sort by frequency
                                .ThenBy(group => group.Key) // Then by slotUid for ties
                                .Select(group => group.Key)
                                .ToList()
        );

        SaveNameSlotUidFrequencyLookup(sortedNameToSlotUids);
    }

    private void SaveNameSlotUidFrequencyLookup(Dictionary<string, List<int>> sortedNameToSlotUids)
    {
        string outputPath = Path.Combine(Application.dataPath, "StreamingAssets/SlotData/SlotUidLookup_Empty.json");
        var jsonOutput = JsonConvert.SerializeObject(sortedNameToSlotUids, Formatting.Indented);
        File.WriteAllText(outputPath, jsonOutput);
        Debug.Log($"Name Slot UID Frequency Lookup saved to: {outputPath}");
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
