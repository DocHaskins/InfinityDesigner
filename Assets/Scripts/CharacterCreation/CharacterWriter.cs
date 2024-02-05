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
        {"ALL_mask", "HEADCOVER"},
        {"ALL_armor_helmet", "HEADCOVER"},
        {"ALL_hands", "HANDS"},
        {"ALL_rhand", "HANDS_PART_1"},
        {"ALL_lhand", "HANDS_PART_1"},
        {"ALL_gloves", "HANDS_PART_1"},
        {"ALL_arm_access", "HANDS_PART_1"},
        {"ALL_rings", "HANDS_PART_1"},
        {"ALL_backpack", "TORSO_PART_1"},
        {"ALL_torso", "TORSO"},
        {"ALL_cape", "TORSO_PART_1"},
        {"ALL_necklace", "TORSO_PART_1"},
        {"ALL_torso_access", "TORSO_PART_1"},
        {"ALL_torso_extra", "TORSO_PART_1"},
        {"ALL_armor_torso", "TORSO_PART_1"},
        {"ALL_armor_torso_lowerleft", "ARMS_PART_1"},
        {"ALL_armor_torso_lowerright", "ARMS_PART_1"},
        {"ALL_armor_torso_upperleft", "ARMS_PART_1"},
        {"ALL_armor_torso_upperright", "ARMS_PART_1"},
        {"ALL_sleeve", "ARMS_PART_1"},
        {"ALL_legs", "LEGS"},
        {"ALL_leg_access", "LEGS_PART_1"},
        {"ALL_shoes", "LEGS_PART_1"},
        {"ALL_decals", "OTHER_PART_1"},
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

            string dateSubfolder = DateTime.Now.ToString("yyyy_MM_dd");
            string jsonOutputDirectory = Path.Combine(Application.streamingAssetsPath, "Output", dateSubfolder);
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
            string jsonOutputPath = Path.Combine(jsonOutputDirectory, $"{saveName.text}.json");
            string modelOutputPath = Path.Combine(targetPath, $"{fileName}.model");

            var sliderValues = characterBuilder.GetSliderValues();
            var currentlyLoadedModels = characterBuilder.GetCurrentlyLoadedModels();

            var slotPairs = new List<ModelData.SlotDataPair>();
            var usedSlots = new HashSet<string>();

            // Create a list to hold the ALL_head slot pair if found
            List<ModelData.SlotDataPair> allHeadSlotPairs = new List<ModelData.SlotDataPair>();

            int nextSlotUid = 101; // Initialize the next slot's uid to 101

            foreach (var slider in sliderValues)
            {
                //Debug.Log($"Processing slider: {slider.Key} with value: {slider.Value}");

                if (slider.Value > 0)
                {
                    if (currentlyLoadedModels.TryGetValue(slider.Key, out GameObject model))
                    {
                        if (sliderToSlotMapping.TryGetValue(slider.Key, out string slotKey))
                        {
                            ModelData.SlotDataPair slotPair;

                            if (slotKey == "HEAD")
                            {
                                slotPair = CreateSlotDataPair(model, slotKey);
                                slotPair.slotData.slotUid = 100;
                                slotPairs.Insert(0, slotPair);
                            }
                            else
                            {
                                string assignedSlot = GetAvailableSlot(slotKey, usedSlots);
                                if (assignedSlot == null)
                                {
                                    Debug.LogWarning($"No available slots for {slider.Key}");
                                    continue;
                                }

                                slotPair = CreateSlotDataPair(model, assignedSlot);
                                slotPair.slotData.slotUid = nextSlotUid++;
                                slotPairs.Add(slotPair);

                                //Debug.Log($"Assigned {assignedSlot} slot for {slider.Key}");
                            }

                            usedSlots.Add(slotKey);
                            string modelName = model.name;
                            string potentialSkeletonName = skeletonLookup.FindMatchingSkeleton(modelName);
                            if (!string.IsNullOrEmpty(potentialSkeletonName) && potentialSkeletonName != "default_skeleton.msh" && potentialSkeletonName != skeletonName)
                            {
                                skeletonName = potentialSkeletonName;
                                skeletonUpdated = true;
                                Debug.Log($"Skeleton updated to {skeletonName} based on loaded models.");
                                break; // If you prefer the first match; otherwise, remove this to check all models
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"No slot mapping found for slider {slider.Key}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Model not found in currentlyLoadedModels for slider {slider.Key}");
                    }
                }
            }

            // Insert the ALL_head slot pairs before other slot pairs
            slotPairs.InsertRange(1, allHeadSlotPairs);

            if (slotPairs.Count == 0)
            {
                Debug.LogWarning("No slot pairs were added. JSON will be empty.");
            }

            var outputData = new ModelData
            {
                skeletonName = skeletonName,
                slotPairs = slotPairs,
                modelProperties = new ModelData.ModelProperties
                {
                    // Use a ternary operator to conditionally set the class
                    @class = saveClass == "ALL" ? "NPC" : saveClass,
                    race = "Unknown",
                    sex = DetermineCharacterSex(slotPairs)
                }
            };

            string json = JsonConvert.SerializeObject(outputData, Formatting.Indented);

            // Creating directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(jsonOutputPath));
            File.WriteAllText(jsonOutputPath, json);
            Debug.Log($"Character configuration saved to {jsonOutputPath}");
            string outputPakPath = Path.Combine(targetPath, "Data4.pak");

            // After writing the JSON, call ModelWriter to convert it to .model format
            if (modelWriter != null)
            {
                modelWriter.ConvertJsonToModelFormat(jsonOutputPath, modelOutputPath);
                Debug.Log($"Model configuration saved to {modelOutputPath}");

                // Update the .pak file with the .model file
                ZipUtility.AddOrUpdateFilesInZip(modelOutputPath, outputPakPath);
                Debug.Log($"Data4.pak updated with model data at {outputPakPath}");

                // Cleanup: Delete the .model file after it has been added to the .pak
                try
                {
                    if (File.Exists(modelOutputPath))
                    {
                        File.Delete(modelOutputPath);
                        Debug.Log($"{modelOutputPath} was successfully deleted.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to delete {modelOutputPath}: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("ModelWriter not set in CharacterWriter.");
            }

            if (modelWriter != null)
            {
                modelWriter.ConvertJsonToModelFormat(jsonOutputPath, modelOutputPath);
                Debug.Log($"Model configuration saved to {modelOutputPath}");

                // Update the .pak file with the .model file
                ZipUtility.AddOrUpdateFilesInZip(modelOutputPath, outputPakPath);
                Debug.Log($"Data4.pak updated with model data at {outputPakPath}");

                // Conditionally create an additional model if the flag is true
                if (createAdditionalModel)
                {
                    // Define a modified list of slot pairs excluding specified sliders
                    var modifiedSlotPairs = slotPairs.Where(pair => !ExcludedSliders.Contains(pair.slotData.name)).ToList();
                    var modifiedOutputData = new ModelData
                    {
                        skeletonName = "player_fpp_skeleton.msh", // Use the specific skeleton
                        slotPairs = modifiedSlotPairs,
                        modelProperties = outputData.modelProperties // Copy other properties
                    };

                    string modifiedJson = JsonConvert.SerializeObject(modifiedOutputData, Formatting.Indented);
                    string fppModelPath = modelOutputPath.Replace(".model", "_fpp.model");

                    // Directly convert to .model format without saving the JSON
                    modelWriter.ConvertJsonToModelFormat(modifiedJson, fppModelPath);
                    Debug.Log($"Additional model configuration saved to {fppModelPath}");

                    // Add the additional .model file to the .pak
                    ZipUtility.AddOrUpdateFilesInZip(fppModelPath, outputPakPath);
                    Debug.Log($"Data4.pak updated with additional model data at {outputPakPath}");

                    // Cleanup: Delete the additional .model file
                    if (File.Exists(fppModelPath))
                    {
                        File.Delete(fppModelPath);
                        Debug.Log($"{fppModelPath} was successfully deleted.");
                    }
                }

                // Cleanup: Delete the original .model file
                if (File.Exists(modelOutputPath))
                {
                    File.Delete(modelOutputPath);
                    Debug.Log($"{modelOutputPath} was successfully deleted.");
                }
            }
            else
            {
                Debug.LogError("ModelWriter not set in CharacterWriter.");
            }
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

        private ModelData.SlotDataPair CreateSlotDataPair(GameObject model, string slotKey)
        {
            Debug.Log($"Creating SlotDataPair for {model.name} in slot {slotKey}");

            // Format the model name: remove "(Clone)" and add ".msh"
            string formattedModelName = FormatModelName(model.name);

            var slotData = new ModelData.SlotData
            {
                name = slotKey,
                models = new List<ModelData.ModelInfo>
        {
            new ModelData.ModelInfo
            {
                name = formattedModelName,
                materialsData = GetMaterialsDataFromStreamingAssets(formattedModelName), // Get materials from JSON in streaming assets
            }
        }
            };

            slotData.models[0].materialsResources = GetMaterialsResourcesFromModel(model);

            return new ModelData.SlotDataPair
            {
                key = slotKey,
                slotData = slotData
            };
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
                    Debug.Log("Original Materials from Streaming Assets:");

                    // Iterate through the materialsData in the JSON file
                    foreach (var materialData in modelInfo.materialsData)
                    {
                        // Ensure each material name ends with ".mat"
                        string materialName = materialData.name;
                        if (!materialName.EndsWith(".mat"))
                        {
                            materialName += ".mat";
                        }
                        Debug.Log($"Material Name: {materialName}");
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
                    // Ensure each material name ends with ".mat"
                    string materialName = material.name;
                    if (!materialName.EndsWith(".mat"))
                    {
                        materialName += ".mat";
                    }

                    materialsResources.Add(new ModelData.MaterialResource
                    {
                        number = materialNumber,
                        resources = new List<ModelData.Resource>
                {
                    new ModelData.Resource
                    {
                        name = materialName
                    }
                }
                    });

                    materialNumber++; // Increment the material number
                }
            }

            return materialsResources;
        }
    }
}