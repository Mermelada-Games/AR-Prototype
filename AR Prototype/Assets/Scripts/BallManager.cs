using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class BallManager : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private GameObject stick;

    private List<GameObject> balls = new List<GameObject>(); 
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject canvasElementToActivate;
    
    private ImageTracking imageTracker;
    private bool buttonActivated = false;
    private bool isPlaying = false;
    private GameObject currentStick;

    private void Start()
    {
        imageTracker = FindObjectOfType<ImageTracking>();
        if (startButton != null)
        {
            startButton.gameObject.SetActive(false);
        }
    }

    public void StartGame()
    {
        DestroyAllBalls();
        
        if (ImageTracking.IsBallImageTracked && !isPlaying)
        {
            Vector3 spawnPosition = ImageTracking.BallSpawnPosition;
            GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
            balls.Add(ball);
            startButton.gameObject.SetActive(false);
            isPlaying = true;

            CreateStick();

            if (canvasElementToActivate != null)
            {
                canvasElementToActivate.SetActive(true);
            }
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

    private void CreateStick()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 cameraPosition = mainCamera.transform.position;
            Vector3 spawnPosition = cameraPosition + new Vector3(0, -0.5f, 0);

            currentStick = Instantiate(stick, spawnPosition, Quaternion.identity);
            currentStick.transform.SetParent(mainCamera.transform); 
            currentStick.transform.localRotation = Quaternion.Euler(90, -180, 0);


        }
    }
}
