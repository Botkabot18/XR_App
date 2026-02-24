using UnityEngine;
using UnityEngine.EventSystems;

public class TouchController : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float rotationSpeed = 0.3f; // Adjusted for smoother mobile feel
    public float zoomSpeed = 0.005f;

    [Header("Scale Limits")]
    public float minScale = 0.05f; // Prevents the model from turning inside out
    public float maxScale = 5.0f;  // Prevents the model from filling the whole screen

    private bool userHasInteracted = false;
    private Camera mainCam;

    void Start()
    {
        // Cache the main camera so we know which way the user is looking
        mainCam = Camera.main;
    }

    void Update()
    {
        // 1. If no fingers are on the screen, do nothing
        if (Input.touchCount == 0) return;

        Touch touch0 = Input.GetTouch(0);

        // 2. THE UI SHIELD
        // If the finger is touching your UI Canvas (Scroll View, Buttons, etc.), ignore it!
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch0.fingerId))
        {
            return;
        }

        // 3. THE ANIMATION KILL SWITCH
        // The exact millisecond the user touches the AR zone, freeze all default animations
        if (!userHasInteracted)
        {
            StopAnimations();
            userHasInteracted = true;
        }

        // --- 1-FINGER SWIPE: ROTATE ---
        if (Input.touchCount == 1 && touch0.phase == TouchPhase.Moved)
        {
            // Rotate relative to the camera's up and right vectors.
            // This ensures swipes always feel correct regardless of where the phone is pointing.
            transform.Rotate(mainCam.transform.up, -touch0.deltaPosition.x * rotationSpeed, Space.World);
            transform.Rotate(mainCam.transform.right, touch0.deltaPosition.y * rotationSpeed, Space.World);
        }

        // --- 2-FINGER PINCH: ZOOM ---
        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(1);

            // Edge-case protection: Ignore zoom if the second finger accidentally hits the UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch1.fingerId))
            {
                return;
            }

            // Calculate the distance between fingers in the previous frame vs this frame
            float oldDist = (touch0.position - touch0.deltaPosition - (touch1.position - touch1.deltaPosition)).magnitude;
            float newDist = (touch0.position - touch1.position).magnitude;
            float delta = oldDist - newDist;

            // Apply scale change
            Vector3 newScale = transform.localScale - Vector3.one * (delta * zoomSpeed);

            // Clamp the scale to your min/max limits
            newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
            newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
            newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);

            transform.localScale = newScale;
        }
    }

    private void StopAnimations()
    {
        // 1. Hunt down and disable modern Animators
        foreach (Animator anim in GetComponentsInChildren<Animator>())
        {
            anim.enabled = false;
        }

        // 2. Hunt down and disable Legacy Animations
        foreach (Animation anim in GetComponentsInChildren<Animation>())
        {
            anim.enabled = false;
        }

        // 3. Hunt down and disable any custom spin/float scripts attached by the 3D artist
        foreach (MonoBehaviour script in GetComponentsInChildren<MonoBehaviour>())
        {
            // Disable everything EXCEPT this specific TouchController script
            if (script != this)
            {
                script.enabled = false;
            }
        }
    }
}