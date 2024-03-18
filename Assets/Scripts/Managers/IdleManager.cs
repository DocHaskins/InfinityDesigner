using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

public class IdleManager : MonoBehaviour
{
    public TMP_Text debugText;
    public bool debugDisplay;

    private float deltaTime = 0.0f;
    public Canvas mainCanvas;
    public Camera mainCamera;
    public RawImage renderTextureDisplay;
    public RenderTexture unfocusedRenderTexture;
    public Volume globalVolume;

    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;

        if (debugDisplay)
        {
            debugText.gameObject.SetActive(true);
            debugText.text = $"FPS: {Mathf.Ceil(fps)}\nFocused: {Application.isFocused}\nGraphics Level: {QualitySettings.GetQualityLevel()}";
        }
        else
        {
            debugText.gameObject.SetActive(false);
        }
    }

    private IEnumerator LimitFrameRate(int targetFrameRate)
    {
        while (!Application.isFocused)
        {
            yield return new WaitForSeconds(1f / targetFrameRate);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            ReduceRendering();
        }
        else
        {
            RestoreRendering();
        }
    }

    private void ReduceRendering()
    {
        StartCoroutine(CaptureScreen());
        renderTextureDisplay.gameObject.SetActive(true);
        if (mainCamera != null)
        {
            // Disable the main camera to stop it from updating
            mainCamera.enabled = false;
        }
        mainCanvas.enabled = false;
        QualitySettings.SetQualityLevel(0, true);
        QualitySettings.vSyncCount = 0;
        StartCoroutine(LimitFrameRate(0));

        if (globalVolume != null)
        {
            var rayTracingSettings = (RayTracingSettings)globalVolume.profile.components.Find(x => x is RayTracingSettings);
            if (rayTracingSettings != null)
            {
                rayTracingSettings.active = false;
            }
        }
    }

    private void RestoreRendering()
    {
        mainCanvas.enabled = true;
        renderTextureDisplay.gameObject.SetActive(false);
        if (mainCamera != null)
        {
            // Re-enable the main camera and reset its target texture to render to the screen again
            mainCamera.enabled = true;
            mainCamera.targetTexture = null;
        }

        // Hide the RenderTexture display
        if (renderTextureDisplay != null)
        {
            renderTextureDisplay.enabled = false;
        }

        QualitySettings.SetQualityLevel(5, true);
        QualitySettings.vSyncCount = 0;
        StopAllCoroutines();

        if (globalVolume != null)
        {
            var rayTracingSettings = (RayTracingSettings)globalVolume.profile.components.Find(x => x is RayTracingSettings);
            if (rayTracingSettings != null)
            {
                rayTracingSettings.active = true; // Enable Ray Tracing
            }
        }
    }

    private IEnumerator CaptureScreen()
    {
        // Wait until the end of the frame to ensure all rendering is complete
        yield return new WaitForEndOfFrame();

        // Create a Texture2D with the size of the screen
        Texture2D screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        // Read screen contents into the texture
        screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTexture.Apply();

        // Convert Texture2D to RenderTexture
        if (unfocusedRenderTexture == null || unfocusedRenderTexture.width != screenTexture.width || unfocusedRenderTexture.height != screenTexture.height)
        {
            // Create or resize unfocusedRenderTexture
            if (unfocusedRenderTexture != null)
            {
                unfocusedRenderTexture.Release();
            }
            unfocusedRenderTexture = new RenderTexture(screenTexture.width, screenTexture.height, 0);
        }

        // Copy the content to the RenderTexture
        Graphics.Blit(screenTexture, unfocusedRenderTexture);

        // Clean up the Texture2D now that we're done with it
        Destroy(screenTexture);

        // Update the RawImage to display the new RenderTexture
        if (renderTextureDisplay != null)
        {
            renderTextureDisplay.texture = unfocusedRenderTexture;
            renderTextureDisplay.enabled = true; // Display the screenshot
        }
    }
}