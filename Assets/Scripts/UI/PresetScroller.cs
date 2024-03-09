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
using Newtonsoft.Json;
using System.IO.Compression;

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

        public void LoadPresets()
        {
            // Dictionary holding folder mappings, assume it's class-level if used elsewhere
            var folderMappings = new Dictionary<string, string>
    {
        {"Custom", "Custom/"},
        {"Player", "Human/Player"},
        {"Man", "Human/Man"},
        {"Woman", "Human/Woman"},
        {"Child", "Human/Child"},
        {"Biter", "Infected/Biter"},
        {"Special Infected", "Infected/Special Infected"},
        {"Viral", "Infected/Viral"}
    };

            // List to hold all valid json file paths
            List<string> allJsonFiles = new List<string>();
            string searchFilter = filterInputField.text.Trim().ToLower();
            bool filesFound = false;

            foreach (var entry in folderMappings)
            {
                string selectedOption = presetTypeDropdown.options[presetTypeDropdown.value].text;
                string selectedFolder = folderMappings.TryGetValue(selectedOption, out string path) ? path : string.Empty;

                // Create the directory if it does not exist
                string fullPath = Path.Combine(Application.streamingAssetsPath, "Jsons", selectedFolder);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath); // Create the missing directory
                }

                // Check JSON files in the directory and subdirectories
                var jsonFiles = Directory.GetFiles(fullPath, "*.json", SearchOption.AllDirectories);
                if (jsonFiles.Length > 0) // Check if there are JSON files
                {
                    allJsonFiles.AddRange(jsonFiles);
                    filesFound = true; // Mark that we found files
                    break; // Exit the loop as we've found valid files
                }

                if (!filesFound)
                {
                    presetTypeDropdown.value = (presetTypeDropdown.value + 1) % presetTypeDropdown.options.Count;
                }
                else
                {
                    break;
                }
            }

            // Now, load JSONs from the output_path if it is set
            string output_path = ConfigManager.LoadSetting("SavePath", "Output_Path");
            if (!string.IsNullOrEmpty(output_path))
            {
                if (!Directory.Exists(output_path))
                {
                    Directory.CreateDirectory(output_path); // Create the directory if it does not exist
                }
                var outputJsonFiles = Directory.GetFiles(output_path, "*.json", SearchOption.AllDirectories);
                allJsonFiles.AddRange(outputJsonFiles);
            }

            // Filter out unwanted files and apply search filter if necessary
            var filteredFiles = allJsonFiles.Where(file =>
            {
                string fileNameLower = Path.GetFileName(file).ToLower();
                bool exclude = fileNameLower.Contains("fpp") ||
                               (fileNameLower.Contains("skeleton") && fileNameLower != "player_tpp_skeleton.json") ||
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

        public void InstallZips()
        {
            // Define the custom folder path base
            string baseCustomFolderPath = Path.Combine(Application.streamingAssetsPath, "Jsons", "Custom");
            Debug.Log($"Checking or creating base custom folder path: {baseCustomFolderPath}");

            // Ensure the custom base folder exists
            if (!Directory.Exists(baseCustomFolderPath))
            {
                Directory.CreateDirectory(baseCustomFolderPath);
                Debug.Log($"Created base custom folder path: {baseCustomFolderPath}");
            }

            // Call the Standalone File Browser to select a folder
            string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                Debug.Log($"Selected folder for zip files: {paths[0]}");

                // Load all ZIP files from the selected folder
                List<string> zipFiles = new List<string>();
                try
                {
                    zipFiles.AddRange(Directory.GetFiles(paths[0], "*.zip", SearchOption.AllDirectories));
                    Debug.Log($"Found {zipFiles.Count} zip files in selected directory.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error while loading ZIP files: {e.Message}");
                    return; // Stop execution if we can't load the files
                }

                foreach (string zipFile in zipFiles)
                {
                    //Debug.Log($"Processing zip file: {zipFile}");
                    using (ZipArchive archive = ZipFile.OpenRead(zipFile))
                    {
                        if (archive.GetEntry("PLACEHOLDER_InfinityDesigner_json.file") != null)
                        {
                            //Debug.Log($"Found placeholder file in: {zipFile}");

                            ZipArchiveEntry installJsonEntry = archive.GetEntry("install.json");
                            if (installJsonEntry != null)
                            {
                                using (StreamReader reader = new StreamReader(installJsonEntry.Open()))
                                {
                                    string jsonContent = reader.ReadToEnd();
                                    dynamic installInfo = JsonConvert.DeserializeObject(jsonContent);
                                    string userName = installInfo.Metadata.Username;
                                    string category = installInfo.Metadata.Category;
                                    string cls = installInfo.Metadata.Class;

                                    //Debug.Log($"Read from install.json: Category = {category}, Class = {cls}");

                                    // Define the custom folder path based on category and class
                                    string customFolderPath = Path.Combine(baseCustomFolderPath, userName);
                                    if (category != "ALL")
                                    {
                                        customFolderPath = Path.Combine(customFolderPath, category);
                                    }
                                    if (cls != "ALL")
                                    {
                                        customFolderPath = Path.Combine(customFolderPath, cls);
                                    }

                                    // Ensure the custom folder exists
                                    if (!Directory.Exists(customFolderPath))
                                    {
                                        Directory.CreateDirectory(customFolderPath);
                                        //Debug.Log($"Created custom folder path for extraction: {customFolderPath}");
                                    }

                                    // Initialize paths for files to delete after extraction
                                    List<string> filesToDelete = new List<string>();

                                    // Extract files except the placeholder
                                    foreach (ZipArchiveEntry entry in archive.Entries)
                                    {
                                        string destinationPath = Path.Combine(customFolderPath, entry.FullName);
                                        // Direct extraction for non-placeholder and non-install.json files
                                        if (!entry.FullName.Equals("PLACEHOLDER_InfinityDesigner_json.file", StringComparison.OrdinalIgnoreCase) &&
                                            !entry.FullName.Equals("install.json", StringComparison.OrdinalIgnoreCase))
                                        {
                                            entry.ExtractToFile(destinationPath, true);
                                            //Debug.Log($"Extracted file: {entry.FullName} to {destinationPath}");
                                        }
                                        else
                                        {
                                            // Add placeholder and install.json to the deletion list
                                            filesToDelete.Add(destinationPath);
                                        }
                                    }

                                    // Now delete placeholder and install.json after extraction
                                    foreach (string fileToDelete in filesToDelete)
                                    {
                                        if (File.Exists(fileToDelete))
                                        {
                                            File.Delete(fileToDelete);
                                            //Debug.Log($"Deleted file: {fileToDelete}");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogError("install.json not found within: " + zipFile);
                            }
                        }
                        else
                        {
                            Debug.Log("Placeholder file not found in: " + zipFile);
                        }
                    }
                }
                RefreshCustomPresets(baseCustomFolderPath);
                LoadPresets();
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