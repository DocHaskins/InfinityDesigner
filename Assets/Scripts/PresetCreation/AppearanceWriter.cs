using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class AppearanceWriter : MonoBehaviour
{
    public AppearanceLoader appearanceLoader;
    public string filePath;

    public void UpdateAppearance(CharacterAppearance updatedAppearance)
    {
        int index = appearanceLoader.characterAppearances.FindIndex(a => a.id == updatedAppearance.id);
        if (index != -1)
        {
            appearanceLoader.characterAppearances[index] = updatedAppearance;
            WriteAllAppearancesForCharacter("PlayerMan1");
        }
    }

    private void WriteAllAppearancesForCharacter(string characterName)
    {
        // Ensure the file exists
        if (!File.Exists(appearanceLoader.filePath))
        {
            Debug.LogError("File not found: " + appearanceLoader.filePath);
            return;
        }

        // Read the whole file into memory
        string fileContents = File.ReadAllText(appearanceLoader.filePath);

        // Find the start and end indices of the character's appearance section
        string characterStartTag = $"\tCharacter(\"{characterName}\")";
        string characterEndTag = "\t}";
        int startCharIndex = fileContents.IndexOf(characterStartTag);
        int endCharIndex = fileContents.IndexOf(characterEndTag, startCharIndex) + characterEndTag.Length;

        if (startCharIndex == -1 || endCharIndex == -1)
        {
            Debug.LogError($"Character section \"{characterName}\" not found in file.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        // Copy everything before the character's appearance section
        sb.Append(fileContents.Substring(0, startCharIndex));

        // Start the character's appearance section
        sb.AppendLine(characterStartTag);
        sb.AppendLine("\t{");

        // Append all appearances for this character
        foreach (var appearance in appearanceLoader.characterAppearances)
        {
            AppendAppearanceToStringBuilder(appearance, sb);
        }

        // End the character's appearance section
        sb.AppendLine("\t}");

        // Copy everything after the character's appearance section
        sb.Append(fileContents.Substring(endCharIndex));

        // Overwrite the file with the new contents
        File.WriteAllText(appearanceLoader.filePath, sb.ToString());

        Debug.Log($"Appearances for character \"{characterName}\" updated.");
    }

    private void AppendAppearanceToStringBuilder(CharacterAppearance appearance, StringBuilder sb)
    {
        // Append the appearance in the same format as before
        sb.AppendLine($"\t\tAppearance(\"{appearance.Name}\")");
        sb.AppendLine("\t\t{");
        sb.AppendLine($"\t\t\tModelFpp(\"{appearance.modelFpp}\");");
        sb.AppendLine($"\t\t\tModelTpp(\"{appearance.modelTpp}\");");
        if (appearance.overridesOutfit)
        {
            sb.AppendLine("\t\t\tOverridesOutfit(true);");
        }
        // Add other fields similarly
        sb.AppendLine($"\t\t\tColor(\"{appearance.color}\");");
        sb.AppendLine($"\t\t\tImage(\"{appearance.image}\");");
        sb.AppendLine($"\t\t\tCategory({appearance.category});");
        sb.AppendLine($"\t\t\tID({appearance.id});");
        foreach (var dlc in appearance.requiredDLCs)
        {
            sb.AppendLine($"\t\t\tRequiredDLCs(\"{dlc}\");");
        }
        if (appearance.availableOnStart)
        {
            sb.AppendLine("\t\t\tAvailableOnStart();");
        }
        sb.AppendLine($"\t\t\tHint(\"{appearance.hint}\");");
        sb.AppendLine("\t\t}"); // Close the appearance section
    }


    public void AddNewAppearance(CharacterAppearance appearance)
    {
        if (appearanceLoader == null)
        {
            Debug.LogError("AppearanceLoader reference not set in AppearanceWriter.");
            return;
        }

        // Automatically set the ID for the new appearance
        appearance.id = appearanceLoader.GetNextAvailableId();

        StringBuilder sb = new StringBuilder();

        // Start of the appearance section
        sb.AppendLine($"\tAppearance(\"{appearance.Name}\")");
        sb.AppendLine("\t{");
        sb.AppendLine($"\t\tModelFpp(\"{appearance.modelFpp}\");");
        sb.AppendLine($"\t\tModelTpp(\"{appearance.modelTpp}\");");
        if (appearance.overridesOutfit)
        {
            sb.AppendLine("\t\tOverridesOutfit(true);");
        }
        sb.AppendLine($"\t\tColor(\"{appearance.color}\");");
        // Add more fields as needed
        sb.AppendLine($"\t\tImage(\"{appearance.image}\");");
        sb.AppendLine($"\t\tCategory({appearance.category});");
        sb.AppendLine($"\t\tID({appearance.id});");
        foreach (var dlc in appearance.requiredDLCs)
        {
            sb.AppendLine($"\t\tRequiredDLCs(\"{dlc}\");");
        }
        if (appearance.availableOnStart)
        {
            sb.AppendLine("\t\tAvailableOnStart();");
        }
        sb.AppendLine("\t\tHint(\"{appearance.hint}\");");
        // End of the appearance section
        sb.AppendLine("\t}");

        // Now, append this appearance to the file.
        File.AppendAllText(filePath, sb.ToString());

        Debug.Log("New appearance added: " + appearance.Name);
    }
}