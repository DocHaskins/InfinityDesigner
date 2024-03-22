using Newtonsoft.Json;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using UnityEngine;

namespace doppelganger
{
    public class PresetManager : MonoBehaviour
    {
        public PresetScroller presetScroller;

        public void OpenInFileBrowser()
        {
            if (string.IsNullOrEmpty(presetScroller.currentPath))
            {
                UnityEngine.Debug.LogWarning("Current path is empty.");
                return;
            }

            // Correct the path format for Windows
            string correctedPath = presetScroller.currentPath.Replace('/', '\\');

            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            {
                try
                {
                    Process.Start("explorer.exe", $"/select,\"{correctedPath}\"");
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Failed to open directory: {e.Message}");
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"Unsupported platform: {Application.platform}");
            }
        }

        public void BrowseAndLoadJsons()
        {
            string customFolderPath = Path.Combine(Application.streamingAssetsPath, "Jsons", "Custom");
            if (!Directory.Exists(customFolderPath))
            {
                Directory.CreateDirectory(customFolderPath);
            }

            string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);

            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                UnityEngine.Debug.Log($"Selected folder: {paths[0]}");

                List<string> jsonFiles = new List<string>();
                try
                {
                    jsonFiles.AddRange(Directory.GetFiles(paths[0], "*.json", SearchOption.AllDirectories));
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Error while loading JSON files: {e.Message}");
                }

                foreach (string jsonFile in jsonFiles)
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(jsonFile);
                    string pngFile = Path.ChangeExtension(jsonFile, ".png");

                    if (File.Exists(pngFile))
                    {
                        string destJson = Path.Combine(customFolderPath, Path.GetFileName(jsonFile));
                        string destPng = Path.Combine(customFolderPath, Path.GetFileName(pngFile));

                        File.Copy(jsonFile, destJson, true);
                        File.Copy(pngFile, destPng, true);
                    }
                }
                presetScroller.RefreshCustomPresets(customFolderPath);
            }
        }

        public void InstallZips()
        {
            string baseCustomFolderPath = Path.Combine(Application.streamingAssetsPath, "Jsons", "Custom");
            UnityEngine.Debug.Log($"Checking or creating base custom folder path: {baseCustomFolderPath}");

            if (!Directory.Exists(baseCustomFolderPath))
            {
                Directory.CreateDirectory(baseCustomFolderPath);
                UnityEngine.Debug.Log($"Created base custom folder path: {baseCustomFolderPath}");
            }

            string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                UnityEngine.Debug.Log($"Selected folder for zip files: {paths[0]}");
                List<string> zipFiles = new List<string>();
                try
                {
                    zipFiles.AddRange(Directory.GetFiles(paths[0], "*.zip", SearchOption.AllDirectories));
                    UnityEngine.Debug.Log($"Found {zipFiles.Count} zip files in selected directory.");
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Error while loading ZIP files: {e.Message}");
                    return;
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
                                    string customFolderPath = Path.Combine(baseCustomFolderPath, userName);
                                    if (category != "ALL")
                                    {
                                        customFolderPath = Path.Combine(customFolderPath, category);
                                    }
                                    if (cls != "ALL")
                                    {
                                        customFolderPath = Path.Combine(customFolderPath, cls);
                                    }

                                    if (!Directory.Exists(customFolderPath))
                                    {
                                        Directory.CreateDirectory(customFolderPath);
                                        //Debug.Log($"Created custom folder path for extraction: {customFolderPath}");
                                    }

                                    List<string> filesToDelete = new List<string>();
                                    foreach (ZipArchiveEntry entry in archive.Entries)
                                    {
                                        string destinationPath = Path.Combine(customFolderPath, entry.FullName);
                                        if (!entry.FullName.Equals("PLACEHOLDER_InfinityDesigner_json.file", StringComparison.OrdinalIgnoreCase) &&
                                            !entry.FullName.Equals("install.json", StringComparison.OrdinalIgnoreCase))
                                        {
                                            entry.ExtractToFile(destinationPath, true);
                                            //Debug.Log($"Extracted file: {entry.FullName} to {destinationPath}");
                                        }
                                        else
                                        {
                                            filesToDelete.Add(destinationPath);
                                        }
                                    }
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
                                UnityEngine.Debug.LogError("install.json not found within: " + zipFile);
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.Log("Placeholder file not found in: " + zipFile);
                        }
                    }
                }
                presetScroller.RefreshCustomPresets(baseCustomFolderPath);
                presetScroller.LoadPresets();
            }
        }
    }
}