using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PreBuildScript : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "config.ini");
        string version = PlayerSettings.bundleVersion;
        WriteVersionToConfig(configPath, version);
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
                if (i + 1 < lines.Count)
                {
                    lines[i + 1] = $"Engine_Version={version}";
                    versionSectionFound = true;
                    break;
                }
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