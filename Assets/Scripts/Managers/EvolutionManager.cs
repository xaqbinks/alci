using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EvolutionManager : MonoBehaviour
{
    private SimulationManager simManager;
    private List<SpeciesData> activeSpecies;

    // A threshold to decide if a pressure is significant enough to cause mutation.
    // e.g., if more than 10% of total pop dies from one cause in a cycle, mutation may occur.
    private const float MUTATION_PRESSURE_THRESHOLD = 0.1f;
    private const int TICKS_BETWEEN_MUTATIONS = 50; // Cooldown period for mutations for a single species.

    private Dictionary<SpeciesData, int> mutationCooldowns = new Dictionary<SpeciesData, int>();

    public void Initialize(SimulationManager manager, List<SpeciesData> speciesList)
    {
        simManager = manager;
        activeSpecies = speciesList;
    }

    /// <summary>
    /// Called each tick to check for evolutionary pressures and trigger mutations.
    /// </summary>
    public void ProcessEvolutionTick()
    {
        if (activeSpecies == null) return;

        foreach (var species in activeSpecies)
        {
            // Update and check cooldown. A species cannot mutate if on cooldown.
            if (mutationCooldowns.ContainsKey(species))
            {
                mutationCooldowns[species]--;
                if (mutationCooldowns[species] <= 0)
                {
                    mutationCooldowns.Remove(species);
                }
                else
                {
                    continue; // On cooldown, skip to next species.
                }
            }

            // --- Pressure Analysis ---
            string primaryPressure = GetPrimaryPressure(species);

            if (!string.IsNullOrEmpty(primaryPressure))
            {
                TriggerMutation(species, primaryPressure);

                // Reset death tolls for this species to start a new analysis cycle.
                var keys = species.deathTolls.Keys.ToList();
                foreach(var key in keys) { species.deathTolls[key] = 0; }

                // Put the species on mutation cooldown.
                mutationCooldowns[species] = TICKS_BETWEEN_MUTATIONS;
            }
        }
    }

    private string GetPrimaryPressure(SpeciesData species)
    {
        if (species.deathTolls == null || species.deathTolls.Count == 0) return null;

        long totalPopulation = simManager.GetTotalPopulation(species);
        if (totalPopulation == 0)
        {
            // Clear tolls if extinct
            var keys = species.deathTolls.Keys.ToList();
            foreach(var key in keys) { species.deathTolls[key] = 0; }
            return null;
        }

        // Find the cause with the highest death toll for this analysis cycle.
        var maxDeathCause = species.deathTolls.Aggregate((l, r) => l.Value > r.Value ? l : r);

        // Check if the pressure is significant enough to trigger an adaptation.
        if (maxDeathCause.Value > totalPopulation * MUTATION_PRESSURE_THRESHOLD)
        {
            return maxDeathCause.Key;
        }

        return null;
    }

    private void TriggerMutation(SpeciesData species, string pressure)
    {
        Debug.Log($"<color=green>Evolutionary pressure '{pressure}' detected for {species.proceduralName}! Triggering mutation.</color>");

        // Find a tile to play the sound from
        PlanetTile soundOrigin = simManager.GetPlanetGraph().Values
            .Where(t => t.populations.ContainsKey(species))
            .OrderByDescending(t => t.populations[species])
            .FirstOrDefault();

        switch (pressure)
        {
            case "Environment":
                // Adapt to temperature
                species.temperatureTolerance += 0.5f;
                if (soundOrigin != null) simManager.audioManager.PlayDynamicSound("MutationApplied", soundOrigin.position, 1200f);
                Debug.Log($"{species.proceduralName} adapted: Temperature Tolerance increased to {species.temperatureTolerance}.");
                break;

            case "Starvation":
                // Become more metabolically efficient
                species.foodConsumptionRate *= 0.98f; // 2% more efficient
                if (soundOrigin != null) simManager.audioManager.PlayDynamicSound("MutationApplied", soundOrigin.position, 1000f);
                Debug.Log($"{species.proceduralName} adapted: Food Consumption Rate decreased to {species.foodConsumptionRate}.");
                break;

            case "Geology":
            case "Predation": // Grouping predation here for now
                // Adapt to be stronger in combat/defense
                species.baseCombatStrength *= 1.05f; // 5% stronger
                if (soundOrigin != null) simManager.audioManager.PlayDynamicSound("MutationApplied", soundOrigin.position, 800f);
                Debug.Log($"{species.proceduralName} adapted: Base Combat Strength increased to {species.baseCombatStrength}.");
                break;
        }
        // TODO: Create a TimelineEvent for this mutation.
    }
}
