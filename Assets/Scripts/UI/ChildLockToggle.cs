using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace doppelganger
{
    public class ChildLockToggle : MonoBehaviour
    {
        [Header("Toggle Components")]
        public Toggle childToggle;
        public Image toggleImage;
        public TextMeshProUGUI statusText;

        [Header("State Visuals")]
        public Sprite lockedSprite;
        public Sprite unlockedSprite;

        [Header("State Texts")]
        public string textWhenLocked = "Locked";
        public string textWhenUnlocked = "Unlocked";

        [Header("Audio")]
        public AudioSource audioSource; // Assign an AudioSource component
        public AudioClip lockSound;
        public AudioClip unlockSound;

        void Awake()
        {
            if (childToggle == null)
            {
                Debug.LogError("ChildLockToggle script is not attached to a GameObject with a Toggle component", this);
                return;
            }

            // Subscribe to the toggle's value changed event
            childToggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        public void UpdateLockState(bool isLocked)
        {
            if (childToggle != null)
            {
                childToggle.isOn = isLocked;
                UpdateVisuals(isLocked);
            }
        }

        private void OnToggleValueChanged(bool isOn)
        {
            UpdateVisuals(isOn);

            // Play the appropriate sound
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

        private void UpdateVisuals(bool isOn)
        {
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
    }
}