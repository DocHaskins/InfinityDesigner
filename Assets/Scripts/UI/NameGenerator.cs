using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;

namespace doppelganger
{
    public class NameGenerator : MonoBehaviour
    {
        [Header("Interface")]
        public TMP_InputField saveName;
        public TMP_Dropdown saveCategoryDropdown;

        public void GenerateName()
        {
            string selectedCategory = saveCategoryDropdown.options[saveCategoryDropdown.value].text;
            string filePath = "";

            if (selectedCategory == "Man")
            {
                filePath = Path.Combine(Application.streamingAssetsPath, "male_names.json");
            }
            else if (selectedCategory == "Wmn")
            {
                filePath = Path.Combine(Application.streamingAssetsPath, "female_names.json");
            }

            if (selectedCategory == "Player")
            {
                saveName.text = "Aiden";
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                string dataAsJson = File.ReadAllText(filePath);
                Names names = JsonUtility.FromJson<Names>(dataAsJson);

                if (names != null)
                {
                    List<string> nameList = null;

                    if (selectedCategory == "Man")
                    {
                        nameList = new List<string>(names.male);
                    }
                    else if (selectedCategory == "Wmn")
                    {
                        nameList = new List<string>(names.female);
                    }

                    if (nameList != null && nameList.Count > 0)
                    {
                        string randomName = nameList[Random.Range(0, nameList.Count)];
                        saveName.text = randomName;
                    }
                }
            }
        }

        [System.Serializable]
        private class Names
        {
            public string[] male;
            public string[] female;
        }
    }
}