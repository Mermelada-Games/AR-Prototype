using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class BallScript : MonoBehaviour
{
    private bool hasWon = false;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hole"))
        {
            hasWon = true;
            SceneManager.LoadScene("ResultsScene");
            Destroy(gameObject); 
        }
    }
}
