using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class ScreenshotManager : MonoBehaviour
{
    public Camera screenshotCamera;
    public Canvas canvasToExclude;
    public Rect captureArea;

    private void Start()
    {
        
    }

    public void CaptureAndMoveScreenshot(string fileNameWithoutExtension)
    {
        string dateSubfolder = System.DateTime.Now.ToString("yyyy_MM_dd");
        string screenshotFileName = fileNameWithoutExtension;

        // Temporary path in the persistent data path to ensure write access
        string tempScreenshotPath = Path.Combine(Application.persistentDataPath, screenshotFileName);

        // Final path for the screenshot
        string finalScreenshotPath = Path.Combine(Application.dataPath, "StreamingAssets/Output", dateSubfolder, screenshotFileName);

        // Save the current culling mask
        int originalCullingMask = screenshotCamera.cullingMask;

        // Exclude the UI layer from the culling mask
        // Assuming your UI is on layer 5 (UI layer)
        screenshotCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));

        StartCoroutine(CaptureScreenshot(captureArea, tempScreenshotPath, () =>
        {
            Debug.Log($"Screenshot captured and saved to temporary path: {tempScreenshotPath}");

            // Restore the original culling mask
            screenshotCamera.cullingMask = originalCullingMask;

            // Start the coroutine to wait for the screenshot to be saved and then move it
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
        // Wait for a very short time to ensure the file is written
        yield return new WaitForSeconds(0.1f);

        // Ensure the directory exists for the final path
        string directoryPath = Path.GetDirectoryName(finalPath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Move the screenshot to the final path
        File.Move(tempPath, finalPath);

        Debug.Log($"Screenshot moved to: {finalPath}");
    }
}