using UnityEngine;

namespace doppelganger
{
    public class UIToggleButton : MonoBehaviour
    {
        [Header("Interface")]
        public CanvasGroup uiGroup;
        public AudioSource audioSource;

        [Header("Audio")]
        public AudioClip onClip;
        public AudioClip offClip;

        private bool isUIVisible = false;

        public void ToggleUI()
        {
            isUIVisible = !isUIVisible;

            // Set CanvasGroup properties based on the toggled state
            uiGroup.alpha = isUIVisible ? 1 : 0;
            uiGroup.interactable = isUIVisible;
            uiGroup.blocksRaycasts = isUIVisible;

            PlaySound(isUIVisible);
        }

        void PlaySound(bool isVisible)
        {
            audioSource.clip = isVisible ? onClip : offClip;
            audioSource.Play();
        }
    }
}