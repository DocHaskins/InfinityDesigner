using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Needed for IEnumerator

namespace doppelganger
{
    public class SceneLoader : MonoBehaviour
    {
        public AudioManager audioManager;
        public UnityEngine.UI.Slider progressBar;

        public void LoadScene1()
        {
            StartCoroutine(LoadSceneAsync("ModelViewer"));
        }

        public void LoadScene2()
        {
            StartCoroutine(LoadSceneAsync("CharacterDesigner"));
        }

        IEnumerator LoadSceneAsync(string sceneName)
        {
            AudioManager.instance?.FadeOut(AudioManager.instance.backgroundMusicSource, 1f);
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                //progressBar.value = progress;
                if (asyncLoad.progress >= 0.9f)
                {
                    asyncLoad.allowSceneActivation = true;
                    AudioManager.instance.FadeIn(AudioManager.instance.backgroundMusicSource, AudioManager.instance.focusFadeTime);
                }

                yield return null;
            }

            SceneManager.UnloadSceneAsync("Start");
        }

        public void LoadWebsite(string url)
        {
            Application.OpenURL(url);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SceneManager.LoadScene("Start");
            }
        }
    }
}