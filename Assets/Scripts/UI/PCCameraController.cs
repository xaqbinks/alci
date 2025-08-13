using UnityEngine;

/// <summary>
/// Manages camera movement for a standard PC, allowing orbit, zoom, and panning.
/// </summary>
public class PCCameraController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The object the camera will orbit around.")]
    public Transform target;

    [Header("Movement Speeds")]
    public float distance = 50.0f;
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;
    public float zoomSpeed = 10.0f;

    [Header("Constraints")]
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;
    public float distanceMin = 15f;
    public float distanceMax = 100f;

    private float x = 0.0f;
    private float y = 0.0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }

    void LateUpdate()
    {
        if (target)
        {
            // --- Orbiting ---
            // Only orbit when the right mouse button is held down.
            if (Input.GetMouseButton(1))
            {
                x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
            }

            y = ClampAngle(y, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(y, x, 0);

            // --- Zooming ---
            distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * zoomSpeed, distanceMin, distanceMax);

            // --- Positioning ---
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + target.position;

            transform.rotation = rotation;
            transform.position = position;
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
