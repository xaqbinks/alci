using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles player input for a standard PC setup (mouse and keyboard).
/// </summary>
public class PCInputController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the main simulation manager.")]
    public SimulationManager simManager;
    [Tooltip("The main camera used for raycasting.")]
    public Camera mainCamera;

    private PlanetTile lastSelectedTile;

    void Update()
    {
        // --- Tile Selection ---
        // Check for a left mouse button click and ensure the mouse is not over a UI element.
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Check if the ray hits the planet's collider.
            if (Physics.Raycast(ray, out hit, 1000f))
            {
                // Find the closest tile in the planet graph to the hit point.
                PlanetTile selectedTile = simManager.GetClosestTile(hit.point);

                if (selectedTile != null)
                {
                    // If we selected the same tile again, hide the panel. Otherwise, display the new tile's info.
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
                    simManager.audioManager.PlaySound("UIClick");
                }
            }
            else
            {
                // If the player clicks in empty space, hide the info panel.
                simManager.planetViewController.Hide();
                lastSelectedTile = null;
            }
        }
    }
}
