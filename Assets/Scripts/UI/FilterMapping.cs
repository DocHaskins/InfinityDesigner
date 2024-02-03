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
        public CharacterBuilder characterBuilder;
        public CinemachineCameraZoomTool cameraZoomTool;

        [Header("Interface")]
        public GameObject filtersPanel;

        public static Dictionary<string, string> buttonToCameraTargetMapping = new Dictionary<string, string>()
{
    { "Button_All", "pelvis" },
    { "Button_Face", "neck" },
    { "Button_Face_access", "neck" },
    { "Button_UpperBody", "spine1" },
    { "Button_UpperBody_armor", "spine1" },
    { "Button_UpperBody_access", "spine1" },
    { "Button_Hands", "pelvis" },
    { "Button_Legs", "legs" },
    { "Button_Legs_armor", "legs" }
};

        public Dictionary<string, List<string>> buttonMappings = new Dictionary<string, List<string>>()
    {
        { "Button_All", new List<string>
            {
                "ALL_head",
                "ALL_hat",
                "ALL_hat_access",
                "ALL_mask",
                "ALL_mask_access",
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
                "ALL_torso",
                "ALL_torso_2",
                "ALL_torso_extra",
                "ALL_torso_access",
                "ALL_tattoo",
                "ALL_hands",
                "ALL_lhand",
                "ALL_rhand",
                "ALL_gloves",
                "ALL_arm_access",
                "ALL_sleeve",
                "ALL_backpack",
                "ALL_decals",
                "ALL_decals_extra",
                "ALL_decals_logo",
                "ALL_legs",
                "ALL_legs_extra",
                "ALL_legs_access",
                "ALL_shoes",
                "ALL_armor_helmet",
                "ALL_armor_helmet_access",
                "ALL_armor_torso",
                "ALL_armor_torso_access",
                "ALL_armor_torso_upperright",
                "ALL_armor_torso_upperleft",
                "ALL_armor_torso_lowerright",
                "ALL_armor_torso_lowerleft",
                "ALL_armor_legs",
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
                "ALL_facial_hair"
            }
        },
        { "Button_Face_access", new List<string>
            {
                "ALL_hat",
                "ALL_hat_access",
                "ALL_mask_access",
                "ALL_glasses",
                "ALL_mask",
                "ALL_cape",
                "ALL_necklace",
                "ALL_earrings"
            }
        },
        { "Button_UpperBody", new List<string>
            {
                "ALL_torso",
                "ALL_torso_2",
                "ALL_torso_access",
                "ALL_torso_extra",
                "ALL_sleeve",
                "ALL_arm_access",
                "ALL_tattoo"
            }
        },
        { "Button_UpperBody_armor", new List<string>
            {
                "ALL_armor_helmet",
                "ALL_armor_helmet_access",
                "ALL_armor_torso",
                "ALL_armor_torso_access",
                "ALL_armor_torso_upperright",
                "ALL_armor_torso_upperleft",
                "ALL_armor_torso_lowerright",
                "ALL_armor_torso_lowerleft"
            }
        },
        { "Button_UpperBody_access", new List<string>
            {
                "ALL_backpack",
                "ALL_cape",
                "ALL_decals",
                "ALL_decals_extra",
                "ALL_decals_logo",
            }
        },
        { "Button_Hands", new List<string>
            {
                "ALL_hands",
                "ALL_lhand",
                "ALL_rhand",
                "ALL_gloves",
                "ALL_rings"
            }
        },
        { "Button_Legs", new List<string>
            {
                "ALL_legs",
                "ALL_legs_extra",
                "ALL_legs_access",
                "ALL_shoes"
            }
        },
        { "Button_Legs_armor", new List<string>
            {
                "ALL_armor_legs",
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

        private Transform FindTransformRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                Debug.Log($"Found target transform '{name}'");
                return parent;
            }

            foreach (Transform child in parent)
            {
                Transform result = FindTransformRecursive(child, name);
                if (result != null) return result;
            }

            return null; // Not found
        }

        private FocusPoint GetFocusPointForButton(string buttonName)
        {
            GameObject loadedSkeleton = GameObject.FindGameObjectWithTag("Skeleton");
            if (loadedSkeleton == null)
            {
                Debug.LogError("No GameObject with the tag 'Skeleton' found in the scene.");
                return null;
            }

            Transform FindTransformByName(string name)
            {
                Debug.Log($"Searching for '{name}' in the loaded skeleton.");
                return FindTransformRecursive(loadedSkeleton.transform, name);
            }

            Transform targetTransform = null; // Initialize to null
            float radiusAdjustment = 0;
            float heightAdjustment = 0;

            // Example, adjust based on actual implementation
            switch (buttonName)
            {
                case "Button_Face":
                case "Button_Face_access":
                    targetTransform = FindTransformByName("neck1");
                    radiusAdjustment = 0.0f;
                    break;
                case "Button_UpperBody":
                case "Button_UpperBody_armor":
                case "Button_UpperBody_access":
                    targetTransform = FindTransformByName("spine3");
                    radiusAdjustment = 0.0f;
                    break;
                case "Button_Hands":
                    targetTransform = FindTransformByName("pelvis");
                    radiusAdjustment = 0.0f;
                    break;
                case "Button_Legs":
                case "Button_Legs_armor":
                    targetTransform = FindTransformByName("legs");
                    radiusAdjustment = 0.0f;
                    break;
                case "Button_All":
                    targetTransform = FindTransformByName("pelvis");
                    radiusAdjustment = 0.0f;
                    break;
            }

            if (targetTransform == null)
            {
                Debug.LogError($"Target transform for button {buttonName} not found.");
                return null;
            }
            else
            {
                Debug.Log($"Creating FocusPoint for '{buttonName}' with adjustments: radius {radiusAdjustment}, height {heightAdjustment}.");
            }

            return new FocusPoint(targetTransform, radiusAdjustment, heightAdjustment);
        }

        void GetFilterButtons()
        {
            foreach (var mapping in buttonMappings)
            {
                var buttonName = mapping.Key;
                var button = filtersPanel.transform.Find(buttonName)?.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        Debug.Log($"Button '{buttonName}' pressed. Attempting to focus.");
                        characterBuilder.FilterCategory(mapping.Key);
                        FocusPoint focusPoint = GetFocusPointForButton(buttonName);
                        if (cameraZoomTool != null && focusPoint != null)
                        {
                            Debug.Log($"Focusing on '{focusPoint.targetTransform.name}' with duration 1.0f.");
                            cameraZoomTool.FocusOn(focusPoint);
                        }
                        else
                        {
                            Debug.LogError("FocusPoint is null or CameraZoomTool is not assigned.");
                        }
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