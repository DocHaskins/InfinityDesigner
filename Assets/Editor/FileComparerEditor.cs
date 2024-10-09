using UnityEngine;
using UnityEditor;
using System.IO;
using SFB;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

public class FileComparerEditor : EditorWindow
{
    string filePath1 = "";
    string filePath2 = "";

    [MenuItem("Tools/File Comparer")]
    public static void ShowWindow()
    {
        GetWindow<FileComparerEditor>("File Comparer");
    }

    void OnGUI()
    {
        GUILayout.Label("Compare Two Files", EditorStyles.boldLabel);

        if (GUILayout.Button("Select File 1"))
        {
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select File 1", "", "", false);
            if (paths.Length > 0) filePath1 = paths[0];
        }
        EditorGUILayout.LabelField("File Path 1", filePath1);

        if (GUILayout.Button("Select File 2"))
        {
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select File 2", "", "", false);
            if (paths.Length > 0) filePath2 = paths[0];
        }
        EditorGUILayout.LabelField("File Path 2", filePath2);

        if (GUILayout.Button("Compare"))
        {
            CompareFiles(filePath1, filePath2);
        }
    }

    void CompareFiles(string path1, string path2)
    {
        if (!File.Exists(path1) || !File.Exists(path2))
        {
            Debug.LogError("One or both files do not exist.");
            return;
        }

        // Read the files into lines
        var file1Lines = File.ReadAllLines(path1).ToList();
        var file2Lines = File.ReadAllLines(path2).ToList();

        // Placeholder for comparison results
        CompareData modelCompare = new CompareData();

        // Process sections from File 1
        var file1Sections = ExtractSections(file1Lines);
        var file2Sections = ExtractSections(file2Lines);

        foreach (var file1Section in file1Sections)
        {
            // Find corresponding section in File 2 by section name
            var file2Section = file2Sections.FirstOrDefault(s => s.Key == file1Section.Key);
            if (file2Section.Key == null || !file1Section.Value.SequenceEqual(file2Section.Value))
            {
                // Sections are different
                modelCompare.Differences.Add(new Difference
                {
                    SectionName = file1Section.Key,
                    File1Lines = file1Section.Value,
                    File2Lines = file2Section.Value ?? new List<string>() { "Section missing in File 2" }
                });
            }
        }

        // Check for sections in File 2 not present in File 1
        foreach (var file2Section in file2Sections)
        {
            if (!file1Sections.ContainsKey(file2Section.Key))
            {
                modelCompare.Differences.Add(new Difference
                {
                    SectionName = file2Section.Key,
                    File1Lines = new List<string>() { "Section missing in File 1" },
                    File2Lines = file2Section.Value
                });
            }
        }

        string json = JsonConvert.SerializeObject(modelCompare, Formatting.Indented);

        // Determine the output file name based on the first file name
        string outputFileName = Path.GetFileNameWithoutExtension(path1) + "_differences.json";
        string outputPath = Path.Combine(Application.streamingAssetsPath, "Out", outputFileName);

        // Ensure the Out directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        // Write the JSON string to the file
        File.WriteAllText(outputPath, json);

        Debug.Log($"Differences written to {outputPath}");
    }

    private Dictionary<string, List<string>> ExtractSections(List<string> lines)
    {
        var sections = new Dictionary<string, List<string>>();
        string currentSectionName = "";
        bool isCollecting = false;
        int bracketDepth = 0;
        bool skipMain = false; // Additional flag to handle skipping "Sub Main()" specifically

        Debug.Log("Starting to extract sections...");

        foreach (var line in lines)
        {
            string trimmedLine = line.Trim();
            bool isSubMainLine = trimmedLine.ToLower().Equals("sub main()");

            Debug.Log($"Processing line: {trimmedLine}");

            // Start or continue collecting when inside Sub Main or other sections
            if (trimmedLine.StartsWith("sub ") && trimmedLine.EndsWith("()"))
            {
                if (isSubMainLine)
                {
                    // Skip "Sub Main()" itself but mark to start collection within it
                    skipMain = true;
                    bracketDepth = 0; // Ensure depth is reset when entering Sub Main
                    Debug.Log("Skipping 'Sub Main()' section...");
                    continue;
                }

                // For sections other than Sub Main, reset state to start collection
                currentSectionName = trimmedLine;
                sections[currentSectionName] = new List<string>();
                isCollecting = true;
                bracketDepth = 0; // Reset depth for new section
                Debug.Log($"Starting new section: {currentSectionName}");
            }
            else if (skipMain || isCollecting)
            {
                // Adjust bracket depth for determining section scope
                bracketDepth += trimmedLine.Count(c => c == '{');
                bracketDepth -= trimmedLine.Count(c => c == '}');

                if (isCollecting)
                {
                    sections[currentSectionName].Add(line);
                    Debug.Log($"Adding line to section '{currentSectionName}': {line}");
                }

                // Once we reach the closing bracket of Sub Main or any section, stop collecting
                if (bracketDepth <= 0 && isCollecting)
                {
                    Debug.Log($"Closing section: {currentSectionName}");
                    isCollecting = false; // Stop collecting this section
                }
                else if (bracketDepth <= 0)
                {
                    // Exiting Sub Main, reset skipMain and continue to next iteration
                    skipMain = false;
                    Debug.Log("Exited 'Sub Main()' scope.");
                }
            }
        }
        return sections;
    }
}