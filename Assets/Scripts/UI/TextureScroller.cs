using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnlimitedScrollUI.Example;
using UnlimitedScrollUI;
using static ModelData;
using System.IO;

namespace doppelganger
{
    public class TextureScroller : MonoBehaviour
    {
        [Header("Public Fields")]
        public bool autoGenerate;
        public bool isTextures = true;
        public bool isMaterials = false;
        public string searchTerm = "";
        public string additionalFilterTerm = "";
        public GameObject currentModel;

        [Header("Managers")]
        public GridUnlimitedScroller gridUnlimitedScroller;
        public VariationBuilder variationBuilder;
        public VariationTextureSlotsPanel currentSelectionPanel;
        private UnlimitedScrollUI.IUnlimitedScroller unlimitedScroller;

        [Header("Interface")]
        public GameObject cellPrefab;
        public GameObject materialCellPrefab;
        public Transform contentPanel;
        public TMP_Dropdown imageTypeDropdown;
        public TMP_InputField filterInputField;
        public Button materialsButton;
        public Button texturesButton;
        public Color selectedButtonColor;
        public Color defaultButtonColor;

        private string currentSlotForSelection;
        private bool materialsInitialized = false;
        private bool texturesInitialized = false;
        private SkinnedMeshRenderer currentRenderer;
        private Material currentMaterial;
        private List<Texture2D> allTextures;
        private List<Material> allMaterials;
        private List<Texture2D> filteredTextures;
        private List<Material> filteredMaterials;
        public event Action<Texture2D> onTextureSelected;
        public event Action<Material> MaterialSelected;
        public event Action<Texture2D, string> TextureSelected;
        private List<string> materialNamesFromJson = new List<string>();

        public List<string> options = new List<string> { "_dif", "_ems", "_gra", "_nrm", "_spc", "_rgh", "_msk", "_idx", "_clp", "_ocl" };

        private void Start()
        {
            allTextures = new List<Texture2D>();
            allMaterials = new List<Material>();
            unlimitedScroller = GetComponent<UnlimitedScrollUI.IUnlimitedScroller>();
            LoadMaterialNamesFromJson();
            imageTypeDropdown.AddOptions(options);
            if (options.Count > 0)
            {
                searchTerm = options[0].ToLower();
                imageTypeDropdown.value = 0;
            }

            if (autoGenerate)
            {
                StartCoroutine(DelayedResourceInitialization());
            }

            filterInputField.onValueChanged.AddListener(delegate { RefreshResources(); });
            imageTypeDropdown.onValueChanged.AddListener(delegate { DropdownIndexChanged(imageTypeDropdown); });
            materialsButton.onClick.AddListener(() => ReloadMaterials());
            texturesButton.onClick.AddListener(() => ReloadTextures());
        }

        private void LoadMaterialNamesFromJson()
        {
            string materialsIndexPath = Path.Combine(Application.dataPath, "StreamingAssets", "Mesh references", "materials_index.json");
            if (File.Exists(materialsIndexPath))
            {
                string jsonContent = File.ReadAllText(materialsIndexPath);
                MaterialsIndex materialsIndex = JsonUtility.FromJson<MaterialsIndex>(jsonContent);
                if (materialsIndex != null && materialsIndex.materials != null)
                {
                    // If materialsIndex and materialsIndex.materials are not null, update materialNamesFromJson
                    materialNamesFromJson = new List<string>(materialsIndex.materials);
                    Debug.Log("Loaded material names from JSON");
                }
                else
                {
                    // Log error if materialsIndex is null or materialsIndex.materials is null
                    Debug.LogError("Failed to load material names from JSON: JSON is not properly formatted or is null");
                }
            }
            else
            {
                Debug.LogError($"Materials index file not found: {materialsIndexPath}");
            }
        }

