using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallManager : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab;

    private List<GameObject> balls = new List<GameObject>(); 

    public void GenerateBall()
    {

        if (balls.Count > 0)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 cameraPosition = mainCamera.transform.position;
            Vector3 spawnPosition = cameraPosition + new Vector3(0f, -0.5f, 1f);
            
            GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
            balls.Add(ball); 
        }
        else
        {
            Debug.LogError("Main camera not found!");
        }
    }

    public void DestroyAllBalls()
    {
        foreach (GameObject ball in balls)
        {
            Destroy(ball);
        }
        balls.Clear(); 
    }
}
