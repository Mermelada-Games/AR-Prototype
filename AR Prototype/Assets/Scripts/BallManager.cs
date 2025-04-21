using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class BallManager : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab;

    private List<GameObject> balls = new List<GameObject>(); 
    [SerializeField] private Button startButton;
    
    private ImageTracking imageTracker;
    private bool buttonActivated = false;
    private bool isPlaying = false;

    private void Start()
    {
        imageTracker = FindObjectOfType<ImageTracking>();
        if (startButton != null)
        {
            startButton.gameObject.SetActive(false);
        }
    }

    public void GenerateBall()
    {
        DestroyAllBalls();
        
        if (ImageTracking.IsBallImageTracked && !isPlaying)
        {
            Vector3 spawnPosition = ImageTracking.BallSpawnPosition;
            GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
            balls.Add(ball);
            startButton.gameObject.SetActive(false);
            isPlaying = true;
        }
    }

    public void DestroyAllBalls()
    {
        foreach (GameObject ball in balls)
        {
            if (ball != null)
            {
                Destroy(ball);
            }
        }
        balls.Clear(); 
    }

    public void Update()
    {
        bool canStart = imageTracker != null && imageTracker.CanStartGame();
        
        if (canStart != buttonActivated && startButton != null && !isPlaying)
        {
            startButton.gameObject.SetActive(canStart);
            buttonActivated = canStart;
            
            if (canStart)
            {
                Debug.Log("Start button activated: All conditions met!");
            }
        }
    }
}
