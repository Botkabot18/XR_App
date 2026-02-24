using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTracker : MonoBehaviour
{
    [Header("Distance Settings")]
    [Tooltip("If Camera is farther than this (meters), object hides.")]
    public float maxDistance = 2.0f;

    [Header("References")]
    public GameObject[] ArPrefabs;

    private ARTrackedImageManager trackedImages;
    private Transform camTransform;

    // Dictionary to link Image Name -> Spawned GameObject
    private Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();

    void Awake()
    {
        trackedImages = GetComponent<ARTrackedImageManager>();
        camTransform = Camera.main.transform;
    }

    void OnEnable() => trackedImages.trackedImagesChanged += OnTrackedImagesChanged;
    void OnDisable() => trackedImages.trackedImagesChanged -= OnTrackedImagesChanged;

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // 1. SPAWN
        foreach (var trackedImage in eventArgs.added)
        {
            string imageName = trackedImage.referenceImage.name;

            foreach (var arPrefab in ArPrefabs)
            {
                // Case-Insensitive Match
                if (string.Equals(imageName, arPrefab.name, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Instantiate as CHILD (Glue)
                    var newPrefab = Instantiate(arPrefab, trackedImage.transform);
                    newPrefab.name = imageName;
                    spawnedObjects[imageName] = newPrefab;
                    break;
                }
            }
        }

        // 2. UPDATE (Logic for Hiding/Showing)
        foreach (var trackedImage in eventArgs.updated)
        {
            string imageName = trackedImage.referenceImage.name;

            if (spawnedObjects.TryGetValue(imageName, out GameObject spawnedObj))
            {
                // CONDITION 1: IS IT TRACKING?
                bool isTracking = trackedImage.trackingState == TrackingState.Tracking;

                // CONDITION 2: IS IT CLOSE ENOUGH?
                float distance = Vector3.Distance(camTransform.position, trackedImage.transform.position);
                bool isCloseEnough = distance <= maxDistance;

                // FINAL DECISION: VISIBLE ONLY IF BOTH ARE TRUE
                bool shouldBeVisible = isTracking && isCloseEnough;

                spawnedObj.SetActive(shouldBeVisible);

                // FORCE GLUE: If visible, ensure it stays parented (Sucks to image)
                if (shouldBeVisible)
                {
                    // If for some reason it got detached, re-attach it
                    if (spawnedObj.transform.parent != trackedImage.transform)
                    {
                        spawnedObj.transform.SetParent(trackedImage.transform);
                        spawnedObj.transform.localPosition = Vector3.zero; // Or your prefab's default
                        spawnedObj.transform.localRotation = Quaternion.identity;
                    }
                }
            }
        }

        // 3. REMOVED
        foreach (var trackedImage in eventArgs.removed)
        {
            if (spawnedObjects.TryGetValue(trackedImage.referenceImage.name, out GameObject spawnedObj))
            {
                Destroy(spawnedObj);
                spawnedObjects.Remove(trackedImage.referenceImage.name);
            }
        }
    }

    // Visual Debugging (Yellow Sphere)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        if (Application.isPlaying)
        {
            foreach (var obj in spawnedObjects.Values)
            {
                if (obj != null && obj.activeSelf)
                    Gizmos.DrawWireSphere(obj.transform.position, maxDistance);
            }
        }
    }
}