using Assets.SimpleLocalization.Scripts;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace doppelganger
{
    public enum MessageType
    {
        Notification,
        Warning,
        Error
    }
    public class NotificationManager : MonoBehaviour
    {
        public GameObject notificationPopup;
        public Text categoryLabel;
        public TMP_Text notificationMessageText;
        public LocalizedText displayTextComponent;
        public CanvasGroup canvasGroup;
        public string logFilePath;
        private Coroutine fadeCoroutine;

        

        void Awake()
        {
            FindNotificationPopupComponents();
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

        void FindNotificationPopupComponents()
        {
            GameObject foundPopup = GameObject.FindGameObjectWithTag("ErrorPanel");
            if (foundPopup != null)
            {
                notificationPopup = foundPopup;
                canvasGroup = notificationPopup.GetComponent<CanvasGroup>();
                categoryLabel = notificationPopup.transform.Find("CategoryLabel")?.GetComponent<Text>();
                notificationMessageText = notificationPopup.transform.Find("notifyMsg")?.GetComponent<TMP_Text>();
            }
            else
            {
                Debug.LogError("Error popup with 'ErrorPanel' tag not found.");
            }

            if (canvasGroup == null)
            {
                Debug.LogError("CanvasGroup component not found on the error popup.");
            }

            if (categoryLabel == null)
            {
                Debug.LogError("TMP_Text component named 'categoryLabel' not found in the error popup's children.");
            }

            if (notificationMessageText == null)
            {
                Debug.LogError("TMP_Text component named 'notificationMessageText' not found in the error popup's children.");
            }
        }

        void InitializeLogFile()
        {
            File.WriteAllText(logFilePath, "");
        }

        public void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                File.AppendAllText(logFilePath, logString + "\n" + stackTrace + "\n");
                ShowError(logString);
            }
        }

        public void ShowNotification(string message)
        {
            ShowMessage(message, MessageType.Notification);
        }

        public void ShowWarning(string message)
        {
            ShowMessage(message, MessageType.Warning);
        }

        public void ShowError(string message)
        {
            ShowMessage(message, MessageType.Error);
        }

        private void ShowMessage(string message, MessageType type)
        {
            if (notificationMessageText != null && canvasGroup != null)
            {
                notificationMessageText.text = message;

                if (categoryLabel != null)
                {
                    switch (type)
                    {
                        case MessageType.Notification:
                            categoryLabel.text = "Notification";
                            displayTextComponent.LocalizationKey = "Menu.Notification";
                            break;
                        case MessageType.Warning:
                            categoryLabel.text = "Warning";
                            displayTextComponent.LocalizationKey = "Menu.Warning";
                            break;
                        case MessageType.Error:
                            categoryLabel.text = "Error";
                            displayTextComponent.LocalizationKey = "Menu.Error";
                            break;
                    }
                }

                if (fadeCoroutine != null)
                    StopCoroutine(fadeCoroutine);

                fadeCoroutine = StartCoroutine(FadePopup(1, 0.5f));
                fadeCoroutine = StartCoroutine(HidePopupAfterDelay(5));
            }
        }

        IEnumerator HidePopupAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadePopup(0, 0.5f));
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