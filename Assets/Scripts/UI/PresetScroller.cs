using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace doppelganger
{

    public class PresetScroller : MonoBehaviour
    {
        public Transform contentPanel;
        public GameObject cellPrefab;
        public TMP_Dropdown presetTypeDropdown;
        public TMP_InputField filterInputField;
        private UnlimitedScrollUI.IUnlimitedScroller unlimitedScroller;
        public CharacterBuilder_InterfaceManager interfaceManager; // Reference to your character builder script

        private void Start()
        {
            unlimitedScroller = GetComponent<UnlimitedScrollUI.IUnlimitedScroller>();
            LoadPresets(); // Initial load

            filterInputField.onValueChanged.AddListener(delegate { RefreshPresets(); });
            presetTypeDropdown.onValueChanged.AddListener(delegate { RefreshPresets(); });
        }

        private string GetSelectedFolderPath(string selectedOption)
        {
            // Example mapping logic. Update according to your specific requirements.
            var folderMappings = new Dictionary<string, string>
    {
        {"Custom", "Custom/"},
        {"Player", "Human/Player"},
        {"Man", "Human/Man"},
        {"Woman", "Human/Wmn"},
        {"Child", "Human/Child"},
        {"Biter", "Infected/Biter"},
        {"Special Infected", "Infected/Special Infected"},
        {"Viral", "Infected/Viral"}
    };

            return folderMappings.TryGetValue(selectedOption, out string path) ? path : string.Empty;
        }

        public void LoadPresets()
        {
            string selectedOption = presetTypeDropdown.options[presetTypeDropdown.value].text;
            string selectedFolder = GetSelectedFolderPath(selectedOption);
            Debug.Log($"Selected folder: {selectedFolder}");

            string searchFilter = filterInputField.text.Trim().ToLower();
            Debug.Log($"Search filter: {searchFilter}");

            string fullPath = Path.Combine(Application.streamingAssetsPath, "Jsons", selectedFolder);
            Debug.Log($"Full path: {fullPath}");

            // Update query to exclude files containing "skeleton", "db_", and ending with "_fpp"
            var jsonFiles = Directory.GetFiles(fullPath, "*.json", SearchOption.AllDirectories)
                                      .Where(file => !(Path.GetFileName(file).ToLower().EndsWith("_fpp") ||
                                                       Path.GetFileName(file).ToLower().Contains("skeleton") ||
                                                       Path.GetFileName(file).ToLower().Contains("db_")))
                                      .Where(file => string.IsNullOrEmpty(searchFilter) || Path.GetFileName(file).ToLower().Contains(searchFilter))
                                      .ToList();

            Debug.Log($"Found {jsonFiles.Count} JSON files, excluding '_fpp', 'skeleton', and 'db_'.");
            GenerateButtons(jsonFiles);
        }


        private void GenerateButtons(List<string> files)
        {
            ClearExistingCells();
            Debug.Log($"Generating buttons for {files.Count} files.");
            unlimitedScroller.Generate(cellPrefab, files.Count, (index, iCell) =>
            {
                var cell = iCell as UnlimitedScrollUI.RegularCell;
                if (cell != null)
                {
                    string jsonPath = files[index];
                    string jsonName = Path.GetFileNameWithoutExtension(jsonPath);

                    Texture2D thumbnail = LoadThumbnailForJson(jsonPath);
                    var imageComponent = cell.GetComponent<Image>();
                    var textComponent = cell.GetComponentInChildren<TextMeshProUGUI>();
                    textComponent.text = jsonName;

                    // Check if thumbnail exists
                    if (thumbnail != null)
                    {
                        imageComponent.sprite = Sprite.Create(thumbnail, new Rect(0, 0, thumbnail.width, thumbnail.height), new Vector2(0.5f, 0.5f), 100);
                        imageComponent.enabled = true; // Ensure the Image component is enabled
                        imageComponent.color = Color.white; // Set the Image component color to white
                    }

                    // Ensure the button's click listener is properly set up
                    cell.GetComponent<Button>().onClick.RemoveAllListeners(); // Remove existing listeners to prevent stacking
                    cell.GetComponent<Button>().onClick.AddListener(() => 
                        interfaceManager.OnPresetLoadButtonPressed(jsonPath, jsonName));
                    cell.transform.localScale = Vector3.one; // Ensure the cell's scale is reset to default
                }
            });
        }

        private Texture2D LoadThumbnailForJson(string jsonPath)
        {
            string thumbnailPath = Path.ChangeExtension(jsonPath, ".png");
            if (File.Exists(thumbnailPath))
            {
                byte[] fileData = File.ReadAllBytes(thumbnailPath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);
                return texture;
            }
            return null; // No thumbnail found
        }

        public void RefreshPresets()
        {
            LoadPresets();
        }

        private void ClearExistingCells()
        {
            // Clear logic here, similar to the TextureScroller's ClearExistingCells method
            if (unlimitedScroller != null)
            {
                unlimitedScroller.Clear();
            }
            else
            {
                Debug.LogError("unlimitedScroller is null.");
            }
        }
    }
}