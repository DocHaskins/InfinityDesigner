using UnityEngine;
using UnityEngine.UI;

namespace doppelganger
{
    public class MasterLockToggle : MonoBehaviour
    {
        [Header("Toggle Components")]
        public Toggle masterToggle;
        public Image toggleImage;
        public Text statusLockedText;
        public Text statusUnlockedText;

        [Header("State Visuals")]
        public Sprite lockedSprite;
        public Sprite unlockedSprite;

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
            SetSlidersLockState(isOn);

            PlaySound(isOn);
        }

        private void SetSlidersLockState(bool unlockState)
        {
            if (sliderPanel == null) return;

            ChildLockToggle[] childLocks = sliderPanel.GetComponentsInChildren<ChildLockToggle>(true);
            foreach (ChildLockToggle childLock in childLocks)
            {
                childLock.UpdateLockState(unlockState);
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
}