using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARImageScanner : MonoBehaviour
{
    [Header("Core AR Components")]
    public ARTrackedImageManager imageTracker;
    public Transform mainCamera;

    [Header("UI & Prefabs")]
    public GameObject exitButton;
    public GameObject[] planetPrefabs;

    [Header("Spawn Settings")]
    [Tooltip("How far away from the camera the planet spawns (in meters)")]
    public float spawnDistance = 1.2f; // Pushed back to 1.2 meters so you can see the moon!
    [Tooltip("The starting size of the model")]
    public float initialScale = 0.5f;  // Starts the model at half size

    private GameObject currentSpawnedModel;
    private bool isModelActive = false;

    void Start() { exitButton.SetActive(false); }

    void OnEnable() { imageTracker.trackedImagesChanged += OnImageTracked; }
    void OnDisable() { imageTracker.trackedImagesChanged -= OnImageTracked; }

    private void OnImageTracked(ARTrackedImagesChangedEventArgs args)
    {
        if (isModelActive) return;

        foreach (var trackedImage in args.added)
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                SpawnPlanet(trackedImage.referenceImage.name);
                return;
            }

        foreach (var trackedImage in args.updated)
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                SpawnPlanet(trackedImage.referenceImage.name);
                return;
            }
    }

    private void SpawnPlanet(string imageName)
    {
        if (isModelActive) return;

        GameObject prefabToSpawn = null;
        foreach (var prefab in planetPrefabs)
        {
            if (prefab.name.ToLower().Contains(imageName.ToLower()) || imageName.ToLower().Contains(prefab.name.ToLower()))
            {
                prefabToSpawn = prefab;
                break;
            }
        }

        if (prefabToSpawn == null) return;

        isModelActive = true;

        // Spawn the model locked to the camera
        currentSpawnedModel = Instantiate(prefabToSpawn, mainCamera);

        // Push it further back (using your new spawnDistance variable)
        currentSpawnedModel.transform.localPosition = new Vector3(0, 0, spawnDistance);
        currentSpawnedModel.transform.localRotation = Quaternion.identity;

        // Shrink it down so it fits nicely on screen
        currentSpawnedModel.transform.localScale = new Vector3(initialScale, initialScale, initialScale);

        exitButton.SetActive(true);
    }

    public void CloseViewer()
    {
        if (currentSpawnedModel != null) Destroy(currentSpawnedModel);
        exitButton.SetActive(false);
        Invoke(nameof(UnlockScanner), 0.5f);
    }

    private void UnlockScanner()
    {
        isModelActive = false;
        foreach (var trackable in imageTracker.trackables)
        {
            if (trackable.trackingState == TrackingState.Tracking)
            {
                SpawnPlanet(trackable.referenceImage.name);
                if (isModelActive) break;
            }
        }
    }
}