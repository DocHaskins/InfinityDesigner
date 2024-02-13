using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene1()
    {
        SceneManager.LoadScene("ModelViewer");
    }

    public void LoadScene2()
    {
        SceneManager.LoadScene("CharacterDesigner");
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