using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace doppelganger
{
    public class ScreenshotManager : MonoBehaviour
    {
        public CharacterBuilder_InterfaceManager interfaceManager;
        public PresetScroller presetScroller;

        public RawImage currentPresetScreenshot_save;

        public Camera screenshotCamera;
        public Canvas canvasToExclude;
        public Rect captureArea;
        public bool debug = false;

        private void Update()
        {
            // Check if debug mode is enabled and F12 is pressed
            if (debug && Input.GetKeyDown(KeyCode.F5))
            {
                string currentPresetPath = interfaceManager.currentPresetPath;
                if (!string.IsNullOrEmpty(currentPresetPath))
                {
                    CaptureAndSaveScreenshot(currentPresetPath, interfaceManager.currentPreset);
                }
                else
                {
                    Debug.LogError("No preset is currently loaded.");
                }
            }
        }

        public void TakeScreenshot()
        {
            string currentPresetPath = interfaceManager.currentPresetPath;
            Debug.Log($"currentPresetPath {currentPresetPath}");
            if (!string.IsNullOrEmpty(currentPresetPath))
            {
                CaptureAndSaveScreenshot(currentPresetPath, interfaceManager.currentPreset);
            }
            else
            {
                Debug.LogError("No preset is currently loaded.");
            }
        }

        public void SetCurrentScreenshot()
        {
            string jsonPath = interfaceManager.currentPresetPath;
            string imagePath = Path.ChangeExtension(jsonPath, ".png");

            if (File.Exists(imagePath))
            {
                Texture2D texture = new Texture2D(2, 2);
                byte[] fileData = File.ReadAllBytes(imagePath);
                if (texture.LoadImage(fileData))
                {
                    currentPresetScreenshot_save.texture = texture;
                }
                else
                {
                    Debug.LogError("Failed to load image from path: " + imagePath);
                }
            }
            else
            {
                Debug.LogError("Image file not found at path: " + imagePath);
            }
        }

        public void DocumentPreset()
        {
            string currentPresetPath = interfaceManager.currentPresetPath;
            if (!string.IsNullOrEmpty(currentPresetPath))
            {
                CaptureAndSaveScreenshot(currentPresetPath, interfaceManager.currentPreset);
            }
            else
            {
                Debug.LogError("No preset is currently loaded.");
            }
        }

        public void CaptureAndSaveScreenshot(string currentPresetPath, string currentPreset)
        {
            string cleanedPath = currentPresetPath.Replace(".json", ".png");
            string cleanedPreset = currentPreset.Replace(".json", ".png");
            string tempScreenshotPath = Path.Combine(Application.persistentDataPath, currentPreset);

            // Save the current culling mask
            int originalCullingMask = screenshotCamera.cullingMask;

            screenshotCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));

            StartCoroutine(CaptureScreenshot(captureArea, tempScreenshotPath, () =>
            {
                //Debug.Log($"Screenshot {currentPreset} captured and saved to temporary path: {tempScreenshotPath}");

                // Restore the original culling mask
                screenshotCamera.cullingMask = originalCullingMask;

                // Start the coroutine to wait for the screenshot to be saved and then move it
                StartCoroutine(WaitAndMoveScreenshot(tempScreenshotPath, cleanedPath));
            }));
        }

        public void CaptureAndMoveScreenshot(string fileNameWithoutExtension)
        {
            string dateSubfolder = System.DateTime.Now.ToString("yyyy_MM_dd");
            string screenshotFileName = fileNameWithoutExtension;
            string tempScreenshotPath = Path.Combine(Application.persistentDataPath, screenshotFileName);
            string finalScreenshotPath = Path.Combine(Application.dataPath, "StreamingAssets/Output", dateSubfolder, screenshotFileName);

            int originalCullingMask = screenshotCamera.cullingMask;

            screenshotCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));

            StartCoroutine(CaptureScreenshot(captureArea, tempScreenshotPath, () =>
            {
                Debug.Log($"Screenshot captured and saved to temporary path: {tempScreenshotPath}");
                screenshotCamera.cullingMask = originalCullingMask;
                StartCoroutine(WaitAndMoveScreenshot(tempScreenshotPath, finalScreenshotPath));
            }));
        }

        private IEnumerator CaptureScreenshot(Rect captureArea, string path, Action onComplete)
        {
            yield return new WaitForEndOfFrame();

            // Save the original culling mask of the camera
            int originalCullingMask = screenshotCamera.cullingMask;

            // Temporarily exclude the UI layer by adjusting the camera's culling mask
            screenshotCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));

            RenderTexture originalRenderTexture = screenshotCamera.targetTexture; // Save original RenderTexture
            RenderTexture tempRenderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            screenshotCamera.targetTexture = tempRenderTexture; // Assign temporary RenderTexture
            screenshotCamera.Render(); // Manually render the camera

            // Read the pixels from the RenderTexture and create a full-screen Texture2D
            RenderTexture.active = tempRenderTexture;
            Texture2D fullScreenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            fullScreenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            fullScreenshot.Apply();

            // Restore the camera's original state
            screenshotCamera.targetTexture = originalRenderTexture;
            RenderTexture.active = null; // Reset active RenderTexture
            screenshotCamera.cullingMask = originalCullingMask; // Restore original culling mask
            Destroy(tempRenderTexture); // Cleanup the temporary RenderTexture

            // Correct y calculation for cropping based on Unity's coordinate system (origin at bottom left)
            int x = Mathf.FloorToInt(captureArea.x * Screen.width);
            int y = Mathf.FloorToInt(captureArea.y * Screen.height);
            int width = Mathf.Clamp(Mathf.FloorToInt(captureArea.width * Screen.width), 0, Screen.width - x);
            int height = Mathf.Clamp(Mathf.FloorToInt(captureArea.height * Screen.height), 0, Screen.height - y);

            // Validate the cropping area to ensure it does not exceed the bounds of the full screenshot
            if (x >= 0 && y >= 0 && x + width <= Screen.width && y + height <= Screen.height)
            {
                // Crop the screenshot to the specified area
                Texture2D croppedScreenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
                Color[] pixels = fullScreenshot.GetPixels(x, y, width, height);
                croppedScreenshot.SetPixels(pixels);
                croppedScreenshot.Apply();

                // Encode to PNG and save
                byte[] bytes = croppedScreenshot.EncodeToPNG();
                File.WriteAllBytes(path, bytes);

                // Cleanup
                Destroy(croppedScreenshot);
            }
            else
            {
                Debug.LogError("Requested capture area is out of bounds.");
            }

            // Cleanup
            Destroy(fullScreenshot);

            onComplete?.Invoke();
        }

        private IEnumerator WaitAndMoveScreenshot(string tempPath, string finalPath)
        {
            yield return new WaitForSeconds(0.2f); // Ensure the file is written

            // Check if the paths are identical
            if (tempPath.Equals(finalPath, StringComparison.OrdinalIgnoreCase))
            {
                //Debug.LogError("Temporary path and final path are identical, which means the file cannot be moved to the same location.");
                yield break; // Stop the coroutine
            }

            // Ensure the directory exists for the final path
            string directoryPath = Path.GetDirectoryName(finalPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Debug.Log($"Created directory at {directoryPath} for the screenshot.");
            }

            // Attempt to delete if the file already exists at the final path
            if (File.Exists(finalPath))
            {
                try
                {
                    File.Delete(finalPath);
                    //Debug.Log($"Existing file at {finalPath} was successfully deleted.");
                }
                catch (IOException e)
                {
                    Debug.LogError($"Failed to delete the existing file at {finalPath}. Exception: {e.Message}");
                    yield break; // Stop execution to prevent data loss
                }
            }

            try
            {
                File.Move(tempPath, finalPath);
                Debug.Log($"Screenshot successfully moved to: {finalPath}");
                SetCurrentScreenshot();
                presetScroller.LoadPresets();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to move the screenshot from {tempPath} to {finalPath}. Exception: {e.ToString()}");
            }
        }
    }
}