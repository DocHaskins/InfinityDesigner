using doppelganger;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderKeyboardControl : MonoBehaviour
{
    public CharacterBuilder_InterfaceManager characterBuilder;
    
    public bool DebugMode = false;
    
    private int currentSliderIndex = 0;
    public ScrollRect scrollArea;

    private List<Slider> sliders = new List<Slider>();
    private List<TMP_InputField> inputFields = new List<TMP_InputField>();

    void Start()
    {
        RefreshSliders();
    }

    void Update()
    {
        if (IsAnyInputFieldFocused())
        {
            return;
        }

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
            AdjustCurrentSlider(-1.0f);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            AdjustCurrentSlider(1.0f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ResetCurrentSlider();
        }
    }

    private bool IsAnyInputFieldFocused()
    {
        foreach (var inputField in inputFields)
        {
            if (inputField.isFocused)
            {
                return true;
            }
        }
        return false;
    }

    private void ResetCurrentSlider()
    {
        if (sliders.Count > 0 && currentSliderIndex >= 0 && currentSliderIndex < sliders.Count)
        {
            sliders[currentSliderIndex].value = 0;
            Debug.Log($"Slider '{sliders[currentSliderIndex].name}' reset to 0.");
        }
    }

    private bool isMovingSlider = false;

    private IEnumerator ContinuousSliderMovement(KeyCode keyCode)
    {
        isMovingSlider = true;
        yield return new WaitForSeconds(0.1f);

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

            yield return new WaitForSeconds(0.1f);
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
        yield return new WaitForSeconds(2.0f);
        sliders.Clear();
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("PrimarySlider");
        foreach (GameObject obj in taggedObjects)
        {
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
                Slider sliderFallback = obj.GetComponent<Slider>();
                if (sliderFallback != null)
                {
                    sliders.Add(sliderFallback);
                }
            }
        }

        inputFields.Clear();
        TMP_InputField[] allInputFields = FindObjectsOfType<TMP_InputField>();
        foreach (TMP_InputField inputField in allInputFields)
        {
            inputFields.Add(inputField);
        }
        HighlightCurrentSlider();
    }

    private void MoveToNextSlider()
    {
        if (sliders.Count == 0) return;

        currentSliderIndex = (currentSliderIndex + 1) % sliders.Count;
        HighlightCurrentSlider();
    }

    private void MoveToPreviousSlider()
    {
        if (sliders.Count == 0) return;

        currentSliderIndex--;
        if (currentSliderIndex < 0) currentSliderIndex = sliders.Count - 1;
        HighlightCurrentSlider();
    }

    private void AdjustCurrentSlider(float direction)
    {
        if (sliders.Count == 0 || currentSliderIndex < 0 || currentSliderIndex >= sliders.Count) return;

        Slider currentSlider = sliders[currentSliderIndex];
        currentSlider.value += direction;
    }

    private void HighlightCurrentSlider()
    {
        bool DebugMode = false;

        Color defaultColor = new Color(0f, 0f, 0f, 0.8f);
        Color selectedBackgroundColor = new Color(0.2803922f, 0.2803922f, 0.2803922f, 1f);

        GameObject[] primarySliders = GameObject.FindGameObjectsWithTag("PrimarySlider");
        for (int i = 0; i < primarySliders.Length; i++)
        {
            GameObject sliderParent = primarySliders[i];
            Image bckgrdImage = sliderParent.transform.Find("bckgrd")?.GetComponent<Image>();
            Text labelText = sliderParent.transform.Find("Button_currentSlider/labelText")?.GetComponent<Text>();

            if (bckgrdImage != null && labelText != null)
            {
                if (i == currentSliderIndex)
                {
                    labelText.fontStyle = FontStyle.Bold;
                    labelText.fontSize += 2;

                    if (!DebugMode)
                    {
                        bckgrdImage.color = selectedBackgroundColor;
                    }

                    // Adjust scroll area to ensure current slider is visible
                    RectTransform sliderRectTransform = sliderParent.GetComponent<RectTransform>();
                    RectTransform viewportRect = scrollArea.viewport;

                    // Calculate the relative position of the slider within the viewport
                    Vector3[] sliderCorners = new Vector3[4];
                    sliderRectTransform.GetWorldCorners(sliderCorners);

                    Vector3[] viewportCorners = new Vector3[4];
                    viewportRect.GetWorldCorners(viewportCorners);

                    float sliderTopY = sliderCorners[1].y; // Top of the slider
                    float sliderBottomY = sliderCorners[0].y; // Bottom of the slider
                    float viewportTopY = viewportCorners[1].y; // Top of the viewport
                    float viewportBottomY = viewportCorners[0].y; // Bottom of the viewport

                    // Check if the slider is out of bounds (above or below the visible viewport area)
                    if (sliderTopY > viewportTopY)
                    {
                        // Slider is above the visible area, move the scroll rect down
                        scrollArea.verticalNormalizedPosition += 0.1f; // Adjust this value as needed
                    }
                    else if (sliderBottomY < viewportBottomY)
                    {
                        // Slider is below the visible area, move the scroll rect up
                        scrollArea.verticalNormalizedPosition -= 0.1f; // Adjust this value as needed
                    }
                }
                else
                {
                    labelText.fontStyle = FontStyle.Normal;
                    labelText.fontSize = Mathf.Max(labelText.fontSize - 2, 24);

                    if (bckgrdImage != null)
                    {
                        bckgrdImage.color = defaultColor;
                    }
                    bckgrdImage.color = DebugMode ? defaultColor : new Color(0f, 0f, 0f, 0.8f);
                }
            }
            else
            {
                if (bckgrdImage == null) Debug.LogError($"'bckgrd' Image component not found on {sliderParent.name}");
                if (labelText == null) Debug.LogError($"'labelText' Text component not found on {sliderParent.name}");
            }
        }
    }
}