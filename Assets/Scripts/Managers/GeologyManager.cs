using UnityEngine;
using System.Collections.Generic;

public class GeologyManager : MonoBehaviour
{
    private SimulationManager simManager;
    private Dictionary<int, PlanetTile> planetGraph;
    private PlanetData planetData;
    private int ticksUntilNextEvent;

    /// <summary>
    /// Initializes the manager with references from the main SimulationManager.
    /// </summary>
    public void Initialize(SimulationManager manager, Dictionary<int, PlanetTile> graph, PlanetData data)
    {
        simManager = manager;
        planetGraph = graph;
        planetData = data;
        ticksUntilNextEvent = planetData.ticksPerGeologicalEvent;
    }

    /// <summary>
    /// This method is called by the SimulationManager each tick to process geological changes.
    /// </summary>
    public void ProcessGeologyTick()
    {
        ticksUntilNextEvent--;
        if (ticksUntilNextEvent <= 0)
        {
            if (Random.value < planetData.tectonicActivityChance)
            {
                TriggerGeologicalEvent();
            }
            ticksUntilNextEvent = planetData.ticksPerGeologicalEvent;
        }
    }

    private void TriggerGeologicalEvent()
    {
        if (planetGraph == null || planetGraph.Count == 0) return;

        // Find a random non-ocean tile to affect.
        var potentialTiles = planetGraph.Values.Where(t => t.terrainType != TerrainType.Ocean).ToList();
        if (potentialTiles.Count == 0) return;

        PlanetTile targetTile = potentialTiles[Random.Range(0, potentialTiles.Count)];

        // Decide what kind of event to trigger
        float eventType = Random.value;
        if (eventType < 0.4f)
        {
            TriggerEarthquake(targetTile);
        }
        else if (eventType < 0.7f)
        {
            TriggerVolcano(targetTile);
        }
        else
        {
            TriggerMineralDeposit(targetTile);
        }
    }

    private void TriggerEarthquake(PlanetTile tile)
    {
        Debug.Log($"<color=brown>An earthquake strikes tile {tile.id}!</color>");
        // Make each earthquake sound slightly different
        simManager.audioManager.PlayDynamicSound("Earthquake", tile.position, frequencyOverride: Random.Range(80f, 120f));

        // Earthquakes can damage populations.
        if (tile.populations.Count > 0)
        {
            var speciesOnTile = tile.populations.Keys.ToList();
            foreach (var species in speciesOnTile)
            {
                long initialPop = tile.populations[species];
                // Earthquakes cause a random amount of damage, especially to non-adapted species.
                // TODO: Link damage to GeologicalDamageResistance tech.
                long popLoss = (long)(initialPop * Random.Range(0.1f, 0.3f));
                tile.populations[species] -= popLoss;
                species.deathTolls["Geology"] += (int)popLoss;
                Debug.Log($"{species.speciesName} on tile {tile.id} lost {popLoss} population to the earthquake.");
            }
        }
    }

    private void TriggerVolcano(PlanetTile tile)
    {
        Debug.Log($"<color=red>A volcano erupts on tile {tile.id}, forming a new mountain!</color>");
        // Lower frequency for higher elevation volcanoes
        float frequency = Mathf.Lerp(150f, 50f, (tile.position.magnitude - planetData.seaLevel) / (planetData.noiseStrength * 0.7f));
        simManager.audioManager.PlayDynamicSound("Volcano", tile.position, frequencyOverride: frequency);

        // Volcanoes change the terrain and add valuable resources.
        tile.terrainType = TerrainType.Mountain;

        if (!tile.resources.ContainsKey("Silicon"))
        {
            tile.resources["Silicon"] = 0;
        }
        tile.resources["Silicon"] += Random.Range(100f, 250f); // Add a large amount of minerals.

        // Volcanoes are highly destructive to existing life.
        if (tile.populations.Count > 0)
        {
            var speciesOnTile = tile.populations.Keys.ToList();
            foreach (var species in speciesOnTile)
            {
                species.deathTolls["Geology"] += (int)tile.populations[species];
            }
            Debug.Log($"All populations on tile {tile.id} were wiped out by the eruption.");
            tile.populations.Clear();
        }
    }

    private void TriggerMineralDeposit(PlanetTile tile)
    {
        // A beneficial event that seeds the crust with new resources.
        string resourceName = "Silicon"; // Example, could be randomized
        float amount = Random.Range(200f, 500f);

        if (!tile.resources.ContainsKey(resourceName))
        {
            tile.resources[resourceName] = 0;
        }
        tile.resources[resourceName] += amount;

        Debug.Log($"<color=green>A rich mineral deposit of {resourceName} has been discovered on tile {tile.id}!</color>");
        // More significant deposits have a slightly higher pitch and more intense FM synthesis
        float frequency = Mathf.Lerp(400f, 600f, amount / 500f);
        float fmAmount = Mathf.Lerp(50f, 150f, amount / 500f);
        simManager.audioManager.PlayDynamicSound("MineralDeposit", tile.position, frequencyOverride: frequency, fmAmountOverride: fmAmount);
    }
}
