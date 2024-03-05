using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using doppelganger;
using System.IO.Compression;
using System;

[System.Serializable]
public class FileModification
{
    public string filePath;
    public List<LineModification> modifications;
}

[System.Serializable]
public class LineModification
{
    public string searchTerm;
    public string replaceWith;
    public List<string> appendLines;
}

[System.Serializable]
public class SyntaxPattern
{
    public string key;
    public string pattern;
}

[System.Serializable]
public class SyntaxPatternList
{
    public List<SyntaxPattern> patterns = new List<SyntaxPattern>();
}

public class AdvancedFileEditor : MonoBehaviour
{
    public bool ExtractData = false;
    public bool loadDefs = true;
    public string defFilesDirectory;
    public List<FileModification> fileModifications;
    private Dictionary<string, string> syntaxPatterns = new Dictionary<string, string>();

    void Start()
    {
        if (ExtractData)
        {
            string defLocation = ConfigManager.LoadSetting("SavePath", "Path");
            Task.Run(() => UpdateCode(defLocation)) // Run UpdateCode in a new task
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"Extraction failed: {task.Exception}");
                    }
                    else
                    {
                        Debug.Log("Extraction completed, loading .Def files...");
                        UnityMainThreadDispatcher.Instance.Enqueue(() => LoadDefFiles());
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        else
        {
            defFilesDirectory = ConfigManager.LoadSetting("SavePath", "ExtractionPath");
            LoadDefFiles();
        }
    }

    async Task UpdateCode(string rootPath)
    {
        string dataSourcePath = Path.Combine(rootPath, "ph", "source", "data0.pak");
        Debug.Log($"dataSourcePath found {dataSourcePath}");

        // Update the tempPath to the new specified path
        string extractedDataPath = Path.Combine(rootPath, "IALR_Installer", "Extracted", "Data0");

        if (Directory.Exists(extractedDataPath))
        {
            Directory.Delete(extractedDataPath, true);
        }
        Directory.CreateDirectory(extractedDataPath);
        Debug.Log($"Extracted path for data0 created {extractedDataPath}");

        List<string> foldersToExtract = new List<string> { "ai/", "ailife/", "presets/", "scripts/", "spawn/" };

        try
        {
            using (ZipArchive archive = ZipFile.OpenRead(dataSourcePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // Check if the entry is in one of the folders we're interested in
                    bool shouldExtract = foldersToExtract.Any(folder => entry.FullName.StartsWith(folder, StringComparison.OrdinalIgnoreCase));
                    if (shouldExtract)
                    {
                        string destinationPath = Path.GetFullPath(Path.Combine(extractedDataPath, entry.FullName));

                        if (entry.FullName.EndsWith("/")) // Check if it is a directory
                        {
                            if (!Directory.Exists(destinationPath))
                            {
                                Directory.CreateDirectory(destinationPath);
                            }
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)); // Ensure the directory exists
                            entry.ExtractToFile(destinationPath, true);
                        }
                    }
                }
            }

            Debug.Log("Successfully extracted specified folders from data0.pak files.");
            defFilesDirectory = extractedDataPath; // Update the directory to the new extracted data path

            // Save the new extracted data path into the config
            ConfigManager.SaveSetting("SavePath", "ExtractionPath", extractedDataPath);
            Debug.Log($"ExtractedDataPath saved in config: {extractedDataPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to extract specific folders from {dataSourcePath} to {extractedDataPath}: {ex.Message}");
        }
    }

    void LoadDefFiles()
    {
        Debug.Log("Starting to load .Def files...");
        // Search the directory and all subdirectories for .Def files
        string[] defFilePaths = Directory.GetFiles(defFilesDirectory, "*.Def", SearchOption.AllDirectories);
        Debug.Log($"Found {defFilePaths.Length} .Def files to process.");

        SyntaxPatternList syntaxPatternList = new SyntaxPatternList();

        foreach (string filePath in defFilePaths)
        {
            Debug.Log($"Processing .Def file: {filePath}");
            // Assuming each .Def file contains lines in the format: key=regex_pattern
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2)
                {
                    SyntaxPattern newPattern = new SyntaxPattern
                    {
                        key = parts[0].Trim(),
                        pattern = parts[1].Trim()
                    };
                    syntaxPatternList.patterns.Add(newPattern);
                    Debug.Log($"Added new pattern: {newPattern.key} = {newPattern.pattern}");
                }
            }
        }

        // Convert syntaxPatterns list to JSON
        string json = JsonUtility.ToJson(syntaxPatternList, true);
        Debug.Log("Converted syntax patterns to JSON.");

        // Store the JSON string - for example, in a file
        string outputPath = Path.Combine(defFilesDirectory, "syntaxPatterns.json");
        File.WriteAllText(outputPath, json);
        Debug.Log($"JSON stored at: {outputPath}");
    }

    void ApplyModifications()
    {
        // Modify this function as before, but integrate regex/syntax parsing using syntaxPatterns
        foreach (FileModification fileMod in fileModifications)
        {
            ModifyFile(fileMod);
        }
    }

    void ModifyFile(FileModification fileMod)
    {
        // Check if file exists
        if (File.Exists(fileMod.filePath))
        {
            List<string> newFileContent = new List<string>();
            string[] lines = File.ReadAllLines(fileMod.filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                bool lineModified = false;

                foreach (LineModification lineMod in fileMod.modifications)
                {
                    // If the line contains the search term
                    if (line.Contains(lineMod.searchTerm))
                    {
                        // Replace the line if necessary
                        if (!string.IsNullOrEmpty(lineMod.replaceWith))
                        {
                            line = line.Replace(lineMod.searchTerm, lineMod.replaceWith);
                        }

                        // Append new lines if necessary
                        if (lineMod.appendLines != null && lineMod.appendLines.Count > 0)
                        {
                            // Assuming you want to append inside brackets
                            int bracketIndex = line.IndexOf('}'); // Find the closing bracket
                            if (bracketIndex != -1)
                            {
                                // Insert new lines before the closing bracket
                                string beforeBracket = line.Substring(0, bracketIndex);
                                string afterBracket = line.Substring(bracketIndex);
                                line = beforeBracket + string.Join("\n", lineMod.appendLines) + "\n" + afterBracket;
                            }
                        }

                        lineModified = true;
                    }
                }

                // Add the modified or original line to new content
                newFileContent.Add(line);

                // If the line was modified and had lines to be appended, we might skip adding them again as they are already included
                if (lineModified)
                {
                    continue;
                }
            }

            // Write the new content back to the file
            File.WriteAllLines(fileMod.filePath, newFileContent.ToArray());
        }
        else
        {
            Debug.LogError("File not found: " + fileMod.filePath);
        }
    }
}
