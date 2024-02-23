using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnlimitedScrollUI.Example;
using UnlimitedScrollUI;

namespace doppelganger
{
    public class TextureScroller : MonoBehaviour
    {
        public GridUnlimitedScroller gridUnlimitedScroller;

        public Transform contentPanel;
        public GameObject cellPrefab;
        public GameObject materialCellPrefab;
        public bool autoGenerate;
        private List<Texture2D> allTextures;
        private List<Material> allMaterials;
        public bool isTextures = true;
        public bool isMaterials = false;
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
        public event MaterialSelectedHandler MaterialSelected;
        public event Action<Texture2D, SkinnedMeshRenderer, Material, string> TextureSelected;

        public delegate void MaterialSelectedHandler(Material material, SkinnedMeshRenderer renderer, string slotName);
        private void Start()
        {
            unlimitedScroller = GetComponent<UnlimitedScrollUI.IUnlimitedScroller>();
            if (autoGenerate)
            {
                StartCoroutine(DelayedResourceInitialization());
            }

            filterInputField.onEndEdit.AddListener(delegate { RefreshTexturesWithAdditionalFilter(); });
            imageTypeDropdown.onValueChanged.AddListener(delegate { DropdownIndexChanged(imageTypeDropdown); });

            UpdateDropdownForTextureOrMaterial();
        }

        private IEnumerator DelayedResourceInitialization()
        {
            // Wait for the end of the frame to ensure all setups are done
            yield return new WaitForEndOfFrame();
            if (isTextures)
            {
                ReloadTextures(); // This will now handle the initialization of textures
            }
            else if (isMaterials)
            {
                ReloadMaterials(); // This will handle the initialization of materials
            }
        }

        private void UpdateDropdownForTextureOrMaterial()
        {
            imageTypeDropdown.ClearOptions(); // Clear existing options
            if (isTextures)
            {
                List<string> options = new List<string> { "_msk", "_idx", "_gra", "_spc", "_clp", "_rgh", "_ocl", "_ems", "_dif", "_nrm" };
                imageTypeDropdown.AddOptions(options);
            }
            else if (isMaterials)
            {
                List<string> matoptions = new List<string> { "_msk", "_idx", "_gra", "_spc", "_clp", "_rgh", "_ocl", "_ems", "_dif", "_nrm" };
                imageTypeDropdown.AddOptions(matoptions);
            }
        }

        public void ReloadMaterials()
        {
            searchTerm = "";
            isMaterials = true;
            isTextures = false;
            UpdateDropdownForTextureOrMaterial();
            RefreshResources();
        }

        public void ReloadTextures()
        {
            isTextures = true;
            isMaterials = false;
            UpdateDropdownForTextureOrMaterial();
            RefreshResources();
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
            if (isMaterials)
            {
                RefreshMaterials();
            }
            else
            {
                RefreshTextures();
            }
        }

        private void RefreshResources()
        {
            ClearExistingCells(); // Clear all existing cells
            additionalFilterTerm = filterInputField.text.Trim().ToLower(); // Update the filter from UI

            if (isTextures)
            {
                LoadTextures(searchTerm + additionalFilterTerm);
            }
            else if (isMaterials)
            {
                LoadMaterials(searchTerm + additionalFilterTerm);
            }
        }

