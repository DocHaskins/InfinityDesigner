using UnityEngine;
using System.IO;
using System.IO.Compression;
using System;

namespace doppelganger
{
    public class ZipUtility
    {
        // Method to compress files into a zip archive with a specified subfolder for the model file
        public static void AddOrUpdateFilesInZip(string filePath, string outputZipPath, string fileNameWithinZip)
        {
            // Temporary directory to stage zip contents
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                if (File.Exists(outputZipPath))
                {
                    // Extract existing zip
                    ZipFile.ExtractToDirectory(outputZipPath, tempDir);
                }

                // Create "models" subfolder in the temporary directory if not working with the placeholder file
                string targetPath;
                if (!fileNameWithinZip.Equals("PLACEHOLDER_InfinityDesigner.file"))
                {
                    string modelsDirectory = Path.Combine(tempDir, "models");
                    Directory.CreateDirectory(modelsDirectory);
                    // Determine the target path for the file within the "models" subfolder
                    targetPath = Path.Combine(modelsDirectory, fileNameWithinZip);
                }
                else
                {
                    // If adding the placeholder, it should be at the top level of the zip
                    targetPath = Path.Combine(tempDir, fileNameWithinZip);
                }

                // Add or update the file at the determined path
                File.Copy(filePath, targetPath, true);

                // Before zipping, ensure the placeholder file exists at the topmost level
                string placeholderPath = Path.Combine(tempDir, "PLACEHOLDER_InfinityDesigner.file");
                if (!File.Exists(placeholderPath))
                {
                    // Create an empty placeholder file
                    File.WriteAllText(placeholderPath, "");
                }

                // Recreate the zip, now including the placeholder at the topmost level along with any other contents
                if (File.Exists(outputZipPath))
                {
                    File.Delete(outputZipPath); // Delete the old zip if it exists
                }
                ZipFile.CreateFromDirectory(tempDir, outputZipPath);
            }
            finally
            {
                // Clean up the temporary directory
                Directory.Delete(tempDir, true);
            }
        }

        public static bool ZipContainsFile(string zipPath, string fileName)
        {
            if (!File.Exists(zipPath))
            {
                return false;
            }

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true; // Found the file
                    }
                }
            }

            return false; // File not found
        }
    }
}