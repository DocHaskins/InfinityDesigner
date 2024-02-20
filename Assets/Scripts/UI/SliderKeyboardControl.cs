using doppelganger;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderKeyboardControl : MonoBehaviour
{
    public CharacterBuilder_InterfaceManager characterBuilder; // Assuming this has a reference to your UI elements
    
    public bool DebugMode = false;
    private List<Slider> sliders = new List<Slider>();
    private int currentSliderIndex = 0;

    void Start()
    {
        RefreshSliders();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            if (!isMovingSlider)
            {
                MoveToPreviousSlider();
                StartCoroutine(ContinuousSliderMovement(KeyCode.UpArrow));
            }
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            if (!isMovingSlider)
            {
                MoveToNextSlider();
                StartCoroutine(ContinuousSliderMovement(KeyCode.DownArrow));
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            AdjustCurrentSlider(-1.0f); // Adjust the value by a small increment to the left
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            AdjustCurrentSlider(1.0f); // Adjust the value by a small increment to the right
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ResetCurrentSlider();
        }
    }

    private void ResetCurrentSlider()
    {
        if (sliders.Count > 0 && currentSliderIndex >= 0 && currentSliderIndex < sliders.Count)
        {
            // Set the current slider's value to 0
            sliders[currentSliderIndex].value = 0;
            Debug.Log($"Slider '{sliders[currentSliderIndex].name}' reset to 0.");
        }
    }

    private bool isMovingSlider = false; // To prevent multiple coroutine instances

    private IEnumerator ContinuousSliderMovement(KeyCode keyCode)
    {
        isMovingSlider = true;
        yield return new WaitForSeconds(0.1f); // Initial delay before continuous movement starts

        while (Input.GetKey(keyCode))
        {
            if (keyCode == KeyCode.UpArrow)
            {
                MoveToPreviousSlider();
            }
            else if (keyCode == KeyCode.DownArrow)
            {
                MoveToNextSlider();
            }
            else if (keyCode == KeyCode.LeftArrow)
            {
                AdjustCurrentSlider(-1.0f);
            }
            else if (keyCode == KeyCode.RightArrow)
            {
                AdjustCurrentSlider(1.0f);
            }

            yield return new WaitForSeconds(0.1f); // Adjust this value as needed for faster or slower movement
        }

        isMovingSlider = false;
    }

    public void RefreshSliders()
    {
        StopAllCoroutines();
        StartCoroutine(RefreshSlidersCoroutine());
    }

    private IEnumerator RefreshSlidersCoroutine()
    {
        yield return new WaitForSeconds(2.0f); // Wait for potential UI updates
        sliders.Clear();
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("PrimarySlider");
        foreach (GameObject obj in taggedObjects)
        {
            // Attempt to find a child named "PrimarySlider" which should contain the actual Slider component
            Transform primarySliderChild = obj.transform.Find("primarySlider");
            if (primarySliderChild != null)
            {
                Slider slider = primarySliderChild.GetComponent<Slider>();
                if (slider != null)
                {
                    sliders.Add(slider);
                }
            }
            else
            {
                // Fallback to checking the parent object if no such child is found
                Slider sliderFallback = obj.GetComponent<Slider>();
                if (sliderFallback != null)
                {
                    sliders.Add(sliderFallback);
                }
            }
        }
        HighlightCurrentSlider();
    }

    private void MoveToNextSlider()
    {
        if (sliders.Count == 0) return;

        currentSliderIndex = (currentSliderIndex + 1) % sliders.Count; // Loop through sliders
        HighlightCurrentSlider();
    }

    private void MoveToPreviousSlider()
    {
        if (sliders.Count == 0) return;

        currentSliderIndex--;
        if (currentSliderIndex < 0) currentSliderIndex = sliders.Count - 1; // Loop to the last slider
        HighlightCurrentSlider();
    }

    private void AdjustCurrentSlider(float direction)
    {
        if (sliders.Count == 0 || currentSliderIndex < 0 || currentSliderIndex >= sliders.Count) return;

        Slider currentSlider = sliders[currentSliderIndex];
        currentSlider.value += direction; // Adjust the slider value by the direction
    }

    private void HighlightCurrentSlider()
    {
        bool DebugMode = false; // Assuming DebugMode is defined elsewhere, set it accordingly

        // Default color for deselected sliders
        Color defaultColor = new Color(0f, 0f, 0f, 0.8f); // White color, adjust if necessary

        // Color to use for the selected slider's background when not in DebugMode
        Color selectedBackgroundColor = new Color(0.2803922f, 0.2803922f, 0.2803922f, 1f); // Equivalent to #616161

        GameObject[] primarySliders = GameObject.FindGameObjectsWithTag("PrimarySlider");
        for (int i = 0; i < primarySliders.Length; i++)
        {
            GameObject sliderParent = primarySliders[i];

            // Find the "bckgrd" and "labelText" child GameObjects within each primary slider
            Image bckgrdImage = sliderParent.transform.Find("bckgrd")?.GetComponent<Image>();
            TMP_Text labelText = sliderParent.transform.Find("Button_currentSlider/labelText")?.GetComponent<TMP_Text>();

            if (bckgrdImage != null && labelText != null)
            {
                if (i == currentSliderIndex)
                {
                    labelText.fontStyle = FontStyles.Bold;
                    labelText.fontSize += 2; // Increase font size for the selected slider

                    if (!DebugMode)
                    {
                        bckgrdImage.color = selectedBackgroundColor;
                    }
                }
                else
                {
                    labelText.fontStyle = FontStyles.Normal;
                    labelText.fontSize = Mathf.Max(labelText.fontSize - 2, 24); // Ensure font size does not go below a minimum value

                    // Revert the background color for non-selected sliders
                    if (bckgrdImage != null)
                    {
                        bckgrdImage.color = defaultColor;
                    }
                    bckgrdImage.color = DebugMode ? defaultColor : new Color(0f, 0f, 0f, 0.8f); // Make transparent if not in DebugMode
                }
            }
            else
            {
                if (bckgrdImage == null) Debug.LogError($"'bckgrd' Image component not found on {sliderParent.name}");
                if (labelText == null) Debug.LogError($"'labelText' TextMeshPro component not found on {sliderParent.name}");
            }
        }
    }
}