using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SkeletonLookup : MonoBehaviour
{
    public TMP_Dropdown categoryDropdown;
    public TMP_Dropdown classDropdown;

    private Dictionary<string, Dictionary<string, string>> skeletonMapping = new Dictionary<string, Dictionary<string, string>>
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
            {"volatile", "zmb_volataile_skeleton"},
            {"ALL", "player_skeleton"}
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
            Debug.Log("Selected Category: " + selectedCategory + ", Selected Class: " + selectedClass);
            string selectedSkeleton = skeletonMapping[selectedCategory][selectedClass];
            Debug.Log("Selected Skeleton: " + selectedSkeleton);
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
            Debug.Log("Selected Category: " + selectedCategory + ", Selected Class: " + selectedClass);
            string selectedSkeleton = skeletonMapping[selectedCategory][selectedClass];
            Debug.Log("Selected Skeleton: " + selectedSkeleton);
            return selectedSkeleton + ".msh";
        }
        else
        {
            Debug.LogError("Skeleton mapping not found for Category: " + selectedCategory + ", Class: " + selectedClass);
            return "default_skeleton";
        }
    }
}
