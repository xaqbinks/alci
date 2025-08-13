using UnityEngine;
using TMPro;
using System.Text;
using System.Linq;

/// <summary>
/// Manages the UI panel that displays detailed information about a selected planet tile.
/// </summary>
public class PlanetViewController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The root object of the tile view panel.")]
    public GameObject viewPanel;
    [Tooltip("Text field to display the tile's ID.")]
    public TextMeshProUGUI tileIDText;
    [Tooltip("Text field to display terrain and temperature.")]
    public TextMeshProUGUI terrainInfoText;
    [Tooltip("Text field to display a list of resources.")]
    public TextMeshProUGUI resourcesText;
    [Tooltip("Text field to display a list of populations.")]
    public TextMeshProUGUI populationsText;

    void Start()
    {
        // Start with the panel hidden.
        Hide();
    }

    /// <summary>
    /// Populates the UI fields with data from the given tile and shows the panel.
    /// </summary>
    public void DisplayTileInfo(PlanetTile tile)
    {
        if (tile == null)
        {
            Hide();
            return;
        }

        tileIDText.text = $"Tile #{tile.id}";
        terrainInfoText.text = $"{tile.terrainType} | {tile.currentTemperature:F1}°C";

        // Build resources string
        StringBuilder resourcesBuilder = new StringBuilder("Resources:\n");
        if (tile.resources.Count > 0)
        {
            foreach (var resource in tile.resources)
            {
                resourcesBuilder.AppendLine($"- {resource.Key}: {resource.Value:F0}");
            }
        }
        else
        {
            resourcesBuilder.AppendLine("- None");
        }
        resourcesText.text = resourcesBuilder.ToString();

        // Build populations string
        StringBuilder populationsBuilder = new StringBuilder("Populations:\n");
        if (tile.populations.Count > 0)
        {
            foreach (var pop in tile.populations.OrderByDescending(p => p.Value))
            {
                populationsBuilder.AppendLine($"- {pop.Key.proceduralName}: {pop.Value:N0}");
            }
        }
        else
        {
            populationsBuilder.AppendLine("- None");
        }
        populationsText.text = populationsBuilder.ToString();

        Show();
    }

    /// <summary>
    /// Shows the tile information panel.
    /// </summary>
    public void Show()
    {
        if (viewPanel != null)
        {
            viewPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the tile information panel.
    /// </summary>
    public void Hide()
    {
        if (viewPanel != null)
        {
            viewPanel.SetActive(false);
        }
    }
}
