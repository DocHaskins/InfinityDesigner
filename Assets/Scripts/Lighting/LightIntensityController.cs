using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class SpotlightIntensityController : MonoBehaviour
{
    public HDAdditionalLightData spotlight; // Assign this in the inspector
    public float intensityIncrement = 1.0f; // The step by which the intensity will be increased or decreased

    void Update()
    {
        if (spotlight == null) return;

        // Increase intensity with the '+' key
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            spotlight.intensity += intensityIncrement;
        }
        // Decrease intensity with the '-' key
        else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            spotlight.intensity -= intensityIncrement;
            spotlight.intensity = Mathf.Max(0, spotlight.intensity); // Ensure the intensity does not go below 0
        }
    }
}