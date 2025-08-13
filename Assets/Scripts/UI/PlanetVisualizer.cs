using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles the visual representation of the planet's surface by coloring its mesh vertices.
/// Note: This requires a material on the planet's renderer that supports vertex colors.
/// </summary>
public class PlanetVisualizer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The MeshFilter of the planet GameObject whose mesh will be colored.")]
    public MeshFilter planetMeshFilter;

    [Header("Terrain Colors")]
    public Color oceanColor = new Color(0.2f, 0.4f, 0.8f);
    public Color landColor = new Color(0.4f, 0.6f, 0.2f);
    public Color mountainColor = new Color(0.5f, 0.4f, 0.3f);
    public Color errorColor = Color.magenta;

    /// <summary>
    /// Colors the planet mesh vertices based on the terrain type of each tile.
    /// This should be called once after the planet graph is generated.
    /// </summary>
    /// <param name="planetGraph">The generated graph of the planet, mapping vertex indices to tiles.</param>
    public void DrawPlanet(Dictionary<int, PlanetTile> planetGraph)
    {
        if (planetMeshFilter == null || planetMeshFilter.mesh == null)
        {
            Debug.LogError("PlanetVisualizer: Planet Mesh Filter is not assigned! Cannot draw planet.");
            return;
        }

        Mesh mesh = planetMeshFilter.mesh;
        if (mesh.vertexCount != planetGraph.Count)
        {
            Debug.LogError("PlanetVisualizer: Mesh vertex count does not match planet graph size! Aborting.");
            return;
        }

        Color[] colors = new Color[mesh.vertexCount];

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            if (planetGraph.ContainsKey(i))
            {
                PlanetTile tile = planetGraph[i];
                switch (tile.terrainType)
                {
                    case TerrainType.Ocean:
                        colors[i] = oceanColor;
                        break;
                    case TerrainType.Land:
                        colors[i] = landColor;
                        break;
                    case TerrainType.Mountain:
                        colors[i] = mountainColor;
                        break;
                    default:
                        colors[i] = errorColor;
                        break;
                }
            }
            else
            {
                // This case should ideally not be reached if the graph is generated correctly.
                colors[i] = errorColor;
            }
        }

        // Apply the new array of colors to the mesh.
        mesh.colors = colors;
        Debug.Log("PlanetVisualizer: Planet mesh has been colored based on terrain.");
    }
}
