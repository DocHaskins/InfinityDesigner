using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Difference
{
    public string SectionName;
    public List<string> File1Lines = new List<string>();
    public List<string> File2Lines = new List<string>();
}

public class CompareData
{
    public List<Difference> Differences = new List<Difference>();
}


[System.Serializable]
public class SkillData
{
    public string Name;
    public List<string> Parameters = new List<string>();
}

[System.Serializable]
public class SubData
{
    public string Name;
    public List<SkillData> Skills = new List<SkillData>();
}

