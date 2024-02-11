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
        public ModelWriter modelWriter;
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
        public string skeletonJsonPath = "Assets/StreamingAssets/Jsons/Human/Player/player_tpp_skeleton.json";
        SlotUIDLookup slotUIDLookup = SlotUIDLookup.LoadFromJson("Assets/StreamingAssets/SlotData/SlotUIDLookup.json");
        private Dictionary<string, string> sliderToSlotMapping = new Dictionary<string, string>()
    {
        {"ALL_head", "HEAD"},
        {"ALL_hair", "HEAD_PART_1"},
        {"ALL_hair_base", "HEAD_PART_1"},
        {"ALL_hair_1", "HEAD_PART_1"},
        {"ALL_hair_2", "HEAD_PART_1"},
        {"ALL_facial_hair", "HEAD_PART_1"},
        {"ALL_earrings", "HEAD_PART_1"},
        {"ALL_glasses", "HEAD_PART_1"},
        {"ALL_hat", "HAT"},
        {"ALL_hat_access", "HAT"},
        {"ALL_mask", "HEADCOVER"},
        {"ALL_mask_access", "HEADCOVER"},
        {"ALL_armor_helmet", "HEADCOVER"},
        {"ALL_armor_helmet_access", "HEADCOVER"},
        {"ALL_hands", "HANDS"},
        {"ALL_rhand", "HANDS_PART_1"},
        {"ALL_lhand", "HANDS_PART_1"},
        {"ALL_gloves", "HANDS_PART_1"},
        {"ALL_arm_access", "HANDS_PART_1"},
        {"ALL_rings", "HANDS_PART_1"},
        {"ALL_backpack", "TORSO_PART_1"},
        {"ALL_torso", "TORSO"},
        {"ALL_torso_2", "TORSO_PART_1"},
        {"ALL_cape", "TORSO_PART_1"},
        {"ALL_necklace", "TORSO_PART_1"},
        {"ALL_torso_access", "TORSO_PART_1"},
        {"ALL_torso_extra", "TORSO_PART_1"},
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
    {"HAT", new List<string> { "HEADCOVER_PART_1", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"HEADCOVER_PART_1", new List<string> { "HEADCOVER", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"HEAD_PART_1", new List<string> { "HEADCOVER", "HEAD_PART_1", "HEAD_PART_2", "HEAD_PART_3", "HEAD_PART_4", "HEAD_PART_5", "HEAD_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"TORSO_PART_1", new List<string> { "TORSO_PART_1", "TORSO_PART_2", "TORSO_PART_3", "TORSO_PART_4", "TORSO_PART_5", "TORSO_PART_6", "TORSO_PART_7", "TORSO_PART_8", "TORSO_PART_9", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"ARMS_PART_1", new List<string> { "ARMS_PART_2", "ARMS_PART_3", "ARMS_PART_4", "ARMS_PART_5", "ARMS_PART_6", "OTHER", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"OTHER_PART_1", new List<string> { "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
    {"LEGS_PART_1", new List<string> { "PANTS", "PANTS_PART_1", "LEGS_PART_2", "LEGS_PART_3", "LEGS_PART_4", "LEGS_PART_5", "OTHER_PART_1", "OTHER_PART_2", "OTHER_PART_3", "OTHER_PART_4", "OTHER_PART_5" }},
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

        void Start()
        {
            string savedPath = LoadPathFromConfig();
            if (!string.IsNullOrEmpty(savedPath))
            {
                pathInputField.text = savedPath;
            }

            if (characterBuilder == null)
            {
                characterBuilder = FindObjectOfType<CharacterBuilder>();
                Debug.Log("CharacterBuilder found: " + (characterBuilder != null));
            }
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
            string configPath = Path.Combine(Application.streamingAssetsPath, "config.ini");
            List<string> lines = File.Exists(configPath) ? new List<string>(File.ReadAllLines(configPath)) : new List<string>();
            bool sectionFound = false;
            bool pathUpdated = false;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Trim().Equals("[SavePath]", StringComparison.InvariantCultureIgnoreCase))
                {
                    sectionFound = true;
                    if (i + 1 < lines.Count && !lines[i + 1].StartsWith("["))
                    {
                        lines[i + 1] = newPath;
                        pathUpdated = true;
                    }
                    else
                    {
                        lines.Insert(i + 1, newPath);
                        pathUpdated = true;
                    }
                    break;
                }
            }

            if (!sectionFound)
            {
                lines.Add("[SavePath]");
                lines.Add(newPath);
                pathUpdated = true;
            }

            if (pathUpdated)
            {
                try
                {
                    File.WriteAllLines(configPath, lines);
                    Debug.Log($"Path saved to config: {newPath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to save path to config: {e.Message}");
                }
            }
        }

        private string LoadPathFromConfig()
        {
            string configPath = Path.Combine(Application.streamingAssetsPath, "config.ini");
            bool savePathSectionFound = false;

            if (File.Exists(configPath))
            {
                foreach (var line in File.ReadLines(configPath))
                {
                    if (savePathSectionFound)
                    {
                        if (!line.StartsWith("["))
                        {
                            return line;
                        }
                        break;
                    }

                    if (line.Trim().Equals("[SavePath]", StringComparison.InvariantCultureIgnoreCase))
                    {
                        savePathSectionFound = true;
                    }
                }
            }

            Debug.Log("No saved path in config.");
            return null; // Return null if no path found
        }

        private void UpdatePlayerConfiguration(Dictionary<string, GameObject> currentlyLoadedModels, string jsonOutputPath, string modelOutputPath)
        {
            string skeletonJson = File.ReadAllText(skeletonJsonPath);
            ModelData skeletonData = JsonConvert.DeserializeObject<ModelData>(skeletonJson);

            // Ensure the directories exist
            Directory.CreateDirectory(Path.GetDirectoryName(jsonOutputPath));
            Directory.CreateDirectory(Path.GetDirectoryName(modelOutputPath));

            // Update slot data based on sliders
            foreach (var slider in interfaceManager.GetSliderValues())
            {
                if (slider.Value > 0 && currentlyLoadedModels.TryGetValue(slider.Key, out GameObject model))
                {
                    UpdateSlotDataBasedOnSlider(skeletonData, model, slider.Key);
                }
            }

            // Write the updated configuration
            WriteConfigurationOutput(skeletonData, jsonOutputPath, modelOutputPath);
        }

        private void UpdateSlotDataBasedOnSlider(ModelData skeletonData, GameObject model, string sliderKey)
        {
            string modelName = model.name.Replace("(Clone)", "").ToLower() + ".msh";

            // Attempt to assign model to an available slot
            if (!AssignModelToAvailableSlot(skeletonData, model, modelName, sliderKey))
            {
                Debug.LogWarning($"No available slots found for '{modelName}'. Unable to assign.");
            }
        }

        private bool AssignModelToAvailableSlot(ModelData skeletonData, GameObject model, string modelName, string sliderKey)
        {
            // First, attempt direct assignment using slotUIDLookup
            if (AttemptDirectAssignment(skeletonData, model, modelName, sliderKey))
            {
                return true;
            }

            // If direct assignment fails, try using the generic slot and fallbacks
            return AttemptAssignmentWithFallback(skeletonData, model, modelName, sliderKey);
        }

        private bool AttemptDirectAssignment(ModelData skeletonData, GameObject model, string modelName, string sliderKey)
        {
            if (slotUIDLookup.ModelSlots.TryGetValue(modelName, out List<ModelData.SlotInfo> possibleSlots))
            {
                foreach (var possibleSlot in possibleSlots)
                {
                    if (!IsSlotOccupied(skeletonData, possibleSlot.name))
                    {
                        // Call AddModelToSlot with correct arguments
                        AddModelToSlot(skeletonData, possibleSlot.name, model);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool AttemptAssignmentWithFallback(ModelData skeletonData, GameObject model, string modelName, string sliderKey)
        {
            string mappedSlotKey = sliderToSlotMapping.ContainsKey(sliderKey) ? sliderToSlotMapping[sliderKey] : null;
            if (mappedSlotKey == null || !fallbackSlots.ContainsKey(mappedSlotKey))
            {
                Debug.LogWarning($"No mapped slot or fallback slots found for '{sliderKey}'.");
                return false;
            }

            foreach (var fallbackSlotKey in fallbackSlots[mappedSlotKey])
            {
                var slotUid = GetSlotUid(skeletonData, fallbackSlotKey);
                if (slotUid != -1 && !IsSlotOccupied(skeletonData, fallbackSlotKey))
                {
                    // A suitable fallback slot is found and not occupied, assign the model to this slot
                    AddModelToSlot(skeletonData, fallbackSlotKey, model);
                    return true;
                }
            }

            // If we've gone through all fallback slots and none are available, indicate failure
            Debug.LogWarning($"No available slots found for model '{modelName}' with slider key '{sliderKey}'.");
            return false;
        }

        private bool IsSlotOccupied(ModelData skeletonData, string slotKey)
        {
            return skeletonData.slotPairs.Any(sp => sp.key.Equals(slotKey, StringComparison.OrdinalIgnoreCase) && sp.slotData.models.Any());
        }

        private void AddModelToSlot(ModelData skeletonData, string slotKey, GameObject model)
        {
            // Ensure the model name is formatted correctly (remove "(Clone)" and ensure ".msh" extension)
            string formattedModelName = FormatModelName(model.name);

            // Check if the slot exists in the skeleton data
            var slotPair = skeletonData.slotPairs.FirstOrDefault(sp => sp.key == slotKey);
            if (slotPair != null)
            {
                // Use CreateSlotDataPair to prepare the slot data pair with the formatted model name
                ModelData.SlotDataPair newSlotPair = CreateSlotDataPair(model, slotKey);
                if (newSlotPair != null)
                {
                    // If newSlotPair contains valid data, update the existing slot's model list
                    slotPair.slotData.models.AddRange(newSlotPair.slotData.models);
                    Debug.Log($"Model '{formattedModelName}' successfully added to slot '{slotKey}'.");
                }
                else
                {
                    Debug.LogError($"Failed to create slot data pair for model '{formattedModelName}'.");
                }
            }
            else
            {
                Debug.LogError($"Slot '{slotKey}' not found for model '{formattedModelName}'. No assignment made.");
            }
        }


        private int GetSlotUid(ModelData skeletonData, string slotKey)
        {
            // Attempt to find an existing slot with the given key
            var slotPair = skeletonData.slotPairs.FirstOrDefault(sp => sp.key.Equals(slotKey, StringComparison.OrdinalIgnoreCase));
            if (slotPair != null)
            {
                // If found, return the existing slot UID
                return slotPair.slotData.slotUid;
            }
            else
            {
                // If not found, log an error indicating no available slots and return an invalid UID
                Debug.Log($"No available slot found for key '{slotKey}'. Unable to assign.");
                return -1; // Indicate an invalid UID since we cannot generate new ones
            }
        }

        public void WriteCurrentConfigurationToJson()
        {
            if (characterBuilder == null || skeletonLookup == null)
            {
                Debug.LogError("Dependencies are null. Cannot write configuration.");
                return;
            }
            
            string customBasePath = LoadPathFromConfig();
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

            
            
            Directory.CreateDirectory(jsonOutputDirectory);

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

            bool skeletonUpdated = false;

            // Overwrite fileName if saveCategory is "Player"
            string fileName = saveCategory.Equals("Player", StringComparison.OrdinalIgnoreCase) ? (string.IsNullOrWhiteSpace(saveName.text) || saveName.text.Equals("Aiden", StringComparison.OrdinalIgnoreCase) ? "player_tpp_skeleton" : saveName.text) : saveName.text;

            // Ensure fileName is valid
            if (string.IsNullOrWhiteSpace(fileName))
            {
                Debug.LogError("Save name is empty. Cannot write configuration.");
                return;
            }

            // Constructing the JSON and .model file paths dynamically based on the fileName
            string jsonOutputPath = Path.Combine(Application.streamingAssetsPath, "Output", DateTime.Now.ToString("yyyy_MM_dd"), saveName.text + ".json");
            string modelOutputPath = Path.Combine(customBasePath, "ph/source", fileName + ".model");

            var sliderValues = interfaceManager.GetSliderValues();
            var currentlyLoadedModels = characterBuilder.GetCurrentlyLoadedModels();

            var slotPairs = new List<ModelData.SlotDataPair>();
            var usedSlots = new HashSet<string>();

            if (saveCategory.Equals("Player", StringComparison.OrdinalIgnoreCase))
            {
                
                if (File.Exists(skeletonJsonPath))
                {
                    // Call the method to update player configuration with the loaded skeleton data and the paths
                    UpdatePlayerConfiguration(currentlyLoadedModels, jsonOutputPath, modelOutputPath);
                    return;
                }
                else
                {
                    Debug.LogError($"Skeleton JSON file not found: {skeletonJsonPath}");
                }
            }

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
                            ModelData.SlotDataPair slotPair = null;

                            if (slotKey == "HEAD")
                            {
                                slotPair = CreateSlotDataPair(model, slotKey);
                                slotPair.slotData.slotUid = 100;
                                slotPairs.Add(slotPair);
                                slotAssigned = true;
                            }
                            else
                            {
                                string modelName = model.name.Replace("(Clone)", "").ToLower() + ".msh";
                                string potentialSkeletonName = skeletonLookup.FindMatchingSkeleton(modelName);
                                if (!string.IsNullOrEmpty(potentialSkeletonName) && potentialSkeletonName != "default_skeleton.msh" && potentialSkeletonName != skeletonName)
                                {
                                    skeletonName = potentialSkeletonName;
                                    skeletonUpdated = true;
                                    Debug.Log($"Skeleton updated to {skeletonName} based on loaded models.");
                                }
                                if (slotUIDLookup.ModelSlots.TryGetValue(modelName, out List<ModelData.SlotInfo> possibleSlots))
                                {
                                    Debug.Log($"Found possible slots for model {modelName}: {possibleSlots.Count}");

                                    foreach (var possibleSlot in possibleSlots)
                                    {
                                        if (!usedSlots.Contains(possibleSlot.name))
                                        {
                                            slotPair = CreateSlotDataPair(model, possibleSlot.name);
                                            slotPair.slotData.slotUid = possibleSlot.slotUid;
                                            slotPairs.Add(slotPair);
                                            usedSlots.Add(possibleSlot.name);
                                            Debug.Log($"Assigned {possibleSlot.name} slot for {slider.Key} with Slot UID: {possibleSlot.slotUid}");
                                            slotAssigned = true;
                                            break; // Exit the loop once a slot is assigned
                                        }
                                    }
                                }
                            }

                            if (slotPair != null && slotAssigned)
                            {
                                Debug.Log($"Assigned {slotPair.slotData.name} slot for {slider.Key} with Slot UID: {slotPair.slotData.slotUid}");
                            }
                            else
                            {
                                Debug.LogWarning($"No available slots found for model {slotKey}");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Model not found in currentlyLoadedModels for slider {slider.Key}");
                    }
                }
            }

            //int maxSlotUid = 199; // Define the maximum slotUid for the "100s" range

            //// Find the highest used slotUid within the "100s" range or start from 99 if none are used
            //int highestUsedSlotUid = slotPairs
            //    .Where(pair => pair.slotData.slotUid >= 100 && pair.slotData.slotUid <= maxSlotUid)
            //    .Select(pair => pair.slotData.slotUid)
            //    .DefaultIfEmpty(99)
            //    .Max();
            //StringBuilder sb = new StringBuilder();
            //// Fill in the gaps
            //HashSet<int> usedSlotUids = new HashSet<int>(slotPairs.Select(pair => pair.slotData.slotUid));
            //for (int slotUid = 101; slotUid <= maxSlotUid; slotUid++)
            //{
            //    if (!usedSlotUids.Contains(slotUid))
            //    {
            //        // Assuming GetFilterText returns a string based on slotName
            //        string slotName = $"Empty_{slotUid}"; // Customize this name as needed
            //        bool isLastSlot = slotUid == maxSlotUid || !usedSlotUids.Any(uid => uid > slotUid && uid <= maxSlotUid);
            //        modelWriter.AppendEmptySlot(sb, slotUid, slotName, isLastSlot);
            //    }
            //}

            // Ensure slotPairs are sorted after appending empty slots
            slotPairs = slotPairs.OrderBy(pair => pair.slotData.slotUid).ToList();

            // Write the configuration to JSON
            var outputData = new ModelData
            {
                skeletonName = skeletonName, // Make sure skeletonName is updated from the loaded models
                slotPairs = slotPairs,
                modelProperties = new ModelData.ModelProperties
                {
                    @class = saveClass.Equals("ALL", StringComparison.OrdinalIgnoreCase) ? "NPC" : saveClass,
                    race = "Unknown", // Customize as needed
                    sex = DetermineCharacterSex(slotPairs) // Implement this method based on your logic
                }
            };
            WriteConfigurationOutput(outputData, jsonOutputPath, modelOutputPath);
        }

        private void WriteConfigurationOutput(ModelData outputData, string jsonOutputPath, string modelOutputPath)
        {
            // Serialize the outputData to JSON
            string json = JsonConvert.SerializeObject(outputData, Formatting.Indented);

            // Ensure the output directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(jsonOutputPath));

            // Write the JSON file
            File.WriteAllText(jsonOutputPath, json);
            Debug.Log($"Configuration saved to {jsonOutputPath}");

            // Convert JSON to model format and update the .pak file, if applicable
            if (modelWriter != null)
            {
                modelWriter.ConvertJsonToModelFormat(jsonOutputPath, modelOutputPath);
                Debug.Log($"Model file created at {modelOutputPath}");

                // Assuming you have a method to update .pak file, like ZipUtility.AddOrUpdateFilesInZip
                string outputPakPath = Path.Combine(Path.GetDirectoryName(modelOutputPath), "Data4.pak");
                ZipUtility.AddOrUpdateFilesInZip(modelOutputPath, outputPakPath);
                Debug.Log($"Data4.pak updated with model data at {outputPakPath}");

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
            }
            else
            {
                Debug.LogError("ModelWriter not set.");
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
                string jsonText = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<SlotUIDLookup>(jsonText);
            }
        }

        private string GetAvailableSlot(string initialSlot, HashSet<string> usedSlots)
        {
            if (!usedSlots.Contains(initialSlot))
            {
                return initialSlot;
            }

            if (fallbackSlots.TryGetValue(initialSlot, out List<string> fallbacks))
            {
                foreach (var slot in fallbacks)
                {
                    if (!usedSlots.Contains(slot))
                    {
                        return slot;
                    }
                }
            }
            return null; // Return null if no slots are available
        }

        private Dictionary<string, string> ReadSkeletonLookup()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Skeletons", "skeleton_lookup.json");
            if (!File.Exists(path))
            {
                Debug.LogError("Skeleton lookup file not found.");
                return new Dictionary<string, string>();
            }

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

        private ModelData.SlotDataPair CreateSlotDataPair(GameObject model, string slotKey)
        {
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
                name = originalSlotKey,
                models = new List<ModelData.ModelInfo>()
            };

            string materialJsonFilePath = Path.Combine(Application.streamingAssetsPath, "Mesh references", $"{formattedModelName.Replace(".msh", "")}.json");
            Debug.Log($"Looking for material JSON at: {materialJsonFilePath}");

            if (File.Exists(materialJsonFilePath))
            {
                Debug.Log($"Found material JSON for {formattedModelName}");
                string materialJsonData = File.ReadAllText(materialJsonFilePath);
                VariationOutput variationOutput = JsonUtility.FromJson<VariationOutput>(materialJsonData);

                Debug.Log($"Checking selectedVariationIndexes for key: {lookupSlotKey}");
                if (interfaceManager.selectedVariationIndexes.TryGetValue(lookupSlotKey, out int variationIndex))
                {
                    Debug.Log($"Found variation index: {variationIndex} for slot {lookupSlotKey}");
                    if (variationIndex > 0)
                    {
                        Variation selectedVariation = variationOutput.variations.FirstOrDefault(v => int.Parse(v.id) == variationIndex + 1);
                        if (selectedVariation != null)
                        {
                            Debug.Log($"Applying variation {variationIndex} to slot {lookupSlotKey}");
                            // Apply the selected variation's materials data and resources
                            slotData.models.Add(new ModelData.ModelInfo
                            {
                                name = formattedModelName,
                                materialsData = selectedVariation.materialsData,
                                materialsResources = selectedVariation.materialsResources
                            });
                        }
                        else
                        {
                            Debug.LogWarning($"No matching variation found for index {variationIndex} in slot {lookupSlotKey}.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Variation index for slot {lookupSlotKey} is not valid: {variationIndex}");
                    }
                }
                else
                {
                    Debug.Log($"No variation index found for slot {lookupSlotKey}. Using original materials data and resources.");
                    // Fallback to using original materials data and resources
                    slotData.models.Add(new ModelData.ModelInfo
                    {
                        name = formattedModelName,
                        materialsData = GetMaterialsDataFromStreamingAssets(formattedModelName.Replace(".msh", "")), // Get original materials data
                        materialsResources = GetMaterialsResourcesFromModel(model)
                    });
                }
            }
            else
            {
                Debug.LogError($"Material JSON file does not exist for model: {formattedModelName}. Cannot load variation or original materials.");
            }

            return new ModelData.SlotDataPair { key = originalSlotKey, slotData = slotData };
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
            string cleanName = modelName.Replace("(Clone)", "").Trim();

            // Add ".msh" extension
            return cleanName + ".msh";
        }


        private List<ModelData.MaterialResource> GetMaterialsResourcesFromModel(GameObject model)
        {
            List<ModelData.MaterialResource> materialsResources = new List<ModelData.MaterialResource>();
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();

            int materialNumber = 1; // Start numbering from 1

            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    string materialName = material.name + (material.name.EndsWith(".mat") ? "" : ".mat");

                    var resource = new ModelData.Resource
                    {
                        name = materialName,
                    };

                    materialsResources.Add(new ModelData.MaterialResource
                    {
                        number = materialNumber,
                        resources = new List<ModelData.Resource> { resource }
                    });

                    materialNumber++;
                }
            }

            return materialsResources;
        }
    }
}