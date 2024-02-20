using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnlimitedScrollUI.Example;

namespace doppelganger
{
    public class TextureScroller : MonoBehaviour
    {
        public Transform contentPanel;
        public GameObject cellPrefab;
        public bool autoGenerate;
        private List<Texture2D> allTextures;
        public string searchTerm = "_msk";
        public string additionalFilterTerm = "";
        public TMP_Dropdown imageTypeDropdown;
        public TMP_InputField filterInputField;
        private UnlimitedScrollUI.IUnlimitedScroller unlimitedScroller;
        public VariationTextureSlotsPanel currentSelectionPanel;
        private string currentSlotForSelection;
        public GameObject currentModel;
        private SkinnedMeshRenderer currentRenderer;
        private Material currentMaterialForSelection;
        public event Action<Texture2D> onTextureSelected;
        public event Action<Texture2D, SkinnedMeshRenderer, Material, string> TextureSelected;

        private void Start()
        {
            unlimitedScroller = GetComponent<UnlimitedScrollUI.IUnlimitedScroller>();
            LoadTextures(searchTerm + additionalFilterTerm); // Initialize with both terms combined
            if (autoGenerate)
            {
                StartCoroutine(DelayGenerate());
            }

            // Add listener to filter input field to refresh textures on change
            filterInputField.onEndEdit.AddListener(delegate { RefreshTexturesWithAdditionalFilter(); });

            imageTypeDropdown.onValueChanged.AddListener(delegate {
                DropdownIndexChanged(imageTypeDropdown);
            });
        }

        public void PrepareForSelection(VariationTextureSlotsPanel selectionPanel, string slotName)
        {
            currentSelectionPanel = selectionPanel;
            currentSlotForSelection = slotName;
            Debug.Log($"Preparing for texture selection for selectionPanel {selectionPanel} on slotName {slotName}");
            SetSearchTermFromOtherUI(slotName);
        }

        public void DropdownIndexChanged(TMP_Dropdown dropdown)
        {
            searchTerm = dropdown.options[dropdown.value].text.Trim().ToLower();
            additionalFilterTerm = filterInputField.text.Trim().ToLower();
            RefreshTextures(searchTerm + additionalFilterTerm);
        }

        public void LoadTextures(string filter)
        {
            // Use both searchTerm and additionalFilterTerm for filtering
            string combinedFilter = searchTerm + additionalFilterTerm; // This assumes your logic for combining terms is correct

            // Update filtering logic to use combinedFilter
            allTextures = Resources.LoadAll<Texture2D>("Textures").Where(t => t.name.Contains(searchTerm) && t.name.Contains(additionalFilterTerm)).ToList();
            Debug.Log($"Total Textures: {allTextures.Count}");
            GenerateTextures();
        }

        private IEnumerator DelayGenerate()
        {
            // Wait for the end of the frame to ensure all setups are done
            yield return new WaitForEndOfFrame();
            GenerateTextures();
        }

        public void UpdateDropdownSelection(string searchTerm)
        {
            for (int i = 0; i < imageTypeDropdown.options.Count; i++)
            {
                if (imageTypeDropdown.options[i].text.Trim().ToLower() == searchTerm)
                {
                    imageTypeDropdown.value = i;
                    break;
                }
            }
        }

        public void TriggerGenerateTextures()
        {
            if (!autoGenerate)
            {
                StartCoroutine(DelayGenerate());
            }
        }

        private void GenerateTextures()
        {
            unlimitedScroller.Generate(cellPrefab, allTextures.Count, (index, iCell) =>
            {
                var cell = iCell as UnlimitedScrollUI.RegularCell; // Assuming RegularCell is your cell class
                if (cell != null)
                {
                    // Generate and assign thumbnail to the cell
                    Texture2D texture = allTextures[index];
                    Texture2D thumbnail = GenerateThumbnail(texture);
                    cell.GetComponent<Image>().sprite = Sprite.Create(thumbnail, new Rect(0.0f, 0.0f, thumbnail.width, thumbnail.height), new Vector2(0.5f, 0.5f), 100.0f);
                    cell.GetComponent<Button>().onClick.AddListener(() => SelectTexture(texture));
                    cell.transform.localScale = Vector3.one;
                    cell.onGenerated?.Invoke(index);
                }
            });
        }

        private Texture2D GenerateThumbnail(Texture2D sourceTexture)
        {
            // Generate a thumbnail from the source texture
            RenderTexture rt = RenderTexture.GetTemporary(48, 48);
            RenderTexture.active = rt;
            Graphics.Blit(sourceTexture, rt);
            Texture2D thumbnail = new Texture2D(48, 48, TextureFormat.RGB24, false);
            thumbnail.ReadPixels(new Rect(0, 0, 48, 48), 0, 0);
            thumbnail.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return thumbnail;
        }

        private void SelectTexture(Texture2D texture)
        {
            if (currentSelectionPanel != null && !string.IsNullOrWhiteSpace(currentSlotForSelection))
            {
                currentSelectionPanel.GetTextureChange(texture, currentSlotForSelection);
                currentSelectionPanel.currentButtonClickedText.text = texture.name;
            }
            else
            {
                Debug.LogError("Select a material slot to change and try again");
            }
        }

        public void RefreshTexturesWithAdditionalFilter()
        {
            // Update the additionalFilterTerm from the input field
            additionalFilterTerm = filterInputField.text.Trim().ToLower();

            // Now clear existing cells and load textures again with the new filter
            ClearExistingCells();
            LoadTextures(searchTerm); // This method now inherently uses both searchTerm and additionalFilterTerm
        }

        private void ClearExistingCells()
        {
            allTextures.Clear();

            if (unlimitedScroller != null)
            {
                unlimitedScroller.Clear();
            }
            else
            {
                Debug.LogError("unlimitedScroller is null.");
            }
        }

        public void SetSearchTermFromOtherUI(string newSearchTerm)
        {
            searchTerm = newSearchTerm.Trim().ToLower();
            UpdateDropdownSelection(searchTerm);
            RefreshTextures(searchTerm);
        }

        public void RefreshTextures(string newSearchTerm)
        {
            // Update the searchTerm
            searchTerm = newSearchTerm.Trim().ToLower();

            // Clear existing textures and UI cells
            ClearExistingCells();

            // Load and display new textures using both the searchTerm and additionalFilterTerm
            LoadTextures(searchTerm);
        }
    }
}