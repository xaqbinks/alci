using UnityEngine;

/// <summary>
/// An interface that defines a generic provider for player input,
/// abstracting away the difference between PC (mouse/keyboard) and VR (controllers).
/// </summary>
public interface IInputProvider
{
    /// <summary>
    /// Returns true on the frame the primary selection action is initiated.
    /// (e.g., left mouse button click, VR controller trigger press).
    /// </summary>
    bool GetSelectDown();

    /// <summary>
    /// Gets the ray representing the player's pointer, originating from the
    /// camera (for PC) or the VR controller (for VR).
    /// </summary>
    Ray GetPointerRay();
}
