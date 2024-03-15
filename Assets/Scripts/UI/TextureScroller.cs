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
        // General settings
        public bool autoGenerate;
        public bool isTextures = true;
        public bool isMaterials = false;
        private bool useFilteredMaterials = false;
        public string searchTerm = "";
        public string additionalFilterTerm = "";
        public GameObject currentModel;

        [Header("Managers")]
        // Managers and controllers
        public ConfigManager configManager;
        public GridUnlimitedScroller gridUnlimitedScroller;
        public VariationBuilder variationBuilder;
        public VariationTextureSlotsPanel currentSelectionPanel;
        private UnlimitedScrollUI.IUnlimitedScroller unlimitedScroller;

        [Header("Interface")]
        // UI elements and controls
        public GameObject cellPrefab;
        public GameObject materialCellPrefab;
        public Transform contentPanel;
        public TMP_Dropdown imageTypeDropdown;
        public TMP_InputField filterInputField;
        public Button materialsButton;
        public Button texturesButton;
        public Color selectedButtonColor;
        public Color defaultButtonColor;

        // Internal variables
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

        // Additional options
        public List<string> options = new List<string> { "_dif", "_ems", "_gra", "_nrm", "_spc", "_rgh", "_msk", "_idx", "_clp", "_ocl" };


        private void Start()
        {
            allTextures = new List<Texture2D>();
            allMaterials = new List<Material>();
            unlimitedScroller = GetComponent<UnlimitedScrollUI.IUnlimitedScroller>();
            LoadMaterialNamesFromJson();
            imageTypeDropdown.ClearOptions();
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
            LoadTextures(searchTerm, additionalFilterTerm);
            filterInputField.onEndEdit.AddListener(delegate { RefreshResources(); });
            imageTypeDropdown.onValueChanged.AddListener(delegate { RefreshResources(); });
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

        public void ReloadMaterials()
        {
            isMaterials = true;
            isTextures = false;
            UpdateButtonColors(materialsButton);
            RefreshResources();
        }

        public void ReloadTextures()
        {
            isTextures = true;
            isMaterials = false;
            UpdateButtonColors(texturesButton);
            RefreshResources();
        }

        public void FilterTextures(string filter)
        {
            filteredTextures = allTextures.Where(t => t.name.Contains(filter)).ToList();
            HandleTextureGeneration();
        }

        private void FilterMaterialsByTextureProperty(string property)
        {
            allMaterials = allMaterials.Where(m => m.HasProperty(property) && m.GetTexture(property) != null).ToList();
            //Debug.Log($"Total Materials after filter for property {property}: {allMaterials.Count}");
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
            //Debug.Log($"ClearCurrentSelectionPanel: currentSelectionPanel: {currentSelectionPanel}");
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

        private void RefreshResources()
        {
            ClearExistingCells();
            string containsFilter = filterInputField.text.Trim().ToLower();
            string searchTerm = imageTypeDropdown.options[imageTypeDropdown.value].text;
            if (isMaterials)
            {
                LoadAndFilterMaterials(searchTerm, containsFilter);
                //Debug.Log($"RefreshResources: Material - searchTerm {searchTerm}, containsFilter {containsFilter}");
            }
            else if (isTextures)
            {
                string endingFilter = searchTerm;
                LoadTextures(endingFilter, containsFilter);
            }
        }

        private void LoadAndFilterMaterials(string property, string containsFilter)
        {
            LoadMaterialsFromJson();
            FilterMaterialsByTextureProperty(property);
            ApplyContainsFilterToMaterials(containsFilter);
            HandleMaterialGeneration();
        }

        private void ApplyContainsFilterToMaterials(string containsFilter)
        {
            if (!string.IsNullOrEmpty(containsFilter))
            {
                allMaterials = allMaterials.Where(m => m.name.ToLower().Contains(containsFilter)).ToList();
                Debug.Log($"ApplyContainsFilterToMaterials: Total Materials after filter for property {containsFilter}: {allMaterials.Count}");
            }
        }

        private void LoadMaterialsFromJson()
        {
            allMaterials.Clear();
            if (materialNamesFromJson == null || materialNamesFromJson.Count == 0)
            {
                Debug.LogError("Material names list is empty.");
                return;
            }

            foreach (string matName in materialNamesFromJson)
            {
                Material mat = Resources.Load<Material>($"Materials/{matName.Replace(".mat", "").Trim()}");
                if (mat != null)
                {
                    allMaterials.Add(mat);
                }
            }
        }


        public void LoadTextures(string endingFilter, string containsFilter)
        {
            List<Texture2D> loadedTextures = new List<Texture2D>();

            // Load textures from content_path if it exists
            string content_path = ConfigManager.LoadSetting("SavePath", "Content_Path");
            if (!string.IsNullOrEmpty(content_path))
            {
                Debug.Log($"Attempting to load textures from content path: {content_path}");
                try
                {
                    var textureFiles = Directory.GetFiles(content_path, "*.*")
                        .Where(file => file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg")).ToList();

                    Debug.Log($"Found {textureFiles.Count} texture files in content path.");

                    foreach (var file in textureFiles)
                    {
                        byte[] fileData = File.ReadAllBytes(file);
                        Texture2D tex = new Texture2D(2, 2);
                        if (tex.LoadImage(fileData))
                        {
                            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                            tex.name = fileNameWithoutExtension;
                            // Apply filters here as well
                            if (tex.name.EndsWith(endingFilter) && tex.name.Contains(containsFilter))
                            {
                                loadedTextures.Add(tex);
                                Debug.Log($"Successfully loaded and filtered texture: {tex.name} from file: {file}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Failed to load texture from file: {file}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load textures from content_path: {e.Message}");
                }
            }
            else
            {
                Debug.Log("Custom content path is not set in the configuration.");
            }

            try
            {
                var resourcesTextures = Resources.LoadAll<Texture2D>("Textures");
                if (resourcesTextures != null)
                {
                    var filteredTextures = resourcesTextures
                        .Where(t => t != null
                                    && !t.name.EndsWith("_tmb")
                                    && t.name.EndsWith(endingFilter)
                                    && t.name.Contains(containsFilter))
                        .ToList();
                    loadedTextures.AddRange(filteredTextures);
                }
                else
                {
                    Debug.LogError("Failed to load any resources from the specified path.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occurred while loading textures from Resources: {e.Message}");
                Debug.LogError($"Stack Trace: {e.StackTrace}");
            }

            allTextures = loadedTextures;

            Debug.Log($"Total textures after applying filters: {allTextures.Count}");
            HandleTextureGeneration();
        }

        public void LoadMaterials(string containsFilter)
        {
            if (materialNamesFromJson == null || materialNamesFromJson.Count == 0)
            {
                Debug.LogError("Material names list is empty.");
                return;
            }

            allMaterials.Clear();
            foreach (string matName in materialNamesFromJson)
            {
                // Format the material name as per your requirement
                string formattedMatName = matName.Replace(".mat", "").Trim().ToLower();
                // Load the material from Resources
                Material mat = Resources.Load<Material>($"Materials/{formattedMatName}");

                // Apply containsFilter to check if the formatted material name contains the filter term
                if (mat != null && formattedMatName.Contains(containsFilter))
                {
                    allMaterials.Add(mat);
                }
            }

            Debug.Log($"Total Materials after filter: {allMaterials.Count}");
            HandleMaterialGeneration();
        }

        private void HandleTextureGeneration()
        {
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
                    // Try to load existing thumbnail for texture
                    Texture2D thumbnail = LoadThumbnail(texture.name, true); // true for isTexture
                                                                             // If no thumbnail exists, generate a new one
                    if (thumbnail == null)
                    {
                        Debug.Log($"Thumbnail is null for {thumbnail}, generating the thumbnail");
                        thumbnail = GenerateThumbnail(texture);
                    }
                    cell.GetComponent<Image>().sprite = Sprite.Create(thumbnail, new Rect(0.0f, 0.0f, thumbnail.width, thumbnail.height), new Vector2(0.5f, 0.5f), 100.0f);
                    cell.GetComponent<Button>().onClick.AddListener(() => SelectTexture(texture));
                    cell.transform.localScale = Vector3.one;
                    cell.onGenerated?.Invoke(index);
                }
            });
        }

        private void HandleMaterialGeneration()
        {
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
                    string mainTextureName = DetermineMainTextureName(material);
                    Texture2D thumbnail = LoadThumbnail(material.name, false); // Load using material name instead

                    if (thumbnail == null)
                    {
                        Texture2D mainTexture = null;
                        if (!string.IsNullOrEmpty(mainTextureName))
                        {
                            mainTexture = material.GetTexture(mainTextureName) as Texture2D;
                        }

                        if (mainTexture != null)
                        {
                            thumbnail = GenerateThumbnail(mainTexture);
                        }
                    }

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

        private Texture2D LoadThumbnail(string resourceName, bool isTexture)
        {
            string pathPrefix = "Thumbnails/";
            string thumbnailPath = pathPrefix + resourceName + "_tmb";
            return Resources.Load<Texture2D>(thumbnailPath);
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

        private string DetermineMainTextureName(Material material)
        {
            // List of common texture property names used in different shaders
            string[] textureProperties = new string[] { "_dif", "_MainTex", "_BaseColorMap" };

            foreach (string propertyName in textureProperties)
            {
                if (material.HasProperty(propertyName))
                {
                    Texture tex = material.GetTexture(propertyName);
                    if (tex != null)
                    {
                        return tex.name;
                    }
                }
            }
            return null; // Return null if no suitable texture is found
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

    }
}