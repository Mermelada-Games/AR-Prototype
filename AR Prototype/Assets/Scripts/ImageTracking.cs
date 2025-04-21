using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTracking : MonoBehaviour
{
    [SerializeField] private GameObject[] numberPrefabs;
    [SerializeField] private GameObject holePrefab;
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private int maxNumber;
    [SerializeField] private Material planeMaterial;

    private ARTrackedImageManager trackedImageManager;
    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();
    private List<GameObject> orderedPrefabs = new List<GameObject>();
    private List<GameObject> collisionSegments = new List<GameObject>();
    private GameObject planeObject;

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
        bool shouldUpdateSegments = false;
        
        SpawnBall(trackedImage);
        
        if (imageName == holeImage)
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
        else if (imageName.StartsWith(prefix))
        {
            string numberPart = imageName.Substring(prefix.Length);

            if (int.TryParse(numberPart, out int number) && number > 0 && number <= maxNumber)
            {
                number -= 1;
                Vector3 position = trackedImage.transform.position;

                if (spawnedPrefabs.ContainsKey(imageName))
                {
                    Vector3 oldPosition = spawnedPrefabs[imageName].transform.position;
                    if (Vector3.Distance(oldPosition, position) > 0.01f)
                    {
                        spawnedPrefabs[imageName].transform.position = position;
                        shouldUpdateSegments = true;
                    }
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
                    shouldUpdateSegments = true;
                }
            }
        }

        if (shouldUpdateSegments)
        {
            UpdateSegments();
            CreatePlane();
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
                CreateSegment(a, b, $"Segment_{i}_{i + 1}");
            }
        }
        
        if (orderedPrefabs.Count > 1)
        {
            GameObject first = orderedPrefabs[0];
            GameObject last = orderedPrefabs[orderedPrefabs.Count - 1];

            if (first != null && last != null)
            {
                CreateSegment(last, first, $"Segment_{orderedPrefabs.Count - 1}_0");
            }
        }
    }

    private void CreateSegment(GameObject a, GameObject b, string segmentName)
    {
        Vector3 start = a.transform.position;
        Vector3 end = b.transform.position;
        Vector3 center = (start + end) / 2;
        Vector3 direction = end - start;
        float length = direction.magnitude;

        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = segmentName;
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

    private void CreatePlane()
    {
        if (planeObject != null)
            Destroy(planeObject);

        List<Vector3> points = new List<Vector3>();
        foreach (GameObject prefab in orderedPrefabs)
        {
            if (prefab != null)
                points.Add(prefab.transform.position);
        }

        if (points.Count < 3)
            return;

        planeObject = new GameObject("GolfPlane");
        MeshFilter meshFilter = planeObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = planeObject.AddComponent<MeshRenderer>();
        meshRenderer.material = planeMaterial;

        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;

        Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;

        if (normal.y < 0)
            normal = -normal;
        
        Vector3[] vertices = points.ToArray();
        mesh.vertices = vertices;
        
        int[] triangles = new int[(points.Count - 2) * 3];
        for (int i = 0; i < points.Count - 2; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        mesh.triangles = triangles;

        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = normal;
        }
        mesh.normals = normals;

        Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            uv[i] = new Vector2(vertices[i].x, vertices[i].z);
        }
        mesh.uv = uv;
        
        mesh.RecalculateBounds();
    }
}