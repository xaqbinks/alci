using UnityEngine;

/// <summary>
/// An implementation of the input provider for a standard PC with mouse and keyboard.
/// </summary>
public class PCInputProvider : IInputProvider
{
    private Camera mainCamera;

    public PCInputProvider(Camera camera)
    {
        mainCamera = camera;
    }

    public bool GetSelectDown()
    {
        // The primary selection action on PC is the left mouse button.
        return Input.GetMouseButtonDown(0);
    }

    public Ray GetPointerRay()
    {
        if (mainCamera == null)
        {
            Debug.LogError("PCInputProvider: Main camera is not set!");
            return new Ray();
        }
        // Create a ray from the camera through the mouse cursor's position.
        return mainCamera.ScreenPointToRay(Input.mousePosition);
    }
}
