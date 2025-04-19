using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallScript : MonoBehaviour
{
    private bool hasWon = false;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hole"))
        {
            hasWon = true;
            Destroy(gameObject); 
        }
    }
}
