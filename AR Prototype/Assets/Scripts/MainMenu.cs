using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject panelHowToPlay;
    public GameObject panelSettings;

    public void StartGame()
    {
        SceneManager.LoadScene("TestScene");
    }

    public void ShowHowToPlay()
    {
        panelHowToPlay.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        panelHowToPlay.SetActive(false);
    }

}
