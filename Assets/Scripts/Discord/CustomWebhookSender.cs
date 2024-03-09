using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro; // Include this for TMP_Text
using ShadowGroveGames.WebhooksForDiscord.Scripts;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO.Compression;

namespace doppelganger
{
    public class CustomWebhookSender : MonoBehaviour
    {
        public CharacterBuilder_InterfaceManager interfaceManager;
        
        [SerializeField]
        private TMP_InputField _titleTextField;
        private string discordUserName;
        private string discordUserId;
        public Button discordSubmit;

        public TMP_Dropdown categoryDropdown;
        public TMP_Dropdown classDropdown;

        private string webhookUrl = "https://discord.com/api/webhooks/1215421716709511300/_HrqfZWUwhVUI5dBYXo1GprE6hkeYvk2gS9R6Zfgc_CW7n8MasxlorhCxab9Q3HrsapX";
        private bool _isForumChannel = true;
        private List<string> badWords = new List<string>();

        private readonly static List<string> tags = new List<string>()
        {
            "Survivor",
            "PeaceKeeper",
            "GRE"
        };

        private readonly Dictionary<string, ulong> categoryToTagId = new Dictionary<string, ulong>()
{
    {"Survivor", 1},
    {"PeaceKeeper", 2},
    {"GRE", 3}
};

        private void Start()
        {
            string discordUserName = ConfigManager.LoadSetting("Discord", "Name");
            string discordUserId = ConfigManager.LoadSetting("Discord", "ID");
            if (discordUserName != null)
            {
                discordSubmit.gameObject.SetActive(true);
            }
            LoadBadWords();
        }

        private void LoadBadWords()
        {
            TextAsset offensiveText = Resources.Load<TextAsset>("Strings/offensive_text");
            if (offensiveText != null)
            {
                // Assuming the bad words are comma-separated
                badWords = new List<string>(offensiveText.text.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                Debug.LogError("Failed to load offensive words list.");
            }
        }

        private bool ContainsBadWords(string input, out string foundWord)
        {
            foreach (var inputWord in input.ToLower().Split(' ', '.', ',', '!', '?', ';', ':', '\n', '\r', '\t'))
            {
                foreach (string badWord in badWords)
                {
                    if (inputWord.Equals(badWord.ToLower()))
                    {
                        foundWord = badWord;
                        return true;
                    }
                }
            }
            foundWord = string.Empty;
            return false;
        }

        public void SendWebhookWithAttachments()
        {
            string discordDisplayName = ConfigManager.LoadSetting("Discord", "Name");
            Debug.LogError($"discordDisplayName: {discordDisplayName}");
            string message = "Category: " + categoryDropdown.options[categoryDropdown.value].text + "\n" +
                             "Class: " + classDropdown.options[classDropdown.value].text + "\n";
            string title = discordDisplayName + ": " + _titleTextField?.text;

            string foundWordInMessage, foundWordInTitle;

            bool badWordInMessage = ContainsBadWords(message, out foundWordInMessage);
            bool badWordInTitle = ContainsBadWords(title, out foundWordInTitle);

            if (badWordInMessage || badWordInTitle)
            {
                string badWordFound = badWordInMessage ? foundWordInMessage : foundWordInTitle;
                Debug.LogError($"Message or Title contains inappropriate content: {badWordFound}, Message: {title}, {message}");
                return; // Prevent sending the message
            }

            string jsonFilePath = interfaceManager.currentPresetPath;
            string imagePath = Path.ChangeExtension(jsonFilePath, ".png");
            string currentJsonImageName = Path.GetFileName(imagePath);

            // Prepare data for the main JSON file
            string jsonFileName = Path.GetFileName(jsonFilePath);

            string jsonFileNameCleaned = Path.GetFileNameWithoutExtension(jsonFilePath);
            string fppFileNamePattern = jsonFileNameCleaned + "_fpp.json";
            string directoryPath = Path.GetDirectoryName(jsonFilePath);

            // Find the _fpp version of the JSON if it exists
            string fppJsonFilePath = Path.Combine(directoryPath, fppFileNamePattern);

            // Preparing the ZIP file
            string zipFilePath = Path.Combine(directoryPath, jsonFileNameCleaned + ".zip");
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath); // Ensure the old ZIP is deleted
            }

            using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                // Add main JSON file and _fpp JSON if exists
                zip.CreateEntryFromFile(jsonFilePath, jsonFileName);
                if (File.Exists(fppJsonFilePath))
                {
                    zip.CreateEntryFromFile(fppJsonFilePath, fppFileNamePattern);
                }

                // Add the screenshot
                zip.CreateEntryFromFile(imagePath, currentJsonImageName);

                // Create and add the placeholder file
                string placeholderFilePath = Path.Combine(directoryPath, "PLACEHOLDER_InfinityDesigner_json.file");
                File.WriteAllText(placeholderFilePath, "");
                zip.CreateEntryFromFile(placeholderFilePath, "PLACEHOLDER_InfinityDesigner_json.file");
                File.Delete(placeholderFilePath);

                // Define the content for install.json
                var installInfo = new
                {
                    Metadata = new
                    {
                        Category = categoryDropdown.options[categoryDropdown.value].text,
                        Class = classDropdown.options[classDropdown.value].text
                    },
                    Files = new[] { jsonFileName, fppJsonFilePath != null ? Path.GetFileName(fppJsonFilePath) : null, currentJsonImageName }
                };

                string installJsonContent = JsonConvert.SerializeObject(installInfo, Formatting.Indented);

                var zipEntry = zip.CreateEntry("install.json");
                using (var writer = new StreamWriter(zipEntry.Open()))
                {
                    writer.Write(installJsonContent);
                }
            }

            // Now delete the temporary files outside of the using statement to avoid file lock issues
            File.Delete(Path.Combine(directoryPath, "PLACEHOLDER_InfinityDesigner_json.file"));
            File.Delete(Path.Combine(directoryPath, "install.json"));

            // Prepare byte array for ZIP file to upload after closing the ZIP to ensure all changes are applied
            byte[] zipFileData = File.ReadAllBytes(zipFilePath);

            
            byte[] screenshotData = File.ReadAllBytes(imagePath);

            // Create and configure the webhook message
            var webhook = DiscordWebhook.Create(webhookUrl)
                                        .WithUsername(discordUserName)
                                        .WithContent(message);

            // Attach ZIP file and screenshot
            webhook.AddAttachment(jsonFileNameCleaned + ".zip", zipFileData);
            webhook.AddAttachment(currentJsonImageName, screenshotData);

            // If in forum channel, set the thread name
            if (_isForumChannel && !string.IsNullOrWhiteSpace(title))
            {
                webhook.WithThreadName(title);
            }

            // Send the webhook message
            webhook.Send();

            File.Delete(zipFilePath);
        }
    }
}