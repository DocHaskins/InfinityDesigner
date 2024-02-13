using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

namespace doppelganger
{
    public class ErrorManagerChild : ErrorManager
    {
        protected new void OnEnable()
        {
            // Call base class OnEnable explicitly to ensure logFilePath is initialized
            base.OnEnable(); // This ensures logFilePath is set before any operations on it

            // Re-attach the log handler - consider if you need this since base.OnEnable already does it
            // Application.logMessageReceived += HandleLog; // Might be redundant

            // Check and create the log file if it does not exist, without clearing it.
            EnsureLogFileExists();

            // Update UI components to use the new "ErrorPanel" Tag
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
                // Using StreamWriter to create or open the file for appending
                using (var sw = File.AppendText(logFilePath)) { }
            }
        }

        private void UpdateUIComponents()
        {
            GameObject errorPanel = GameObject.FindGameObjectWithTag("ErrorPanel");
            if (errorPanel != null)
            {
                errorPopup = errorPanel; // Update the errorPopup reference
                canvasGroup = errorPopup.GetComponent<CanvasGroup>();
                errorMessageText = errorPopup.GetComponentInChildren<TMP_Text>();
            }
            else
            {
                Debug.LogError("ErrorPanel tag not found in the scene.");
            }
        }
    }
}