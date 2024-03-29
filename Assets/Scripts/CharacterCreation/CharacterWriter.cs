using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using doppelganger;
using System;
using TMPro;
using SFB;
using System.Text;
using System.Linq;
using static ModelData;
using static doppelganger.CharacterWriter;
using UnityEngine.Rendering.HighDefinition;


/// <summary>
/// CharacterWriter facilitates character customization saving to JSON/model formats. 
/// It uses CharacterBuilder for slider/model data, ModelWriter for JSON to model conversion, and SkeletonLookup for skeleton determination. 
/// Supports UI-based configuration saving, slot mapping, fallback logic, gender determination, and material data handling. 
/// Enables dynamic character appearance customization.
/// </summary>

namespace doppelganger
{
    public class CharacterWriter : MonoBehaviour
    {
        [Header("Managers")]
        public CharacterBuilder_InterfaceManager interfaceManager;
        public CharacterBuilder characterBuilder;
        public ConfigManager configManager;
        public ModelWriter modelWriter;
        public ScreenshotManager screenshotManager;
        public SkeletonLookup skeletonLookup;

        [Header("Save Fields")]
        public TMP_InputField saveName;
        public TMP_InputField pathInputField;
        public TMP_Dropdown saveTypeDropdown;
        public TMP_Dropdown saveCategoryDropdown;
        public TMP_Dropdown saveClassDropdown;

        [Header("Options")]
        public bool createAdditionalModel = false;

