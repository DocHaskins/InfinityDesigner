using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using SFB;
using TMPro;
using System.Linq;

namespace doppelganger
{
    public class DataManager : MonoBehaviour
    {
        public ConfigManager configManager;
        public UpdateManager updateManager;

        public TMP_InputField pathInputField;
        public TMP_Text gameVersionLabel;
        public TMP_Text engineVersion;
        public GameObject popupPrefab;
        private CanvasGroup popupCanvasGroup;

        void Start()
        {
            if (IsGameVersionAndPathSet())
            {
                Debug.Log($"savePathFound and versionFound");
            }
            else
            {
                SpawnPopup();
            }
        }

        private bool IsGameVersionAndPathSet()
        {
            Debug.Log($"IsGameVersionAndPathSet check");
            string savePath = ConfigManager.LoadSetting("SavePath", "Path");
            string gameVersion = ConfigManager.LoadSetting("Game", "Version");
            string exePath = Path.Combine(savePath, "ph", "work", "bin", "x64", "DyingLightGame_x64_rwdi.exe");

            // Check if both settings are found and not empty
            bool savePathFound = !string.IsNullOrEmpty(savePath);
            bool versionFound = !string.IsNullOrEmpty(gameVersion);

            if (savePathFound)
            {
                pathInputField.text = savePath;
                Debug.Log($"savePathFound {savePath}");
                updateManager.GetExeVersion(exePath);
            }

            if (versionFound)
            {
                gameVersionLabel.text = gameVersion;
                Debug.Log($"savePathFound {gameVersion}");
            }

            string projectVersion = ConfigManager.LoadSetting("Version", "Engine_Version");
            ConfigManager.SaveSetting("Version", "Engine_Version", projectVersion);
            engineVersion.text = projectVersion;
            Debug.Log($"engineVersion set {projectVersion}");
            return savePathFound && versionFound;
        }

        private void SpawnPopup()
        {
            GameObject popupInstance = Instantiate(popupPrefab, Vector3.zero, Quaternion.identity);
            // Now find the CanvasGroup in the instantiated popup
            popupCanvasGroup = popupInstance.GetComponent<CanvasGroup>();
            if (popupCanvasGroup != null)
            {
                // Make the popup fully visible and interactive immediately upon spawning
                popupCanvasGroup.alpha = 1.0f; // Fully opaque
                popupCanvasGroup.interactable = true; // Enable interaction
                popupCanvasGroup.blocksRaycasts = true; // Block raycasts
            }
            else
            {
                Debug.LogError("Popup Prefab does not have a CanvasGroup component.");
            }
        }

        public void OpenSetPathDialog()
        {
            // Open folder browser and then save the selected path
            var paths = StandaloneFileBrowser.OpenFolderPanel("Select Dying Light 2 Root Folder", "", false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                string path = paths[0];
                SavePathToConfig(path);
                if (pathInputField != null)
                {
                    pathInputField.text = path; // Update the input field with the selected path
                }
                Debug.Log($"Path set and saved: {path}");

                updateManager.SaveVersionInfo(path);
            }
            else
            {
                Debug.LogError("No path selected.");
            }
        }


        private void SavePathToConfig(string newPath)
        {
            ConfigManager.SaveSetting("SavePath", "Path", newPath);
            Debug.Log($"Path saved to config: {newPath}");
        }

    }
}