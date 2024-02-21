using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;

/// <summary>
/// The LightToggle script dims specified HDRP lights to a lower intensity and toggles other lights on or off when the space key is pressed. 
/// It preserves the original intensities of the excluded lights to restore them later. 
/// This functionality allows for dynamic lighting changes in the scene, enhancing visual effects or signaling events within a Unity game developed using HDRP.
/// </summary>

namespace doppelganger
{
    public class LightToggle : MonoBehaviour
    {
        [Tooltip("HDRP Lights in this list will have their intensity set to 0.25 lumens when space is pressed.")]
        public HDAdditionalLightData[] excludedLights; // Array to hold HDRP lights that should not be toggled

        private Dictionary<HDAdditionalLightData, float> originalIntensities = new Dictionary<HDAdditionalLightData, float>();
        private bool isDimmed = false;

        void Start()
        {
            // Store the original intensities of the excluded lights
            foreach (HDAdditionalLightData light in excludedLights)
            {
                if (light != null)
                {
                    originalIntensities[light] = light.intensity;
                }
            }
        }

        void Update()
        {
            // Check if the space bar was pressed down this frame
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Toggle the state between dimmed and original intensity
                isDimmed = !isDimmed;

                if (isDimmed)
                {
                    foreach (HDAdditionalLightData excludedLight in excludedLights)
                    {
                        if (excludedLight != null)
                        {
                            excludedLight.intensity = 14.0f;
                        }
                    }
                }
                else
                {
                    // Restore the original intensity of the excluded lights
                    foreach (HDAdditionalLightData excludedLight in excludedLights)
                    {
                        if (excludedLight != null && originalIntensities.ContainsKey(excludedLight))
                        {
                            excludedLight.intensity = originalIntensities[excludedLight];
                        }
                    }
                }

                // Toggle the enabled state for each light that is not in the exclusion list
                Light[] lights = FindObjectsOfType<Light>();
                foreach (Light light in lights)
                {
                    HDAdditionalLightData hdLight = light.GetComponent<HDAdditionalLightData>();
                    if (hdLight != null && System.Array.IndexOf(excludedLights, hdLight) < 0) // If not in the excluded list
                    {
                        light.enabled = !light.enabled; // Toggle light
                    }
                }
            }
        }
    }
}