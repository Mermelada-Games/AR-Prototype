using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class BallScript : MonoBehaviour
{
    private bool hasWon = false;
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
            SceneManager.LoadScene("ResultsScene");
            Destroy(gameObject); 
        }
    }

}