        private void GenerateResources()
        {
            if (isTextures)
            {
                allTextures = Resources.LoadAll<Texture2D>("Textures")
                .Where(t => t.name.Contains(searchTerm) && t.name.Contains(additionalFilterTerm))
                .ToList();
                Debug.Log($"Total Textures after filter: {allTextures.Count}");
                if (gridUnlimitedScroller != null)
                {
                    gridUnlimitedScroller.cellPerRow = 5;
                    gridUnlimitedScroller.cellX = 64;
                    gridUnlimitedScroller.cellY = 64;
                }

                unlimitedScroller.Generate(cellPrefab, allTextures.Count, (index, iCell) =>
                {
                    var cell = iCell as UnlimitedScrollUI.RegularCell;
                    if (cell != null)
                    {
                        Texture2D texture = allTextures[index];
                        Texture2D thumbnail = GenerateThumbnail(texture);
                        cell.GetComponent<Image>().sprite = Sprite.Create(thumbnail, new Rect(0.0f, 0.0f, thumbnail.width, thumbnail.height), new Vector2(0.5f, 0.5f), 100.0f);
                        cell.GetComponent<Button>().onClick.AddListener(() => SelectTexture(texture));
                        cell.transform.localScale = Vector3.one;
                        cell.onGenerated?.Invoke(index);
                    }
                });
            }
            else if (isMaterials)
            {
                allMaterials = allMaterials.Where(m =>
                {
                    string[] textureProperties = new string[] { "_msk", "_idx", "_gra", "_spc", "_clp", "_rgh", "_ocl", "_ems", "_dif", "_nrm" };
                    foreach (string property in textureProperties)
                    {
                        if (m.HasProperty(property))
                        {
                            Texture2D tex = m.GetTexture(property) as Texture2D;
                            if (tex != null && tex.name.Contains(searchTerm))
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                })
                .Where(m => m.name.Contains(additionalFilterTerm)) // Further filter by additionalFilterTerm in the material name
                .ToList();
                Debug.Log($"Total Materials after filter: {allMaterials.Count}");
                if (gridUnlimitedScroller != null)
                {
                    gridUnlimitedScroller.cellPerRow = 5;
                }
                unlimitedScroller.Generate(materialCellPrefab, allMaterials.Count, (index, iCell) =>
                {
                    var cell = iCell as UnlimitedScrollUI.RegularCell;
                    if (cell != null)
                    {
                        Material material = allMaterials[index];
                        Texture2D thumbnail = null;
                        // Check for specific texture properties and generate thumbnail
                        if (material.HasProperty("_MainTex"))
                        {
                            thumbnail = material.GetTexture("_MainTex") as Texture2D;
                        }
                        else if (material.HasProperty("_BaseColorMap"))
                        {
                            thumbnail = material.GetTexture("_BaseColorMap") as Texture2D;
                        }
                        else if (material.HasProperty("_dif"))
                        {
                            thumbnail = material.GetTexture("_dif") as Texture2D;
                        }
                        // Apply the thumbnail if available
                        if (thumbnail != null)
                        {
                            cell.GetComponent<Image>().sprite = Sprite.Create(thumbnail, new Rect(0, 0, thumbnail.width, thumbnail.height), new Vector2(0.5f, 0.5f), 100f);
                        }
                        TMP_Text label = cell.transform.Find("label").GetComponent<TMP_Text>();
                        label.text = material.name; // Set the name of the material as the label text
                        cell.GetComponent<Button>().onClick.AddListener(() => SelectMaterial(material, currentRenderer, currentSlotForSelection));
                        cell.transform.localScale = Vector3.one;
                        cell.onGenerated?.Invoke(index);
                    }
                });
            }
        }

        public void LoadTextures(string filter)
        {
            allTextures = Resources.LoadAll<Texture2D>("Textures")
                .Where(t => t.name.Contains(searchTerm) && t.name.Contains(additionalFilterTerm))
                .ToList();
            Debug.Log($"Total Textures after filter: {allTextures.Count}");
            GenerateResources();
        }

        public void LoadMaterials(string filter)
        {
            allMaterials = Resources.LoadAll<Material>("Materials")
                .Where(m => m.HasProperty(filter) && m.GetTexture(filter) != null)
                .ToList();
            Debug.Log($"Total Materials after filter: {allMaterials.Count}");
            GenerateResources(); // Refresh the UI with the filtered materials
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

        public void SelectMaterial(Material material, SkinnedMeshRenderer renderer, string slotName)
        {
            if (MaterialSelected != null)
            {
                MaterialSelected(material, renderer, slotName);
            }
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

        private void RefreshTexturesWithAdditionalFilter()
        {
            RefreshResources();
        }

        private void ClearExistingCells()
        {
            allTextures?.Clear();
            allMaterials?.Clear();

            if (unlimitedScroller != null)
            {
                unlimitedScroller.Clear();
            }
            else
            {
                Debug.LogError("unlimitedScroller is null.");
            }
        }

        private void SetSearchTermFromOtherUI(string searchTermFromOtherUI)
        {
            if (!String.IsNullOrWhiteSpace(searchTermFromOtherUI))
            {
                searchTerm = searchTermFromOtherUI;
                UpdateDropdownSelection(searchTerm);
                RefreshResources();
            }
        }

        public void RefreshMaterials()
        {
            ClearExistingCells();
            LoadMaterials(searchTerm);
        }

        public void RefreshTextures()
        {
            ClearExistingCells();
            LoadTextures(searchTerm);
        }
    }
}