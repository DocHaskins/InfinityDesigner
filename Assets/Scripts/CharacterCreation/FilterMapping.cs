using Cinemachine;
using doppelganger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace doppelganger
{
    public class FilterMapping : MonoBehaviour
    {
        [Header("Managers")]
        public CharacterBuilder_InterfaceManager characterBuilder;
        public AutoTargetCinemachineCamera autoTargetCinemachineCamera;

        [Header("Interface")]
        public GameObject filtersPanel;

        public static Dictionary<string, string> buttonToCameraTargetMapping = new Dictionary<string, string>()
{
    { "Button_All", "All" },
    { "Button_Face", "Face" },
    { "Button_Face_access", "Face" },
    { "Button_UpperBody", "UpperBody" },
    { "Button_UpperBody_armor", "UpperBody" },
    { "Button_UpperBody_access", "UpperBody" },
    { "Button_Decals", "All" },
    { "Button_Hands", "Hands" },
    { "Button_Legs", "LowerBody" },
    { "Button_Legs_armor", "Feet" }
};

        public Dictionary<string, List<string>> buttonMappings = new Dictionary<string, List<string>>()
    {
        { "Button_All", new List<string>
            {
                "ALL_head",
                "ALL_hat",
                "ALL_hat_access",
                "ALL_hood",
                "ALL_mask",
                "ALL_mask_access",
                "ALL_hazmat_head",
                "ALL_glasses",
                "ALL_necklace",
                "ALL_earrings",
                "ALL_rings",
                "ALL_hair",
                "ALL_hair_base",
                "ALL_hair_2",
                "ALL_hair_3",
                "ALL_facial_hair",
                "ALL_cape",
                "ALL_chest",
                "ALL_torso",
                "ALL_torso_2",
                "ALL_torso_extra",
                "ALL_torso_access",
                "ALL_shirt",
                "ALL_jacket",
                "ALL_flashlight",
                "ALL_waist",
                "ALL_weapons",
                "ALL_belts",
                "ALL_hands",
                "ALL_lhand",
                "ALL_rhand",
                "ALL_gloves",
                "ALL_gloves_2",
                "ALL_arm_access",
                "ALL_arm_access_2",
                "ALL_sleeve",
                "ALL_backpack",
                "ALL_bumbag",
                "ALL_gastank",
                "ALL_blisters_1",
                "ALL_blisters_2",
                "ALL_blisters_3",
                "ALL_blisters_4",
                "ALL_pockets",
                "ALL_decals",
                "ALL_decals_2",
                "ALL_decals_extra",
                "ALL_decals_logo",
                "ALL_tattoo",
                "ALL_tattoo_2",
                "ALL_legs",
                "ALL_legs_2",
                "ALL_legs_extra",
                "ALL_legs_access",
                "ALL_shoes",
                "ALL_armor_helmet",
                "ALL_armor_helmet_access",
                "ALL_armor_torso",
                "ALL_armor_torso_2",
                "ALL_armor_torso_access",
                "ALL_armor_torso_extra",
                "ALL_armor_torso_upperright",
                "ALL_armor_torso_upperleft",
                "ALL_armor_torso_lowerright",
                "ALL_armor_torso_lowerleft",
                "ALL_armor_legs",
                "ALL_armor_legs_access",
                "ALL_armor_legs_upperright",
                "ALL_armor_legs_upperleft",
                "ALL_armor_legs_lowerright",
                "ALL_armor_legs_lowerleft"
            }
        },
        { "Button_Face", new List<string>
            {
                "ALL_head",
                "ALL_hair",
                "ALL_hair_base",
                "ALL_hair_2",
                "ALL_hair_3",
                "ALL_facial_hair",
            }
        },
        { "Button_Face_access", new List<string>
            {
                "ALL_hat",
                "ALL_hat_access",
                "ALL_armor_helmet",
                "ALL_armor_helmet_access",
                "ALL_hood",
                "ALL_mask",
                "ALL_mask_access",
                "ALL_hazmat_head",
                "ALL_glasses",
                "ALL_necklace",
                "ALL_earrings",
                "ALL_blister_1",
                "ALL_blister_2",
                "ALL_blister_3",
                "ALL_blister_4"
            }
        },
        { "Button_UpperBody", new List<string>
            {
                "ALL_chest",
                "ALL_torso",
                "ALL_torso_2",
                "ALL_torso_access",
                "ALL_torso_extra",
                "ALL_cape",
                "ALL_shirt",
                "ALL_jacket",
                "ALL_waist",
                "ALL_belts",
                "ALL_sleeve"
            }
        },
        { "Button_UpperBody_armor", new List<string>
            {
                "ALL_armor_torso",
                "ALL_armor_torso_2",
                "ALL_armor_torso_access",
                "ALL_armor_torso_extra",
                "ALL_armor_torso_upperright",
                "ALL_armor_torso_upperleft",
                "ALL_armor_torso_lowerright",
                "ALL_armor_torso_lowerleft"
            }
        },
        { "Button_UpperBody_access", new List<string>
            {
                "ALL_cape",
                "ALL_backpack",
                "ALL_bag",
                "ALL_gastank",
                "ALL_flashlight",
                "ALL_weapons",
                "ALL_pockets",
                "ALL_belts"
            }
        },
        { "Button_Decals", new List<string>
            {
                "ALL_tattoo",
                "ALL_tattoo_2",
                "ALL_decals",
                "ALL_decals_2",
                "ALL_decals_extra",
                "ALL_decals_logo"
            }
        },
        { "Button_Hands", new List<string>
            {
                "ALL_sleeve",
                "ALL_hands",
                "ALL_lhand",
                "ALL_rhand",
                "ALL_gloves",
                "ALL_gloves_2",
                "ALL_arm_access",
                "ALL_arm_access_2",
                "ALL_rings"
            }
        },
        { "Button_Legs", new List<string>
            {
                "ALL_legs",
                "ALL_legs_2",
                "ALL_legs_extra",
                "ALL_legs_access",
                "ALL_pockets",
                "ALL_shoes"
            }
        },
        { "Button_Legs_armor", new List<string>
            {
                "ALL_armor_legs",
                "ALL_armor_legs_access",
                "ALL_armor_legs_upperright",
                "ALL_armor_legs_upperleft",
                "ALL_armor_legs_lowerright",
                "ALL_armor_legs_lowerleft"
            }
        }
    };

        void Start()
        {
            GetFilterButtons();
        }

        void GetFilterButtons()
        {
            foreach (var mapping in buttonToCameraTargetMapping)
            {
                var buttonName = mapping.Key;
                var targetArea = mapping.Value; // Area to focus based on button
                var button = filtersPanel.transform.Find(buttonName)?.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        //Debug.Log($"Button '{buttonName}' pressed. Attempting to focus on {targetArea}.");
                        characterBuilder.FilterCategory(buttonName);
                        autoTargetCinemachineCamera.FocusOnArea(targetArea);
                    });
                }
                else
                {
                    Debug.LogWarning($"Button '{buttonName}' not found in filters panel.");
                }
            }
        }
    }
}