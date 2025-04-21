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
        if (ImageTracking.IsBallImageTracked)
        {
            Vector3 spawnPosition = ImageTracking.BallSpawnPosition;
            GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
            balls.Add(ball);
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
