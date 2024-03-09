using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class PreBuildScript : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    private string _sessionSavePath = Path.Combine(Application.dataPath, "StreamingAssets", "discordSession.json");

    public void OnPreprocessBuild(BuildReport report)
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "config.ini");
        string version = PlayerSettings.bundleVersion;

        ClearSpecificConfigData(configPath);
        DestroyDiscordSession();
        WriteVersionToConfig(configPath, version);
    }

    private void ClearSpecificConfigData(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return; // If the config file doesn't exist, there's nothing to clear
        }

        List<string> lines = new List<string>(File.ReadAllLines(configPath));
        List<string> keysToRemove = new List<string> { "Path=", "DL2_Game=", "ProcessedVersion=", "Name=", "ID=" };

        // Iterate through the lines in reverse to safely remove items without affecting the iteration
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            foreach (var key in keysToRemove)
            {
                if (lines[i].Contains(key))
                {
                    lines.RemoveAt(i);
                    break; // Break after removal to avoid multiple checks for a single line
                }
            }
        }

        // Rewrite the modified lines back to the file
        File.WriteAllLines(configPath, lines.ToArray());
    }

    private void DestroyDiscordSession()
    {
        // Check if the file exists
        if (File.Exists(_sessionSavePath))
        {
            // Delete the file
            File.Delete(_sessionSavePath);
            Debug.Log("Discord session destroyed.");
        }
        else
        {
            Debug.Log("No Discord session to destroy.");
        }
    }

    private void WriteVersionToConfig(string configPath, string version)
    {
        // Ensure the StreamingAssets folder exists
        if (!Directory.Exists(Application.streamingAssetsPath))
        {
            Directory.CreateDirectory(Application.streamingAssetsPath);
        }

        // Write or append version to config.ini
        List<string> lines = File.Exists(configPath) ? new List<string>(File.ReadAllLines(configPath)) : new List<string>();
        bool versionSectionFound = false;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim().Equals("[Version]", System.StringComparison.InvariantCultureIgnoreCase))
            {
                versionSectionFound = true;
                int searchIndex = i + 1;
                bool engineVersionLineFound = false;
                while (searchIndex < lines.Count && !lines[searchIndex].StartsWith("["))
                {
                    if (lines[searchIndex].StartsWith("Engine_Version=", System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        lines[searchIndex] = $"Engine_Version={version}";
                        engineVersionLineFound = true;
                        break;
                    }
                    searchIndex++;
                }
                if (!engineVersionLineFound)
                {
                    lines.Insert(searchIndex, $"Engine_Version={version}");
                }
                break;
            }
        }

        if (!versionSectionFound)
        {
            lines.Add("[Version]");
            lines.Add($"Engine_Version={version}");
        }

        File.WriteAllLines(configPath, lines.ToArray());
    }
}