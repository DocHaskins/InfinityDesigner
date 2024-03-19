using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterAppearance
{
    public string Name;
    public string color;
    public string headName;
    public string bodyName;
    public string appearanceId;
    public string modelFpp;
    public string modelTpp;
    public bool availableOnStart;
    public string hint;
    public string image;
    public int category;
    public List<string> requiredDLCs;
    public bool overridesOutfit;
    public int id;
    
    public CharacterAppearance()
    {
        availableOnStart = false;
        requiredDLCs = new List<string>();
    }
}