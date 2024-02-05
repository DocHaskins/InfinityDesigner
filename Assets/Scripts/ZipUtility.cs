using UnityEngine;
using System.IO;
using System.IO.Compression;

namespace doppelganger
{
    public class ZipUtility
    {
        // Method to compress files into a zip archive with a specified subfolder for the model file
        public static void AddOrUpdateFilesInZip(string modelFilePath, string outputZipPath)
        {
            // Check if the output zip (pak) file already exists
            if (File.Exists(outputZipPath))
            {
                // Extract the existing zip to a temporary directory
                string tempExtractPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                ZipFile.ExtractToDirectory(outputZipPath, tempExtractPath);

                try
                {
                    // Add or update the model file in the "models" subfolder of the extracted directory
                    string modelsDirectory = Path.Combine(tempExtractPath, "models");
                    Directory.CreateDirectory(modelsDirectory); // Ensure the models directory exists
                    string modelFileName = Path.GetFileName(modelFilePath);
                    File.Copy(modelFilePath, Path.Combine(modelsDirectory, modelFileName), true);

                    // Delete the old zip file
                    File.Delete(outputZipPath);

                    // Create a new zip file from the updated temporary directory
                    ZipFile.CreateFromDirectory(tempExtractPath, outputZipPath);
                    Debug.Log("Updated zip archive at " + outputZipPath);
                }
                finally
                {
                    // Clean up: Delete the temporary extract directory
                    Directory.Delete(tempExtractPath, true);
                }
            }
            else
            {
                // If the pak file doesn't exist, just create a new one
                CompressFilesIntoZip(modelFilePath, outputZipPath);
            }
        }

        public static void CompressFilesIntoZip(string modelFilePath, string outputZipPath)
        {
            // Create a temporary directory
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);

            try
            {
                string modelsDirectory = Path.Combine(tempDirectory, "models");
                Directory.CreateDirectory(modelsDirectory);
                string modelFileName = Path.GetFileName(modelFilePath);
                string tempModelPath = Path.Combine(modelsDirectory, modelFileName);
                File.Copy(modelFilePath, tempModelPath, true);

                ZipFile.CreateFromDirectory(tempDirectory, outputZipPath);
                Debug.Log("Zip archive created at " + outputZipPath);
            }
            finally
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }
}