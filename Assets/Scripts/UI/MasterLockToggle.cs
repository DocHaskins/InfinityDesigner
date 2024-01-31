using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MasterLockToggle : MonoBehaviour
{
    [Header("Toggle Components")]
    public Toggle masterToggle;
    public Image toggleImage;
    public TextMeshProUGUI statusText;

    [Header("State Visuals")]
    public Sprite lockedSprite;
    public Sprite unlockedSprite;

    [Header("State Texts")]
    public string textWhenLocked = "Locked";
    public string textWhenUnlocked = "Unlocked";

    [Header("Audio")]
    public AudioClip unlockSound;
    public AudioClip lockSound;
    public AudioSource audioSource;

    [Header("Sliders")]
    public GameObject sliderPanel;

    void Start()
    {
        masterToggle.onValueChanged.AddListener(OnToggleValueChanged);
        UpdateVisuals(masterToggle.isOn);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        UpdateVisuals(isOn);
        SetSlidersLockState(isOn); // Ensure this line is included to update child toggles

        PlaySound(isOn); // This function encapsulates the sound playing logic
    }

    private void SetSlidersLockState(bool unlockState)
    {
        if (sliderPanel == null) return;

        // Find all child toggles with the ChildLockToggle script
        ChildLockToggle[] childLocks = sliderPanel.GetComponentsInChildren<ChildLockToggle>(true);
        foreach (ChildLockToggle childLock in childLocks)
        {
            // Update each child lock state without removing listeners
            childLock.UpdateLockState(unlockState);
        }
    }

    private void UpdateVisuals(bool isOn)
    {
        // Reverse the sprite and text assignment logic to match the intended semantics
        if (toggleImage != null)
        {
            toggleImage.sprite = isOn ? lockedSprite : unlockedSprite;
        }

        // Update the text to reflect the correct state based on `isOn`
        if (statusText != null)
        {
            statusText.text = isOn ? textWhenLocked : textWhenUnlocked;
        }
    }

    private void PlaySound(bool isOn)
    {
        if (audioSource != null)
        {
            audioSource.clip = isOn ? unlockSound : lockSound;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("AudioSource is missing", this);
        }
    }
}