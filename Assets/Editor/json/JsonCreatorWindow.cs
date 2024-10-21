#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
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
    private bool storeClassData = false;
    private Dictionary<string, List<string>> filters;
    private Dictionary<string, string[]> categoryPrefixes;
    private Dictionary<string, List<string>> exclude_filters;
    private Dictionary<string, string> skeletonToCategory = new Dictionary<string, string>
{
    {"child_skeleton", "Child"},{"child_skeleton_mia", "Child"},{"man_basic_skeleton", "Man"},{"man_basic_skeleton_no_sh", "Man"},{"man_bdt_heavy_coat_skeleton", "Man"},{"man_bdt_heavy_skeleton", "Man"},{"man_bdt_heavy_torso_d_skeleton", "Man"},{"man_bdt_light_skeleton", "Man"},{"man_bdt_medium_skeleton", "Man"},{"man_bdt_skeleton", "Man"},{"man_npc_hakon_arrow_skeleton", "Man"},{"man_npc_skeleton", "Man"},{"man_pk_heavy_skeleton", "Man"},{"man_pk_light_skeleton", "Man"},{"man_pk_medium_skeleton", "Man"},{"man_pk_skeleton", "Man"},{"man_plr_skeleton", "Man"},{"man_sc_heavy_skeleton", "Man"},{"man_sc_light_skeleton", "Man"},{"man_sc_medium_skeleton", "Man"},{"man_sc_skeleton", "Man"},{"man_skeleton", "Man"},{"man_srv_heavy_skeleton", "Man"},{"man_srv_light_skeleton", "Man"},{"man_srv_medium_skeleton", "Man"},{"man_srv_skeleton", "Man"},{"npc_frank_skeleton", "Man"},{"npc_hakon_sh_skeleton", "Man"},{"player_fpp_phx_skeleton", "Player"},{"player_fpp_skeleton", "Player"},{"player_phx_skeleton", "Player"},{"player_skeleton", "Player"},{"viral_skeleton", "Viral"},{"woman_basic_skeleton", "Wmn"},{"woman_light_skeleton", "Wmn"},{"woman_npc_meredith_skeleton", "Wmn"},{"woman_npc_singer_skeleton", "Wmn"},{"woman_sc_skeleton", "Wmn"},{"woman_skeleton", "Wmn"},{"woman_srv_skeleton", "Wmn"},{"zmb_banshee_skeleton", "Special Infected"},{"zmb_bolter_skeleton", "Special Infected"},{"zmb_charger_skeleton", "Special Infected"},{"zmb_corruptor_skeleton", "Special Infected"},{"zmb_demolisher_phx_skeleton", "Special Infected"},{"zmb_demolisher_skeleton", "Special Infected"},{"zmb_goon_skeleton", "Special Infected"},{"zmb_screamer_skeleton", "Special Infected"},{"zmb_spitter_skeleton", "Special Infected"},{"zmb_spitter_tier_a_skeleton", "Special Infected"},{"zmb_suicider_skeleton", "Special Infected"},{"zmb_volataile_hive_skeleton", "Special Infected"},{"zmb_volataile_skeleton", "Special Infected"},{"man_zmb_heavy_skeleton", "Biter"},{"man_zmb_light_skeleton", "Biter"},{"man_zmb_medium_skeleton", "Biter"},{"man_zmb_skeleton", "Biter"},
{"woman_zmb_skeleton", "Biter"},{"man_srv_skinybiter_skeleton", "Biter"}
    };

    void OnEnable()
    {
        InitializeDictionaries();
    }

    private void InitializeDictionaries()
    {
        filters = new Dictionary<string, List<string>>()
        {
            {"head", new List<string> {"sh_scan_", "sh_npc_", "sh_wmn_", "aiden_young", "sh_", "sh_man_", "sh_biter_", "sh_man_viral_a", "head", "sh_biter_deg_b"}},
            {"hat", new List<string> {"hat", "cap", "headwear", "beret", "gastank_headgear", "bracken_bandage", "beanie", "sh_man_pk_headcover_c", "headband", "man_bdt_headcover_i", "coverlet"}},
            {"hat_access", new List<string> {"hat", "cap", "headwear", "gastank_headgear", "horns", "beret", "bracken_bandage", "beanie", "hood", "sh_man_pk_headcover_c", "headband", "man_bdt_headcover_i", "coverlet"}},
            {"mask", new List<string> {"mask", "balaclava", "bandana", "payday2_headwear_a", "scarf_a_part_d" }},
            {"mask_access", new List<string> {"mask", "balaclava", "bandana", "payday2_headwear_a", "scarf_a_part_d" }},
            {"hood", new List<string> {"hood"}},
            {"glasses", new List<string> {"glasses", "goggles", "crane_goggles", "rahim_headwear_a", "man_ren_scarf_a_part_c", "player_tank_headwear_a_tpp"}},
            {"necklace", new List<string> {"jewelry", "necklace", "man_bdt_chain_i", "man_bdt_chain_g", "man_bdt_chain_h", "man_bdt_chain_f"}},
            {"earrings", new List<string> {"earing", "earring"}},
            {"rings", new List<string> {"npc_man_ring"}},
            {"hair", new List<string> {"hair", "npc_aiden_hair_headwear", "extension"}},
            {"hair_base", new List<string> {"base"}},
            {"hair_2", new List<string> {"sides", "braids", "extension"}},
            {"hair_3", new List<string> {"fringe", "extension"}},
            {"facial_hair", new List<string> {"facial_hair", "beard", "jaw_"}},
            {"cape", new List<string> {"cape", "scarf", "chainmail", "sweater", "shawl", "shalw", "choker", "cloak", "npc_man_torso_g_hood_b"}},
            {"chest", new List<string> { "torso_fat", "torso_muscular", "wrestler_torso_tank", "frank_torso_wounded", "torso_leader_a"}},
            {"jacket", new List<string> { "jacket", "man_bdt_torso_d", "man_bdt_torso_f", "pk_craftmaster_torso_b", "man_pk_torso_f", "man_ren_torso_c", "player_rick_torso_a_tpp", "player_outfit_rais_torso_tpp" }},
            {"torso", new List<string> {"torso", "npc_anderson", "zmb_torso_d_coverlet", "player_reload_outfit", "singer_top", "npc_frank", "bolter", "goon", "charger_body", "corruptor", "screamer", "spitter", "viral_body_torso", "swamp_viral", "volatile", "zmb_suicider_corpse"}},
            {"torso_2", new List<string> { "torso", "centre", "vest", "wmn_torso_e", "wmn_torso_i", "center", "top", "fronttop", "front", "dress", "coat", "jacket", "bubbles", "horns", "guts", "gastank"}},
            {"torso_extra", new List<string> {"torso", "addon", "wpntmp", "upper_feathers", "centre", "detail", "vest", "wmn_torso_e", "wmn_torso_i", "stethoscope", "center", "top", "fronttop", "front", "dress", "machete_tpp", "coat", "plates", "vest", "jacket", "bubbles", "horns", "guts", "gastank"}},
            {"torso_access", new List<string> {"belt", "bumbag", "wpntmp", "upper_feathers", "npc_fitz_pin_a", "centre", "detail", "vest", "wmn_torso_e", "wmn_torso_i", "stethoscope", "center", "top", "fronttop", "front", "torso_b_cape_spikes", "spikes",  "add_", "wrap", "npc_colonel_feathers_a", "axe", "npc_wmn_pants_b_torso_g_tank_top_bumbag_a", "zipper", "rag", "pouch", "collar", "neck", "pocket", "waist_shirt", "chain_armour", "skull_", "plates", "sport_bag", "gastank", "walkietalkie", "man_bdt_belt_c_addon_a", "chain", "battery", "torso_a_pk_top", "man_bdt_torso_d_shirt_c", "wrench", "suspenders", "turtleneck", "npc_waltz_torso_a_glasses_addon", "apron"}},
            {"tattoo", new List<string> {"tatoo", "tattoo"}},
            {"tattoo_2", new List<string> {"tatoo", "tattoo"}},
            {"hands", new List<string> {"hands", "arms"}},
            {"lhand", new List<string> {"arm", "_left", "hand", "hands", "arms"}},
            {"rhand", new List<string> {"arm", "_right", "hand", "hands", "arms"}},
            {"gloves", new List<string> {"gloves", "arms_rag", "glove"}},
            {"gloves_2", new List<string> {"gloves", "arms_rag", "glove"}},
            {"arm_access", new List<string> {"biomarker", "basic_watch", "bracelet", "npc_barney_band","gloves_a_addon"}},
            {"arm_access_2", new List<string> {"biomarker", "basic_watch", "bracelet", "npc_barney_band","gloves_a_addon"}},
            {"sleeve", new List<string> {"sleeve", "sleeves", "forearm", "man_srv_torso_b_arms", "arm_", "arms_"}},
            {"backpack", new List<string> {"backpack", "bag", "parachute", "backback"}},
            {"flashlight", new List<string> {"flashlight"}},
            {"gastank", new List<string> {"gastank_tank"}},
            {"blisters_1", new List<string> { "blisters", "blister"}},
            {"blisters_2", new List<string> { "blisters", "blister"}},
            {"blisters_3", new List<string> { "blisters", "blister"}},
            {"blisters_4", new List<string> { "blisters", "blister"}},
            {"belts", new List<string> {"belt", "belt", "g_tank_top_bumbag_a"}},
            {"bag", new List<string> {"bag"}},
            {"decals", new List<string> {"decal", "patch"}},
            {"decals_2", new List<string> {"decal", "patch"}},
            {"decals_extra", new List<string> {"decal", "patch"}},
            {"decals_logo", new List<string> {"logo"}},
            {"weapons", new List<string> {"gunslinger_sixshooter", "shield", "machete", "axe", "knife" }},
            {"legs", new List<string> {"leg", "legs", "pants", "trousers"}},
            {"legs_2", new List<string> {"leg", "legs", "pants", "trousers"}},
            {"legs_extra", new List<string> {"pocket", "socks", "bottom", "sc_ov_legs", "legs_a_part", "child_torso_a_bottom", "skirt", "man_srv_legs_b_bottom_a", "belt", "legs_c_add_", "legs_a_addon", "pants_b_rag"}},
            {"legs_access", new List<string> {"pocket", "socks", "bottom", "sc_ov_legs", "pants_a_add_a", "legs_a_part", "pouch", "child_torso_a_bottom", "holster", "skirt", "chain", "equipment", "npc_jack_legs_adds", "man_srv_legs_b_bottom_a", "belt", "pad_", "legs_c_add_", "legs_a_addon", "pants_b_rag", "bumbag", "bag", "patch_", "bandage", "element", "tapes"}},
            {"pockets", new List<string> {"pocket", "pockets"}},
            {"shoes", new List<string> {"shoes", "shoe", "feet", "boots", "child_pants_b"}},
            {"armor_helmet", new List<string> {"helmet", "tank_headwear", "fh_kensei_headwear", "warden_headwear_a", "samurai_headwear_a", "perun_headwear_a", "hadwear", "anubis_headwear", "armor_b_head", "skullface_helmet", "headcover", "head_armor", "headgear"}},
            {"armor_helmet_access", new List<string> {"helmet", "tank_headwear", "fh_kensei_headwear", "warden_headwear_a", "samurai_headwear_a", "perun_headwear_a", "hadwear", "anubis_headwear", "armor_b_head", "skullface_helmet", "headcover", "head_armor", "headgear"}},
            {"armor_torso", new List<string> {"armor", "nuwa_torso_a", "warden_torso_a_sleeves"}},
            {"armor_torso_2", new List<string> {"armor", "nuwa_torso_a", "warden_torso_a_sleeves"}},
            {"armor_torso_access", new List<string> {"armor", "upper_feathers", "warden_chainmail", "warden_torso_a_sleeves", "addon" }},
            {"armor_torso_extra", new List<string> {"armor", "upper_feathers", "warden_chainmail", "warden_torso_a_sleeves", "armor_d_pin", "addon" }},
            {"armor_torso_upperright", new List<string> {"upperright", "arm_upper_armor", "armor_upper_arm" }},
            {"armor_torso_upperleft", new List<string> {"upperleft", "arm_upper_armor", "armor_upper_arm", "man_bandit_shoulders_armor_left_a", "shoulderpad"}},
            {"armor_torso_lowerright", new List<string> {"lowerright", "bracers", "bracer", "hand_tapes_a_right", "pad_a_r", "elbow_pad_a_normal_r", "elbow_pad_a_muscular_r"}},
            {"armor_torso_lowerleft", new List<string> {"lowerleft", "bracers", "npc_skullface_shield", "pad_a_l", "hand_tapes_a_left", "elbow_pad_a_muscular_l", "elbow_pad_a_normal_l"}},
            {"armor_legs", new List<string> { "legs_armor", "legs_b_armor", "pants_a_pad", "leggings_a_pad", "leg_armor", "pants_armor", "legs_a_armor", "pants_b_armor", "leg_armor", "pants_a_armor"}},
            {"armor_legs_access", new List<string> { "legs_armor", "legs_b_armor", "pants_a_pad", "leggings_a_pad", "leg_armor", "pants_armor", "legs_a_armor", "pants_b_armor", "leg_armor", "pants_a_armor"}},
            {"armor_legs_upperright", new List<string> { "armor"}},
            {"armor_legs_upperleft", new List<string> { "armor"}},
            {"armor_legs_lowerright", new List<string> { "armor"}},
            {"armor_legs_lowerleft", new List<string> { "armor"}}
        };

        categoryPrefixes = new Dictionary<string, string[]>
        {
            {"Biter", new[] {"biter", "ialr_zmb_", "zmb_man", "zmb_wmn", "man_zmb_", "wmn_zmb_", "_zmb"}},
            {"Viral", new[] {"viral", "viral_man", "ialr_viral_", "wmn_viral"}},
            {"Player", new[] { "sh_npc_aiden", "player", "player_outfit_lubu_tpp", "player_outfit_carrier_leader_tpp", "player_outfit_brecken", "player_outfit_gunslinger_tpp"}},
            {"Man", new[] {"man", "man_srv_craftmaster", "dlc_opera_man_shopkeeper_special", "npc_pipsqueak", "dlc_opera_man_npc_ciro", "npc_mc_dispatcher", "dlc_opera_man_npc_ferka", "dlc_opera_man_npc_hideo", "dlc_opera_man_npc_ogar", "npc_carl", "npc_alberto_paganini", "npc_outpost_guard", "npc_callum", "npc_abandon_srv_emmett", "npc_abandon_pk_master_brewer", "npc_feliks", "npc_marcus", "npc_mq_stan", "npc_hank", "npc_jack", "npc_colonel", "npc_pilgrim", "sh_baker", "npc_simon", "npc_juan", "npc_rowe", "npc_vincente", "sh_bruce",  "sh_frank", "sh_dlc_opera_npc_tetsuo", "sh_dlc_opera_npc_ogar", "sh_dlc_opera_npc_ciro", "sh_dlc_opera_npc_andrew", "npc_dylan", "npc_waltz", "dlc_opera_man", "npc_skullface", "sh_johnson", "npc_hakon", "npc_steve", "npc_barney", "multihead007_npc_carl_"}},
            {"Wmn", new[] {"wmn", "dlc_opera_wmn", "npc_anderson", "dlc_opera_man_wmn_brienne", "sh_mother", "npc_thalia", "npc_hilda", "npc_lola", "npc_mq_singer", "npc_astrid", "npc_dr_veronika", "npc_lawan", "npc_meredith", "npc_mia", "npc_nuwa", "npc_plaguewitch", "npc_sophie"}},
            {"Child", new[] {"child", "kid", "girl", "npc_arya", "npc_kevin", "npc_dominik", "prologue_npc_theo", "npc_rose", "npc_mq_maya", "npc_mq_zapalka", "npc_liam", "npc_moe", "ialr_viral_child", "ialr_zmb_child_", "boy", "young", "chld"}},
            {"Special Infected", new[] { "volatile", "suicider", "spitter", "goon", "demolisher", "screamer", "bolter", "corruptor", "banshee", "charger" }},
        };

        exclude_filters = new Dictionary<string, List<string>>()
        {
            {"head", new List<string> {"hat", "cap", "headwear", "headgear", "armor", "blisters", "mask", "bdt_balaclava", "horns", "sh_benchmark_npc_hakon_cc", "beret", "glasses", "hair", "bandana", "facial_hair", "beard", "headcover", "emblem", "cap", "headwear", "bandana", "beanie", "hood"}},
            {"hat", new List<string> {"part", "player_camo_headwear_a_tpp", "fh_kensei_headwear", "warden_headwear_a", "crane_goggles", "payday2_headwear_a", "rahim_headwear_a", "samurai_headwear_a", "perun_headwear_a", "belt_a_for_torso_a_hood_b", "hood", "emblem", "tube", "zmb_torso_d_coverlet", "horns", "fringe", "base", "braids", "player_headwear_crane_b_tpp", "player_tank_headwear_a_tpp", "player_camo_hood_a_tpp", "npc_man_torso_g_hood_b", "glasses", "hair", "decal_logo", "cape", "mask", "_addon", "decal", "facial_hair"}},
            {"hat_access", new List<string> {"player_camo_headwear_a_tpp", "fh_kensei_headwear", "warden_headwear_a",  "crane_goggles", "payday2_headwear_a", "rahim_headwear_a", "samurai_headwear_a", "perun_headwear_a", "player_headwear_crane_b_tpp", "horns", "zmb_torso_d_coverlet", "player_tank_headwear_a_tpp", "player_camo_hood_a_tpp", "npc_man_torso_g_hood_b", "glasses", "hair", "cape", "mask", "facial_hair"}},
            {"mask", new List<string> {"glasses", "hair", "decal_logo", "cape", "_addon", "chr_player_healer_mask", "facial_hair"}},
            {"mask_access", new List<string> {"glasses", "hair", "decal_logo", "cape", "chr_player_healer_mask", "facial_hair"}},
            {"hood", new List<string> {"fur"}},
            {"glasses", new List<string> {"hat", "cap", "npc_waltz_torso_a_glasses_addon", "mask", "hair", "facial_hair"}},
            {"necklace", new List<string> {"bandana"}},
            {"earrings", new List<string> {"bandana", "torso"}},
            {"rings", new List<string> {"bandana", "torso", "earing", "earring"}},
            {"hair", new List<string> {"cap", "mask", "glasses", "facial_hair", "decal"}},
            {"hair_base", new List<string> {"cap", "headwear", "mask", "torso", "glasses", "decal", "facial_hair", "young_hair", "fringe", "sides"}},
            {"hair_2", new List<string> {"cap", "headwear", "mask", "glasses", "decal", "facial_hair", "young_hair", "fringe", "base"}},
            {"hair_3", new List<string> {"cap", "headwear", "mask", "glasses", "decal", "facial_hair", "young_hair", "sides", "base"}},
            {"facial_hair", new List<string> {"hat", "cap", "headwear", "mask", "decal", "glasses" }},
            {"cape", new List<string> {"mask", "spikes", "belts", "scarf_a_part_d", "bags", "scarf_a_part_c"}},
            {"jacket", new List<string> {"mask", "spikes", "belts", "scarf_a_part_d", "scarf", "pin", "bags", "belta", "decal", "adds", "upperright", "bottom", "sleeve", "hood", "belt", "armor", "cape", "shirt", "bag", "scarf_a_part_c" }},
            {"chest", new List<string> {"armor", "sleeve", "plates", "wrench", "jacket", "_adds", "upperright", "upperleft", "bracelet", "back_", "pin", "beard", "shawl", "detail", "vest", "skirt", "center",  "wmn_torso_e", "bracer", "bracers", "sh_npc_anderson", "wmn_torso_i", "bottom", "base", "front_", "fronttop",  "torso_c_sweater", "rag", "stethoscope", "plaguewitch", "centre", "nuwa_torso_a", "horns", "wpntmp", "equipment", "head", "hair", "shoulderpad", "player_camo_torso_a_tpp", "hat", "mask", "player_inquisitor_torso_a_tpp", "player_torso_tpp_a", "addon", "machete_tpp", "skull", "chain_armour", "hands", "battery", "arms", "cape", "turtleneck", "apron", "pants", "backpack", "bag", "parachute", "suspenders", "legs", "hood", "spikes", "chain", "decal", "collar", "scarf", "pouch", "zipper", "belt", "part", "patch", "bag", "pocket", "jewelry", "necklace", "ring"}},
            {"torso", new List<string> {"armor", "sleeve", "plates", "wrench", "jacket", "chain", "_adds", "upperright", "upperleft", "sh_wmn_viral_tier3_b", "bracelet", "back_", "pin", "beard", "shawl", "detail", "vest", "skirt", "center",  "wmn_torso_e", "bracer", "bracers", "sh_npc_anderson", "wmn_torso_i", "bottom", "base", "front_", "fronttop",  "torso_c_sweater", "rag", "stethoscope", "plaguewitch", "centre", "nuwa_torso_a", "horns", "wpntmp", "equipment", "head", "hair", "shoulderpad", "player_camo_torso_a_tpp", "hat", "mask", "player_inquisitor_torso_a_tpp", "player_torso_tpp_a", "addon", "machete_tpp", "skull", "chain_armour", "hands", "battery", "arms", "cape", "turtleneck", "apron", "pants", "backpack", "bag", "parachute", "suspenders", "legs", "hood", "spikes", "chain", "decal", "collar", "scarf", "pouch", "zipper", "belt", "part", "patch", "bag", "pocket", "jewelry", "necklace", "ring"}},
            {"torso_2", new List<string> {"armor", "plates", "wrench", "detail", "bracer", "bracers", "sh_npc_anderson", "sh_wmn_viral_tier3_b", "bottom", "base", "torso_c_sweater", "stethoscope", "plaguewitch", "horns", "wpntmp", "equipment", "head", "hair", "shoulderpad", "player_camo_torso_a_tpp", "hat", "mask", "player_inquisitor_torso_a_tpp", "player_torso_tpp_a", "addon", "machete_tpp", "skull", "chain_armour", "hands", "battery", "arms", "cape", "turtleneck", "apron", "pants", "backpack", "bag", "parachute", "suspenders", "legs", "hood", "spikes", "chain", "decal", "collar", "scarf", "pouch", "zipper", "belt", "part", "patch", "bag", "pocket", "jewelry", "necklace", "ring"}},
            {"torso_extra", new List<string> {"armor", "mask", "gloves", "hands", "battery", "arms", "cape", "pants", "backpack", "bag", "parachute", "legs", "hood", "spikes", "chain", "decal", "collar", "scarf", "pouch", "zipper", "belt", "bag", "pocket", "jewelry", "necklace", "ring"}},
            {"torso_access", new List<string> {"pants", "leg", "legs", "shoes", "man_bdt_chain_i", "bracken_bandage", "man_bdt_chain_g", "man_bdt_chain_h", "man_bdt_chain_f"}},
            {"hands", new List<string> {"sleeve", "upper_arms", "pk", "rag", "zmb_torso_f_arms", "man_srv_arms_a", "man_srv_torso_b_arms", "_left", "belts", "chains", "elbow", "_right", "armor", "decal_tattoo", "decal"}},
            {"lhand", new List<string> {"_right", "headwear", "rag", "belts", "wrapper", "bracer", "bracers", "chains", "elbow", "balaclava", "shoes", "sleeve", "armor", "glove", "decal_tattoo", "tattoo", "torso", "leg", "legs", "pants", "decal"}},
            {"rhand", new List<string> {"_left", "headwear", "rag", "belts", "wrapper", "bracer", "bracers", "chains", "elbow", "balaclava", "shoes", "sleeve", "armor", "glove", "decal_tattoo", "tattoo", "torso", "leg", "legs", "pants", "decal"}},
            {"tattoo", new List<string> {"mask"}},
            {"tattoo_2", new List<string> {"mask"}},
            {"gloves", new List<string> {"arm_","hand", "decal", "addon", "player_camo_gloves_a_tpp"}},
            {"gloves_2", new List<string> {"arm_","hand", "decal", "addon", "player_camo_gloves_a_tpp"}},
            {"arm_access", new List<string> {"torso"}},
            {"arm_access_2", new List<string> {"torso"}},
            {"sleeve", new List<string> {"decal", "logo", "armor", "balaclava", "zmb_arms_b", "viral_a_torso_d_arms_a", "warden_torso_a_sleeves", "chains", "belts", "army_torso_a", "bracers", "headwear", "gloves", "pants", "shoes", "leg", "addon", "torso_armor"}},
            {"backpack", new List<string> {"pants", "sweets", "bag_a_addon_a", "top_b_bumbag_a", "torso_c_bag_a", "torso_a_bags_a", "e_b_belt_bag", "torso_e_b_bumbag_a", "wmn_torso_e_belt_bag"}},
            {"flashlight", new List<string> {"pants"}},
            {"bag", new List<string> {"mask", "element", "armor"}},
            {"gastank", new List<string> {"pants"}},
            {"infection", new List<string> { "mask"}},
            {"infection_2", new List<string> { "mask"}},
            {"infection_3", new List<string> { "mask"}},
            {"infection_4", new List<string> { "mask"}},
            {"belts", new List<string> {"arm", "bag", "wrench", "man_bdt_torso_a_with_belt_a"}},
            {"decals", new List<string> {"mask"}},
            {"decals_2", new List<string> {"mask"}},
            {"decals_extra", new List<string> {"mask"}},
            {"decals_logo", new List<string> {"mask"}},
            {"weapons", new List<string> {"mask"}},
            {"legs", new List<string> {"mask", "shoes", "player_legs_a", "sc_ov_legs", "feet", "part", "child_torso_a_bottom", "child_pants_b", "player_camo_pants_a_tpp", "arm_", "chain", "elbow", "_right", "npc_jack_legs_adds", "bandage", "sleeve", "patch", "add_", "armor", "glove", "addon", "bag", "tapes", "man_srv_legs_b_bottom_a", "element", "hand", "decal", "equipment", "pocket", "pouch", "element", "decal_logo", "belt", "pad", "pants_b_rag", "bumbag", "bag", "patch_"}},
            {"legs_2", new List<string> {"mask", "shoes", "player_legs_a", "sc_ov_legs", "feet", "part", "child_torso_a_bottom", "child_pants_b", "player_camo_pants_a_tpp", "arm_", "chain", "elbow", "_right", "npc_jack_legs_adds", "bandage", "sleeve", "patch", "add_", "armor", "glove", "addon", "bag", "tapes", "man_srv_legs_b_bottom_a", "element", "hand", "decal", "equipment", "pocket", "pouch", "element", "decal_logo", "belt", "pad", "pants_b_rag", "bumbag", "bag", "patch_"}},
            {"legs_extra", new List<string> {"armor", "man_bdt_belt_d_pouches_a", "hair", "chainmail", "bracken_bandage", "man_bdt_belt_g", "man_bdt_belt_c_addon_a", "man_bdt_belt_c", "man_bzr_belt_c", "man_bdt_belt_d", "man_bzr_belt_a", "man_srv_belt_bags_a", "torso", "elbow", "decal_logo", "shoes", "arm", "sleeve", "armor", "glove", "torso", "hand", "hat"}},
            {"legs_access", new List<string> {"armor", "man_bdt_belt_d_pouches_a", "hair", "chainmail", "bracken_bandage", "man_bdt_belt_g", "man_bdt_belt_c_addon_a", "man_bdt_belt_c", "man_bzr_belt_c", "man_bdt_belt_d", "man_bzr_belt_a", "man_srv_belt_bags_a", "torso", "elbow", "decal_logo", "shoes", "arm", "sleeve", "armor", "glove", "torso", "hand", "hat"}},
            {"pockets", new List<string> {"mask"}},
            {"shoes", new List<string> {"mask"}},
            {"armor_helmet", new List<string> {"hat", "cap", "part", "gastank", "sh_man_pk_headcover_c", "emblem", "man_bdt_headcover_i", "bandana", "beanie", "hood", "headband", "torso", "glasses", "hair", "decal", "cape"}},
            {"armor_helmet_access", new List<string> {"hat", "cap", "gastank", "sh_man_pk_headcover_c", "man_bdt_headcover_i", "beanie", "hood", "torso", "glasses", "hair", "cape"}},
            {"armor_torso", new List<string> {"legs", "pants", "bottom", "gastank", "armor_b_head", "pouches", "walkietalkie", "pin", "leg", "upper", "element", "bottom_a_armor", "cape", "addon", "upperleft", "lowerright", "lowerleft", "upperright"}},
            {"armor_torso_2", new List<string> {"legs", "pants", "bottom", "gastank", "armor_b_head", "pouches", "walkietalkie", "pin", "leg", "upper", "element", "bottom_a_armor", "cape", "addon", "upperleft", "lowerright", "lowerleft", "upperright"}},
            {"armor_torso_access", new List<string> {"legs", "pants", "mask", "gastank", "scarf", "hair", "pouches", "pin", "leg", "upper", "cape", "upperleft", "lowerright", "lowerleft", "upperright"}},
            {"armor_torso_extra", new List<string> {"legs", "pants", "mask", "gastank", "scarf", "hair", "pouches",  "leg", "upper", "cape", "upperleft", "lowerright", "lowerleft", "upperright"}},
            {"armor_torso_upperright", new List<string> {"legs", "pants", "gastank", "leg", "left"}},
            {"armor_torso_upperleft", new List<string> {"legs", "pants", "gastank", "leg", "right"}},
            {"armor_torso_lowerright", new List<string> {"legs", "pants", "gastank", "leg", "left"}},
            {"armor_torso_lowerleft", new List<string> {"legs", "pants", "gastank", "leg", "right"}},
            {"armor_legs", new List<string> {"mask", "gastank"}},
            {"armor_legs_access", new List<string> {"mask", "gastank"}},
            {"armor_legs_upperright", new List<string> { "lowerleft", "gastank", "lowerright", "upperleft", "arm_", "arms", "left"}},
            {"armor_legs_upperleft", new List<string> { "lowerleft", "gastank", "lowerright", "upperright", "mask", "arm_", "arms", "right" }},
            {"armor_legs_lowerright", new List<string> { "lowerleft", "gastank", "upperright", "upperleft", "mask", "arm_", "arms", "left" }},
            {"armor_legs_lowerleft", new List<string> { "upperright", "gastank", "lowerright", "upperleft", "mask", "arm_", "arms", "right" }}
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

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Skeleton Lookup"))
        {
            BuildSkeletonLookup();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Material Reference Jsons"))
        {
            GenerateMaterialDataJsons();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Variation Jsons"))
        {
            CreateVariationJsons();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Build Unique Mesh SlotUID Lookup"))
        {
            GenerateModelSlotLookup();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Build Unique Empty SlotUID Lookup"))
        {
            GenerateNameSlotUidFrequencyLookup();
        }

    }

    
    public static void BuildSkeletonLookup()
    {
        // Open file dialog to select the models_metadata.scr file
        string filePath = EditorUtility.OpenFilePanel("Select models_metadata.scr", "", "scr");
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.Log("File selection canceled or invalid.");
            return;
        }

        try
        {
            // Read the contents of the file
            string[] lines = File.ReadAllLines(filePath);
            var lookup = new Dictionary<string, string>();

            // Simple state machine for parsing the .scr file content
            string currentModel = null;
            foreach (var line in lines)
            {
                if (line.Contains("model("))
                {
                    // Extract model name
                    currentModel = line.Split('"')[1].Split('.')[0]; // Gets the name before the first dot
                }
                else if (line.Contains("skeleton(") && currentModel != null)
                {
                    // Extract skeleton name and add to dictionary
                    string skeletonName = line.Split('"')[1];
                    lookup[currentModel] = skeletonName;
                    currentModel = null; // Reset current model as we only want the first skeleton encountered per model
                }
            }

            // Convert the lookup dictionary to JSON
            string json = JsonConvert.SerializeObject(lookup, Formatting.Indented);

            // Ensure the target directory exists
            string outputDir = Path.Combine(Application.dataPath, "StreamingAssets/skeletons");
            Directory.CreateDirectory(outputDir);

            // Write the JSON to the skeleton_lookup.json file
            File.WriteAllText(Path.Combine(outputDir, "skeleton_lookup.json"), json);

            Debug.Log("Skeleton lookup JSON successfully created.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error processing file: {ex.Message}");
        }
    }

    void OrganizeJsonFiles()
    {
        storeClassData = EditorGUILayout.Toggle("Store Class Data", storeClassData);
        string jsonsDir = Path.Combine(Application.dataPath, "StreamingAssets/Jsons");
        List<string> jsonFiles = new List<string>();
        Dictionary<string, Dictionary<string, List<string>>> modelsSortedByCategory = new Dictionary<string, Dictionary<string, List<string>>>();
        HashSet<string> unsortedModels = new HashSet<string>();
        HashSet<string> ignoreList = new HashSet<string> { "sh_benchmark_npc_hakon_cc_new_eyes.msh", "player_torso_tpp_a_hood.msh", "zmb_volatile_hive_a_guts_b.msh", "player_cyber_rider_torso_a_tpp.msh", "player_cyberknight_torso_a_top_a_tpp.msh", "player_cyberknight_torso_a_tpp.msh", "player_outfit_rais_soldier_torso_tpp.msh", "player_shoes_a.msh", "player_cyber_rider_gloves_a_fpp.msh", "player_cyberknight_gloves_a_tpp.msh", "player_cyber_rider_headwear_a_tpp.msh", "viral_torso_k_rag.msh", "player_cyberknight_pants_a_tpp.msh", "player_cyber_rider_gloves_a_tpp.msh", "player_cyberknight_bracers_a_tpp.msh", "player_cyber_rider_bracers_a_fpp.msh", "player_cyberknight_gloves_a_fpp.msh", "player_gloves_a_fpp.msh", "player_cyber_rider_bracers_a_tpp.msh", "player_cyberknight_headwear_a_tpp.msh", "player_outfit_rais_soldier_gloves_tpp.msh", "player_cyberknight_shoes_a_tpp.msh", "player_torso_tpp_a_hood.msh" , "player_legs_a.msh", "man_bdt_torso_c_shawl_b.msh", "chr_player_healer_mask.msh", "reporter_woman_old_skeleton.msh", "npc_colonel_coat_b.msh" };
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

        foreach (var typeDir in Directory.GetDirectories(jsonsDir))
        {
            Debug.Log($"Processing Type Directory: {typeDir}");
            int categoryCount = 0;

            foreach (var categoryDir in Directory.GetDirectories(typeDir))
            {
                jsonFiles.AddRange(Directory.GetFiles(categoryDir, "*.json"));
                categoryCount++;
                Debug.Log($"Processing Category Directory: {categoryDir}");

                // Move jsonFiles declaration outside the loop to avoid conflict
                List<string> jsonFileList = Directory.GetFiles(categoryDir, "*.json").ToList();
                Debug.Log($"Found {jsonFileList.Count} JSON files in {categoryDir}");

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
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing file {file}: {ex.Message}");
                    }
                }
            }

            Debug.Log($"Processed {categoryCount} categories in Type Directory: {typeDir}");
        }

        SaveSortedModels(modelsSortedByCategory, modelsByClassAndFilter, storeClassData);
        SaveUnsortedModels(unsortedModels);
        SaveModelFilterLookup(modelToFilterLookup);
        ReorganizeAndCombineJsonFiles();
        GenerateModelSlotLookup();
        GenerateNameSlotUidFrequencyLookup();
        Debug.Log("JSON files organized and saved.");
    }

    void SortModel(
    string modelName,
    Dictionary<string, Dictionary<string, List<string>>> modelsSortedByCategory,
    HashSet<string> unsortedModels,
    Dictionary<string, List<string>> modelToFilterLookup,
    HashSet<string> ignoreList,
    Dictionary<string, string> specificTermsToCategory,
    string initialCategory,
    ModelData modelData,
    Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> modelsByClassAndFilter)
    {
        // Check if the modelName is in the ignoreList or nameExclusion list
        if (ignoreList.Contains(modelName))
        {
            Debug.Log($"Model '{modelName}' is in the ignore or exclusion list and will be skipped.");
            return;
        }

        // Determine if the model's name matches specific terms to override the category
        var overrideCategory = specificTermsToCategory.FirstOrDefault(pair => modelName.Contains(pair.Key)).Value;
        string finalCategory = overrideCategory ?? initialCategory;

        Debug.Log($"Sorting model '{modelName}' under category '{finalCategory}'.");



        if (!modelsSortedByCategory.ContainsKey(finalCategory))
        {
            modelsSortedByCategory[finalCategory] = new Dictionary<string, List<string>>();
        }

        bool matchedAnyFilter = false;
        string modelClass = string.Empty;

        if (storeClassData && !string.IsNullOrEmpty(modelData.modelProperties.@class))
        {
            modelClass = modelData.modelProperties.@class.ToLower();
            Debug.Log($"Model '{modelName}' has class '{modelClass}'.");

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
                bool isExcluded = exclude_filters.ContainsKey(filterName) && exclude_filters[filterName].Any(excludeTerm => modelName.Contains(excludeTerm));

                if (modelName.Contains(filterTerm) && !isExcluded)
                {
                    Debug.Log($"Model '{modelName}' matched filter '{filterName}' using term '{filterTerm}'.");

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
                else if (modelName.Contains(filterTerm) && isExcluded)
                {
                    string excludedTerm = exclude_filters[filterName].FirstOrDefault(excludeTerm => modelName.Contains(excludeTerm));
                    Debug.Log($"Model '{modelName}' was excluded from filter '{filterName}' by exclusion term '{excludedTerm}'.");
                }
            }
        }

        if (!matchedAnyFilter)
        {
            Debug.LogWarning($"Model '{modelName}' did not match any filters or was excluded in category '{finalCategory}'.");
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
        string slotDataPath = Path.Combine(Application.dataPath, "StreamingAssets/SlotData");

        // Check if the SlotData directory exists and delete it
        if (Directory.Exists(slotDataPath))
        {
            Directory.Delete(slotDataPath, true);
        }

        // Recreate the SlotData directory
        Directory.CreateDirectory(slotDataPath);


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
        foreach (var categoryKey in modelsSortedByCategory.Keys)
        {
            // Directly use the categoryKey as part of the path, assuming it's structured like "Category/Subcategory"
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
        storeClassData = EditorGUILayout.Toggle("Store Class Data", storeClassData);

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

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        HashSet<string> targetSkeletonNames = new HashSet<string>
    {
        "player_phx_skeleton.msh",
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

        Dictionary<string, HashSet<string>> skeletonMeshes = new Dictionary<string, HashSet<string>>();

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
        Debug.Log($"Slot data path set to: {slotDataPath}");

        // Create Human and Infected folders
        string humanPath = Path.Combine(slotDataPath, "Human");
        Debug.Log($"Human folder path set to: {humanPath}");

        string infectedPath = Path.Combine(slotDataPath, "Infected");
        Debug.Log($"Infected folder path set to: {infectedPath}");

        // Combine ALL_{filters}.json files
        CombineJsonFiles(humanPath);
        CombineJsonFiles(infectedPath);

        // Now merge into the ALL folder
        string allPath = Path.Combine(slotDataPath, "ALL");
        Debug.Log($"Merging files into ALL folder at: {allPath}");

        // Create the ALL folder if it doesn't exist
        CreateDirectoryIfNotExists(allPath);

        // Copy contents from Infected and Human folders to ALL folder
        MergeDirectories(humanPath, allPath);
        MergeDirectories(infectedPath, allPath);

        Debug.Log("Finished Reorganizing and Combining JSON files into ALL folder.");
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
        foreach (var file in Directory.GetFiles(sourceDir, "ALL_*.json"))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destDir, fileName);

            // If the destination file already exists, merge its contents
            if (File.Exists(destFile))
            {
                var sourceJson = File.ReadAllText(file);
                var destJson = File.ReadAllText(destFile);

                var sourceData = JsonConvert.DeserializeObject<AllFiltersModelData>(sourceJson);
                var destData = JsonConvert.DeserializeObject<AllFiltersModelData>(destJson);

                // Merge meshes, ensuring no duplicates
                var combinedMeshes = sourceData.meshes.Concat(destData.meshes).Distinct().ToList();

                // Write the merged data back to the destination file
                var combinedData = JsonConvert.SerializeObject(new { meshes = combinedMeshes }, Formatting.Indented);
                File.WriteAllText(destFile, combinedData);

                Debug.Log($"Merged {fileName}: Source contains {sourceData.meshes.Count} meshes, Destination contains {destData.meshes.Count} meshes. Resulting in {combinedMeshes.Count} unique meshes.");
            }
            else
            {
                // If no file exists, simply copy it
                File.Copy(file, destFile);
                Debug.Log($"Copied {fileName} to {destDir}");
            }
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
        Debug.Log($"Starting CombineJsonFiles method for parent folder: {parentFolderPath}");

        // Retrieve all subfolder paths
        var subFolders = Directory.GetDirectories(parentFolderPath);
        Debug.Log($"Found {subFolders.Length} subfolders in {parentFolderPath}");

        // Dictionary to store combined data
        var combinedData = new Dictionary<string, List<string>>();
        var fileCountPerSlot = new Dictionary<string, int>(); // To track how many files per slot (e.g., ALL_backpack.json)
        var itemCountPerSlot = new Dictionary<string, int>(); // To track how many items per slot

        foreach (var folder in subFolders)
        {
            string folderName = Path.GetFileName(folder);

            // Skip the "Child" folder
            if (folderName.Equals("Child", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"Skipping 'Child' folder: {folder}");
                continue;
            }

            var jsonFiles = Directory.GetFiles(folder, "ALL_*.json");
            Debug.Log($"Found {jsonFiles.Length} JSON files in folder: {folder}");

            foreach (var file in jsonFiles)
            {
                string fileName = Path.GetFileName(file);

                if (!combinedData.ContainsKey(fileName))
                {
                    combinedData[fileName] = new List<string>();
                    fileCountPerSlot[fileName] = 0; // Initialize count for this slot
                    itemCountPerSlot[fileName] = 0; // Initialize item count for this slot
                }

                var jsonData = File.ReadAllText(file);
                var allFiltersModelData = JsonConvert.DeserializeObject<AllFiltersModelData>(jsonData);
                int meshesCount = allFiltersModelData.meshes.Count;

                // Track file count and item count for this slot
                fileCountPerSlot[fileName]++;
                itemCountPerSlot[fileName] += meshesCount;

                combinedData[fileName].AddRange(allFiltersModelData.meshes);

                Debug.Log($"Added {meshesCount} meshes from {fileName}");
            }
        }

        // Write combined data to new JSON files
        foreach (var entry in combinedData)
        {
            string combinedFilePath = Path.Combine(parentFolderPath, entry.Key);
            var distinctMeshes = entry.Value.Distinct().ToList();
            string jsonContent = JsonConvert.SerializeObject(new { meshes = distinctMeshes }, Formatting.Indented);
            File.WriteAllText(combinedFilePath, jsonContent);

            Debug.Log($"Written combined data to {combinedFilePath} with {distinctMeshes.Count} unique meshes.");
            Debug.Log($"Slot {entry.Key}: Found {fileCountPerSlot[entry.Key]} JSON files, containing {itemCountPerSlot[entry.Key]} meshes in total. Combined into {distinctMeshes.Count} unique meshes.");
        }

        // Summarize totals across all slots
        int totalFiles = fileCountPerSlot.Values.Sum();
        int totalMeshes = itemCountPerSlot.Values.Sum();
        int totalUniqueMeshes = combinedData.Values.SelectMany(v => v).Distinct().Count();

        Debug.Log($"Finished CombineJsonFiles method for {parentFolderPath}. Processed {totalFiles} files containing {totalMeshes} meshes in total. Created {totalUniqueMeshes} unique meshes in the ALL folder.");
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
                Debug.Log($"ProcessModelsInFolder: modelFile: {modelFile}");
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
                        Debug.Log($"ProcessModelsInFolder: Skeleton name: {processedData.skeletonName}");
                        // Now passing skeletonName to DetermineCategory
                        string category = DetermineCategory(processedData.skeletonName);
                        Debug.Log($"ProcessModelsInFolder: category: {category}");
                        string normalizedFileName = fileNameWithoutExtension.ToLower();
                        if (category == "Man" || category == "Wmn")
                        {
                            if (normalizedFileName.Contains("_test_player"))
                            {
                                category = "Player";
                                Debug.Log($"Category changed to Player due to name: {normalizedFileName}");
                            }
                            else if (normalizedFileName.Contains("zmb"))
                            {
                                category = "Biter";
                                Debug.Log($"Category changed to Biter due to name: {normalizedFileName}");
                            }
                            else if (normalizedFileName.Contains("viral"))
                            {
                                category = "Viral";
                                Debug.Log($"Category changed to Viral due to name: {normalizedFileName}");
                            }
                            if (category == "Biter" || category == "Viral")
                            {
                                if (normalizedFileName.Contains("npc_waltz"))
                                {
                                    category = "Man";
                                    Debug.Log($"Category changed to Man due to name: {normalizedFileName}");
                                }
                            }
                        }
                        string type = DetermineType(category, fileNameWithoutExtension);

                        string outputDir = Path.Combine(Application.dataPath, "StreamingAssets/Jsons", type, category);
                        Directory.CreateDirectory(outputDir);

                        string outputFilePath = Path.Combine(outputDir, fileNameWithoutExtension + ".json");
                        Debug.Log($"outputFilePath {outputFilePath}");
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
        Debug.Log($"ProcessModelData: Skeleton name: {skeletonName}");
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
        Debug.Log($"DetermineCategory: Processing skeleton name: {skeletonName}");

        // Normalize skeletonName for comparison
        string normalizedSkeletonName = skeletonName.ToLower().Replace(".msh", "");

        foreach (var entry in skeletonToCategory)
        {
            string normalizedKey = entry.Key.ToLower();
            // Use exact match instead of Contains
            if (normalizedSkeletonName.Equals(normalizedKey))
            {
                Debug.Log($"Exact match found: {normalizedSkeletonName} -> {entry.Value}");
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
                Debug.Log($"Category changed to Wmn due to name: {normalizedFileName}");
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

    public static void GenerateMaterialDataJsons()
    {
        string jsonsDir = Path.Combine(Application.dataPath, "StreamingAssets/Jsons");
        string outputDir = Path.Combine(Application.dataPath, "StreamingAssets/Mesh References");

        var jsonFiles = Directory.GetFiles(jsonsDir, "*.json", SearchOption.AllDirectories);
        foreach (var file in jsonFiles)
        {
            string jsonContent = File.ReadAllText(file);
            ModelData modelData = JsonConvert.DeserializeObject<ModelData>(jsonContent);

            foreach (var slotPair in modelData.slotPairs)
            {
                foreach (var modelInfo in slotPair.slotData.models)
                {
                    if (string.IsNullOrWhiteSpace(modelInfo.name)) continue;

                    string meshName = Path.GetFileNameWithoutExtension(modelInfo.name);
                    string sanitizedMeshName = SanitizeFilename(meshName);
                    string outputFilePath = Path.Combine(outputDir, $"{sanitizedMeshName}.json");

                    MeshReferenceData meshReferenceData = new MeshReferenceData();
                    if (File.Exists(outputFilePath))
                    {
                        string existingJson = File.ReadAllText(outputFilePath);
                        meshReferenceData = JsonConvert.DeserializeObject<MeshReferenceData>(existingJson);
                        // Assuming you want to update the existing materialsData
                        meshReferenceData.materialsData = modelInfo.materialsData;
                    }
                    else
                    {
                        meshReferenceData.materialsData = modelInfo.materialsData;
                    }

                    string outputJson = JsonConvert.SerializeObject(meshReferenceData, Formatting.Indented);
                    File.WriteAllText(outputFilePath, outputJson);
                }
            }
        }

        Debug.Log("Finished generating material data JSONs.");
    }


    public static void CreateVariationJsons()
    {
        Debug.Log("Starting to create variation JSONs...");

        string jsonsDir = Path.Combine(Application.dataPath, "StreamingAssets/Jsons");
        string outputDir = Path.Combine(Application.dataPath, "StreamingAssets/Mesh References");

        Debug.Log($"Reading JSON files from: {jsonsDir}");
        Debug.Log($"Outputting variation JSONs to: {outputDir}");

        var jsonFiles = Directory.GetFiles(jsonsDir, "*.json", SearchOption.AllDirectories);
        foreach (var file in jsonFiles)
        {
            Debug.Log($"Processing file: {file}");
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

                    Debug.Log($"Processing model: {model.name} for variation creation");

                    VariationOutput variationOutput;
                    if (File.Exists(outputFilePath))
                    {
                        Debug.Log($"Reading existing variation for: {sanitizedMeshName}");
                        string existingJson = File.ReadAllText(outputFilePath);
                        variationOutput = JsonConvert.DeserializeObject<VariationOutput>(existingJson);
                    }
                    else
                    {
                        Debug.Log($"Creating new variation for: {sanitizedMeshName}");
                        variationOutput = new VariationOutput();
                    }

                    // Update or add new variations here based on your logic
                    UpdateVariations(variationOutput, model);

                    // Write updated or new JSON file
                    Debug.Log($"Writing variation data for: {sanitizedMeshName}");
                    string outputJson = JsonConvert.SerializeObject(variationOutput, Formatting.Indented);
                    File.WriteAllText(outputFilePath, outputJson);
                }
            }
        }
        Debug.Log("Finished creating variation JSONs.");
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
        Debug.Log($"Updating variations for model: {model.name}");

        if (variationOutput.materialsData == null || variationOutput.materialsData.Count == 0)
        {
            variationOutput.materialsData = model.materialsData;
        }

        HashSet<string> existingVariationSignatures = new HashSet<string>();

        foreach (var existingVariation in variationOutput.variations)
        {
            string existingSignature = GenerateVariationSignature(existingVariation);
            existingVariationSignatures.Add(existingSignature);
        }

        Variation newVariation = CreateNewVariationWithAllMaterials(variationOutput, model);

        if (DoesVariationIntroduceChanges(newVariation, model, existingVariationSignatures))
        {
            Debug.Log($"Adding new variation for model: {model.name}");
            variationOutput.variations.Add(newVariation);
        }
        else
        {
            Debug.Log($"No new variations added for model: {model.name} as no unique changes found");
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
        // First, check if the variation matches the original materialsData without any RTTI modifications.
        if (DoesVariationMatchOriginalMaterialsWithoutRTTI(newVariation, model))
        {
            // If it matches and doesn't introduce new RTTI values, it's not considered a new variation.
            return false;
        }

        // Generate a signature for the new variation based on material names and RTTI values.
        string variationSignature = GenerateVariationSignature(newVariation);

        // Check if this variation signature has already been processed.
        if (existingVariationSignatures.Contains(variationSignature))
        {
            // This variation does not introduce changes since its signature matches one that's already processed.
            return false;
        }

        // The variation is new or introduces changes, add its signature to the set for future comparisons.
        existingVariationSignatures.Add(variationSignature);

        // Since the variation is new, it introduces changes.
        return true;
    }

    private static bool DoesVariationMatchOriginalMaterialsWithoutRTTI(Variation variation, ModelInfo model)
    {
        // Check if all material names in the variation's materialsResources match the original materialsData names.
        bool allNamesMatch = model.materialsData.All(md =>
            variation.materialsResources.SelectMany(mr => mr.resources).Any(r => r.name == md.name));

        // Check if there are no RTTI values in the variation's materialsResources that match the original materialsData names.
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
#endif