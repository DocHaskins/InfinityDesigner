using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelScaler : MonoBehaviour
{
    [SerializeField] private GameObject subButtonsPanel;
    [SerializeField] private GameObject slidersPanel;
    [SerializeField] private int numberOfColumns = 3;

    private List<string> slots;
    public void ScalePanels()
    {
        if (slots == null || subButtonsPanel == null || slidersPanel == null)
            return;

        float buttonHeight = 100.0f;
        int numberOfRows = Mathf.CeilToInt((float)slots.Count / numberOfColumns);
        float newHeight = numberOfRows * buttonHeight;

        // Adjust the height of subButtonsPanel
        RectTransform subButtonsPanelRect = subButtonsPanel.GetComponent<RectTransform>();
        subButtonsPanelRect.sizeDelta = new Vector2(subButtonsPanelRect.sizeDelta.x, newHeight);

        // Adjust the position of slidersPanel
        RectTransform slidersPanelRect = slidersPanel.GetComponent<RectTransform>();
        slidersPanelRect.offsetMin = new Vector2(slidersPanelRect.offsetMin.x, newHeight);
    }

    // Example of setting slots and scaling panels
    public void SetSlotsAndScale(List<string> newSlots)
    {
        slots = newSlots;
        ScalePanels();
    }
}