using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PresetSummary_Scroller : MonoBehaviour
{
    private UnlimitedScrollUI.IUnlimitedScroller unlimitedScroller;

    public Transform contentPanel;
    public GameObject cellPrefab;
    public bool debug = false;

    private GameObject selectedCell;
    private List<GameObject> instantiatedCells = new List<GameObject>();

    private struct ButtonAction
    {
        public Action Action;
        public bool HasThumbnail;

        public ButtonAction(Action action, bool hasThumbnail)
        {
            Action = action;
            HasThumbnail = hasThumbnail;
        }
    }

    private List<ButtonAction> buttonPressActions = new List<ButtonAction>();

    private void Start()
    {
        unlimitedScroller = GetComponent<UnlimitedScrollUI.IUnlimitedScroller>();
        if (unlimitedScroller == null)
        {
            Debug.LogError("UnlimitedScroller component not found on the GameObject. Make sure it is attached.");
        }
    }

    public void LoadPresets(List<CharacterAppearance> appearances)
    {
        ClearExistingCells(); // Clear existing cells before loading new ones
        Debug.Log($"Generating cells for {appearances.Count} appearances.");
        instantiatedCells.Clear(); // Clear the list of instantiated cells

        unlimitedScroller.Generate(cellPrefab, appearances.Count, (index, iCell) =>
        {
            var cell = iCell as UnlimitedScrollUI.RegularCell;
            if (cell != null)
            {
                CharacterAppearance appearance = appearances[index];
                var displayComponent = cell.GetComponent<AppearanceDisplay>();
                if (displayComponent != null)
                {
                    displayComponent.Setup(appearance, this);
                    cell.GetComponent<Button>().onClick.AddListener(() => SelectCell(cell.gameObject)); // Attach selection logic to the button
                }
                else
                {
                    Debug.LogError("Cell prefab does not contain an AppearanceDisplay component.");
                }
                instantiatedCells.Add(cell.gameObject); // Add the instantiated cell to the list
            }
        });
    }

    public void SelectCell(GameObject selectedCell)
    {
        if (this.selectedCell == selectedCell)
        {
            return; // Clicked the same cell, no action needed
        }

        Debug.Log($"Selecting cell: {selectedCell.name}");

        if (this.selectedCell != null)
        {
            // Deselect previous cell and detach listeners
            var previousDisplayComponent = this.selectedCell.GetComponent<AppearanceDisplay>();
            previousDisplayComponent?.DetachListeners();
        }

        // Select new cell and attach listeners
        var newDisplayComponent = selectedCell.GetComponent<AppearanceDisplay>();
        newDisplayComponent?.OnSelected();

        this.selectedCell = selectedCell; // Update the reference to the currently selected cell
    }

    private void ClearExistingCells()
    {
        if (unlimitedScroller != null)
        {
            unlimitedScroller.Clear();
        }
        else
        {
            Debug.LogError("unlimitedScroller is null.");
        }
        instantiatedCells.Clear(); // Clear the list since we are removing all cells
        selectedCell = null; // Clear the reference to the selected cell
    }
}
