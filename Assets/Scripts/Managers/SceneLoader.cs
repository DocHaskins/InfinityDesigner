using UnityEngine;
using UnityEngine.SceneManagement;

namespace doppelganger
{
    public class SceneLoader : MonoBehaviour
    {
        public AudioManager audioManager;
        public void LoadScene1()
        {
            audioManager.FadeOut(audioManager.backgroundMusicSource, 1f);
            SceneManager.LoadScene("ModelViewer");
        }

        public void LoadScene2()
        {
            audioManager.FadeOut(audioManager.backgroundMusicSource, 1f);
            SceneManager.LoadScene("CharacterDesigner");
        }

        public void LoadWebsite(string url)
        {
            Application.OpenURL(url);
        }

        public void QuitApplication()
        {
            Application.Quit();
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