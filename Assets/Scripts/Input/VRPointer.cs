using UnityEngine;

/// <summary>
/// Draws a laser pointer from the VR controller to provide visual feedback for aiming.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class VRPointer : MonoBehaviour
{
    [Tooltip("The maximum distance of the laser pointer.")]
    public float maxDistance = 100.0f;

    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (InputManager.instance == null) return;

        // Get the pointer ray from our platform-agnostic input manager.
        Ray pointerRay = InputManager.instance.GetPointerRay();

        // Set the start position of the line to the ray's origin.
        lineRenderer.SetPosition(0, pointerRay.origin);

        RaycastHit hit;
        if (Physics.Raycast(pointerRay, out hit, maxDistance))
        {
            // If the ray hits something, end the line there.
            lineRenderer.SetPosition(1, hit.point);
        }
        else
        {
            // If the ray hits nothing, extend it to its maximum distance.
            lineRenderer.SetPosition(1, pointerRay.origin + pointerRay.direction * maxDistance);
        }
    }
}
