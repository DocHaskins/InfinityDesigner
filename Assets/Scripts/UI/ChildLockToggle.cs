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
        public Text statusLockedText;
        public Text statusUnlockedText;

        [Header("State Visuals")]
        public Sprite lockedSprite;
        public Sprite unlockedSprite;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip lockSound;
        public AudioClip unlockSound;

        void Awake()
        {
            if (childToggle == null)
            {
                Debug.LogError("ChildLockToggle script is not attached to a GameObject with a Toggle component", this);
                return;
            }

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

            if (statusLockedText != null && statusUnlockedText != null)
            {
                statusLockedText.gameObject.SetActive(isOn);
                statusUnlockedText.gameObject.SetActive(!isOn);
            }
        }
    }
}