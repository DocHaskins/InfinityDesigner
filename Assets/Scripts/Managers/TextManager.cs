using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.Dark;
using Assets.SimpleLocalization.Scripts;
using UnityEngine.Events;

namespace doppelganger
{
    public class TextManager : MonoBehaviour
    {
        public string systemLanguage = "English";
        public CustomDropdown languageDropdown;

        public void Awake()
        {
            LocalizationManager.Read();

            string savedLanguage = ConfigManager.LoadSetting("Language", "Current");
            if (!string.IsNullOrWhiteSpace(savedLanguage))
            {
                systemLanguage = savedLanguage;
            }

            switch (Application.systemLanguage)
            {
                default:
                    LocalizationManager.Language = "English";
                    break;
            }
            SetDropdownToLanguage(systemLanguage);
            languageDropdown.onValueChanged.AddListener(SetLocalization);
            SetLocalization(systemLanguage);
        }

        private void SetDropdownToLanguage(string language)
        {
            for (int i = 0; i < languageDropdown.dropdownItems.Count; i++)
            {
                if (languageDropdown.dropdownItems[i].itemName == language)
                {
                    languageDropdown.ChangeDropdownInfo(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Change localization at runtime.
        /// </summary>
        public void SetLocalization(string localization)
        {
            LocalizationManager.Language = localization;
            ConfigManager.SaveSetting("Language", "Current", localization);
        }
    }
}