using UnityEngine;

/// <summary>
/// An implementation of the input provider for a Meta Quest VR controller.
/// NOTE: This script requires the Oculus Integration SDK to be installed in the project.
/// </summary>
public class VRInputProvider : IInputProvider
{
    private Transform rightControllerAnchor;

    public VRInputProvider(Transform controllerAnchor)
    {
        rightControllerAnchor = controllerAnchor;
    }

    public bool GetSelectDown()
    {
        // The primary selection action in VR is the index trigger on the active controller.
        // We'll default to the right hand for pointing and selecting.
        return OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
    }

    public Ray GetPointerRay()
    {
        if (rightControllerAnchor == null)
        {
            Debug.LogError("VRInputProvider: Right controller anchor is not set!");
            return new Ray();
        }
        // Create a ray pointing forward from the controller's position.
        return new Ray(rightControllerAnchor.position, rightControllerAnchor.forward);
    }
}
