using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("LevelSelector");
    }

    public void StartLevel1()
    {
        SceneManager.LoadScene("TestScene");
    }
}
