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
            string projectVersion;

            // Use UNITY_EDITOR directive to differentiate between editor and build environments
#if UNITY_EDITOR
            // If running in the editor, use the project settings version
            projectVersion = Application.version; // Getting Unity project version
            ConfigManager.SaveSetting("Version", "Engine_Version", projectVersion);
#else
    // If running in the built application, read the version from config.ini
    projectVersion = ConfigManager.LoadSetting("Version", "Engine_Version");
#endif

            Debug.Log($"gameVersion {projectVersion}, savePath {savePath}.");
            bool savePathFound = !string.IsNullOrEmpty(savePath);
            bool outputPathFound = !string.IsNullOrEmpty(outputPath);
            bool contentPathFound = !string.IsNullOrEmpty(contentPath);
            bool versionFound = !string.IsNullOrEmpty(projectVersion);

            if (!savePathFound || !versionFound)
            {
                Debug.LogError("Either save path or game version is not set.");
                return false;
            }

            if (savePathFound)
            {
                pathInputField.text = savePath;
                Debug.Log($"savePathFound {savePath}");
                string exePath = Path.Combine(new string[] { savePath, "ph", "work", "bin", "x64", "DyingLightGame_x64_rwdi.exe" });
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
                Debug.Log($"versionFound {projectVersion}");
                engineVersion.text = projectVersion;
            }

            updateManager.VersionCheck();
            return true;
        }

        private void SpawnPopup()
        {
            if (popupCanvasGroup != null)
            {
                popupCanvasGroup.alpha = 1.0f;
                popupCanvasGroup.interactable = true;
                popupCanvasGroup.blocksRaycasts = true;
            }
            else
            {
                Debug.LogError("Popup Prefab does not have a CanvasGroup component.");
            }
        }

        public void OpenSetPathDialog()
        {
            var paths = StandaloneFileBrowser.OpenFolderPanel("Select Dying Light 2 Root Folder", "", false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                string path = paths[0];
                string exePath = Path.Combine(path, "ph", "work", "bin", "x64", "DyingLightGame_x64_rwdi.exe");

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