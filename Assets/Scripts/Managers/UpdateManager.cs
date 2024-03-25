using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

namespace doppelganger
{
    public class UpdateManager : MonoBehaviour
    {
        public DataManager datamanager;
        public RunTimeDataBuilder runTimeDataBuilder;

        public AudioSource audioSource;
        private bool extractionCompleted = false;
        private String gameVersion;
        public AudioClip updatingClip;
        public AudioClip finishedClip;
        public TMP_Text updateHeader;
        public TMP_Text updatesettingsHeader;
        public GameObject updateCanvasGroup;

        public void SaveVersionInfo(string rootPath)
        {
            string exePath = Path.Combine(rootPath, "ph", "work", "bin", "x64", "DyingLightGame_x64_rwdi.exe");
            string exeVersion = GetExeVersion(exePath);

            ConfigManager.SaveSetting("Version", "DL2_Game", exeVersion);
            string projectVersion = ConfigManager.LoadSetting("Version", "Engine_Version");
            gameVersion = exeVersion;
            //datamanager.gameVersionLabel.text = exeVersion.Replace("na", "");
            datamanager.engineVersion.text = projectVersion;
        }

        public void VersionCheck()
        {
            string savePath = ConfigManager.LoadSetting("SavePath", "Path");
            string storedGameVersion = ConfigManager.LoadSetting("Version", "DL2_Game");
            string exePath = Path.Combine(savePath, "ph", "work", "bin", "x64", "DyingLightGame_x64_rwdi.exe"); // Ensure savePath is used
            string exeVersion = GetExeVersion(exePath);
            //datamanager.gameVersionLabel.text = exeVersion;
            // Check if the stored game version matches the exe version
            if (!string.Equals(storedGameVersion, exeVersion))
            {
                ShowUpdatePopup();
            }
            else
            {
                // Optionally, handle the case where versions match
                Debug.Log("Game version matches the stored version.");
            }
        }

        private void ShowUpdatePopup()
        {
            if (updateCanvasGroup != null)
            {
                Animator animator = updateCanvasGroup.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = true;
                    animator.SetBool("Active", true);
                }
                else
                {
                    Debug.LogError("Popup Prefab does not have an Animator component.");
                }
            }
            else
            {
                Debug.LogError("Popup Prefab does not have a CanvasGroup component.");
            }
        }

        public string GetExeVersion(string path)
        {
            Debug.Log($"GetExeVersion path: {path}");
            try
            {
                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
                Debug.Log($"GetExeVersion path: {versionInfo.FileVersion}");
                //datamanager.gameVersionLabel.text = versionInfo.FileVersion.Replace("na", "");
                gameVersion = versionInfo.FileVersion;
                return versionInfo.FileVersion;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get .exe version: {e.Message}");
                return string.Empty;
            }
        }

        public void RunUpdate()
        {
            PlayFirstClip();
            updatesettingsHeader.text = "Updating Data...";
            float totalDelay = 0.1f;
            Invoke("DelayedUpdateCode", totalDelay);
        }

        void DelayedUpdateCode()
        {
            string rootPath = ConfigManager.LoadSetting("SavePath", "Path");
            UpdateCode(rootPath);
        }

        void PlayFirstClip()
        {
            if (audioSource != null && updatingClip != null)
            {
                audioSource.Stop();
                audioSource.clip = updatingClip;
                audioSource.Play();
            }
            else
            {
                Debug.LogWarning("AudioSource or updatingClip not assigned.");
            }
        }

        public void PlayFinishedClip()
        {
            // Check if the audio source and the second clip are assigned
            if (audioSource != null && finishedClip != null)
            {
                audioSource.Stop();
                audioSource.clip = finishedClip;
                audioSource.Play();
            }
            else
            {
                Debug.LogWarning("AudioSource or secondClip not assigned.");
            }
        }

        public void UpdateCode(string rootPath)
        {
            Debug.Log("Started Update.");
            string dataSourcePath = Path.Combine(rootPath, "ph", "source", "data0.pak");
            Debug.Log($"dataSourcePath found {dataSourcePath}");

            string updateVersion = gameVersion.Replace("na", "");
            string tempPath = Path.Combine(Application.persistentDataPath, "update", updateVersion);

            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
            Directory.CreateDirectory(tempPath);
            Debug.Log($"tempPath for models created {tempPath}");

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(dataSourcePath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.StartsWith("models/", StringComparison.OrdinalIgnoreCase))
                        {
                            string destinationPath = Path.GetFullPath(Path.Combine(tempPath, entry.FullName));

                            if (entry.FullName.EndsWith("/"))
                            {
                                Directory.CreateDirectory(destinationPath);
                            }
                            else
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                                entry.ExtractToFile(destinationPath, true);
                            }
                        }
                    }
                }

                Debug.Log("Successfully extracted models folder.");
                extractionCompleted = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to extract models from {dataSourcePath} to {tempPath}: {ex.Message}");
                extractionCompleted = false;
            }

            // Proceed to process the data if extraction was successful
            if (extractionCompleted)
            {
                ProcessData(tempPath);
            }
        }

        private void ProcessData(string tempPath)
        {
            if (runTimeDataBuilder == null)
            {
                Debug.LogError("runTimeDataBuilder is not initialized.");
                return;
            }

            runTimeDataBuilder.ProcessModelsInFolder(tempPath);
            ConfigManager.SaveSetting("Version", "DL2_Game", gameVersion);
            ConfigManager.SaveSetting("Version", "ProcessedVersion", gameVersion);

            Debug.Log("Data processing completed.");
            PlayFinishedClip();
            updateHeader.text = "Update complete!";
            Animator animator = updateCanvasGroup.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = true;
                animator.SetBool("Active", false);
            }
            updatesettingsHeader.text = "Update Data";
            VersionCheck();
        }
    }
}