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
        SceneManager.LoadScene("Level1");
    }

    public void StartResultsScreen()
    {
        SceneManager.LoadScene("ResultsScene");
    }
    
    public void StartLevel2()
    {
        SceneManager.LoadScene("Level2");
    }

    public void StartLevel3()
    {
        SceneManager.LoadScene("Level3");
    }
}
