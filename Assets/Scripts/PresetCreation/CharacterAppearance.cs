using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class CharacterAppearance
{
    public string id;
    public string headName;
    public string bodyName;
    public string appearanceId;
    public string modelFpp;
    public string modelTpp;
    public bool availableOnStart;
    public string hint;
    public string image;
    public int category;
    // Add more fields as needed

    public CharacterAppearance() // Default constructor
    {
        availableOnStart = false; // Set default values as needed
    }
}

public class AppearanceLoader : MonoBehaviour
{
    public List<CharacterAppearance> appearances = new List<CharacterAppearance>();
    public GameObject rawImagePrefab; // Assign in the inspector
    public Transform contentPanel; // Assign in the inspector

    void Start()
    {
        LoadAppearancesFromFile("path/to/your/file.txt");
        DisplayAppearances();
    }

    void LoadAppearancesFromFile(string filePath)
    {
        // Dummy implementation for loading from a custom file format
        // You would replace this with actual file reading and parsing logic
    }

    void DisplayAppearances()
    {
        foreach (var appearance in appearances)
        {
            GameObject instance = Instantiate(rawImagePrefab, contentPanel);
            // Assume there's a script attached to the prefab that can handle appearance data
            instance.GetComponent<AppearanceDisplay>().Setup(appearance);
        }
    }
}