using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]

public class CameraController : MonoBehaviour {


    [Header("Scroll Controls")]
    [Space]
    public float scrollSpeed = 10f; // Speed of zooming
    public float minHeight = 5f; // Minimum height limit
    public float maxHeight = 50f; // Maximum height limit

    [Header("Movement Limits")]
    [Space]
    public Transform mapCenter; // Reference to the map's center
    public float maxAngle = 45f; // Maximum angle deviation from looking at the map
    public float rotationSmoothing = 2f; // Smoothing factor for rotation

    private float panSpeed;
    private Vector3 initialPos;
    private Vector3 panMovement;
    private Vector3 pos;
    private Quaternion rot;
    private Vector3 lastMousePosition;
    private Quaternion initialRot;
    




    [Header("Rotation")]
    [Space]
    public float minRotationX = -30f; // Minimum allowed X rotation
    public float maxRotationX = 60f; // Maximum allowed X rotation
    public float rotateSpeed = 5f; // Rotation speed





    // Use this for initialization
    void Start () {
        initialPos = transform.position;
        initialRot = transform.rotation;
	}
	
	
	void Update () {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
    

        #region Zoom

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            Vector3 newPosition = transform.position;
            newPosition.y -= scrollInput * scrollSpeed;
            newPosition.y = Mathf.Clamp(newPosition.y, minHeight, maxHeight);
            transform.position = newPosition;
        }
        #endregion

        #region mouse rotation
            // Mouse Rotation
        if (Input.GetMouseButton(1)) // on right click
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            float rotationY = mouseDelta.x * rotateSpeed * Time.deltaTime;
            float rotationX = -mouseDelta.y * rotateSpeed * Time.deltaTime;

            Vector3 currentRotation = transform.rotation.eulerAngles;
            float newRotationX = Mathf.Clamp(currentRotation.x + rotationX, minRotationX, maxRotationX);

            transform.rotation = Quaternion.Euler(newRotationX, currentRotation.y + rotationY, 0);
        }
        lastMousePosition = Input.mousePosition;

        


        #endregion


        #region boundaries


        // Restrict camera rotation within allowed angles
        Vector3 directionToMap = (mapCenter.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToMap);
        Quaternion currentRotationQuat = transform.rotation;
        
        float angleDifference = Quaternion.Angle(currentRotationQuat, targetRotation);
        if (angleDifference > maxAngle)
        {
            transform.rotation = Quaternion.Slerp(currentRotationQuat, targetRotation, Time.deltaTime * rotationSmoothing);
        }
        

        #endregion

    }

}