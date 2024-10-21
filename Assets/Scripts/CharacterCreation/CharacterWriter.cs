using UnityEngine;
using UnityEngine.UI;
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
using Michsky.UI.Dark;
using System.Text.RegularExpressions;

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
        public NotificationManager notificationManager;

        [Header("Save Fields")]
        public TMP_InputField saveName;
        public TMP_InputField pathInputField;
        public TMP_InputField customContentPathInputField;
        public TMP_InputField customOutputPathInputField;
        public TMP_Dropdown saveTypeDropdown;
        public TMP_Dropdown saveCategoryDropdown;
        public TMP_Dropdown saveClassDropdown;
        public SwitchManager fppToggleSwitchManager;

        [Header("Options")]
        public bool createAdditionalModel = false;

        public AudioSource audioSource;
        public string dateSubfolder = DateTime.Now.ToString("yyyy_MM_dd");
        public string jsonOutputDirectory = Path.Combine(Application.streamingAssetsPath, "Output");
        public string outputDirectoryName = "Output";
        public string skeletonJsonPath = "Assets/StreamingAssets/Jsons/Human/Player/player_tpp_skeleton.json";
        public string slotUIDLookupRelativePath = "SlotData/SlotUIDLookup.json";
        private bool skeletonUpdated;
        private Dictionary<string, int> slotNameToUidMap = new Dictionary<string, int>();
        private Dictionary<string, string> sliderToSlotMapping = new Dictionary<string, string>()
    {
        {"ALL_head", "HEAD"},
        {"ALL_hat", "HEADCOVER"},
        {"ALL_hat_access", "HEADCOVER"},
        {"ALL_hood", "HEADCOVER"},
        {"ALL_mask", "HEADCOVER"},
        {"ALL_mask_access", "HEADCOVER"},
        {"ALL_hazmat_head", "HEADCOVER"},
        {"ALL_glasses", "HEAD_PART_1"},
        {"ALL_necklace", "TORSO_PART_1"},
        {"ALL_earrings", "HEAD_PART_1"},
        {"ALL_rings", "HANDS_PART_1"},
        {"ALL_hair", "HEADCOVER"},
        {"ALL_hair_base", "HEADCOVER"},
        {"ALL_hair_2", "HEADCOVER"},
        {"ALL_hair_3", "HEADCOVER"},
        {"ALL_facial_hair", "HEAD_PART_1"},
        {"ALL_cape", "TORSO_PART_1"},
        {"ALL_chest", "TORSO"},
        {"ALL_torso", "TORSO"},
        {"ALL_torso_2", "TORSO_PART_1"},
        {"ALL_torso_extra", "TORSO_PART_1"},
        {"ALL_torso_access", "TORSO_PART_1"},
        {"ALL_shirt", "TORSO"},
        {"ALL_jacket", "TORSO"},
        {"ALL_flashlight", "TORSO_PART_1"},
        {"ALL_waist", "TORSO_PART_1"},
        {"ALL_weapons", "TORSO_PART_1"},
        {"ALL_belts", "TORSO_PART_1"},
        {"ALL_hands", "HANDS"},
        {"ALL_lhand", "HANDS"},
        {"ALL_rhand", "HANDS"},
        {"ALL_gloves", "PLAYER_GLOVES"},
        {"ALL_gloves_2", "PLAYER_GLOVES"},
        {"ALL_arm_access", "ARMS_PART_1"},
        {"ALL_arm_access_2", "ARMS_PART_1"},
        {"ALL_sleeve", "ARMS_PART_1"},
        {"ALL_backpack", "TORSO_PART_1"},
        {"ALL_bumbag", "TORSO_PART_1"},
        {"ALL_gastank", "TORSO_PART_1"},
        {"ALL_blisters_1", "HEAD_PART_1"},
        {"ALL_blisters_2", "HEAD_PART_1"},
        {"ALL_blisters_3", "HEAD_PART_1"},
        {"ALL_blisters_4", "HEAD_PART_1"},
        {"ALL_pockets", "TORSO_PART_1"},
        {"ALL_decals", "OTHER_PART_1"},
        {"ALL_decals_2", "OTHER_PART_1"},
        {"ALL_decals_extra", "OTHER_PART_1"},
        {"ALL_decals_logo", "OTHER_PART_1"},
        {"ALL_tattoo", "OTHER_PART_1"},
        {"ALL_tattoo_2", "OTHER_PART_1"},
        {"ALL_legs", "LEGS"},
        {"ALL_legs_2", "LEGS_PART_1"},
        {"ALL_legs_extra", "LEGS_PART_1"},
        {"ALL_legs_access", "LEGS_PART_1"},
        {"ALL_shoes", "LEGS_PART_1"},
        {"ALL_armor_helmet", "HEADCOVER"},
        {"ALL_armor_helmet_access", "HEADCOVER"},
        {"ALL_armor_torso", "TORSO_PART_1"},
        {"ALL_armor_torso_2", "TORSO_PART_1"},
        {"ALL_armor_torso_access", "TORSO_PART_1"},
        {"ALL_armor_torso_extra", "TORSO_PART_1"},
        {"ALL_armor_torso_upperright", "ARMS_PART_1"},
        {"ALL_armor_torso_upperleft", "ARMS_PART_1"},
        {"ALL_armor_torso_lowerright", "ARMS_PART_1"},
        {"ALL_armor_torso_lowerleft", "ARMS_PART_1"},
        {"ALL_armor_legs", "LEGS_PART_1"},
        {"ALL_armor_legs_access", "LEGS_PART_1"},
        {"ALL_armor_legs_upperright", "LEGS_PART_1"},
        {"ALL_armor_legs_upperleft", "LEGS_PART_1"},
        {"ALL_armor_legs_lowerright", "LEGS_PART_1"},
        {"ALL_armor_legs_lowerleft", "LEGS_PART_1"}
    };
        private Dictionary<string, List<string>> fallbackSlots = new Dictionary<string, List<string>>()
{
    {"HEADCOVER", new List<string> { "HEADCOVER_PART_1", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"HEADCOVER_PART_1", new List<string> { "HEADCOVER", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"HEAD_PART_1", new List<string> { "HEADCOVER", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"TORSO", new List<string> { "TORSO_PART_1", "TORSO_PART_2", "TORSO_PART_3", "TORSO_PART_4", "TORSO_PART_5", "TORSO_PART_6", "TORSO_PART_7", "TORSO_PART_8", "TORSO_PART_9", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"TORSO_PART_1", new List<string> { "TORSO_PART_2", "TORSO_PART_3", "TORSO_PART_4", "TORSO_PART_5", "TORSO_PART_6", "TORSO_PART_7", "TORSO_PART_8", "TORSO_PART_9", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
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
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "SlotData/SlotUidLookup_Empty.json");
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

            string outputPath = ConfigManager.LoadSetting("SavePath", "Output_Path");
            if (!string.IsNullOrEmpty(savePath))
            {
                customOutputPathInputField.text = outputPath;
            }

            string contentPath = ConfigManager.LoadSetting("SavePath", "Content_Path");
            if (!string.IsNullOrEmpty(savePath))
            {
                customContentPathInputField.text = contentPath;
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

                // Attempt to find the executable in the expected directory structure
                string exePath = Path.Combine(path, "ph", "work", "bin", "x64", "DyingLightGame_x64_rwdi.exe");

                // Check if the executable exists
                if (File.Exists(exePath))
                {
                    SavePathToConfig(path);
                    pathInputField.text = path;
                    Debug.Log($"Path set and saved: {path}");
                }
                else
                {
                    pathInputField.text = "Set Path";
                    Debug.LogError($"Dying Light 2 executable not found, make sure this is the Dying Light 2 Root folder");
                }
            }
            else
            {
                Debug.LogError("No path selected.");
            }
        }

        public void OpenSetOutputPathDialog()
        {
            // Open folder browser and then save the selected path
            var paths = StandaloneFileBrowser.OpenFolderPanel("Select Custom Output Folder", "", false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                string Outputpath = paths[0];
                customOutputPathInputField.text = Outputpath;
                ConfigManager.SaveSetting("SavePath", "Output_Path", Outputpath);
            }
            else
            {
                Debug.LogError("No path selected.");
            }
        }

        public void OpenSetContentPathDialog()
        {
            // Open folder browser and then save the selected path
            var paths = StandaloneFileBrowser.OpenFolderPanel("Select Custom Content Folder", "", false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                string Contentpath = paths[0];
                customContentPathInputField.text = Contentpath;
                ConfigManager.SaveSetting("SavePath", "Content_Path", Contentpath);
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
            string targetPath = !string.IsNullOrEmpty(customBasePath) ? Path.Combine(customBasePath, "ph/source") : Path.Combine(Application.dataPath, "StreamingAssets/Jsons/Custom");
            
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
            string screenshotFileName = fileNameWithoutExtension;
            string slotUIDLookupFullPath = Path.Combine(Application.streamingAssetsPath, "", slotUIDLookupRelativePath);

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

            string fileName = saveCategory.Equals("Player", StringComparison.OrdinalIgnoreCase) ? (string.IsNullOrWhiteSpace(saveName.text) || saveName.text.Equals("Aiden", StringComparison.OrdinalIgnoreCase) ? "player_tpp_skeleton" : saveName.text) : saveName.text;

            // Ensure fileName is valid
            if (string.IsNullOrWhiteSpace(fileName))
            {
                Debug.LogError("Save name is empty. Cannot write configuration.");
                return;
            }

            string output_path = ConfigManager.LoadSetting("SavePath", "Output_Path");
            string jsonBaseOutputPath = !string.IsNullOrEmpty(output_path) ? output_path : Path.Combine(Application.dataPath, "StreamingAssets/Jsons/Custom");
            string jsonOutputPath = Path.Combine(jsonBaseOutputPath, saveCategory, DateTime.Now.ToString("yyyy_MM_dd"), saveName.text + ".json");
            string screenshotPath = Path.ChangeExtension(jsonOutputPath, ".png");
            string modelOutputPath = Path.Combine(targetPath, fileName + ".model");
            //Debug.Log($"jsonOutputDirectory {jsonOutputDirectory}, screenshotPath {screenshotPath}");

            var sliderValues = interfaceManager.GetSliderValues();
            var currentlyLoadedModels = characterBuilder.GetCurrentlyLoadedModels();

            var slotPairs = new List<ModelData.SlotDataPair>();
            var usedSlots = new HashSet<string>();
            HashSet<int> usedSlotUids = new HashSet<int>(); // Moved outside the loop

            foreach (var slider in sliderValues)
            {
                //Debug.Log($"Processing slider: {slider.Key} with value: {slider.Value}");

                if (slider.Value > 0)
                {
                    if (currentlyLoadedModels.TryGetValue(slider.Key, out GameObject model))
                    {
                        //Debug.Log($"Processing model for slider key: {slider.Key}");
                        if (sliderToSlotMapping.TryGetValue(slider.Key, out string slotKey))
                        {
                            //Debug.Log($"Mapping found: {slider.Key} maps to {slotKey}");
                            string modelName = model.name.Replace("(Clone)", ".msh").ToLower();
                            if (saveCategory != "Player")
                            {
                                string potentialSkeletonName = skeletonLookup.FindMatchingSkeleton(modelName);
                                //Debug.Log($"{potentialSkeletonName} Skeleton updated to {skeletonName} based on loaded models.");
                                if (!string.IsNullOrEmpty(potentialSkeletonName) && potentialSkeletonName != "player_skeleton.msh" && potentialSkeletonName != skeletonName)
                                {
                                    skeletonName = potentialSkeletonName;
                                    skeletonUpdated = true;
                                    //Debug.Log($"{potentialSkeletonName} Skeleton updated to {skeletonName} based on loaded models.");
                                }
                            }

                            if (slotUIDLookup.ModelSlots.TryGetValue(modelName, out List<ModelData.SlotInfo> possibleSlots))
                            {
                                //Debug.Log($"Found possible slots for model {modelName}: {possibleSlots.Count}");
                                ModelData.SlotDataPair slotPair = null; // Declaration moved outside of the loop

                                foreach (var possibleSlot in possibleSlots)
                                {
                                    //Debug.Log($"Checking possible slot: {possibleSlot.name} with Slot UID: {possibleSlot.slotUid} against used slots and UIDs.");
                                    if (!usedSlotUids.Contains(possibleSlot.slotUid) && !usedSlots.Contains(possibleSlot.name))
                                    {
                                        //Debug.Log($"Creating SlotDataPair for model {modelName} with intended slot {slotKey} and assigning to actual slot {possibleSlot.name}.");
                                        slotPair = CreateSlotDataPair(model, possibleSlot.name, possibleSlot.slotUid);
                                        slotPairs.Add(slotPair);
                                        usedSlotUids.Add(possibleSlot.slotUid);
                                        usedSlots.Add(possibleSlot.name); // Also mark the slot name as used
                                        //Debug.Log($"Assigned {possibleSlot.name} slot for {slider.Key} with Slot UID: {possibleSlot.slotUid}");
                                        break; // Break since a slot has been successfully assigned
                                    }
                                }

                                if (slotPair == null) // No slot was assigned in the initial attempt
                                {
                                    string initialFallbackSlotName = sliderToSlotMapping[slider.Key];
                                    List<string> fallbackOptions = fallbackSlots.TryGetValue(initialFallbackSlotName, out var initialFallbacks) ? initialFallbacks : new List<string>();

                                    //Debug.Log($"Fallback options for {initialFallbackSlotName}: {string.Join(", ", fallbackOptions)}");

                                    foreach (var fallbackOption in fallbackOptions)
                                    {
                                        //Debug.Log($"Considering fallback option: {fallbackOption}");
                                        if (!usedSlots.Contains(fallbackOption))
                                        {
                                            var nextAvailableSlotUid = DetermineNextAvailableSlotUid(fallbackOption, usedSlotUids);
                                            if (nextAvailableSlotUid != -1) // Assuming -1 indicates failure to find an available UID
                                            {
                                                //Debug.Log($"Using next available Slot UID '{nextAvailableSlotUid}' for fallback slot '{fallbackOption}'.");
                                                slotPair = CreateSlotDataPair(model, fallbackOption, nextAvailableSlotUid);
                                                slotPairs.Add(slotPair);
                                                usedSlotUids.Add(nextAvailableSlotUid);
                                                usedSlots.Add(fallbackOption);
                                                //Debug.Log($"Assigned fallback {fallbackOption} slot for {slider.Key} with Slot UID: {nextAvailableSlotUid}");
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (slotPair == null) // Fallback logic exhausted and still no slot assigned
                                {
                                    Debug.LogWarning($"No available slots or names found for model {modelName}. Consider handling this scenario.");
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"No mapping found for {slider.Key}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Model not found in currentlyLoadedModels for slider {slider.Key}");
                    }
                }
            }

            void FinalizeAndWrite(ModelData modelData, string jsonPath, string modelPath, string skeletonOverride = null)
            {
                if (!string.IsNullOrWhiteSpace(skeletonOverride))
                {
                    modelData.skeletonName = skeletonOverride;
                }

                Debug.Log($"outputData {modelData.skeletonName}");
                WriteConfigurationOutput(modelData, jsonPath, modelPath);
            }

            slotPairs = slotPairs.OrderBy(pair => pair.slotData.slotUid).ToList();
            interfaceManager.currentPresetPath = jsonOutputPath;
            interfaceManager.currentPresetLabel.text = Path.GetFileNameWithoutExtension(jsonOutputPath);
            screenshotManager.TakeScreenshot();

            var outputData = new ModelData
            {
                skeletonName = skeletonName,
                slotPairs = slotPairs,
                modelProperties = new ModelData.ModelProperties
                {
                    @class = saveClass.Equals("ALL", StringComparison.OrdinalIgnoreCase) ? "zombie" : saveClass,
                    race = "caucasian",
                    sex = DetermineCharacterSex(slotPairs)
                }
            };

            // Write the first configuration to JSON
            FinalizeAndWrite(outputData, jsonOutputPath, modelOutputPath);

            if (fppToggleSwitchManager.isOn)
            {
                string tppFileName = Path.GetFileNameWithoutExtension(jsonOutputPath);
                bool shouldCreateDualOutputs = saveCategory.Equals("Player", StringComparison.OrdinalIgnoreCase) || saveNameText.Equals("Aiden", StringComparison.OrdinalIgnoreCase);

                if (shouldCreateDualOutputs)
                {
                    var excludedSlots = new HashSet<string> {
        "HEAD", "HEADCOVER", "HEADCOVER_PART_1", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6"
    };

                    var filteredSlotPairs = slotPairs.Where(pair => !excludedSlots.Contains(pair.key)).ToList();

                    //FinalizeAndWrite(outputData, jsonOutputPath, Path.Combine(customBasePath, "ph/source", "player_tpp_skeleton.model"), skeletonName);

                    var outputDataSecond = new ModelData
                    {
                        skeletonName = "player_fpp_skeleton.msh",
                        slotPairs = filteredSlotPairs,
                        modelProperties = outputData.modelProperties
                    };

                    foreach (var slotPair in outputDataSecond.slotPairs)
                    {
                        foreach (var modelInfo in slotPair.slotData.models)
                        {
                            modelInfo.name = UpdateModelAndMaterialNames(modelInfo.name, isMaterial: false);

                            //foreach (var materialData in modelInfo.materialsData)
                            //{
                            //    materialData.name = UpdateModelAndMaterialNames(materialData.name, isMaterial: true);
                            //}

                            foreach (var materialResource in modelInfo.materialsResources)
                            {
                                foreach (var resource in materialResource.resources)
                                {
                                    resource.name = UpdateModelAndMaterialNames(resource.name, isMaterial: true);
                                }
                            }
                        }
                    }

                    // Define the second JSON and model output paths
                    string fppFileName = tppFileName.EndsWith("_tpp") ? tppFileName.Replace("_tpp", "_fpp") : tppFileName + "_fpp";
                    string jsonOutputPathSecond = Path.Combine(jsonBaseOutputPath, saveCategory, DateTime.Now.ToString("yyyy_MM_dd"), fppFileName + ".json");
                    string modelFileNameSecond = saveNameText.Equals("Aiden", StringComparison.OrdinalIgnoreCase) ? "player_fpp_skeleton.model" : fppFileName + ".model";
                    string modelOutputPathSecond = Path.Combine(targetPath, modelFileNameSecond);

                    // Write the second configuration to model
                    FinalizeAndWrite(outputDataSecond, jsonOutputPathSecond, modelOutputPathSecond, "player_fpp_skeleton.msh");
                }
            }
            
        }

        private string UpdateModelAndMaterialNames(string originalName, bool isMaterial = false, string suffix = "_fpp")
        {
            string basePath = isMaterial ? "Assets/Resources/Materials/" : "Assets/Resources/Prefabs/";
            string originalExtension = isMaterial ? ".mat" : ".msh";
            string newExtension = isMaterial ? "" : ".prefab";
            string originalNameWithoutExt = originalName.EndsWith(".msh") ? originalName.Substring(0, originalName.Length - 4) : originalName;
            string newName = originalNameWithoutExt.Contains("_tpp") ? originalNameWithoutExt.Replace("_tpp", suffix) : originalNameWithoutExt;
            string fullPath = basePath + newName + newExtension;

            //Debug.Log($"[UpdateModelAndMaterialNames] Checking for: {fullPath}");

            // Check if the FPP version exists
            if (File.Exists(fullPath))
            {
                //Debug.Log($"[UpdateModelAndMaterialNames] Found FPP version for {originalName}: {newName}");
                return newName.Replace(".mat","") + originalExtension;
            }
            else
            {
                //Debug.Log($"[UpdateModelAndMaterialNames] FPP version does not exist for {originalName}. Keeping original.");
                return originalName;
            }
        }

        private string ReplaceDateInName(string originalName)
        {
            // Define the date format in the file name
            string dateFormat = "yyyy_MM_dd_HH_mm_ss";
            // Create a regular expression to find the date format in the file name
            Regex dateRegex = new Regex(@"\d{4}_\d{2}_\d{2}_\d{2}_\d{2}_\d{2}");

            // Check if the original name contains a date
            Match match = dateRegex.Match(originalName);
            if (match.Success)
            {
                // If a date is found, replace it with the current date and time
                string currentDateString = DateTime.Now.ToString(dateFormat);
                return dateRegex.Replace(originalName, currentDateString);
            }
            else
            {
                // If no date is found, append the current date and time to the original name
                return $"{originalName}_{DateTime.Now.ToString(dateFormat)}";
            }
        }

        private void WriteConfigurationOutput(ModelData outputData, string jsonOutputPath, string modelOutputPath)
        {


            string customBasePath = ConfigManager.LoadSetting("SavePath", "Path");
            if (string.IsNullOrEmpty(customBasePath))
            {
                if (notificationManager != null)
                {
                    notificationManager.ShowWarning("You need to set your Dying Light 2 path in options!");
                }
                return;
            }
            string dataPath;
            try
            {
                dataPath = Path.Combine(customBasePath, "ph/source");
            }
            catch (ArgumentNullException)
            {
                if (notificationManager != null)
                {
                    notificationManager.ShowWarning("You need to set your Dying Light 2 path in options!");
                }
                return;
            }
            string json = JsonConvert.SerializeObject(outputData, Formatting.Indented);
            string outputDir = Path.GetDirectoryName(jsonOutputPath);
            Directory.CreateDirectory(outputDir);

            File.WriteAllText(jsonOutputPath, json);
            Debug.Log($"Configuration saved to {jsonOutputPath}");

            if (modelWriter != null)
            {
                string saveCategory = saveCategoryDropdown.options[saveCategoryDropdown.value].text;
                modelWriter.ConvertJsonToModelFormat(jsonOutputPath, modelOutputPath, saveCategory);
                Debug.Log($"Model file created at {modelOutputPath}");

                string outputPakPath = DeterminePakFilePath(dataPath);
                EnsurePlaceholderInPak(outputPakPath);
                string pakName = Path.GetFileName(outputPakPath);
                string modelFileNameWithinZip = Path.GetFileName(modelOutputPath);
                ZipUtility.AddOrUpdateFilesInZip(modelOutputPath, outputPakPath, modelFileNameWithinZip);
                Debug.Log($"{outputPakPath} updated with model data.");
                if (notificationManager != null)
                {
                    notificationManager.ShowNotification($"Models: {modelFileNameWithinZip} have been added to Dying Light 2 in {pakName}");
                }

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
            //Debug.Log($"Scanning directory for .pak files: {directoryPath}");

            var pakFiles = Directory.GetFiles(directoryPath, "data*.pak")
                            .Where(file => !Path.GetFileNameWithoutExtension(file).StartsWith("data_devtools"))
                            .Select(file => new
                            {
                                FullPath = file,
                                FileName = Path.GetFileNameWithoutExtension(file),
                                Number = int.TryParse(Path.GetFileNameWithoutExtension(file).Replace("data", ""), out int number) ? number : (int?)null
                            })
                            .Where(file => file.Number.HasValue)
                            .OrderBy(file => file.Number.Value)
                            .ToList();

            //Debug.Log($"Filtered and processed .pak files: {string.Join(", ", pakFiles.Select(f => $"{f.FileName} => {f.Number}"))}");

            foreach (var pakFile in pakFiles)
            {
                if (File.Exists(pakFile.FullPath) && ZipUtility.ZipContainsFile(pakFile.FullPath, "PLACEHOLDER_InfinityDesigner.file"))
                {
                    Debug.Log($"Using existing .pak file with placeholder: {pakFile.FullPath}");
                    return pakFile.FullPath;
                }
            }
            int nextAvailableNumber = 1;
            for (int i = 0; i < pakFiles.Count; i++)
            {
                int currentNumber = pakFiles[i].Number.Value;
                bool isLastFile = i == pakFiles.Count - 1;
                bool hasNextFile = i + 1 < pakFiles.Count;
                bool hasGap = hasNextFile && pakFiles[i + 1].Number > currentNumber + 1;

                if (isLastFile || hasGap)
                {
                    nextAvailableNumber = currentNumber + 1;
                    break;
                }
            }

            string newPakPath = Path.Combine(directoryPath, $"data{nextAvailableNumber}.pak");
            //Debug.Log($"Preparing new .pak file: {newPakPath}. This considers both placeholders and gaps in numbering.");

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
                        return "male";
                    }
                    else if (femaleMeshes.Contains(modelName))
                    {
                        Debug.Log("Character Sex: Female");
                        return "female";
                    }
                    break;
                }
            }
            return "male";
        }

        private ModelData.SlotDataPair CreateSlotDataPair(GameObject model, string slotKey, int slotUid)
        {
            VariationBuilder variationBuilder = FindObjectOfType<VariationBuilder>();
            string originalSlotKey = slotKey;
            string lookupSlotKey = "ALL_" + slotKey.ToLower();
            //Debug.Log($"Creating SlotDataPair for {model.name} with original slot {originalSlotKey} and lookup slot {lookupSlotKey}");

            string formattedModelName = FormatModelName(model.name);
            //Debug.Log($"Formatted model name: {formattedModelName}");

            var slotData = new ModelData.SlotData
            {
                name = slotKey,
                slotUid = slotUid,
                models = new List<ModelData.ModelInfo>()
            };

            string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", $"{formattedModelName.Replace(".msh", "")}.json");
            //Debug.Log($"Looking for material JSON at: {materialJsonFilePath}");

            var modelInfo = new ModelData.ModelInfo
            {
                name = formattedModelName,
                materialsData = GetMaterialsDataFromStreamingAssets(formattedModelName.Replace(".msh", "")),
            };

            foreach (var kvp in variationBuilder.modelSpecificChanges)
            {
                string modelChangesJson = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented);
                //Debug.Log($"[CreateSlotDataPair] ModelSpecificChanges for Key: {kvp.Key}, Detailed Changes: \n{modelChangesJson}");
            }

            if (variationBuilder.modelSpecificChanges.TryGetValue(formattedModelName.Replace(".msh", ""), out ModelChange modelChanges))
            {
                modelInfo.materialsResources = GetMaterialsResourcesFromModelChanges(model, modelChanges);
                //Debug.Log($"Using specific materials data and resources for model {formattedModelName}.");
            }
            else
            {
                //Debug.Log($"No specific model changes found for {formattedModelName}. Copying materials data to materials resources.");
                modelInfo.materialsResources = modelInfo.materialsData.Select(md => new ModelData.MaterialResource
                {
                    number = md.number,
                    resources = new List<ModelData.Resource> { new ModelData.Resource { name = md.name.Replace("(Instance)", "").Replace(" ", ""), rttiValues = new List<RttiValue>() } } // Assuming no RTTI values for default materials
                }).ToList();
            }

            slotData.models.Add(modelInfo);

            return new ModelData.SlotDataPair { key = originalSlotKey, slotData = slotData };
        }

        private List<ModelData.MaterialResource> GetMaterialsResourcesFromModelChanges(GameObject model, ModelChange modelChanges)
        {
            //Debug.Log($"[GetMaterialsResourcesFromModelChanges] Starting to retrieve material resources from model changes for '{model.name}'.");

            List<ModelData.MaterialResource> materialsResources = new List<ModelData.MaterialResource>();
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();

            //Debug.Log($"[GetMaterialsResourcesFromModelChanges] Found {renderers.Length} SkinnedMeshRenderers in '{model.name}'.");

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];

                var resource = new ModelData.Resource
                {
                    name = renderer.sharedMaterial.name.Replace("(Instance)", "").Replace(" ", "") + (renderer.sharedMaterial.name.Replace("(Instance)", "").Replace(" ", "").EndsWith(".mat") ? "" : ".mat"),
                    rttiValues = new List<RttiValue>()
                };

                if (modelChanges.MaterialsByRenderer.TryGetValue(rendererIndex, out MaterialChange materialChange))
                {
                    resource.name = materialChange.NewName.Replace("(Instance)", "").EndsWith(".mat") ? materialChange.NewName.Replace("(Instance)", "").Replace(" ", "") : $"{materialChange.NewName.Replace("(Instance)", "").Replace(" ", "")}.mat";
                    resource.rttiValues = materialChange.TextureChanges;

                    //foreach (var texChange in materialChange.TextureChanges)
                    //{
                    //    Debug.Log($"[GetMaterialsResourcesFromModelChanges] Texture change: slot '{texChange.name}' now uses texture '{texChange.val_str}'.");
                    //}
                }
                else
                {
                    //Debug.Log($"[GetMaterialsResourcesFromModelChanges] No specific material change found for renderer {rendererIndex}. Using default material name: '{resource.name}'.");
                }

                materialsResources.Add(new ModelData.MaterialResource
                {
                    number = rendererIndex + 1,
                    resources = new List<ModelData.Resource> { resource }
                });
            }

            //Debug.Log($"[GetMaterialsResourcesFromModelChanges] Completed creating material resources for model '{model.name}'. Total resources created: {materialsResources.Count}.");
            return materialsResources;
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
                        string materialName = materialData.name.Replace("(Instance)", "").Replace(" ", "");
                        if (!materialName.EndsWith(".mat"))
                        {
                            materialName += ".mat";
                        }
                        //Debug.Log($"Material Name: {materialName}");
                        materialsData.Add(new ModelData.MaterialData
                        {
                            number = materialData.number, // Use the correct material number
                            name = materialName.Replace("(Instance)", "").Replace(" ", "")
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