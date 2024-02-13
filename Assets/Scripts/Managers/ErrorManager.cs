using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace doppelganger
{
    public class ErrorManager : MonoBehaviour
    {
        public GameObject errorPopup; // This can be removed if you're finding the popup dynamically
        public TMP_Text errorMessageText; // Dynamically found based on tag and child name
        public CanvasGroup canvasGroup; // Dynamically found based on tag
        public string logFilePath;
        private Coroutine fadeCoroutine;

        void Awake()
        {
            FindErrorPopupComponents();
            if (canvasGroup != null) canvasGroup.alpha = 0;
        }

        public void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
            logFilePath = Path.Combine(Application.streamingAssetsPath, "log.txt");
            InitializeLogFile();
        }

        public void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        void FindErrorPopupComponents()
        {
            // Find the errorPopup GameObject by the tag "ErrorPanel"
            GameObject foundPopup = GameObject.FindGameObjectWithTag("ErrorPanel");
            if (foundPopup != null)
            {
                errorPopup = foundPopup;
                // Assign the CanvasGroup from the found errorPopup
                canvasGroup = errorPopup.GetComponent<CanvasGroup>();
                // Find the TMP_Text component named "ErrorMsg" within the errorPopup's children
                errorMessageText = errorPopup.transform.Find("ErrorMsg")?.GetComponent<TMP_Text>();
            }
            else
            {
                Debug.LogError("Error popup with 'ErrorPanel' tag not found.");
            }

            if (canvasGroup == null)
            {
                Debug.LogError("CanvasGroup component not found on the error popup.");
            }

            if (errorMessageText == null)
            {
                Debug.LogError("TMP_Text component named 'ErrorMsg' not found in the error popup's children.");
            }
        }

        void InitializeLogFile()
        {
            // Clear the log file at the start or create it if it doesn't exist
            File.WriteAllText(logFilePath, "");
        }

        public void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                // Append the error message to the log file
                File.AppendAllText(logFilePath, logString + "\n" + stackTrace + "\n");

                // Update the popup text and make it visible
                ShowError(logString);
            }
        }

        public void ShowError(string message)
        {
            if (errorMessageText != null && canvasGroup != null)
            {
                errorMessageText.text = message;
                // Properly manage the coroutine to avoid stopping it prematurely
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(FadePopup(1, 0.5f)); // Fade in
                                                                    // No need to stop all coroutines before starting the hide coroutine
                fadeCoroutine = StartCoroutine(HidePopupAfterDelay(5)); // Wait 5 seconds, then fade out
            }
        }

        IEnumerator HidePopupAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePopup(0, 0.5f)); // Fade out
        }

        IEnumerator FadePopup(float targetAlpha, float duration)
        {
            float startAlpha = canvasGroup.alpha;
            float time = 0;

            while (time < duration)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
                time += Time.deltaTime;
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            canvasGroup.blocksRaycasts = targetAlpha == 1;
        }
    }
}