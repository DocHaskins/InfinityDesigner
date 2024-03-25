using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
using doppelganger;

public class IdleManager : MonoBehaviour
{
    public TMP_Text debugText;
    public bool debugDisplay;
    public float delaySeconds = 10f;
    private bool delayElapsed = false;
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
    private void Start()
    {
        StartCoroutine(DelayBeforeReducingRendering(delaySeconds));
        Debug.Log($"Starting delay timer for {delaySeconds}secs");
    }

    private IEnumerator DelayBeforeReducingRendering(float delaySeconds)
    {
        float remainingTime = delaySeconds;

        Debug.Log($"DelayBeforeReducingRendering started. Waiting for {delaySeconds} seconds.");

        while (remainingTime > 0)
        {
            Debug.Log($"Time until rendering can be reduced: {remainingTime} seconds remaining.");
            yield return new WaitForSeconds(1);
            remainingTime--;
        }

        delayElapsed = true;
        Debug.Log($"Delay has elapsed. delayElapsed is now set to {delayElapsed}. Ready to reduce rendering if out of focus.");
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
            if (delayElapsed)
            {
                ReduceRendering();
            }
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
            mainCamera.enabled = false;
        }
        mainCanvas.enabled = false;
        QualitySettings.SetQualityLevel(0, true);
        //QualitySettings.vSyncCount = 0;
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
            mainCamera.enabled = true;
            mainCamera.targetTexture = null;
        }

        if (renderTextureDisplay != null)
        {
            renderTextureDisplay.enabled = false;
        }

        QualitySettings.SetQualityLevel(5, true);
        //QualitySettings.vSyncCount = 0;
        StopAllCoroutines();

        if (globalVolume != null)
        {
            var rayTracingSettings = (RayTracingSettings)globalVolume.profile.components.Find(x => x is RayTracingSettings);
            if (rayTracingSettings != null)
            {
                rayTracingSettings.active = true;
            }
        }
    }

    private IEnumerator CaptureScreen()
    {
        yield return new WaitForEndOfFrame();
        Texture2D screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        
        screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTexture.Apply();
        if (unfocusedRenderTexture == null || unfocusedRenderTexture.width != screenTexture.width || unfocusedRenderTexture.height != screenTexture.height)
        {
            if (unfocusedRenderTexture != null)
            {
                unfocusedRenderTexture.Release();
            }
            unfocusedRenderTexture = new RenderTexture(screenTexture.width, screenTexture.height, 0);
        }

        Graphics.Blit(screenTexture, unfocusedRenderTexture);
        Destroy(screenTexture);

        if (renderTextureDisplay != null)
        {
            renderTextureDisplay.texture = unfocusedRenderTexture;
            renderTextureDisplay.enabled = true;
        }
    }
}