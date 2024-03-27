using System.Collections;
using UnityEngine;

namespace doppelganger
{
    public class AudioManager : MonoBehaviour
    {
        public AudioSource backgroundMusicSource;
        public AudioSource uiSoundSource;
        private bool applicationLoaded;
        private bool initialFocusReceived = false;
        private bool isFading = false;
        public float startFadeTime = 5.5f;
        public float focusFadeTime = 5.5f;
        public float volumeChangeAmount = 0.2f;
        public float maxStartVolume = 0.75f;
        public float backgroundMusicLastVolume;
        public float uiSoundLastVolume;

        private void Awake()
        {
            bool hasFocus = true;
        }

        void Start()
        {
            string musicVolume = ConfigManager.LoadSetting("Audio", "musicVolume");
            if (!string.IsNullOrEmpty(musicVolume))
            {
                backgroundMusicLastVolume = float.Parse(musicVolume);
            }
            else
            {
                backgroundMusicLastVolume = maxStartVolume;
            }

            string uiVolume = ConfigManager.LoadSetting("Audio", "uiVolume");
            if (!string.IsNullOrEmpty(uiVolume))
            {
                uiSoundLastVolume = float.Parse(uiVolume);
            }
            else
            {
                uiSoundLastVolume = maxStartVolume;
            }

            backgroundMusicSource.volume = 0;
            //uiSoundSource.volume = 0;
            applicationLoaded = false;
            initialFocusReceived = false;

            FadeIn(backgroundMusicSource, startFadeTime, () => {
                applicationLoaded = true;
            });
            FadeIn(uiSoundSource, startFadeTime);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            //Debug.Log($"OnApplicationFocus: {hasFocus}, applicationLoaded: {applicationLoaded}, initialFocusReceived: {initialFocusReceived}");

            if (!initialFocusReceived && applicationLoaded)
            {
                initialFocusReceived = true;
                Debug.Log("Initial focus received after start-up.");
                return;
            }

            if (applicationLoaded && initialFocusReceived)
            {
                if (hasFocus)
                {
                    //Debug.Log("Application gained focus - fading in audio.");
                    FadeIn(backgroundMusicSource, focusFadeTime);
                    FadeIn(uiSoundSource, focusFadeTime);
                }
                else
                {
                    //Debug.Log("Application lost focus - fading out audio.");
                    FadeOut(backgroundMusicSource, focusFadeTime);
                    FadeOut(uiSoundSource, focusFadeTime);
                }
            }
        }


        void Update()
        {
            if (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus))
            {
                backgroundMusicSource.volume = Mathf.Min(backgroundMusicSource.volume + volumeChangeAmount * Time.deltaTime, 1f);
                backgroundMusicLastVolume = backgroundMusicSource.volume;
                ConfigManager.SaveSetting("Audio", "musicVolume", backgroundMusicLastVolume.ToString());
            }

            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
            {
                backgroundMusicSource.volume = Mathf.Max(backgroundMusicSource.volume - volumeChangeAmount * Time.deltaTime, 0f);
                backgroundMusicLastVolume = backgroundMusicSource.volume;
                ConfigManager.SaveSetting("Audio", "musicVolume", backgroundMusicLastVolume.ToString());
            }
        }

        public void PlayUIAudio(AudioClip clip)
        {
            uiSoundSource.clip = clip;
            uiSoundSource.Play();
        }

        public delegate void OnFadeComplete();

        public void FadeIn(AudioSource audioSource, float duration, OnFadeComplete onComplete = null)
        {
            if (!isFading)
            {
                StartCoroutine(FadeAudioSource(audioSource, duration, backgroundMusicLastVolume, audioSource == backgroundMusicSource, onComplete));
            }
        }

        public void FadeOut(AudioSource audioSource, float duration)
        {
            if (!isFading)
            {
                StartCoroutine(FadeAudioSource(audioSource, duration, 0, audioSource == backgroundMusicSource, null));
            }
        }

        private IEnumerator FadeAudioSource(AudioSource audioSource, float duration, float targetVolume, bool isBackgroundMusic, OnFadeComplete onComplete)
        {
            isFading = true;
            float currentTime = 0;
            float startVolume = audioSource.volume;

            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / duration);
                yield return null;
            }

            audioSource.volume = targetVolume;
            isFading = false;

            if (targetVolume > 0 && isBackgroundMusic && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
            else if (targetVolume == 0 && isBackgroundMusic)
            {
                audioSource.Pause();
            }

            onComplete?.Invoke();
        }
    }
}