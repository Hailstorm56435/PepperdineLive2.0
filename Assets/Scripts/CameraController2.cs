using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController2 : MonoBehaviour
{
    [Header("Pan (Drag) Settings")]
    public float dragSpeed = 2f;
    public float movementRadius = 50f;

    [Header("Zoom Settings")]
    public float scrollSpeed = 10f;
    public float minHeight = 5f;
    public float maxHeight = 50f;

    [Header("Tilt Settings")]
    [Tooltip("Pitch (X rotation) in degrees at minimum height (zoomed in).")]
    public float minPitch = 10f;
    [Tooltip("Pitch (X rotation) in degrees at maximum height (zoomed out).")]
    public float maxPitch = 50f;

    [Header("Rotation Settings")]
    [Tooltip("Speed at which the camera yaws when right‑dragging.")]
    public float rotateSpeed = 100f;

    [Header("Map Reference")]
    public Transform mapCenter;

    private Vector3 dragOrigin;

    void Update()
    {
        // skip if pointer is over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        HandleZoomAndTilt();
        HandlePan();
        HandleRotation();
    }

    private void HandleZoomAndTilt()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.001f)
        {
            Vector3 newPosition = transform.position;
            newPosition.y -= scrollInput * scrollSpeed;
            newPosition.y = Mathf.Clamp(newPosition.y, minHeight, maxHeight);
            transform.position = newPosition;
        }

        float t = Mathf.InverseLerp(minHeight, maxHeight, transform.position.y);
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, t);

        Vector3 currentEuler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(targetPitch, currentEuler.y, 0f);
    }

    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(0))
            dragOrigin = Input.mousePosition;

        if (Input.GetMouseButton(0))
        {
            Vector3 viewportDelta = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
            Vector3 right   = Camera.main.transform.right;
            Vector3 forward = Camera.main.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 move = (right * viewportDelta.x + forward * viewportDelta.y) * -dragSpeed;
            dragOrigin = Input.mousePosition;

            Vector3 candidatePos = transform.position + move;
            Vector3 offset       = candidatePos - mapCenter.position;
            offset.y = 0f;

            if (offset.magnitude > movementRadius)
            {
                offset = offset.normalized * movementRadius;
                candidatePos = new Vector3(
                    mapCenter.position.x + offset.x,
                    candidatePos.y,
                    mapCenter.position.z + offset.z
                );
            }

            transform.position = candidatePos;
        }
    }

    private void HandleRotation()
    {
        if (Input.GetMouseButton(1))  // right mouse held
        {
            float mouseX = Input.GetAxis("Mouse X");
            Vector3 euler = transform.rotation.eulerAngles;
            euler.y += mouseX * rotateSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(euler.x, euler.y, 0f);
        }
    }
}
