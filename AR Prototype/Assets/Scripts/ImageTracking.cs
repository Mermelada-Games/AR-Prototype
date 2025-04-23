using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using UnityEngine.UI;

public class ImageTracking : MonoBehaviour
{
    [SerializeField] private GameObject[] numberPrefabs;
    [SerializeField] private GameObject holePrefab;
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private int maxNumber;
    [SerializeField] private Material planeMaterial;

    [SerializeField] private TextMeshProUGUI trackedCountText;
    [SerializeField] private TextMeshProUGUI ballStatusText;
    [SerializeField] private TextMeshProUGUI holeStatusText;
    [SerializeField] private TextMeshProUGUI currentTrackingText;
    [SerializeField] private Button confirmButton;

    private ARTrackedImageManager trackedImageManager;
    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();
    private List<GameObject> orderedPrefabs = new List<GameObject>();
    private List<GameObject> collisionSegments = new List<GameObject>();
    private GameObject planeObject;
    private List<Vector3> points = new List<Vector3>();

    private int currentTrackingStep = 0;
    private Dictionary<string, bool> confirmedTracking = new Dictionary<string, bool>();
    private enum TrackingType { Number, Ball, Hole }
    private TrackingType currentTrackingType = TrackingType.Number;
    private int currentNumberTracking = 1;

    private float fixedYPosition = float.MinValue;
    private bool heightEstablished = false;

    public static Vector3 BallSpawnPosition { get; private set; }
    public static bool IsBallImageTracked { get; private set; }
    public static bool IsHoleImageTracked { get; private set; }
    public static Vector3 HolePosition { get; private set; }

    private bool AreAllWaypointsTracked => orderedPrefabs.Count == maxNumber && orderedPrefabs.All(p => p != null);

    private void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        IsBallImageTracked = false;

