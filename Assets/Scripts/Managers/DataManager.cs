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
        public TMP_InputField customOutputPathInputField;
        public TMP_InputField customContentPathInputField;
        public TMP_Text gameVersionLabel;
        public TMP_Text engineVersion;
        public GameObject popupPrefab;
        public CanvasGroup popupCanvasGroup;

        void Start()
        {
            if (!IsGameVersionAndPathSet())
            {
                Debug.LogError("Game version or path is not correctly set. Showing popup.");
                SpawnPopup();
            }
            else
            {
                Debug.Log("savePathFound and versionFound");
            }
        }

        private bool IsGameVersionAndPathSet()
        {
            Debug.Log("IsGameVersionAndPathSet check");
            string savePath = ConfigManager.LoadSetting("SavePath", "Path");
            string outputPath = ConfigManager.LoadSetting("SavePath", "Output_Path");
            string contentPath = ConfigManager.LoadSetting("SavePath", "Content_Path");
            string gameVersion = ConfigManager.LoadSetting("Version", "DL2_Game");

            Debug.Log($"gameVersion {gameVersion}, savePath {savePath}.");
            // Check if both settings are found and not empty
            bool savePathFound = !string.IsNullOrEmpty(savePath);
            bool outputPathFound = !string.IsNullOrEmpty(savePath);
            bool contentPathFound = !string.IsNullOrEmpty(savePath);
            bool versionFound = !string.IsNullOrEmpty(gameVersion);

            if (!savePathFound || !versionFound)
            {
                Debug.LogError("Either save path or game version is not set.");
                return false;
            }

            // Proceed with path combination only if the savePath is valid
            string exePath = Path.Combine(new string[] { savePath, "ph", "work", "bin", "x64", "DyingLightGame_x64_rwdi.exe" });

            if (savePathFound)
            {
                pathInputField.text = savePath;
                Debug.Log($"savePathFound {savePath}");
                updateManager.GetExeVersion(exePath);
                
            }

            if (outputPathFound)
            {
                customOutputPathInputField.text = outputPath;
                Debug.Log($"outputPathFound {outputPath}");
            }

            if (contentPathFound)
            {
                customContentPathInputField.text = contentPath;
                Debug.Log($"contentPathFound {contentPath}");
            }

            if (versionFound)
            {
                Debug.Log($"versionFound {gameVersion}");
            }

            string projectVersion = ConfigManager.LoadSetting("Version", "Engine_Version");
            ConfigManager.SaveSetting("Version", "Engine_Version", projectVersion);
            engineVersion.text = projectVersion;
            Debug.Log($"engineVersion set {projectVersion}");
            updateManager.VersionCheck();
            return true;
        }

        private void SpawnPopup()
        {
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

                // Attempt to find the executable in the expected directory structure
                string exePath = Path.Combine(path, "ph", "work", "bin", "x64", "DyingLightGame_x64_rwdi.exe");

                // Check if the executable exists
                if (File.Exists(exePath))
                {
                    SavePathToConfig(path);
                    if (pathInputField != null)
                    {
                        pathInputField.text = path;
                        popupCanvasGroup.alpha = 0.0f;
                        popupCanvasGroup.interactable = false;
                        popupCanvasGroup.blocksRaycasts = false;
                    }
                    Debug.Log($"Path set and saved: {path}");

                    updateManager.SaveVersionInfo(path);
                }
                else
                {
                    pathInputField.text = "Set Path";
                    Debug.LogError($"Dying Light 2 executable not found, make sure this is the Dying Light 2 Root folder");
                }
            }
            else
            {
                Debug.LogError("No path selected.");
            }
        }

        public void OpenSetOutputPathDialog()
        {
            // Open folder browser and then save the selected path
            var paths = StandaloneFileBrowser.OpenFolderPanel("Select Custom Output Folder", "", false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                string Outputpath = paths[0];
                customOutputPathInputField.text = Outputpath;
                ConfigManager.SaveSetting("SavePath", "Output_Path", Outputpath);
            }
            else
            {
                Debug.LogError("No path selected.");
            }
        }

        public void OpenSetContentPathDialog()
        {
            // Open folder browser and then save the selected path
            var paths = StandaloneFileBrowser.OpenFolderPanel("Select Custom Content Folder", "", false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                string Contentpath = paths[0];
                customContentPathInputField.text = Contentpath;
                ConfigManager.SaveSetting("SavePath", "Content_Path", Contentpath);
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