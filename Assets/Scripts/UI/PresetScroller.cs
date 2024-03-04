using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SFB;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnlimitedScrollUI.Example;

namespace doppelganger
{

    public class PresetScroller : MonoBehaviour
    {
        private UnlimitedScrollUI.IUnlimitedScroller unlimitedScroller;
        public CharacterBuilder_InterfaceManager interfaceManager;
        public CharacterBuilder characterBuilder;
        public ScreenshotManager screenshotManager;

        public Transform contentPanel;
        public GameObject cellPrefab;
        public TMP_Dropdown presetTypeDropdown;
        public TMP_InputField filterInputField;
        public bool debug = false;

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
            else
            {
                LoadPresets();

                filterInputField.onValueChanged.AddListener(delegate { RefreshPresets(); });
                presetTypeDropdown.onValueChanged.AddListener(delegate { RefreshPresets(); });
            }
        }

        private void Update()
        {
            if (debug && Input.GetKeyDown(KeyCode.F10))
            {
                StartCoroutine(DocumentationButtonPressesForNoThumbnail(2.0f, 2.0f));
            }
            if (debug && Input.GetKeyDown(KeyCode.F11))
            {
                StartCoroutine(DocumentationButtonPresses(2.0f, 2.0f));
            }
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
            string searchFilter = filterInputField.text.Trim().ToLower();

            // List to hold all valid json file paths
            List<string> allJsonFiles = new List<string>();

            // First, load JSONs from the predefined path
            string fullPath = Path.Combine(Application.streamingAssetsPath, "Jsons", selectedFolder);
            var jsonFiles = Directory.GetFiles(fullPath, "*.json", SearchOption.AllDirectories);
            allJsonFiles.AddRange(jsonFiles);

            // Now, load JSONs from the output_path if it is set
            string output_path = ConfigManager.LoadSetting("SavePath", "Output_Path");
            if (!string.IsNullOrEmpty(output_path))
            {
                if (Directory.Exists(output_path)) // Check if the directory actually exists
                {
                    var outputJsonFiles = Directory.GetFiles(output_path, "*.json", SearchOption.AllDirectories);
                    allJsonFiles.AddRange(outputJsonFiles);
                }
                else
                {
                    Debug.LogWarning($"Output path does not exist: {output_path}");
                }
            }

            // Filter out unwanted files and apply search filter if necessary
            var filteredFiles = allJsonFiles.Where(file =>
            {
                string fileNameLower = Path.GetFileName(file).ToLower();
                bool exclude = fileNameLower.Contains("fpp") ||
                               fileNameLower.Contains("skeleton") ||
                               fileNameLower.Contains("db_");

                return !exclude && (string.IsNullOrEmpty(searchFilter) || fileNameLower.Contains(searchFilter));
            }).ToList();

            GenerateButtons(filteredFiles);
        }

        public void BrowseAndLoadJsons()
        {
            // Define the custom folder path
            string customFolderPath = Path.Combine(Application.streamingAssetsPath, "Jsons", "Custom");

            // Ensure the custom folder exists
            if (!Directory.Exists(customFolderPath))
            {
                Directory.CreateDirectory(customFolderPath);
            }

            // Call the Standalone File Browser to select a folder
            string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);

            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                Debug.Log($"Selected folder: {paths[0]}");

                // Load all JSON files from the selected folder
                List<string> jsonFiles = new List<string>();
                try
                {
                    jsonFiles.AddRange(Directory.GetFiles(paths[0], "*.json", SearchOption.AllDirectories));
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error while loading JSON files: {e.Message}");
                }

                // Copy JSONs and their matching PNGs to the Custom folder
                foreach (string jsonFile in jsonFiles)
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(jsonFile);
                    string pngFile = Path.ChangeExtension(jsonFile, ".png");

                    if (File.Exists(pngFile)) // Check if the matching PNG exists
                    {
                        string destJson = Path.Combine(customFolderPath, Path.GetFileName(jsonFile));
                        string destPng = Path.Combine(customFolderPath, Path.GetFileName(pngFile));

                        File.Copy(jsonFile, destJson, true); // Copy JSON to the Custom folder
                        File.Copy(pngFile, destPng, true); // Copy PNG to the Custom folder
                    }
                }

                // Now reload presets from the Custom folder
                RefreshCustomPresets(customFolderPath); // Implement this based on your existing logic
            }
        }

        private void RefreshCustomPresets(string folderPath)
        {
            var customJsonFiles = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);
            var filteredFiles = FilterJsonFiles(customJsonFiles.ToList());
            GenerateButtons(filteredFiles);
        }

        private List<string> FilterJsonFiles(List<string> files)
        {
            string searchFilter = filterInputField.text.Trim().ToLower();
            var filteredFiles = files.Where(file =>
            {
                string fileNameLower = Path.GetFileName(file).ToLower();
                return !fileNameLower.Contains("fpp") &&
                       !fileNameLower.Contains("skeleton") &&
                       !fileNameLower.Contains("db_") &&
                       (string.IsNullOrEmpty(searchFilter) || fileNameLower.Contains(searchFilter));
            }).ToList();

            return filteredFiles;
        }


        private void GenerateButtons(List<string> files)
        {
            ClearExistingCells();
            //Debug.Log($"Generating buttons for {files.Count} files.");
            buttonPressActions.Clear(); // Clear previous actions
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

                    bool hasThumbnail = thumbnail != null;
                    if (hasThumbnail)
                    {
                        imageComponent.sprite = Sprite.Create(thumbnail, new Rect(0, 0, thumbnail.width, thumbnail.height), new Vector2(0.5f, 0.5f), 100);
                        imageComponent.enabled = true; // Ensure the Image component is enabled
                        imageComponent.color = Color.white; // Set the Image component color to white
                    }
                    else
                    {
                        // Optionally, handle the case when there is no thumbnail
                    }

                    // Ensure the button's click listener is properly set up
                    cell.GetComponent<Button>().onClick.RemoveAllListeners(); // Remove existing listeners to prevent stacking
                    cell.GetComponent<Button>().onClick.AddListener(() => 
                        interfaceManager.OnPresetLoadButtonPressed(jsonPath, jsonName));
                    cell.transform.localScale = Vector3.one; // Ensure the cell's scale is reset to default
                    // Store the action with information about the thumbnail presence
                    Action action = () => interfaceManager.OnPresetLoadButtonPressed(jsonPath, jsonName);
                    buttonPressActions.Add(new ButtonAction(action, hasThumbnail));
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

        public IEnumerator DocumentationButtonPresses(float delayBetweenPresses = 3.0f, float postDocumentationDelay = 2.0f)
        {
            // Assuming buttonPressActions contains all the actions for pressing each preset button
            foreach (var buttonAction in buttonPressActions)
            {
                characterBuilder.Reset();
                buttonAction.Action.Invoke();
                screenshotManager.DocumentPreset();
                yield return new WaitForSeconds(delayBetweenPresses);
                //yield return new WaitForSeconds(postDocumentationDelay);
            }
        }

        public IEnumerator DocumentationButtonPressesForNoThumbnail(float delayBetweenPresses = 3.0f, float postDocumentationDelay = 2.0f)
        {
            foreach (var buttonAction in buttonPressActions)
            {
                if (!buttonAction.HasThumbnail)
                {
                    characterBuilder.Reset();
                    buttonAction.Action.Invoke(); // Simulate the button press
                    screenshotManager.DocumentPreset();
                    yield return new WaitForSeconds(delayBetweenPresses); // Wait for specified delay
                    //yield return new WaitForSeconds(postDocumentationDelay);
                }
            }
        }
    }
}