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

        public string currentPath;
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

            List<string> allJsonFiles = new List<string>();
            string searchFilter = filterInputField.text.Trim().ToLower();
            bool filesFound = false;

            foreach (var entry in folderMappings)
            {
                string selectedOption = presetTypeDropdown.options[presetTypeDropdown.value].text;
                string selectedFolder = folderMappings.TryGetValue(selectedOption, out string path) ? path : string.Empty;

                currentPath = Path.Combine(Application.streamingAssetsPath, "Jsons", selectedFolder);
                if (!Directory.Exists(currentPath))
                {
                    Directory.CreateDirectory(currentPath);
                }

                var jsonFiles = Directory.GetFiles(currentPath, "*.json", SearchOption.AllDirectories);
                if (jsonFiles.Length > 0)
                {
                    allJsonFiles.AddRange(jsonFiles);
                    filesFound = true;
                    break;
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

            string output_path = ConfigManager.LoadSetting("SavePath", "Output_Path");
            if (!string.IsNullOrEmpty(output_path))
            {
                if (!Directory.Exists(output_path))
                {
                    Directory.CreateDirectory(output_path);
                }
                var outputJsonFiles = Directory.GetFiles(output_path, "*.json", SearchOption.AllDirectories);
                allJsonFiles.AddRange(outputJsonFiles);
            }

            var filteredFiles = allJsonFiles.Where(file =>
            {
                string fileNameLower = Path.GetFileName(file).ToLower();
                bool exclude = (fileNameLower.Contains("skeleton") &&
                   fileNameLower != "player_tpp_skeleton.json" &&
                   fileNameLower != "player_fpp_skeleton.json") ||
                   fileNameLower.Contains("db_");

                return !exclude && (string.IsNullOrEmpty(searchFilter) || fileNameLower.Contains(searchFilter));
            }).ToList();

            GenerateButtons(filteredFiles);
        }

        public void RefreshCustomPresets(string folderPath)
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
                return !fileNameLower.Contains("skeleton") &&
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
                        imageComponent.enabled = true;
                        imageComponent.color = Color.white;
                    }

                    cell.GetComponent<Button>().onClick.RemoveAllListeners();
                    cell.GetComponent<Button>().onClick.AddListener(() => 
                        interfaceManager.OnPresetLoadButtonPressed(jsonPath, jsonName));
                    cell.transform.localScale = Vector3.one;

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
            return null;
        }

        public void RefreshPresets()
        {
            LoadPresets();
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
        }

        public IEnumerator DocumentationButtonPresses(float delayBetweenPresses = 3.0f, float postDocumentationDelay = 2.0f)
        {
            var tempButtonPressActions = new List<ButtonAction>(buttonPressActions);
            foreach (var buttonAction in tempButtonPressActions)
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
            var tempButtonPressActions = new List<ButtonAction>(buttonPressActions);

            foreach (var buttonAction in tempButtonPressActions)
            {
                if (!buttonAction.HasThumbnail)
                {
                    characterBuilder.Reset();
                    buttonAction.Action.Invoke();
                    screenshotManager.DocumentPreset();
                    yield return new WaitForSeconds(delayBetweenPresses);
                }
            }
        }
    }
}