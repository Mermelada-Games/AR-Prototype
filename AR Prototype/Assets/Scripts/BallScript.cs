using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class BallScript : MonoBehaviour
{
    private AudioManager audioManager;
    private bool hasWon = false;

    private void Start()
    {
        audioManager = AudioManager.instance;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hole") && !hasWon)
        {
            hasWon = true;

            ScoreManager.Instance.BallEnteredHole();

            // Avisamos al ScoreManager
            ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager != null)
            {
                scoreManager.BallEnteredHole();
            }
            if (audioManager != null)
            {
                audioManager.PlayWinAudio();
            }
            StartCoroutine(LoadResultsScene());
        }
    }

    private IEnumerator LoadResultsScene()
    {
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("ResultsScene");
        Destroy(gameObject); 

    }
}
