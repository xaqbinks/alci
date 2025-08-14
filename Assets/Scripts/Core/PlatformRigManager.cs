using UnityEngine;

/// <summary>
/// Manages the activation and deactivation of platform-specific rigs (PC vs. VR)
/// at startup. This ensures only the correct camera and controllers are active.
/// </summary>
public class PlatformRigManager : MonoBehaviour
{
    public static PlatformRigManager instance;

    [Header("Platform Rigs")]
    [SerializeField] private GameObject pcRig;
    [SerializeField] private GameObject vrRig;

    [Header("Platform-Specific Components")]
    [SerializeField] private Camera pcCamera;
    [SerializeField] private Transform vrControllerAnchor; // Used for pointing

    // Public properties to access the active components
    public Camera ActiveCamera { get; private set; }
    public Transform ActiveControllerAnchor { get; private set; }
    public bool IsVrActive { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        IsVrActive = UnityEngine.XR.XRSettings.isDeviceActive;

        if (IsVrActive)
        {
            Debug.Log("VR platform detected. Activating VR rig.");
            vrRig.SetActive(true);
            pcRig.SetActive(false);
            ActiveCamera = vrRig.GetComponentInChildren<Camera>(); // Assumes OVRCameraRig structure
            ActiveControllerAnchor = vrControllerAnchor;
        }
        else
        {
            Debug.Log("PC platform detected. Activating PC rig.");
            pcRig.SetActive(true);
            vrRig.SetActive(false);
            ActiveCamera = pcCamera;
            ActiveControllerAnchor = null; // No controller anchor in PC mode
        }
    }
}
