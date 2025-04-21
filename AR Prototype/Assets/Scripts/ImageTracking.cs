using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTracking : MonoBehaviour
{
    [SerializeField] private GameObject[] numberPrefabs;
    [SerializeField] private GameObject holePrefab;
    [SerializeField] private GameObject ballPrefab;

    private ARTrackedImageManager trackedImageManager;
    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();
    private List<GameObject> orderedPrefabs = new List<GameObject>();
    private List<GameObject> collisionSegments = new List<GameObject>();

    private void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            UpdateImage(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            UpdateImage(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.removed)
        {
            if (spawnedPrefabs.ContainsKey(trackedImage.referenceImage.name))
            {
                Destroy(spawnedPrefabs[trackedImage.referenceImage.name]);
                spawnedPrefabs.Remove(trackedImage.referenceImage.name);
            }
        }
    }

    private void SpawnBall(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        string ballImage = "ball_spawn";

        if(imageName == ballImage)
        {
            Vector3 position = trackedImage.transform.position;
            if (spawnedPrefabs.ContainsKey(ballImage))
            {
                spawnedPrefabs[ballImage].transform.position = position;
                spawnedPrefabs[ballImage].SetActive(trackedImage.trackingState == TrackingState.Tracking);
            }
            else if (ballPrefab != null)
            {
                GameObject prefab = Instantiate(ballPrefab, position, Quaternion.identity);
                spawnedPrefabs.Add(ballImage, prefab);
            }
        }
    }
    private void UpdateImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        string prefix = "number_";
        string holeImage = "mermelada";

        SpawnBall(trackedImage);

        if(imageName == holeImage)
        {
            Vector3 position = trackedImage.transform.position;
            if (spawnedPrefabs.ContainsKey(holeImage))
            {
                spawnedPrefabs[holeImage].transform.position = position;
                spawnedPrefabs[holeImage].SetActive(trackedImage.trackingState == TrackingState.Tracking);
            }
            else if (holePrefab != null)
            {
                GameObject prefab = Instantiate(holePrefab, position, Quaternion.identity);
                spawnedPrefabs.Add(holeImage, prefab);
            }
        }
        if (imageName.StartsWith(prefix))
        {
            string numberPart = imageName.Substring(prefix.Length);

            if (int.TryParse(numberPart, out int number) && number >= 0 && number < numberPrefabs.Length)
            {
                Vector3 position = trackedImage.transform.position;

                if (spawnedPrefabs.ContainsKey(imageName))
                {
                    spawnedPrefabs[imageName].transform.position = position;
                    spawnedPrefabs[imageName].SetActive(trackedImage.trackingState == TrackingState.Tracking);
                }
                else if (numberPrefabs[number] != null)
                {
                    GameObject prefab = Instantiate(numberPrefabs[number], position, Quaternion.identity);
                    spawnedPrefabs.Add(imageName, prefab);

                    while (orderedPrefabs.Count <= number)
                    {
                        orderedPrefabs.Add(null);
                    }
                    orderedPrefabs[number] = prefab;

                }
        
                UpdateSegments();
            }
        }
    }

    private void UpdateSegments()
    {
        foreach (var segment in collisionSegments)
        {
            if (segment != null) Destroy(segment);
        }
        collisionSegments.Clear();

        for (int i = 0; i < orderedPrefabs.Count - 1; i++)
        {
            GameObject a = orderedPrefabs[i];
            GameObject b = orderedPrefabs[i + 1];

            if (a != null && b != null)
            {
                Vector3 start = a.transform.position;
                Vector3 end = b.transform.position;
                Vector3 center = (start + end) / 2;
                Vector3 direction = end - start;
                float length = direction.magnitude;

                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = $"Segment_{i}_{i + 1}";
                segment.transform.position = center;
                segment.transform.rotation = Quaternion.LookRotation(direction.normalized);
                segment.transform.localScale = new Vector3(0.01f, 0.01f, length);

                BoxCollider collider = segment.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = segment.AddComponent<BoxCollider>();
                }
                collider.isTrigger = false;

                collisionSegments.Add(segment);
            }
        }
    }
}
