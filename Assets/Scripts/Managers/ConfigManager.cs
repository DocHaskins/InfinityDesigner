using Newtonsoft.Json.Bson;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace doppelganger
{
    public class ConfigManager : MonoBehaviour
    {
        public Text discordName;
        public Text discordID;
        private static string configPath = Path.Combine(Application.streamingAssetsPath, "config.ini");

        public static void SaveSetting(string section, string key, string value)
        {
            var config = LoadConfig();
            
            if (!config.ContainsKey(section))
            {
                config[section] = new Dictionary<string, string>();
                Debug.Log($"SaveSetting called");
            }

            config[section][key] = value;
            Debug.Log($"SaveSetting returned with {key}");
            SaveConfig(config);
        }

        public static string LoadSetting(string section, string key)
        {
            var config = LoadConfig();
            if (config.TryGetValue(section, out var sectionData))
            {
                //Debug.Log($"LoadSetting trying to get a value");
                if (sectionData.TryGetValue(key, out var value))
                {
                    return value;
                }
            }

            return null; // or default value as appropriate
        }

        private static Dictionary<string, Dictionary<string, string>> LoadConfig()
        {
            var config = new Dictionary<string, Dictionary<string, string>>();
            if (!File.Exists(configPath))
            {
                return config;
            }

            string currentSection = null;
            foreach (var line in File.ReadAllLines(configPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";")) continue; // Skip empty lines and comments

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Trim('[', ']');
                    config[currentSection] = new Dictionary<string, string>();
                }
                else if (!string.IsNullOrEmpty(currentSection) && line.Contains("="))
                {
                    var keyValue = line.Split(new[] { '=' }, 2);
                    if (keyValue.Length == 2)
                    {
                        config[currentSection][keyValue[0].Trim()] = keyValue[1].Trim();
                    }
                }
            }

            return config;
        }

        private static void SaveConfig(Dictionary<string, Dictionary<string, string>> config)
        {
            using (var writer = new StreamWriter(configPath, false))
            {
                foreach (var section in config)
                {
                    writer.WriteLine($"[{section.Key}]");
                    foreach (var keyValuePair in section.Value)
                    {
                        writer.WriteLine($"{keyValuePair.Key}={keyValuePair.Value}");
                    }
                    writer.WriteLine(); // Add a blank line for readability
                }
            }
        }

        public void StoreDiscordID()
        {

            string name = "";
            string id = "";

            if (discordName != null) name = discordName.text;
            if (discordID != null) id = discordID.text.Replace("ID: ", "");

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(id))
            {
                // Store these values in the config.ini file under the new section "Discord"
                SaveSetting("Discord", "Name", name);
                SaveSetting("Discord", "ID", id);
            }
            else
            {
                Debug.LogError("Discord tagged objects with Name and ID not found in the scene!");
            }
        }

        public void DestroyDiscordID()
        {

            string name = "";
            string id = "";

            if (discordName != null) name = "";
            if (discordID != null) id = "";

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(id))
            {
                SaveSetting("Discord", "Name", name);
                SaveSetting("Discord", "ID", id);
            }
            else
            {
                Debug.LogError("Discord tagged objects with Name and ID not found in the scene!");
            }
        }
    }
}