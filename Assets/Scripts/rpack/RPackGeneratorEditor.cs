using UnityEditor;
using UnityEngine;
using SFB; // Standalone File Browser
using System;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class RPackGeneratorEditor : EditorWindow
{
    private string selectedRpackPath = "";
    private string selectedOutputFolder = "";

    [MenuItem("Tools/RPack Generator")]
    public static void ShowWindow()
    {
        GetWindow<RPackGeneratorEditor>("RPack Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("RPack Parser and Generator", EditorStyles.boldLabel);

        if (GUILayout.Button("Select RPack File for Parsing"))
        {
            var paths = StandaloneFileBrowser.OpenFilePanel("Select RPack File", "", "rpack", false);
            if (paths.Length > 0)
            {
                selectedRpackPath = paths[0];
                ParseAndGenerateRPackTemplate(selectedRpackPath);
            }
        }

        if (GUILayout.Button("Select Folder for RPack Generation"))
        {
            string path = StandaloneFileBrowser.OpenFolderPanel("Select Folder for RPack Generation", "", false)[0];
            if (!string.IsNullOrEmpty(path))
            {
                selectedOutputFolder = path;
                GenerateRPackFromFolder(selectedOutputFolder);
            }
        }

        if (!string.IsNullOrEmpty(selectedRpackPath))
        {
            EditorGUILayout.LabelField("Selected RPack:", selectedRpackPath);
        }

        if (!string.IsNullOrEmpty(selectedOutputFolder))
        {
            EditorGUILayout.LabelField("Selected Folder:", selectedOutputFolder);
        }
    }

    private void GenerateRPackFromFolder(string folderPath)
    {
        try
        {
            // Gather all PNG files from the selected folder
            string[] pngFiles = Directory.GetFiles(folderPath, "*.png");
            List<RPackRuntimeGenerator.FilePart> fileParts = new List<RPackRuntimeGenerator.FilePart>();

            foreach (string pngFile in pngFiles)
            {
                // Read the content of each PNG file
                byte[] fileContent = File.ReadAllBytes(pngFile);

                // Create a FilePart object for each PNG file
                RPackRuntimeGenerator.FilePart filePart = new RPackRuntimeGenerator.FilePart
                {
                    Type = RPackRuntimeGenerator.TEXTURE, // Assuming all PNG files are texture type
                    FileName = Path.GetFileName(pngFile),
                    PartData = new List<byte[]> { fileContent }, // Add the file content as a part
                    Size = fileContent.Length // Set the size of the part
                                              // Note: You might need to calculate Offset based on your RPack format requirements
                };

                fileParts.Add(filePart);
            }

            // Generate the RPack with the collected file parts
            string newRpackPath = Path.Combine(folderPath, "newGeneratedRPack.rpack");
            RPackRuntimeGenerator.CreateRPack(newRpackPath, fileParts); // Pass actual file parts data
            Debug.Log($"New RPack generated at {newRpackPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error generating RPack: {ex.Message}");
        }
    }

    private void ParseAndGenerateRPackTemplate(string rpackPath)
    {
        try
        {
            RPackParser parser = new RPackParser();
            parser.ParseRPack(rpackPath);
            GenerateRPackGuide(parser);
            Debug.Log("RPack parsed successfully. Template generated.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing RPack: {ex.Message}");
        }
    }

    private void GenerateRPackGuide(RPackParser parser)
    {
        var template = new
        {
            parser.MagicID,
            parser.Version,
            parser.Flags,
            parser.TotalParts,
            parser.TotalSections,
            parser.TotalFiles,
            parser.FileNames,
            parser.FileParts
        };

        string templateJson = JsonConvert.SerializeObject(template, Formatting.Indented);
        Debug.Log(templateJson);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, "RPackTemplate.json"), templateJson);
        Debug.Log($"RPack guide generated: {Application.persistentDataPath}/RPackTemplate.json");
    }
}