        for (int i = 1; i <= maxNumber; i++)
        {
            confirmedTracking.Add($"number_{i}", false);
        }
        confirmedTracking.Add("ball_spawn", false);
        confirmedTracking.Add("hole", false);

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmCurrentTracking);
        }
    }

    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void Start()
    {
        UpdateCurrentTrackingText();
    }

    private void Update()
    {
        UpdateUI();
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            if (ShouldProcessImage(trackedImage))
                UpdateImage(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            if (ShouldProcessImage(trackedImage))
                UpdateImage(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.removed)
        {
            string imageName = trackedImage.referenceImage.name;
            if (spawnedPrefabs.ContainsKey(imageName) && !IsImageConfirmed(imageName))
            {
                Destroy(spawnedPrefabs[imageName]);
                spawnedPrefabs.Remove(imageName);
            }
        }
    }

    private bool ShouldProcessImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (IsImageConfirmed(imageName))
            return false;

        if (currentTrackingType == TrackingType.Number && imageName == $"number_{currentNumberTracking}")
            return true;
        else if (currentTrackingType == TrackingType.Ball && imageName == "ball_spawn")
            return true;
        else if (currentTrackingType == TrackingType.Hole && imageName == "hole")
            return true;

        return false;
    }

    private bool IsImageConfirmed(string imageName)
    {
        return confirmedTracking.ContainsKey(imageName) && confirmedTracking[imageName];
    }

    private void UpdateImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        string prefix = "number_";
        string holeImage = "hole";
        bool shouldUpdateSegments = false;
        string ballImage = "ball_spawn";

        Vector3 position = GetAdjustedPosition(trackedImage.transform.position);

        if (imageName == ballImage && currentTrackingType == TrackingType.Ball)
        {
            BallSpawnPosition = position;
            IsBallImageTracked = trackedImage.trackingState == TrackingState.Tracking;

            if (spawnedPrefabs.ContainsKey(ballImage))
            {
                spawnedPrefabs[ballImage].transform.position = position;
            }
        }

        if (imageName == holeImage && currentTrackingType == TrackingType.Hole)
        {
            HolePosition = position;
            IsHoleImageTracked = trackedImage.trackingState == TrackingState.Tracking;

            if (spawnedPrefabs.ContainsKey(holeImage))
            {
                spawnedPrefabs[holeImage].transform.position = position;
            }
            else if (holePrefab != null)
            {
                GameObject prefab = Instantiate(holePrefab, position, Quaternion.identity);
                spawnedPrefabs.Add(holeImage, prefab);
            }
        }
        else if (imageName.StartsWith(prefix) && currentTrackingType == TrackingType.Number)
        {
            string numberPart = imageName.Substring(prefix.Length);

            if (int.TryParse(numberPart, out int number) && number == currentNumberTracking)
            {
                number -= 1;

                if (spawnedPrefabs.ContainsKey(imageName))
                {
                    Vector3 oldPosition = spawnedPrefabs[imageName].transform.position;
                    if (Vector3.Distance(new Vector3(oldPosition.x, 0, oldPosition.z),
                                        new Vector3(position.x, 0, position.z)) > 0.01f)
                    {
                        spawnedPrefabs[imageName].transform.position = position;
                        shouldUpdateSegments = true;
                    }
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

        if (confirmButton != null)
        {
            bool isCurrentImageTracked = false;

            if (currentTrackingType == TrackingType.Number && imageName == $"number_{currentNumberTracking}")
                isCurrentImageTracked = true;
            else if (currentTrackingType == TrackingType.Ball && imageName == "ball_spawn")
                isCurrentImageTracked = IsBallImageTracked;
            else if (currentTrackingType == TrackingType.Hole && imageName == "hole")
                isCurrentImageTracked = IsHoleImageTracked;

            confirmButton.interactable = isCurrentImageTracked;
        }
    }

    private Vector3 GetAdjustedPosition(Vector3 originalPosition)
    {
        if (!heightEstablished)
        {
            return originalPosition;
        }
        else
        {
            return new Vector3(originalPosition.x, fixedYPosition, originalPosition.z);
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
            normals[i] = Vector3.up;
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

    private void UpdateUI()
    {
        bool allPointsTracked = orderedPrefabs.Count(p => p != null) == maxNumber;
        int confirmedPoints = confirmedTracking.Count(kvp => kvp.Key.StartsWith("number_") && kvp.Value);

        trackedCountText.text = $"Tracked Points: {confirmedPoints}/{maxNumber}";
        trackedCountText.color = (confirmedPoints == maxNumber) ? Color.green : Color.red;

        ballStatusText.text = confirmedTracking["ball_spawn"] ? "Ball Tracked" : "Ball Not Tracked";
        ballStatusText.color = confirmedTracking["ball_spawn"] ? Color.green : Color.red;

        holeStatusText.text = confirmedTracking["hole"] ? "Hole Tracked" : "Hole Not Tracked";
        holeStatusText.color = confirmedTracking["hole"] ? Color.green : Color.red;

        UpdateCurrentTrackingText();
    }

    private void UpdateCurrentTrackingText()
    {
        if (currentTrackingText != null)
        {
            string trackingName = "";

            switch (currentTrackingType)
            {
                case TrackingType.Number:
                    trackingName = $"Point {currentNumberTracking}";
                    break;
                case TrackingType.Ball:
                    trackingName = "Ball";
                    break;
                case TrackingType.Hole:
                    trackingName = "Hole";
                    break;
            }

            currentTrackingText.text = $"Colocar: {trackingName}";
        }
    }

    public void ConfirmCurrentTracking()
    {
        switch (currentTrackingType)
        {
            case TrackingType.Number:
                string imageName = $"number_{currentNumberTracking}";
                confirmedTracking[imageName] = true;

                if (!heightEstablished && spawnedPrefabs.ContainsKey(imageName))
                {
                    fixedYPosition = spawnedPrefabs[imageName].transform.position.y;
                    heightEstablished = true;
                }

                currentNumberTracking++;
                if (currentNumberTracking > maxNumber)
                {
                    currentTrackingType = TrackingType.Ball;
                }
                break;

            case TrackingType.Ball:
                confirmedTracking["ball_spawn"] = true;
                currentTrackingType = TrackingType.Hole;
                break;

            case TrackingType.Hole:
                confirmedTracking["hole"] = true;
                break;
        }

        UpdateCurrentTrackingText();
        confirmButton.interactable = false;

        CanStartGame();
    }

    private void AdjustAllObjectsToSameHeight()
    {
        if (!heightEstablished)
            return;

        foreach (var kvp in spawnedPrefabs)
        {
            GameObject obj = kvp.Value;
            if (obj != null)
            {
                Vector3 pos = obj.transform.position;
                obj.transform.position = new Vector3(pos.x, fixedYPosition, pos.z);
            }
        }

        if (IsBallImageTracked)
        {
            BallSpawnPosition = new Vector3(BallSpawnPosition.x, fixedYPosition, BallSpawnPosition.z);
        }

        if (IsHoleImageTracked)
        {
            HolePosition = new Vector3(HolePosition.x, fixedYPosition, HolePosition.z);
        }

        UpdateSegments();
        CreatePlane();
    }

    public bool AreAllPointsTracked()
    {
        bool allNumbersConfirmed = confirmedTracking.Count(kvp => kvp.Key.StartsWith("number_") && kvp.Value) == maxNumber;
        return allNumbersConfirmed && confirmedTracking["ball_spawn"] && confirmedTracking["hole"];
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