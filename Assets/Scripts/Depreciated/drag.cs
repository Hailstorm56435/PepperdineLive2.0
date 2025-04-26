using UnityEngine;
using UnityEngine.EventSystems;
public class Drag : MonoBehaviour
{
    public float dragSpeed = 2;
    private Vector3 dragOrigin;

    public Transform mapCenter;       // Center of the map
    public float movementRadius = 50f;  // Allowed circular movement radius

    void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        if (Input.GetMouseButtonDown(0))
        {
            // Store the starting mouse position when dragging begins.
            dragOrigin = Input.mousePosition;
            return;
        }

        if (!Input.GetMouseButton(0))
            return;

        // Calculate the raw movement in viewport space.
        Vector3 viewportDelta = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);

        // Convert the viewport movement into world space relative to the camera's orientation.
        Vector3 right = Camera.main.transform.right;
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0; // keep movement in the horizontal plane

        // Compute the intended movement.
        Vector3 move = (right * viewportDelta.x + forward * viewportDelta.y) * -dragSpeed;

        // Reset the drag origin for the next frame so that the movement is incremental.
        dragOrigin = Input.mousePosition;

        // Calculate the candidate new position.
        Vector3 currentPos = transform.position;
        Vector3 candidatePos = currentPos + move;

        // Consider only the horizontal displacement (X and Z).
        Vector3 currentOffset = currentPos - mapCenter.position;
        currentOffset.y = 0;
        Vector3 candidateOffset = candidatePos - mapCenter.position;
        candidateOffset.y = 0;

        // If the candidate position is outside the allowed circle...
        if (candidateOffset.magnitude > movementRadius)
        {
            // Determine the radial direction from the map center.
            Vector3 radialDir;
            if (currentOffset.magnitude > 0.001f)
                radialDir = currentOffset.normalized;
            else
                radialDir = candidateOffset.normalized; // fallback if near the center

            // Decompose the move vector into a radial (outward) and tangential (sideways) component.
            float radialMoveAmount = Vector3.Dot(move, radialDir);
            Vector3 tangentialMove = move - radialDir * radialMoveAmount;

            // Only restrict movement that pushes the camera further out.
            if (radialMoveAmount > 0)
            {
                float allowedRadialDistance = movementRadius - currentOffset.magnitude;
                radialMoveAmount = Mathf.Min(radialMoveAmount, allowedRadialDistance);
            }
            // Recompose the allowed move vector.
            Vector3 allowedMove = radialDir * radialMoveAmount + tangentialMove;
            candidatePos = currentPos + allowedMove;
        }

        // Apply the computed position.
        transform.position = candidatePos;
    }
}
