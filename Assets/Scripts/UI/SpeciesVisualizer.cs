using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the visual representation of species on the planet's surface.
/// It creates procedural meshes for species or uses prefabs if they are assigned.
/// </summary>
public class SpeciesVisualizer : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("An optional parent transform to keep the instantiated prefabs organized in the hierarchy.")]
    public Transform speciesParent;
    [Tooltip("A default material that supports vertex colors for procedural meshes.")]
    public Material proceduralMaterial;

    private Dictionary<PlanetTile, Dictionary<SpeciesData, GameObject>> visualInstances;
    private SimulationManager simManager;

    public void Initialize(SimulationManager manager)
    {
        simManager = manager;
        visualInstances = new Dictionary<PlanetTile, Dictionary<SpeciesData, GameObject>>();
    }

    /// <summary>
    /// Updates the visual representation of all species on the planet every tick.
    /// </summary>
    public void UpdateVisuals(Dictionary<int, PlanetTile> planetGraph)
    {
        if (planetGraph == null) return;

        foreach (PlanetTile tile in planetGraph.Values)
        {
            if (!visualInstances.ContainsKey(tile))
            {
                visualInstances[tile] = new Dictionary<SpeciesData, GameObject>();
            }

            var speciesInSim = tile.populations.Keys;
            var speciesInVisuals = visualInstances[tile].Keys.ToList();

            // --- 1. Add new visuals ---
            var speciesToAdd = speciesInSim.Except(speciesInVisuals).ToList();
            foreach (var species in speciesToAdd)
            {
                CreateSpeciesVisual(species, tile);
            }

            // --- 2. Remove old visuals ---
            var speciesToRemove = speciesInVisuals.Except(speciesInSim).ToList();
            foreach (var species in speciesToRemove)
            {
                if (visualInstances[tile].ContainsKey(species))
                {
                    Destroy(visualInstances[tile][species]);
                    visualInstances[tile].Remove(species);
                }
            }

            // --- 3. Update existing visuals to reflect evolution ---
            var speciesToUpdate = speciesInVisuals.Intersect(speciesInSim).ToList();
            foreach (var species in speciesToUpdate)
            {
                UpdateSpeciesVisual(species, tile);
            }
        }
    }

    private void CreateSpeciesVisual(SpeciesData species, PlanetTile tile)
    {
        GameObject instance;
        // If a prefab is assigned, use it. Otherwise, generate a mesh.
        if (species.speciesPrefab != null)
        {
            instance = Instantiate(species.speciesPrefab);
        }
        else
        {
            instance = new GameObject($"{species.proceduralName}_Visual");
            var filter = instance.AddComponent<MeshFilter>();
            var renderer = instance.AddComponent<MeshRenderer>();
            renderer.material = proceduralMaterial;
            filter.mesh = ProceduralSpeciesMesh.GenerateMesh(species); // Generate the unique mesh
        }

        // Position and orient the new visual
        float heightOffset = simManager.activePlanet.planetRadius * 0.01f;
        instance.transform.position = tile.position.normalized * (simManager.activePlanet.planetRadius + heightOffset);
        instance.transform.rotation = Quaternion.LookRotation(tile.position);

        if (speciesParent != null)
        {
            instance.transform.SetParent(speciesParent);
        }
        visualInstances[tile][species] = instance;
    }

    private void UpdateSpeciesVisual(SpeciesData species, PlanetTile tile)
    {
        GameObject instance = visualInstances[tile][species];
        // If we are using a procedural mesh, we can regenerate it to show evolution.
        if (species.speciesPrefab == null)
        {
            MeshFilter filter = instance.GetComponent<MeshFilter>();
            if (filter != null)
            {
                // This is where the magic happens: the mesh is recreated every tick
                // with the species' latest stats, showing visual evolution.
                Destroy(filter.mesh); // Destroy old mesh to prevent memory leaks
                filter.mesh = ProceduralSpeciesMesh.GenerateMesh(species);
            }
        }
        // If using a prefab, one could update material properties here.
        // For example: instance.GetComponent<Renderer>().material.color = ...
    }
}
