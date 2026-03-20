using UnityEngine;
using UnityEngine.EventSystems;

public class TouchController : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float rotationSpeed = 0.3f;
    public float zoomSpeed = 0.005f;

    [Header("Scale Limits")]
    public float minScale = 0.05f;
    public float maxScale = 5.0f;

    private bool userHasInteracted = false;
    private Camera mainCam;
    private RectTransform resultPanel; // found automatically at runtime

    void Start()
    {
        mainCam = Camera.main;

        // Automatically find Result_Panel in the scene at runtime
        GameObject panel = GameObject.Find("Result_Panel");
        if (panel != null)
            resultPanel = panel.GetComponent<RectTransform>();
        else
            Debug.LogWarning("[TouchController] Result_Panel not found in scene!");
    }

    bool IsTouchOverResultPanel(Vector2 screenPos)
    {
        if (resultPanel == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            resultPanel, screenPos, null);
    }

    void Update()
    {
        if (Input.touchCount == 0) return;

        Touch touch0 = Input.GetTouch(0);

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch0.fingerId))
            return;

        if (IsTouchOverResultPanel(touch0.position))
            return;

        if (!userHasInteracted)
        {
            StopAnimations();
            userHasInteracted = true;
        }

        if (Input.touchCount == 1 && touch0.phase == TouchPhase.Moved)
        {
            transform.Rotate(mainCam.transform.up, -touch0.deltaPosition.x * rotationSpeed, Space.World);
            transform.Rotate(mainCam.transform.right, touch0.deltaPosition.y * rotationSpeed, Space.World);
        }

        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(1);

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch1.fingerId))
                return;

            if (IsTouchOverResultPanel(touch1.position))
                return;

            float oldDist = (touch0.position - touch0.deltaPosition - (touch1.position - touch1.deltaPosition)).magnitude;
            float newDist = (touch0.position - touch1.position).magnitude;
            float delta = oldDist - newDist;

            Vector3 newScale = transform.localScale - Vector3.one * (delta * zoomSpeed);
            newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
            newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
            newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);
            transform.localScale = newScale;
        }
    }

    private void StopAnimations()
    {
        foreach (Animator anim in GetComponentsInChildren<Animator>())
            anim.enabled = false;

        foreach (Animation anim in GetComponentsInChildren<Animation>())
            anim.enabled = false;

        foreach (MonoBehaviour script in GetComponentsInChildren<MonoBehaviour>())
            if (script != this)
                script.enabled = false;
    }
}