using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

/// <summary>
/// SkeletonLookup is a Unity script that maps character categories and classes to specific skeleton models using dropdown selections. 
/// It holds a comprehensive dictionary mapping each category and class to a skeleton name, supporting a wide range of characters from players to various infected types. 
/// The script provides functionality to retrieve the correct skeleton model based on the current selections in the category and class dropdowns, ensuring the appropriate skeleton is used for character customization and instantiation.
/// </summary>

namespace doppelganger
{
    public class SkeletonLookup : MonoBehaviour
    {
        [Header("Interface")]
        public TMP_Dropdown categoryDropdown;
        public TMP_Dropdown classDropdown;

        public Dictionary<string, Dictionary<string, string>> skeletonMapping = new Dictionary<string, Dictionary<string, string>>
{
    {
        "ALL", new Dictionary<string, string>
        {
            {"ALL", "man_basic_skeleton"},
            {"Biter", "man_zmb_skeleton"},
            {"Special Infected", "player_skeleton"},
            {"Viral", "viral_skeleton"}
        }
    },
    {
        "Player", new Dictionary<string, string>
        {
            {"ALL", "player_skeleton"}
        }
    },
    {
        "Man", new Dictionary<string, string>
        {
            {"ALL", "man_basic_skeleton"},
            {"bandit", "man_bdt_medium_skeleton"},
            {"peacekeeper", "man_pk_medium_skeleton"},
            {"renegade", "man_bdt_medium_skeleton"},
            {"scavenger", "man_sc_medium_skeleton"},
            {"survivor", "man_srv_medium_skeleton"}
        }
    },
    {
        "Wmn", new Dictionary<string, string>
        {
            {"ALL", "woman_basic_skeleton"},
            {"bandit", "woman_basic_skeleton"},
            {"peacekeeper", "woman_basic_skeleton"},
            {"renegade", "woman_basic_skeleton"},
            {"scavenger", "woman_sc_skeleton"},
            {"survivor", "woman_srv_skeleton"}
        }
    },
    {
        "Child", new Dictionary<string, string>
        {
            {"ALL", "child_skeleton"}
        }
    },
    {
        "Biter", new Dictionary<string, string>
        {
            {"ALL", "man_zmb_skeleton"},
            {"bandit", "man_zmb_skeleton"},
            {"peacekeeper", "man_zmb_skeleton"},
            {"renegade", "man_zmb_skeleton"},
            {"scavenger", "man_zmb_skeleton"},
            {"survivor", "man_zmb_skeleton"}
        }
    },
    {
        "Special Infected", new Dictionary<string, string>
        {
            {"banshee", "zmb_banshee_skeleton"},
            {"bolter", "zmb_bolter_skeleton"},
            {"charger", "zmb_charger_skeleton"},
            {"corruptor", "zmb_corruptor_skeleton"},
            {"demolisher", "zmb_demolisher_skeleton"},
            {"goon", "zmb_goon_skeleton"},
            {"screamer", "zmb_screamer_skeleton"},
            {"spitter", "zmb_spitter_skeleton"},
            {"suicider", "zmb_suicider_skeleton"},
            {"volatile", "zmb_volataile_skeleton"}
        }
    },
    {
        "Viral", new Dictionary<string, string>
        {
            {"scavenger", "viral_skeleton"},
            {"survivor", "viral_skeleton"},
            {"ALL", "viral_skeleton"}
        }
    },
};

        public string GetSelectedSkeleton()
        {
            string selectedCategory = categoryDropdown.options[categoryDropdown.value].text;
            string selectedClass = classDropdown.options[classDropdown.value].text;

            if (skeletonMapping.ContainsKey(selectedCategory) && skeletonMapping[selectedCategory].ContainsKey(selectedClass))
            {
                //Debug.Log("GetSelectedSkeleton: Selected Category: " + selectedCategory + ", Selected Class: " + selectedClass);
                string selectedSkeleton = skeletonMapping[selectedCategory][selectedClass];
                //Debug.Log("GetSelectedSkeleton: Selected Skeleton: " + selectedSkeleton);
                return selectedSkeleton + ".msh";
            }
            else
            {
                Debug.LogError("Skeleton mapping not found for Category: " + selectedCategory + ", Class: " + selectedClass);
                return "default_skeleton";
            }
        }

        public string LookupSkeleton(string selectedCategory, string selectedClass)
        {
            // Directly use the provided selectedCategory and selectedClass parameters
            if (skeletonMapping.ContainsKey(selectedCategory) && skeletonMapping[selectedCategory].ContainsKey(selectedClass))
            {
                //Debug.Log("Selected Category: " + selectedCategory + ", Selected Class: " + selectedClass);
                string selectedSkeleton = skeletonMapping[selectedCategory][selectedClass];
                //Debug.Log("Selected Skeleton: " + selectedSkeleton);
                return selectedSkeleton + ".msh";
            }
            else
            {
                Debug.LogError("Skeleton mapping not found for Category: " + selectedCategory + ", Class: " + selectedClass);
                return "default_skeleton";
            }
        }

        public string FindMatchingSkeleton(string modelName)
        {
            string directoryPath = Path.Combine(Application.dataPath, "StreamingAssets/Skeleton Data");

            // Check if directory exists
            if (!Directory.Exists(directoryPath))
            {
                Debug.LogError("Directory does not exist: " + directoryPath);
                return null;
            }

            // Get all .json files in the directory
            string[] files = Directory.GetFiles(directoryPath, "*.json");

            foreach (string file in files)
            {
                string jsonContent = File.ReadAllText(file);
                // Assuming the json structure is a Dictionary or similar; adjust according to your actual json structure
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

                if (data != null && data.ContainsKey("mesh"))
                {
                    List<string> meshNames = JsonConvert.DeserializeObject<List<string>>(data["mesh"].ToString());
                    if (meshNames.Contains(modelName))
                    {
                        // Found the matching skeleton, return its name without the extension
                        return Path.GetFileNameWithoutExtension(file) + ".msh";
                    }
                }
            }

            // If no match was found
            return "player_skeleton.msh";
        }
    }
}