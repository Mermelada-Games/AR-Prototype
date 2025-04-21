using System.Collections.Generic;
using System.Linq;
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
    private List<Vector3> points = new List<Vector3>();

    public static Vector3 BallSpawnPosition { get; private set; }
    public static bool IsBallImageTracked { get; private set; }
    public static bool IsHoleImageTracked { get; private set; }
    public static Vector3 HolePosition { get; private set; }
    
    private bool AreAllWaypointsTracked => orderedPrefabs.Count == maxNumber && orderedPrefabs.All(p => p != null);

    private void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        IsBallImageTracked = false;
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

    private void UpdateImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        string prefix = "number_";
        string holeImage = "mermelada";
        bool shouldUpdateSegments = false;
        string ballImage = "ball_spawn";

        if(imageName == ballImage)
        {
            Vector3 position = trackedImage.transform.position;
            BallSpawnPosition = position;
            IsBallImageTracked = trackedImage.trackingState == TrackingState.Tracking;

            if (spawnedPrefabs.ContainsKey(ballImage))
            {
                spawnedPrefabs[ballImage].transform.position = position;
                spawnedPrefabs[ballImage].SetActive(trackedImage.trackingState == TrackingState.Tracking);
            }
        }
        
        if (imageName == holeImage)
        {
            Vector3 position = trackedImage.transform.position;
            HolePosition = position;
            IsHoleImageTracked = trackedImage.trackingState == TrackingState.Tracking;
            
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

        points.Clear(); 
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

        Rigidbody rb = planeObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

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
        
        MeshCollider meshCollider = planeObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }
    
    public bool IsPointInsideCourse(Vector3 point)
    {
        if (planeObject == null || points.Count < 3)
            return false;
            
        Vector2 point2D = new Vector2(point.x, point.z);
        List<Vector2> polygon = new List<Vector2>();
        
        foreach (Vector3 vert in points)
        {
            polygon.Add(new Vector2(vert.x, vert.z));
        }
        
        return IsPointInPolygon(point2D, polygon);
    }
    
    private bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        int i, j;
        bool result = false;
        for (i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
            {
                result = !result;
            }
        }
        return result;
    }
    
    public bool AreAllPointsTracked()
    {
        return AreAllWaypointsTracked && IsBallImageTracked && IsHoleImageTracked;
    }
    
    public bool AreSpawnAndHoleInsideCourse()
    {
        return IsPointInsideCourse(BallSpawnPosition) && IsPointInsideCourse(HolePosition);
    }
    
    public bool CanStartGame()
    {
        return AreAllPointsTracked() && AreSpawnAndHoleInsideCourse();
    }
}