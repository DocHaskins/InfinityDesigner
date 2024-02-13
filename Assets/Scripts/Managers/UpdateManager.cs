using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;

namespace doppelganger
{
    public class UpdateManager : MonoBehaviour
    {
        public DataManager datamanager;
        public RunTimeDataBuilder runTimeDataBuilder;
        private bool extractionCompleted = false;
        private String gameVersion;

        public void SaveVersionInfo(string rootPath)
        {
            string exePath = Path.Combine(rootPath, "ph", "work", "bin", "x64", "DyingLightGame_x64_rwdi.exe");
            string exeVersion = GetExeVersion(exePath);

            ConfigManager.SaveSetting("Version", "DL2_Game", exeVersion);
            string projectVersion = ConfigManager.LoadSetting("Version", "Engine_Version");
            gameVersion = exeVersion;
            datamanager.gameVersionLabel.text = exeVersion.Replace("na", "");
            datamanager.engineVersion.text = projectVersion;
        }

        public string GetExeVersion(string path)
        {
            Debug.Log($"GetExeVersion path: {path}");
            try
            {
                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
                Debug.Log($"GetExeVersion path: {versionInfo.FileVersion}");
                datamanager.gameVersionLabel.text = versionInfo.FileVersion.Replace("na", "");
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
            string rootPath = ConfigManager.LoadSetting("SavePath", "Path");
            UpdateCode(rootPath);
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
            ConfigManager.SaveSetting("Version", "ProcessedVersion", gameVersion);
            Debug.Log("Data processing completed.");
        }
    }
}