        public AudioSource audioSource;
        public string dateSubfolder = DateTime.Now.ToString("yyyy_MM_dd");
        public string jsonOutputDirectory = Path.Combine(Application.streamingAssetsPath, "Output");
        public string outputDirectoryName = "Output";
        public string skeletonJsonPath = "Assets/StreamingAssets/Jsons/Human/Player/player_tpp_skeleton.json";
        public string slotUIDLookupRelativePath = "SlotData/SlotUIDLookup.json";
        private Dictionary<string, int> slotNameToUidMap = new Dictionary<string, int>();
        private Dictionary<string, string> sliderToSlotMapping = new Dictionary<string, string>()
    {
        {"ALL_head", "HEAD"},
        {"ALL_hair", "HEADCOVER"},
        {"ALL_hair_base", "HEADCOVER"},
        {"ALL_hair_1", "HEADCOVER"},
        {"ALL_hair_2", "HEADCOVER"},
        {"ALL_facial_hair", "HEAD_PART_1"},
        {"ALL_earrings", "HEAD_PART_1"},
        {"ALL_glasses", "HEAD_PART_1"},
        {"ALL_hat", "HEADCOVER"},
        {"ALL_hat_access", "HEADCOVER"},
        {"ALL_mask", "HEADCOVER"},
        {"ALL_mask_access", "HEADCOVER"},
        {"ALL_hands", "HANDS"},
        {"ALL_rhand", "HANDS_PART_1"},
        {"ALL_lhand", "HANDS_PART_1"},
        {"ALL_gloves", "PLAYER_GLOVES"},
        {"ALL_arm_access", "HANDS_PART_1"},
        {"ALL_rings", "HANDS_PART_1"},
        {"ALL_backpack", "TORSO_PART_1"},
        {"ALL_torso", "TORSO"},
        {"ALL_torso_2", "TORSO_PART_1"},
        {"ALL_cape", "TORSO_PART_1"},
        {"ALL_necklace", "TORSO_PART_1"},
        {"ALL_torso_access", "TORSO_PART_1"},
        {"ALL_torso_extra", "TORSO_PART_1"},
        {"ALL_armor_helmet", "HEADCOVER"},
        {"ALL_armor_helmet_access", "HEADCOVER"},
        {"ALL_armor_torso", "TORSO_PART_1"},
        {"ALL_armor_torso_access", "TORSO_PART_1"},
        {"ALL_armor_torso_lowerleft", "ARMS_PART_1"},
        {"ALL_armor_torso_lowerright", "ARMS_PART_1"},
        {"ALL_armor_torso_upperleft", "ARMS_PART_1"},
        {"ALL_armor_torso_upperright", "ARMS_PART_1"},
        {"ALL_armor_legs", "LEGS_PART_1"},
        {"ALL_armor_legs_lowerleft", "LEGS_PART_1"},
        {"ALL_armor_legs_lowerright", "LEGS_PART_1"},
        {"ALL_armor_legs_upperleft", "LEGS_PART_1"},
        {"ALL_armor_legs_upperright", "LEGS_PART_1"},
        {"ALL_sleeve", "ARMS_PART_1"},
        {"ALL_legs", "LEGS"},
        {"ALL_legs_access", "LEGS_PART_1"},
        {"ALL_legs_extra", "LEGS_PART_1"},
        {"ALL_shoes", "LEGS_PART_1"},
        {"ALL_decals", "OTHER_PART_1"},
        {"ALL_decals_extra", "OTHER_PART_1"},
        {"ALL_decals_logo", "OTHER_PART_1"},
        {"ALL_tattoo", "OTHER_PART_1"},
        // Add other mappings here
    };
        private Dictionary<string, List<string>> fallbackSlots = new Dictionary<string, List<string>>()
{
    {"HEADCOVER", new List<string> { "HEADCOVER_PART_1", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"HEADCOVER_PART_1", new List<string> { "HEADCOVER", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"HEAD_PART_1", new List<string> { "HEADCOVER", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"TORSO_PART_1", new List<string> { "TORSO_PART_1", "TORSO_PART_2", "TORSO_PART_3", "TORSO_PART_4", "TORSO_PART_5", "TORSO_PART_6", "TORSO_PART_7", "TORSO_PART_8", "TORSO_PART_9", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"ARMS_PART_1", new List<string> { "ARMS_PART_2", "ARMS_PART_3", "ARMS_PART_4", "ARMS_PART_5", "ARMS_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"OTHER_PART_1", new List<string> { "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"LEGS_PART_1", new List<string> { "PANTS", "PANTS_PART_1", "LEGS_PART_5", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"PLAYER_GLOVES", new List<string> { "HANDS_PART_1", "HANDS_PART_3", "HANDS_PART_4", "HANDS_PART_5", "HANDS_PART_6", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"HANDS_PART_1", new List<string> { "HANDS_PART_2", "HANDS_PART_3", "HANDS_PART_4", "HANDS_PART_5", "HANDS_PART_6", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"HANDS", new List<string> { "HANDS_PART_1", "HANDS_PART_2", "HANDS_PART_3", "HANDS_PART_4", "HANDS_PART_5", "HANDS_PART_6", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
};

        HashSet<string> maleMeshes = new HashSet<string>
    {
        "sh_npc_aiden_young.msh", "sh_scan_kid_001.msh","sh_scan_kid_002.msh","sh_scan_kid_003.msh","sh_scan_kid_004.msh", "sh_scan_boy_001.msh", "sh_scan_boy_002.msh", "sh_baker.msh", "sh_bruce.msh", "sh_dlc_opera_man_fighter_a.msh", "sh_dlc_opera_npc_andrew.msh", "sh_dlc_opera_npc_ciro.msh", "sh_dlc_opera_npc_ogar.msh", "sh_dlc_opera_npc_skullface.msh", "sh_dlc_opera_npc_tetsuo.msh", "sh_frank.msh", "sh_johnson.msh", "sh_man_a.msh", "sh_man_b.msh", "sh_npc_waltz_young.msh", "sh_man_bdt_a.msh", "sh_man_bdt_b.msh", "sh_man_bdt_c.msh", "sh_man_bdt_d.msh", "sh_man_bdt_e.msh", "sh_man_bdt_f.msh", "sh_man_bdt_g.msh", "sh_man_bdt_h.msh", "sh_man_bdt_i.msh", "sh_man_bdt_j.msh", "sh_man_bdt_k.msh", "sh_man_bdt_l.msh", "sh_man_c.msh", "sh_man_d.msh", "sh_man_infl_001.msh", "sh_man_obese_a.msh", "sh_man_pk_a.msh", "sh_man_pk_b.msh", "sh_man_pk_c.msh", "sh_man_pk_d.msh", "sh_man_pk_e.msh", "sh_man_pk_f.msh", "sh_man_pk_g.msh", "sh_man_pk_h.msh", "sh_man_pk_i.msh", "sh_man_pk_j.msh", "sh_man_sc_a.msh", "sh_man_sc_b.msh", "sh_man_sc_c.msh", "sh_man_sc_d.msh", "sh_man_srv_a.msh", "sh_man_srv_b.msh", "sh_man_srv_c.msh", "sh_man_srv_d.msh", "sh_npc_alberto_paganini.msh", "sh_npc_barney_b.msh", "sh_npc_carl.msh", "sh_npc_colonel.msh", "sh_npc_hakon.msh", "sh_npc_hank.msh", "sh_npc_herman.msh", "sh_npc_jack_matt.msh", "sh_npc_juan.msh", "sh_npc_rowe.msh", "sh_npc_simon.msh", "sh_npc_vincente.msh", "sh_npc_waltz_old.msh", "sh_scan_man_001.msh", "sh_scan_man_002.msh", "sh_scan_man_003.msh", "sh_scan_man_004.msh", "sh_scan_man_006.msh", "sh_scan_man_007.msh", "sh_scan_man_008.msh", "sh_scan_man_009.msh", "sh_scan_man_010.msh", "sh_scan_man_011.msh", "sh_scan_man_012.msh", "sh_scan_man_014.msh", "sh_scan_man_015.msh", "sh_scan_man_016.msh", "sh_scan_man_017.msh", "sh_scan_man_018.msh", "sh_scan_man_019.msh", "sh_scan_man_020.msh", "sh_scan_man_021.msh", "sh_scan_man_022.msh", "sh_scan_man_023.msh", "sh_scan_man_024.msh", "sh_scan_man_025.msh", "sh_scan_man_026.msh", "sh_scan_man_027.msh", "sh_scan_man_028.msh", "sh_scan_man_029.msh", "sh_scan_man_030.msh", "sh_scan_man_032.msh", "sh_scan_man_034.msh", "sh_scan_man_035.msh", "sh_scan_man_036.msh", "sh_scan_man_037.msh", "sh_scan_man_038.msh", "sh_scan_man_039.msh", "sh_scan_man_041.msh", "sh_scan_man_042.msh", "sh_scan_man_043.msh", "sh_scan_man_044.msh", "sh_scan_man_045.msh", "sh_scan_man_047.msh", "sh_scan_man_048.msh", "sh_scan_man_049.msh", "sh_scan_man_050.msh", "sh_scan_man_052.msh", "sh_scan_man_053.msh", "sh_scan_man_054.msh", "sh_scan_man_055.msh", "sh_npc_aiden.msh", "sh_player_fh_berserker.msh", "sh_player_fh_kensei.msh", "sh_player_wod_banuhaqim.msh", "sh_player_wod_brujah.msh", "sh_player_wod_tremere.msh"
    };

        HashSet<string> femaleMeshes = new HashSet<string>
    {
        "sh_npc_mia_young.msh","sh_scan_girl_001.msh","sh_scan_girl_002.msh","sh_scan_girl_003.msh","sh_scan_girl_004.msh","sh_chld_girl_srv_a.msh","sh_dlc_opera_npc_astrid.msh", "sh_dlc_opera_wmn_fighter_a.msh", "sh_mother_3.msh", "sh_npc_anderson.msh", "sh_npc_dr_veronika.msh", "sh_npc_hilda.msh", "sh_npc_lawan.msh", "sh_npc_meredith.msh", "sh_npc_mia_old.msh", "sh_npc_nuwa.msh", "sh_npc_plaguewitch.msh", "sh_npc_sophie.msh", "sh_npc_sophie_b.msh", "sh_npc_thalia.msh", "sh_scan_wmn_001.msh", "sh_scan_wmn_002.msh", "sh_scan_wmn_003.msh", "sh_scan_wmn_004.msh", "sh_scan_wmn_005.msh", "sh_scan_wmn_006.msh", "sh_scan_wmn_007.msh", "sh_scan_wmn_008.msh", "sh_scan_wmn_009.msh", "sh_scan_wmn_010.msh", "sh_scan_wmn_011.msh", "sh_scan_wmn_012.msh", "sh_scan_wmn_013.msh", "sh_scan_wmn_014.msh", "sh_scan_wmn_015.msh", "sh_scan_wmn_016.msh", "sh_scan_wmn_017.msh", "sh_scan_wmn_019.msh", "sh_scan_wmn_020.msh", "sh_scan_wmn_021.msh", "sh_scan_wmn_022.msh", "sh_scan_wmn_026.msh", "sh_scan_wmn_027.msh", "sh_scan_wmn_18.msh", "sh_scan_wmn_infl_001.msh", "sh_wmn_a.msh", "sh_wmn_b.msh", "sh_wmn_c.msh", "sh_wmn_d.msh", "sh_wmn_e.msh", "sh_wmn_pk_a.msh", "sh_wmn_pk_b.msh", "sh_wmn_pk_c.msh", "sh_wmn_sc_a.msh", "sh_wmn_sc_b.msh", "sh_wmn_srv_a.msh", "sh_wmn_srv_b.msh"
    };
        
        private readonly List<string> ExcludedSliders = new List<string>
{
    "ALL_head", "ALL_armor_helmet", "ALL_earrings", "ALL_facial_hair",
    "ALL_glasses", "ALL_hair", "ALL_hair_2", "ALL_hair_3", "ALL_hair_base",
    "ALL_hat", "ALL_hat_access", "ALL_mask", "ALL_mask_access"
};

        private Dictionary<string, Queue<int>> slotUidLookup = new Dictionary<string, Queue<int>>();
        private void LoadSlotUidLookup()
        {
            // Correcting the path to include the "SlotData" directory
            string jsonPath = Path.Combine(Application.dataPath, "StreamingAssets/SlotData/SlotUidLookup_Empty.json");
            if (File.Exists(jsonPath))
            {
                string jsonContent = File.ReadAllText(jsonPath);
                var lookup = JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(jsonContent);
                slotUidLookup = lookup.ToDictionary(kvp => kvp.Key, kvp => new Queue<int>(kvp.Value));
            }
            else
            {
                Debug.LogError($"SlotUidLookup_Empty.json file not found at path: {jsonPath}");
                slotUidLookup = new Dictionary<string, Queue<int>>(); // Initialize to avoid null reference
            }
        }

        void Start()
        {
            string savePath = ConfigManager.LoadSetting("SavePath", "Path");
            if (!string.IsNullOrEmpty(savePath))
            {
                pathInputField.text = savePath;
            }

            if (characterBuilder == null)
            {
                characterBuilder = FindObjectOfType<CharacterBuilder>();
                Debug.Log("CharacterBuilder found: " + (characterBuilder != null));
            }
            string jsonOutputPath = Path.Combine(Application.dataPath, "StreamingAssets/Jsons/Custom", dateSubfolder, saveName.text + ".json");
            LoadSlotUidLookup();
        }

        public void OpenSetPathDialog()
        {
            // Open folder browser and then save the selected path
            var paths = StandaloneFileBrowser.OpenFolderPanel("Select Dying Light 2 Root Folder", "", false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                string path = paths[0];
                SavePathToConfig(path);
                pathInputField.text = path; // Update the input field with the selected path
                Debug.Log($"Path set and saved: {path}");
            }
            else
            {
                Debug.LogError("No path selected.");
            }
        }

        private void SavePathToConfig(string newPath)
        {
            ConfigManager.SaveSetting("SavePath", "Path", newPath);
            Debug.Log($"Path saved to config: {newPath}");
        }

        public void WriteCurrentConfigurationToJson()
        {
            if (characterBuilder == null || skeletonLookup == null)
            {
                Debug.LogError("Dependencies are null. Cannot write configuration.");
                return;
            }
            
            string customBasePath = ConfigManager.LoadSetting("SavePath", "Path");
            string targetPath = Path.Combine(customBasePath, "ph/source");

            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            // Extracting values from the UI components
            string saveType = saveTypeDropdown.options[saveTypeDropdown.value].text;
            string saveCategory = saveCategoryDropdown.options[saveCategoryDropdown.value].text;
            string saveClass = saveClassDropdown.options[saveClassDropdown.value].text;
            string saveNameText = saveName.text;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(saveNameText);
            string screenshotFileName = fileNameWithoutExtension + ".png";
            string slotUIDLookupFullPath = Path.Combine(Application.streamingAssetsPath, slotUIDLookupRelativePath);
            SlotUIDLookup slotUIDLookup = SlotUIDLookup.LoadFromJson(slotUIDLookupFullPath);
            Debug.Log($"saveNameText {saveNameText} found in lookup for {fileNameWithoutExtension}.");

            Dictionary<string, string> skeletonDictLookup = ReadSkeletonLookup();
            string skeletonName;

            
            // First, attempt to find the skeleton name directly in the skeletonDictLookup.
            if (skeletonDictLookup.ContainsKey(fileNameWithoutExtension))
            {
                skeletonName = skeletonDictLookup[fileNameWithoutExtension];
                Debug.Log($"Skeleton name {skeletonName} found in lookup for {fileNameWithoutExtension}.");
            }
            else
            {
                // If not found, fallback to using the SkeletonLookup component's method.
                skeletonName = skeletonLookup.LookupSkeleton(saveCategory, saveClass);
                Debug.Log($"Fallback skeleton name {skeletonName} obtained using the LookupSkeleton method for {saveCategory}, {saveClass}.");
            }

            bool skeletonUpdated;

            // Overwrite fileName if saveCategory is "Player"
            string fileName = saveCategory.Equals("Player", StringComparison.OrdinalIgnoreCase) ? (string.IsNullOrWhiteSpace(saveName.text) || saveName.text.Equals("Aiden", StringComparison.OrdinalIgnoreCase) ? "player_tpp_skeleton" : saveName.text) : saveName.text;

            // Ensure fileName is valid
            if (string.IsNullOrWhiteSpace(fileName))
            {
                Debug.LogError("Save name is empty. Cannot write configuration.");
                return;
            }

            // Constructing the JSON and .model file paths dynamically based on the fileName
            string jsonOutputPath = Path.Combine(Application.dataPath, "StreamingAssets/Jsons/Custom", saveCategory, DateTime.Now.ToString("yyyy_MM_dd"), saveName.text + ".json");
            string screenshotPath = Path.Combine(Application.dataPath, "StreamingAssets/Jsons/Custom", saveCategory, DateTime.Now.ToString("yyyy_MM_dd"), screenshotFileName);
            string modelOutputPath = Path.Combine(customBasePath, "ph/source", fileName + ".model");
            
            Debug.Log($"jsonOutputDirectory {jsonOutputDirectory}, screenshotPath {screenshotPath}, modelOutputPath {modelOutputPath}");

            var sliderValues = interfaceManager.GetSliderValues();
            var currentlyLoadedModels = characterBuilder.GetCurrentlyLoadedModels();

            var slotPairs = new List<ModelData.SlotDataPair>();
            var usedSlots = new HashSet<string>();
            HashSet<int> usedSlotUids = new HashSet<int>(); // Moved outside the loop

            foreach (var slider in sliderValues)
            {
                Debug.Log($"Processing slider: {slider.Key} with value: {slider.Value}");

                if (slider.Value > 0)
                {
                    if (currentlyLoadedModels.TryGetValue(slider.Key, out GameObject model))
                    {
                        if (sliderToSlotMapping.TryGetValue(slider.Key, out string slotKey))
                        {
                            bool slotAssigned = false;
                            string modelName = model.name.Replace("(Clone)", ".msh").ToLower();
                            string potentialSkeletonName = skeletonLookup.FindMatchingSkeleton(modelName);
                            Debug.Log($"{potentialSkeletonName} Skeleton updated to {skeletonName} based on loaded models.");
                            if (!string.IsNullOrEmpty(potentialSkeletonName) && potentialSkeletonName != "player_skeleton.msh" && potentialSkeletonName != skeletonName)
                            {
                                skeletonName = potentialSkeletonName;
                                skeletonUpdated = true;
                                Debug.Log($"{potentialSkeletonName} Skeleton updated to {skeletonName} based on loaded models.");
                            }
                            if (slotUIDLookup.ModelSlots.TryGetValue(modelName, out List<ModelData.SlotInfo> possibleSlots))
                            {
                                Debug.Log($"Found possible slots for model {modelName}: {possibleSlots.Count}");
                                ModelData.SlotDataPair slotPair; // Declaration moved outside of the loop

                                foreach (var possibleSlot in possibleSlots)
                                {
                                    if (!usedSlotUids.Contains(possibleSlot.slotUid))
                                    {
                                        slotPair = CreateSlotDataPair(model, possibleSlot.name, possibleSlot.slotUid);
                                        slotPairs.Add(slotPair);
                                        usedSlotUids.Add(possibleSlot.slotUid);
                                        usedSlots.Add(possibleSlot.name); // Also mark the slot name as used
                                        Debug.Log($"Assigned {possibleSlot.name} slot for {slider.Key} with Slot UID: {possibleSlot.slotUid}");
                                        slotAssigned = true;
                                        break; // Exit the loop since a slot has been successfully assigned
                                    }
                                    else
                                    {
                                        Debug.Log($"Slot UID {possibleSlot.slotUid} for slot {possibleSlot.name} is already in use.");
                                        string initialFallbackSlotName = sliderToSlotMapping[slider.Key];
                                        List<string> fallbackOptions = fallbackSlots.TryGetValue(initialFallbackSlotName, out var initialFallbacks) ? initialFallbacks : new List<string>();

                                        Debug.Log($"Fallback options for {initialFallbackSlotName}: {string.Join(", ", fallbackOptions)}");

                                        if (!slotAssigned)
                                        { 
                                            foreach (var fallbackOption in fallbackOptions)
                                            {
                                                Debug.Log($"Considering fallback option: {fallbackOption}");
                                                // Directly check if the fallback slot name is available and not yet used
                                                if (!usedSlots.Contains(fallbackOption))
                                                {
                                                    // Attempt to find a matching slot UID that hasn't been used yet
                                                    var nextAvailableSlotUid = DetermineNextAvailableSlotUid(fallbackOption, usedSlotUids);
                                                    if (nextAvailableSlotUid != -1) // Assuming -1 indicates failure to find an available UID
                                                    {
                                                        Debug.Log($"Using next available Slot UID '{nextAvailableSlotUid}' for fallback slot '{fallbackOption}'.");
                                                        slotPair = CreateSlotDataPair(model, fallbackOption, nextAvailableSlotUid);
                                                        slotPairs.Add(slotPair);
                                                        usedSlotUids.Add(nextAvailableSlotUid);
                                                        usedSlots.Add(fallbackOption);
                                                        Debug.Log($"Assigned fallback {fallbackOption} slot for {slider.Key} with Slot UID: {nextAvailableSlotUid}");
                                                        slotAssigned = true;
                                                        
                                                    }
                                                    else
                                                    {
                                                        Debug.Log($"Fallback option '{fallbackOption}' available but no unused Slot UID found within the allowed range.");
                                                    }
                                                }
                                                else
                                                {
                                                    Debug.Log($"Fallback option '{fallbackOption}' already used.");
                                                }
                                                if (slotAssigned)
                                                {
                                                    break;
                                                }
                                            }
                                        }

                                        if (!slotAssigned)
                                        {
                                            // Fallback logic exhausted and still no slot assigned
                                            Debug.LogWarning($"No available slots found for model {modelName}. Consider handling this scenario.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Model not found in currentlyLoadedModels for slider {slider.Key}");
                    }
                }
            }

            // Ensure slotPairs are sorted after appending empty slots
            slotPairs = slotPairs.OrderBy(pair => pair.slotData.slotUid).ToList();
            screenshotManager.CaptureAndMoveScreenshot(screenshotPath);
            // Write the configuration to JSON
            var outputData = new ModelData
            {
                skeletonName = skeletonName,
                slotPairs = slotPairs,
                modelProperties = new ModelData.ModelProperties
                {
                    @class = saveClass.Equals("ALL", StringComparison.OrdinalIgnoreCase) ? "NPC" : saveClass,
                    race = "Unknown", // Customize as needed
                    sex = DetermineCharacterSex(slotPairs) // Implement this method based on your logic
                }
            };
            Debug.Log($"outputData {outputData.skeletonName}");
            WriteConfigurationOutput(outputData, jsonOutputPath, modelOutputPath);
        }

        private void WriteConfigurationOutput(ModelData outputData, string jsonOutputPath, string modelOutputPath)
        {
            string customBasePath = ConfigManager.LoadSetting("SavePath", "Path");
            string dataPath = Path.Combine(customBasePath, "ph/source");
            string json = JsonConvert.SerializeObject(outputData, Formatting.Indented);

            // Ensure the output directory exists
            string outputDir = Path.GetDirectoryName(jsonOutputPath);
            Directory.CreateDirectory(outputDir);

            // Write the JSON file
            File.WriteAllText(jsonOutputPath, json);
            Debug.Log($"Configuration saved to {jsonOutputPath}");

            if (modelWriter != null)
            {
                string saveCategory = saveCategoryDropdown.options[saveCategoryDropdown.value].text;
                modelWriter.ConvertJsonToModelFormat(jsonOutputPath, modelOutputPath, saveCategory);
                Debug.Log($"Model file created at {modelOutputPath}");

                string outputPakPath = DeterminePakFilePath(dataPath);
                EnsurePlaceholderInPak(outputPakPath);
                string modelFileNameWithinZip = Path.GetFileName(modelOutputPath);
                ZipUtility.AddOrUpdateFilesInZip(modelOutputPath, outputPakPath, modelFileNameWithinZip);
                Debug.Log($"{outputPakPath} updated with model data.");

                // Cleanup: Optionally delete the temporary model file
                try
                {
                    File.Delete(modelOutputPath);
                    Debug.Log($"{modelOutputPath} was successfully deleted.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to delete {modelOutputPath}: {e.Message}");
                }
                audioSource.Play();
            }
            else
            {
                Debug.LogError("ModelWriter not set.");
            }
        }

        private string DeterminePakFilePath(string directoryPath)
        {
            Debug.Log($"Scanning directory for .pak files: {directoryPath}");

            var pakFiles = Directory.GetFiles(directoryPath, "data*.pak")
                                    .Where(file => !Path.GetFileNameWithoutExtension(file).StartsWith("data_devtools"))
                                    .Select(Path.GetFileNameWithoutExtension)
                                    .Where(fileName => int.TryParse(fileName.Replace("data", ""), out int _))
                                    .Select(fileName => new
                                    {
                                        FileName = fileName,
                                        Number = int.Parse(fileName.Replace("data", ""))
                                    })
                                    .OrderBy(file => file.Number)
                                    .ToList();

            Debug.Log($"Filtered and processed .pak files: {string.Join(", ", pakFiles.Select(f => $"{f.FileName} => {f.Number}"))}");
            int highestNumber = pakFiles.Any() ? pakFiles.Last().Number : 1; // Default to 1 if no .pak files are found
            string highestPakPath = Path.Combine(directoryPath, $"data{highestNumber}.pak");
            if (File.Exists(highestPakPath) && !ZipUtility.ZipContainsFile(highestPakPath, "PLACEHOLDER_InfinityDesigner.file"))
            {
                highestNumber++;
            }

            string newPakPath = Path.Combine(directoryPath, $"data{highestNumber}.pak");
            Debug.Log($"Preparing new .pak file: {newPakPath}. This is the next sequence after checking existing .pak files.");

            return newPakPath;
        }

        private void EnsurePlaceholderInPak(string pakFilePath)
        {
            const string placeholderFileName = "PLACEHOLDER_InfinityDesigner.file";
            if (!ZipUtility.ZipContainsFile(pakFilePath, placeholderFileName))
            {
                // Use a more direct approach to create and add the placeholder file
                string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);

                try
                {
                    string tempPlaceholderPath = Path.Combine(tempDirectory, placeholderFileName);
                    File.WriteAllText(tempPlaceholderPath, ""); // Create an empty placeholder file

                    // Ensure the placeholder is added to the root of the pak file
                    ZipUtility.AddOrUpdateFilesInZip(tempPlaceholderPath, pakFilePath, placeholderFileName);
                }
                finally
                {
                    // Clean up the temporary directory and its contents
                    Directory.Delete(tempDirectory, true);
                }
            }
        }


        public class SlotUIDLookup
        {
            public Dictionary<string, List<ModelData.SlotInfo>> ModelSlots { get; set; }

            public SlotUIDLookup()
            {
                ModelSlots = new Dictionary<string, List<ModelData.SlotInfo>>();
            }

            public static SlotUIDLookup LoadFromJson(string path)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError($"File not found at path: {path}");
                    return null;
                }

                try
                {
                    string jsonText = File.ReadAllText(path);
                    SlotUIDLookup slotUIDLookup = JsonConvert.DeserializeObject<SlotUIDLookup>(jsonText);

                    // Debug log: Print the loaded ModelSlots
                    Debug.Log($"Loaded ModelSlots from JSON: {slotUIDLookup.ModelSlots.Count} entries");

                    return slotUIDLookup;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to load SlotUIDLookup from JSON: {ex.Message}");
                    return null;
                }
            }
        }

        private string GetAvailableSlot(string initialSlot, HashSet<string> usedSlots)
        {
            Debug.Log($"Checking availability of slot: {initialSlot}");

            if (!usedSlots.Contains(initialSlot))
            {
                Debug.Log($"Slot {initialSlot} is available");
                return initialSlot;
            }

            if (fallbackSlots.TryGetValue(initialSlot, out List<string> fallbacks))
            {
                Debug.Log($"Fallback slots for {initialSlot}:");

                foreach (var fallback in fallbacks)
                {
                    Debug.Log($"  Checking fallback slot: {fallback}");
                    // Recursively check for available slots in the fallback list
                    string availableSlot = GetAvailableSlot(fallback, usedSlots);
                    if (availableSlot != null)
                    {
                        Debug.Log($"  Found available fallback slot: {availableSlot}");
                        return availableSlot; // Found an available fallback slot
                    }
                }
            }
            Debug.Log($"No available slots found for {initialSlot}");
            return null; // No available slots found
        }

        private int DetermineNextAvailableSlotUid(string slotName, HashSet<int> existingSlotUids)
        {
            if (slotUidLookup.TryGetValue(slotName, out Queue<int> availableUids))
            {
                while (availableUids.Count > 0)
                {
                    int uid = availableUids.Dequeue(); // Try the next available UID
                    if (!existingSlotUids.Contains(uid))
                    {
                        Debug.Log($"Found available UID '{uid}' for slot '{slotName}'.");
                        return uid;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"No available UIDs found for slot '{slotName}'.");
            }
            int startUid = 100;
            int endUid = 199;
            int newUid = startUid;

            while (existingSlotUids.Contains(newUid))
            {
                newUid++;
                // If it exceeds the range, wrap around or extend your range logic here
                if (newUid > endUid)
                {
                    Debug.LogError($"Exceeded UID range for slot '{slotName}'. Consider expanding UID range or checking for errors.");
                    // Handle error condition, perhaps by extending the range or other logic
                    break; // Or return -1 to indicate failure, based on your error handling policy
                }
            }

            Debug.Log($"Generated new UID '{newUid}' for slot '{slotName}'.");
            return newUid;
        }

        private Dictionary<string, string> ReadSkeletonLookup()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Skeletons", "skeleton_lookup.json");
            if (!File.Exists(path))
            {
                Debug.LogError("Skeleton lookup file not found.");
                return new Dictionary<string, string>();
            }
            Debug.Log($"ReadSkeletonLookup: path: {path}");
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        private string DetermineCharacterSex(List<ModelData.SlotDataPair> slotPairs)
        {
            foreach (var pair in slotPairs)
            {
                if (pair.slotData.name.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    string modelName = pair.slotData.models[0].name;

                    // Check and ensure that modelName ends with ".msh" only once
                    if (modelName.EndsWith(".msh.msh"))
                    {
                        modelName = modelName.Substring(0, modelName.Length - 4); // Remove the last ".msh"
                    }

                    Debug.Log($"Model Name: {modelName}");

                    if (maleMeshes.Contains(modelName))
                    {
                        Debug.Log("Character Sex: Male");
                        return "Male";
                    }
                    else if (femaleMeshes.Contains(modelName))
                    {
                        Debug.Log("Character Sex: Female");
                        return "Female";
                    }
                    break;
                }
            }
            Debug.Log("Character Sex: Unknown");
            return "";
        }

        private ModelData.SlotDataPair CreateSlotDataPair(GameObject model, string slotKey, int slotUid)
        {
            VariationBuilder variationBuilder = FindObjectOfType<VariationBuilder>();

            // Store the original slotKey for exporting purposes
            string originalSlotKey = slotKey;

            // Transform the slotKey for internal lookup
            string lookupSlotKey = "ALL_" + slotKey.ToLower();
            Debug.Log($"Creating SlotDataPair for {model.name} with original slot {originalSlotKey} and lookup slot {lookupSlotKey}");

            // Format the model name: remove "(Clone)" and add ".msh"
            string formattedModelName = FormatModelName(model.name);
            Debug.Log($"Formatted model name: {formattedModelName}");

            // Initialize slotData with the formatted model name and the transformed slot key for internal purposes
            var slotData = new ModelData.SlotData
            {
                name = slotKey, // Use the slotKey as provided; assumes it's already the "original" key or you handle naming elsewhere
                slotUid = slotUid, // Directly use the provided slotUid
                models = new List<ModelData.ModelInfo>() // Initialization remains the same
            };

            string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", $"{formattedModelName.Replace(".msh", "")}.json");
            Debug.Log($"Looking for material JSON at: {materialJsonFilePath}");

            if (File.Exists(materialJsonFilePath))
            {
                Debug.Log($"Found material JSON for {formattedModelName}");
                string materialJsonData = File.ReadAllText(materialJsonFilePath);
                Debug.Log($"No variation index found for slot {lookupSlotKey}. Using original materials data and resources.");
                // Fallback to using original materials data and resources
                slotData.models.Add(new ModelData.ModelInfo
                {
                    name = formattedModelName,
                    materialsData = GetMaterialsDataFromStreamingAssets(formattedModelName.Replace(".msh", "")), // Get original materials data
                    materialsResources = GetMaterialsResourcesFromModel(model)
                });
            }
            else
            {
                Debug.LogError($"Material JSON file does not exist for model: {formattedModelName}. Cannot load variation or original materials.");
            }

            return new ModelData.SlotDataPair { key = slotKey, slotData = slotData };
        }

        private List<ModelData.MaterialData> GetMaterialsDataFromStreamingAssets(string modelName)
        {
            List<ModelData.MaterialData> materialsData = new List<ModelData.MaterialData>();

            // Remove the ".msh" extension from modelName
            modelName = Path.GetFileNameWithoutExtension(modelName);

            // Print original materials from streaming assets
            string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", modelName + ".json");
            if (File.Exists(materialJsonFilePath))
            {
                string materialJsonData = File.ReadAllText(materialJsonFilePath);
                ModelData.ModelInfo modelInfo = JsonUtility.FromJson<ModelData.ModelInfo>(materialJsonData);
                if (modelInfo != null && modelInfo.materialsData != null)
                {
                    // Iterate through the materialsData in the JSON file
                    foreach (var materialData in modelInfo.materialsData)
                    {
                        // Ensure each material name ends with ".mat"
                        string materialName = materialData.name;
                        if (!materialName.EndsWith(".mat"))
                        {
                            materialName += ".mat";
                        }
                        //Debug.Log($"Material Name: {materialName}");
                        materialsData.Add(new ModelData.MaterialData
                        {
                            number = materialData.number, // Use the correct material number
                            name = materialName
                        });
                    }
                }
                else
                {
                    Debug.LogError("Invalid JSON data or missing materialsData in the JSON file.");
                }
            }
            else
            {
                Debug.LogError("Material JSON file not found: " + materialJsonFilePath);
            }

            return materialsData;
        }


        private string FormatModelName(string modelName)
        {
            // Remove "(Clone)" from the model name
            string cleanName = modelName.Replace("(Clone)", ".msh").Trim();

            return cleanName;
        }


        private List<ModelData.MaterialResource> GetMaterialsResourcesFromModel(GameObject model)
        {
            VariationBuilder variationBuilder = FindObjectOfType<VariationBuilder>();
            List<ModelData.MaterialResource> materialsResources = new List<ModelData.MaterialResource>();

            string modelName = model.name.Replace("(Clone)", "");
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();

            if (!variationBuilder.modelSpecificChanges.TryGetValue(modelName, out ModelChange modelChanges))
            {
                //Debug.LogWarning($"No specific changes found for model '{modelName}'. Using default materials.");
            }

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                foreach (var material in renderer.sharedMaterials)
                {
                    // Construct the default material name, ensuring the .mat extension
                    string materialNameWithExtension = material.name + (material.name.EndsWith(".mat") ? "" : ".mat");
                    var resource = new ModelData.Resource
                    {
                        name = materialNameWithExtension,
                        rttiValues = new List<RttiValue>() // Initialize with an empty list to avoid null references
                    };

                    // Adjust the resource based on recorded changes
                    if (modelChanges != null && modelChanges.MaterialsByRenderer.TryGetValue(rendererIndex, out MaterialChange materialChange))
                    {
                        // Update the resource name if there's a new name
                        resource.name = materialChange.NewName.EndsWith(".mat") ? materialChange.NewName : $"{materialChange.NewName}.mat";
                        // Include any texture changes
                        resource.rttiValues = materialChange.TextureChanges;
                    }

                    // Create a material resource for this renderer/material
                    materialsResources.Add(new ModelData.MaterialResource
                    {
                        number = rendererIndex + 1, // Assuming numbering starts from 1 for materials
                        resources = new List<ModelData.Resource> { resource }
                    });
                }
            }

            return materialsResources;
        }
    }
}