        private IEnumerator DelayedResourceInitialization()
        {
            yield return new WaitForEndOfFrame();
            if (isTextures && !texturesInitialized)
            {
                ReloadTextures();
                texturesInitialized = true; // Ensure this initialization only happens once
            }
            else if (isMaterials && !materialsInitialized)
            {
                ReloadMaterials();
                materialsInitialized = true; // Ensure this initialization only happens once
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
            if (materialNamesFromJson == null || materialNamesFromJson.Count == 0)
            {
                return;
            }

            allMaterials.Clear();
            foreach (string matName in materialNamesFromJson)
            {
                string formattedMatName = matName.Replace(".mat", "");
                Material mat = Resources.Load<Material>($"Materials/{formattedMatName}");

                if (mat != null)
                {
                    allMaterials.Add(mat);
                }
            }
            Debug.Log($"Total Materials after filter: {allMaterials.Count}");
            GenerateResources();
        }

        public void FilterTextures(string filter)
        {
            filteredTextures = allTextures.Where(t => t.name.Contains(filter)).ToList();
            GenerateResources();
        }

        public void FilterMaterials(string filter)
        {
            filteredMaterials = allMaterials.Where(m => m.HasProperty(filter)).ToList();
            GenerateResources();
        }

        public void ReloadMaterials()
        {
            isMaterials = true;
            isTextures = false;
            UpdateButtonColors(materialsButton);
            //UpdateFilterFromDropdown();
            RefreshResources();
        }

        public void ReloadTextures()
        {
            isTextures = true;
            isMaterials = false;
            UpdateButtonColors(texturesButton);
            //UpdateFilterFromDropdown();
            RefreshResources();
        }

        private void UpdateFilterFromDropdown()
        {
            if (imageTypeDropdown.options.Count > imageTypeDropdown.value)
            {
                searchTerm = imageTypeDropdown.options[imageTypeDropdown.value].text.Trim().ToLower();
            }
        }

        private void UpdateButtonColors(Button activeButton)
        {
            // Reset both buttons to the default color
            materialsButton.image.color = defaultButtonColor;
            texturesButton.image.color = defaultButtonColor;

            // Set the active button to the selected color
            activeButton.image.color = selectedButtonColor;
        }

        public void SetCurrentSelectionPanel(VariationTextureSlotsPanel panel)
        {
            currentSelectionPanel = panel;
            Debug.Log($"SetCurrentSelectionPanel: currentSelectionPanel: {currentSelectionPanel}");
        }

        public void ClearCurrentSelectionPanel()
        {
            currentSelectionPanel = null;
            Debug.Log($"ClearCurrentSelectionPanel: currentSelectionPanel: {currentSelectionPanel}");
        }

        public VariationTextureSlotsPanel GetCurrentSelectionPanel()
        {
            return currentSelectionPanel;
        }

        public void PrepareForSelection(VariationTextureSlotsPanel selectionPanel, string slotName)
        {
            currentSelectionPanel = selectionPanel;
            currentSlotForSelection = slotName;
            Debug.Log($"Preparing for texture selection for selectionPanel {selectionPanel} on slotName {slotName}");
            ReloadTextures();
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
                    string[] textureProperties = new string[] { "_dif", "_ems", "_gra", "_nrm", "_spc", "_rgh", "_msk", "_idx", "_clp", "_ocl" };
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
                        cell.GetComponent<Button>().onClick.AddListener(() => SelectMaterial(material));
                        cell.transform.localScale = Vector3.one;
                        cell.onGenerated?.Invoke(index);
                    }
                });
            }
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

        public void SelectMaterial(Material material)
        {
            // Assuming MaterialSelected is a delegate, invoke it only if it's not null
            MaterialSelected?.Invoke(material);
        }


        private void SelectTexture(Texture2D texture)
        {
            // Check if there is a current selection panel and a slot selected
            if (currentSelectionPanel != null && !string.IsNullOrWhiteSpace(currentSlotForSelection))
            {
                // Apply the texture change to the current selection panel and slot
                currentSelectionPanel.GetTextureChange(texture, currentSlotForSelection);
                // Update the UI element (e.g., button text) to reflect the change
                currentSelectionPanel.currentButtonClickedText.text = texture.name;
            }
            else
            {
                Debug.LogError("Select a material slot in the active panel and try again");
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