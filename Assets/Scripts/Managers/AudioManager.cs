using System.Collections;
using UnityEngine;

namespace doppelganger
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager instance;

        public AudioSource backgroundMusicSource;
        public AudioSource uiSoundSource;
        private bool applicationLoaded;
        private bool isFading = false;
        public float startFadeTime = 5.5f;
        public float focusFadeTime = 5.5f;
        public float volumeChangeAmount = 0.2f;
        public float maxStartVolume = 0.75f;
        public float backgroundMusicLastVolume;
        public float uiSoundLastVolume;


        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
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
                uiSoundLastVolume = maxStartVolume; // Default volume or another appropriate default for UI sounds
            }

            backgroundMusicSource.volume = 0; // Start with volume at 0 to fade in from silent
            uiSoundSource.volume = 0; // Same for UI sounds
            FadeIn(backgroundMusicSource, startFadeTime, () => applicationLoaded = true);
            FadeIn(uiSoundSource, startFadeTime);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (applicationLoaded)
            {
                if (!hasFocus)
                {
                    FadeOut(backgroundMusicSource, focusFadeTime);
                    FadeOut(uiSoundSource, focusFadeTime);
                }
                else
                {
                    FadeIn(backgroundMusicSource, focusFadeTime);
                    FadeIn(uiSoundSource, focusFadeTime);
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
            bool isBackgroundMusic = audioSource == backgroundMusicSource;
            StartCoroutine(FadeAudioSource.StartFade(audioSource, duration, backgroundMusicLastVolume, isBackgroundMusic, onComplete));
        }

        public void FadeOut(AudioSource audioSource, float duration)
        {
            bool isBackgroundMusic = audioSource == backgroundMusicSource;
            StartCoroutine(FadeAudioSource.StartFade(audioSource, duration, 0, isBackgroundMusic, null));
        }

        public static class FadeAudioSource
        {
            public static IEnumerator StartFade(AudioSource audioSource, float duration, float targetVolume, bool isBackgroundMusic, OnFadeComplete onComplete)
            {
                instance.isFading = true;  // Indicate that fading is starting

                float currentTime = 0;
                float startVolume = audioSource.volume;

                if (isBackgroundMusic && !audioSource.isPlaying)
                {
                    audioSource.Play();
                }

                while (currentTime < duration)
                {
                    currentTime += Time.deltaTime;
                    audioSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / duration);
                    yield return null;
                }
                audioSource.volume = targetVolume;

                if (targetVolume == 0 && isBackgroundMusic)
                {
                    audioSource.Pause();
                }

                instance.isFading = false;

                if (onComplete != null)
                {
                    onComplete();
                }
            }
        }
    }
}