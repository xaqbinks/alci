using UnityEngine;

/// <summary>
/// Handles player input via VR controllers, including pointing at and selecting objects.
/// THIS SCRIPT IS FOR VR AND SHOULD BE DISABLED FOR PC TESTING.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class VRInteractionController : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The VR button used for selecting tiles or UI elements.")]
    public OVRInput.Button interactionButton = OVRInput.Button.PrimaryIndexTrigger;
    [Tooltip("The controller (left or right) this script is attached to.")]
    public OVRInput.Controller controllerHand = OVRInput.Controller.RTouch;

    [Header("References")]
    [Tooltip("Reference to the main simulation manager.")]
    public SimulationManager simManager;

    private LineRenderer laserPointer;
    private PlanetTile lastSelectedTile;

    void Awake()
    {
        laserPointer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        // Point the laser forward from the controller
        Ray ray = new Ray(transform.position, transform.forward);
        laserPointer.SetPosition(0, ray.origin);
        laserPointer.SetPosition(1, ray.origin + ray.direction * 500f); // Default end point

        RaycastHit hit;
        // Check if the laser hits the planet's collider
        if (Physics.Raycast(ray, out hit, 500f))
        {
            laserPointer.SetPosition(1, hit.point);

            if (OVRInput.GetDown(interactionButton))
            {
                PlanetTile selectedTile = simManager.GetClosestTile(hit.point);
                if (selectedTile != null)
                {
                    if (lastSelectedTile == selectedTile && simManager.planetViewController.viewPanel.activeSelf)
                    {
                        simManager.planetViewController.Hide();
                        lastSelectedTile = null;
                    }
                    else
                    {
                        simManager.planetViewController.DisplayTileInfo(selectedTile);
                        lastSelectedTile = selectedTile;
                    }
                    OVRInput.SetControllerVibration(0.5f, 0.2f, controllerHand);
                }
            }
        }
        else
        {
            if (OVRInput.GetDown(interactionButton))
            {
                 simManager.planetViewController.Hide();
                 lastSelectedTile = null;
            }
        }
    }
}

/*
    Other UI Manager Scripts (Placeholders for User Implementation)
    The user can create new scripts based on the PlanetViewController pattern for these systems.

    - NotificationManager.cs: To show pop-up messages for significant events.
    - TimelineManager.cs: To display a scrollable list of past TimelineEvents.
    - CivilizationViewController.cs: To show detailed stats for a selected species.
    - GameSetupManager.cs: To handle the pre-game setup screen.
    - SaveLoadManager.cs: To handle serialization and deserialization of SaveData.
*/


// Dummy OVRInput class to allow compilation without the Oculus SDK installed.
// In a real project, this would be provided by the Oculus Integration package.
public static class OVRInput
{
    public enum Button { PrimaryIndexTrigger }
    public enum Controller { RTouch }
    // The mouse fallback has been removed to prevent conflict with PCInputController.
    public static bool GetDown(Button button) { return false; }
    public static void SetControllerVibration(float frequency, float amplitude, Controller controller) {}
}
