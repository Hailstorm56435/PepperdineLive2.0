using UnityEngine;

public class Billboard : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            // Make the popup face the camera.
            transform.LookAt(Camera.main.transform.position, Vector3.up);
            // Rotate 180° around Y so that the text isn't backwards.
            transform.Rotate(0, 180f, 0);
        }
    }
}
