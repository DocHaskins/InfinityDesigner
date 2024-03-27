using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.SimpleLocalization.Scripts;

namespace doppelganger
{
    public class NotificationManagerChild : NotificationManager
    {
        protected new void OnEnable()
        {
            base.OnEnable();
            EnsureLogFileExists();
            UpdateUIComponents();
        }

        private void EnsureLogFileExists()
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                Debug.LogError("logFilePath is not initialized.");
                return;
            }

            if (!File.Exists(logFilePath))
            {
                using (var sw = File.AppendText(logFilePath)) { }
            }
        }

        private void UpdateUIComponents()
        {
            GameObject errorPanel = GameObject.FindGameObjectWithTag("ErrorPanel");
            if (errorPanel != null)
            {
                notificationPopup = errorPanel;
                canvasGroup = notificationPopup.GetComponent<CanvasGroup>();
                categoryLabel = notificationPopup.transform.Find("CategoryLabel")?.GetComponent<Text>();
                notificationMessageText = notificationPopup.GetComponentInChildren<TMP_Text>();
                displayTextComponent = notificationPopup.GetComponentInChildren<LocalizedText>(); // Make sure to update this reference as well
            }
            else
            {
                Debug.LogError("ErrorPanel tag not found in the scene.");
            }
        }
    }
}