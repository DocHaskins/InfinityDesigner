using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro; // Include this for TMP_Text
using ShadowGroveGames.WebhooksForDiscord.Scripts;
using ShadowGroveGames.FeedbackOverDiscord.Scripts;
using ShadowGroveGames.FeedbackOverDiscord.Scripts.Control;
using ShadowGroveGames.FeedbackOverDiscord.Scripts.Extentions;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO.Compression;
using ShadowGroveGames.WebhooksForDiscord.Scripts.DTO;
using System.Collections;
using System.Threading.Tasks;

namespace doppelganger
{
    public class CustomWebhookSender : MonoBehaviour
    {
        public CharacterBuilder_InterfaceManager interfaceManager;
        private DiscordWebhook _discordWebhook;
        public NotificationManager notificationManager;

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

        [SerializeField]
        [Tooltip("This event is fired after a feedback submit has been successfully sent!")]
        private OnSuccessFeedbackSubmit _onSuccessFeedbackSubmit = new OnSuccessFeedbackSubmit();

        [SerializeField]
        [Tooltip("This event is triggered after a feedback could not be sent!")]
        private OnFailedFeedbackSubmit _onFailedFeedbackSubmit = new OnFailedFeedbackSubmit();

        private readonly Dictionary<string, ulong> categoryToTagId = new Dictionary<string, ulong>()
{
    {"Player", 1215664689317417010},
    {"Man", 1217837300583632927},
    {"Wmn", 1217837312256245761},
    {"Renegade", 1215664706367262740},
    {"PeaceKeeper", 1215664718354714644},
    {"Colonel", 1215664764252848138},
    {"WorldBurner", 1215664791981531157},
    {"Wolves", 1215664819517128856},
    {"PlagueBearer", 1215664836856381531},
    {"Survivor", 1215664868187705364},
    {"NPC", 1215664886781050950},
    {"Biter", 1215664905517137940},
    {"Viral", 1215664915524878377},
    {"Goon", 1215664927520595988},
    {"Volatile", 1215664940858474566},
    {"Demolisher", 1215664954594566154},
    {"Spitter", 1215664971510321152},
    {"Howler", 1215664982147207178},
    {"Player_Skin", 1219448322561933382},
    {"Suicider", 1215665447450447873},
    {"Child", 1215666275456524328},
    {"GRE", 1215667025662316545}
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
            //Debug.LogError($"discordDisplayName: {discordDisplayName}");
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
                        Username = discordDisplayName,
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

            File.Delete(Path.Combine(directoryPath, "PLACEHOLDER_InfinityDesigner_json.file"));
            File.Delete(Path.Combine(directoryPath, "install.json"));

            byte[] zipFileData = File.ReadAllBytes(zipFilePath);

            
            byte[] screenshotData = File.ReadAllBytes(imagePath);

            string selectedCategoryName = categoryDropdown.options[categoryDropdown.value].text;
            ulong selectedCategoryTagId = categoryToTagId.ContainsKey(selectedCategoryName) ? categoryToTagId[selectedCategoryName] : 0;

            if (selectedCategoryTagId == 0)
            {
                Debug.LogError($"Invalid category selected: {selectedCategoryName}");
                return;
            }

            _discordWebhook = DiscordWebhook.Create(webhookUrl)
                                            .WithUsername(discordDisplayName)
                                            .WithContent(message);

            _discordWebhook.AddAttachment(jsonFileNameCleaned + ".zip", zipFileData);
            _discordWebhook.AddAttachment(currentJsonImageName, screenshotData);

            if (_isForumChannel && !string.IsNullOrWhiteSpace(title))
            {
                _discordWebhook.WithThreadName(title);
            }

            _discordWebhook.AddTag(selectedCategoryTagId);
            var asyncSend = _discordWebhook.SendAsync();

            StartCoroutine(WaitForWebhookSend(asyncSend));
            if (notificationManager != null)
            {
                notificationManager.ShowNotification($"Model {jsonFileNameCleaned} has been shared on the Infinity Designer/Share Discord Channel with the tag: {selectedCategoryName}");
            }
            File.Delete(zipFilePath);
        }

        private IEnumerator WaitForWebhookSend(Task<MessageResponse?> sendTask)
        {
            while (!sendTask.IsCompleted)
            {
                yield return null;
            }

            if (sendTask.IsFaulted)
            {
                _onFailedFeedbackSubmit?.Invoke();
            }
            else
            {
                _onSuccessFeedbackSubmit?.Invoke();
            }

            _discordWebhook = DiscordWebhook.Create(webhookUrl);
        }
    }
}