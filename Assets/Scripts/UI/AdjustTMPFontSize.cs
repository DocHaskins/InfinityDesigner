using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Access to TextMeshPro components
using UnityEngine.EventSystems; // Required for event handling

public class AdjustTMPFontSize : MonoBehaviour
{
    public Slider fontSizeSlider; // Assign this in the inspector
    private List<TMP_Text> allTexts = new List<TMP_Text>();
    private float lastSliderValue = 1f; // Store the last slider value

    void Start()
    {
        // Load slider value from config
        LoadSliderValueFromConfig();

        TMP_Text[] texts = FindObjectsOfType<TMP_Text>();
        foreach (var text in texts)
        {
            if (!IsPartOfSlider(text.gameObject))
            {
                allTexts.Add(text);
            }
        }

        EventTrigger trigger = fontSizeSlider.gameObject.GetComponent<EventTrigger>() ?? fontSizeSlider.gameObject.AddComponent<EventTrigger>();
        var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener((data) => { OnSliderRelease((PointerEventData)data); });
        trigger.triggers.Add(pointerUp);
    }

    bool IsPartOfSlider(GameObject textGameObject)
    {
        return textGameObject.transform.IsChildOf(fontSizeSlider.transform);
    }

    public void OnSliderRelease(PointerEventData data)
    {
        AdjustFontSize(fontSizeSlider.value);
        // Save the slider value when it's released
        SaveSliderValueToConfig(fontSizeSlider.value);
    }

    void AdjustFontSize(float sliderValue)
    {
        foreach (var text in allTexts)
        {
            text.fontSize *= sliderValue / lastSliderValue;
        }
        lastSliderValue = sliderValue;
    }

    private void SaveSliderValueToConfig(float sliderValue)
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "config.ini");
        //SaveSettingToConfig(configPath, "[Settings]", "FontSizeMultiplier", sliderValue.ToString());
    }

    private void LoadSliderValueFromConfig()
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "config.ini");
        //string value = LoadSettingFromConfig(configPath, "[Settings]", "FontSizeMultiplier");
        //if (float.TryParse(value, out float sliderValue))
        //{
        //    fontSizeSlider.value = sliderValue;
        //    lastSliderValue = sliderValue; // Ensure lastSliderValue is initialized correctly
        //}
    }
}