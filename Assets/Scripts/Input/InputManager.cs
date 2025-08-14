using UnityEngine;

/// <summary>
/// A singleton manager that provides a platform-agnostic interface for player input.
/// It detects the platform (PC or VR) at runtime and uses the appropriate IInputProvider.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    private IInputProvider activeProvider;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        if (PlatformRigManager.instance == null)
        {
            Debug.LogError("InputManager requires a PlatformRigManager in the scene!");
            return;
        }

        if (PlatformRigManager.instance.IsVrActive)
        {
            Debug.Log("InputManager: VR platform detected. Initializing VRInputProvider.");
            activeProvider = new VRInputProvider(PlatformRigManager.instance.ActiveControllerAnchor);
        }
        else
        {
            Debug.Log("InputManager: PC platform detected. Initializing PCInputProvider.");
            activeProvider = new PCInputProvider(PlatformRigManager.instance.ActiveCamera);
        }
    }

    // --- Public Interface ---

    /// <summary>
    /// Returns true on the frame the primary selection action is initiated.
    /// </summary>
    public bool GetSelectDown()
    {
        if (activeProvider == null) return false;
        return activeProvider.GetSelectDown();
    }

    /// <summary>
    /// Gets the ray representing the player's pointer into the world.
    /// </summary>
    public Ray GetPointerRay()
    {
        if (activeProvider == null) return new Ray();
        return activeProvider.GetPointerRay();
    }
}
