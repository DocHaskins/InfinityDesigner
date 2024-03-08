using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro; // Make sure you have the TextMeshPro package installed

public class ConvertToTMPComponents : Editor
{
    [MenuItem("CONTEXT/Text/Convert to TMP_Text")]
    private static void ConvertTextToTMP(MenuCommand command)
    {
        Text oldText = command.context as Text;
        GameObject gameObject = oldText.gameObject;

        // Record changes for Undo
        Undo.RecordObject(gameObject, "Convert Text to TMP_Text");

        // Copy properties
        string text = oldText.text;
        FontStyles fontStyle = oldText.fontStyle.ToTMPFontStyle();
        float fontSize = oldText.fontSize;
        Color fontColor = oldText.color;
        bool enableAutoSizing = oldText.resizeTextForBestFit;
        float minFontSize = oldText.resizeTextMinSize;
        float maxFontSize = oldText.resizeTextMaxSize;
        TextAnchor alignment = oldText.alignment;
        bool enableWordWrapping = oldText.horizontalOverflow == HorizontalWrapMode.Wrap;
        bool enableRichText = oldText.supportRichText;

        // Remove the old Text component
        Undo.DestroyObjectImmediate(oldText);

        // Add the TextMeshProUGUI component
        TextMeshProUGUI newText = Undo.AddComponent<TextMeshProUGUI>(gameObject);
        newText.text = text;
        newText.fontStyle = fontStyle;
        newText.fontSize = fontSize;
        newText.color = fontColor;
        // TextMeshPro uses different properties for some settings:
        newText.enableAutoSizing = enableAutoSizing;
        newText.fontSizeMin = minFontSize;
        newText.fontSizeMax = maxFontSize;
        newText.alignment = alignment.ToTMPAlignment();
        newText.enableWordWrapping = enableWordWrapping;
        newText.richText = enableRichText;

        // Apply any additional conversions as necessary
    }

    [MenuItem("CONTEXT/InputField/Convert to TMP_InputField")]
    private static void ConvertInputFieldToTMP(MenuCommand command)
    {
        InputField oldInputField = command.context as InputField;
        if (oldInputField == null)
        {
            Debug.LogError("ConvertInputFieldToTMP: No InputField component found.");
            return;
        }

        GameObject gameObject = oldInputField.gameObject;

        // Record changes for Undo
        Undo.RecordObject(gameObject, "Convert InputField to TMP_InputField");

        // First convert the textComponent to TMP_Text
        Text oldTextComponent = oldInputField.textComponent;
        TMP_Text newTextComponent = null;
        if (oldTextComponent != null)
        {
            newTextComponent = ConvertTextComponentToTMP(oldTextComponent);
        }

        // Now, convert the placeholder if it exists
        TMP_Text newPlaceholder = null;
        if (oldInputField.placeholder != null)
        {
            newPlaceholder = ConvertTextComponentToTMP(oldInputField.placeholder as Text);
        }

        // Store remaining properties from the old InputField before it is destroyed.
        string oldText = oldInputField.text;
        int oldCharacterLimit = oldInputField.characterLimit;
        InputField.ContentType oldContentType = oldInputField.contentType;
        InputField.LineType oldLineType = oldInputField.lineType;

        // Remove the old InputField component to avoid conflict.
        Undo.DestroyObjectImmediate(oldInputField);

        // Add the new TMP_InputField component now that the old one is removed.
        TMP_InputField newInputField = Undo.AddComponent<TMP_InputField>(gameObject);

        // Assign the properties from the old InputField to the new TMP_InputField.
        newInputField.text = oldText;
        newInputField.textComponent = newTextComponent as TextMeshProUGUI;
        newInputField.placeholder = newPlaceholder;
        newInputField.characterLimit = oldCharacterLimit;
        newInputField.contentType = (TMP_InputField.ContentType)oldContentType;
        newInputField.lineType = (TMP_InputField.LineType)oldLineType;

        // Assign other properties as necessary.
    }

    // Helper method to convert Text to TMP_Text for InputField placeholders and text components
    private static TMP_Text ConvertTextComponentToTMP(Text oldTextComponent)
    {
        if (oldTextComponent == null) return null;

        // Record changes for Undo
        Undo.RecordObject(oldTextComponent.gameObject, "Convert Text to TMP_Text");

        // Copy properties from old Text to new TextMeshProUGUI
        TextMeshProUGUI newTextComponent = oldTextComponent.gameObject.AddComponent<TextMeshProUGUI>();
        Undo.RecordObject(newTextComponent, "New TMP_Text Component");

        newTextComponent.text = oldTextComponent.text;
        newTextComponent.fontStyle = oldTextComponent.fontStyle.ToTMPFontStyle();
        newTextComponent.fontSize = oldTextComponent.fontSize;
        newTextComponent.color = oldTextComponent.color;
        newTextComponent.alignment = oldTextComponent.alignment.ToTMPAlignment();

        // Remove the old Text component
        Undo.DestroyObjectImmediate(oldTextComponent);

        return newTextComponent;
    }
}

// Extension methods for converting properties
public static class TMPConversionExtensions
{
    public static FontStyles ToTMPFontStyle(this FontStyle fontStyle)
    {
        // Add logic to convert UnityEngine.FontStyle to TMPro.FontStyles
        return FontStyles.Normal; // Placeholder, extend this based on mapping
    }

    public static TextAlignmentOptions ToTMPAlignment(this TextAnchor anchor)
    {
        // Add logic to convert TextAnchor to TextAlignmentOptions
        return TextAlignmentOptions.Center; // Placeholder, extend this based on mapping
    }
}