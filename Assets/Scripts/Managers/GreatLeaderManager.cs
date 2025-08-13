using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GreatLeaderManager : MonoBehaviour
{
    private SimulationManager simManager;
    private List<SpeciesData> activeSpecies;
    private const float LEADER_SPAWN_CHANCE = 0.001f;

    [Tooltip("A list of all possible leader abilities that can be generated. Assign in Editor.")]
    public List<GreatLeaderAbility> possibleAbilities;

    public void Initialize(SimulationManager manager, List<SpeciesData> allSpecies)
    {
        simManager = manager;
        activeSpecies = allSpecies;
    }

    public void ProcessLeaderLifecycle()
    {
        if (activeSpecies == null) return;

        foreach (var species in activeSpecies)
        {
            // Check for new leader spawn
            if (simManager.GetTotalPopulation(species) > 500 && species.activeLeaders.Count < 3)
            {
                if (Random.value < LEADER_SPAWN_CHANCE)
                {
                    SpawnLeader(species);
                }
            }

            // Update and remove dead leaders
            species.activeLeaders.RemoveAll(leader => {
                leader.ageInTicks++;
                if (leader.ageInTicks > leader.maxAgeInTicks)
                {
                    Debug.Log($"<color=purple>Great Leader {leader.leaderName} ({leader.ability.abilityName}) of the {species.proceduralName} has died of old age.</color>");

                    // Find a tile to play the sound from
                    PlanetTile soundOrigin = GetSpeciesSoundOrigin(species);
                    if (soundOrigin != null)
                    {
                        simManager.audioManager.PlayDynamicSound("LeaderDeath", soundOrigin.position, frequencyOverride: 200f);
                    }

                    return true; // Remove from list
                }
                return false;
            });
        }
    }

    private void SpawnLeader(SpeciesData species)
    {
        if (possibleAbilities == null || possibleAbilities.Count == 0)
        {
            Debug.LogWarning("No GreatLeaderAbilities assigned to the GreatLeaderManager.");
            return;
        }

        GreatLeaderAbility ability = possibleAbilities[Random.Range(0, possibleAbilities.Count)];
        string leaderName = "Leader " + (Random.Range(100, 999));
        int lifespan = Random.Range(500, 1000);

        GreatLeader newLeader = new GreatLeader(leaderName, ability, species, simManager.currentTick, lifespan);
        species.activeLeaders.Add(newLeader);

        Debug.Log($"<color=purple>A new Great Leader, {leaderName}, has emerged for the {species.proceduralName}! They are a {ability.abilityName}.</color>");

        PlanetTile soundOrigin = GetSpeciesSoundOrigin(species);
        if (soundOrigin != null)
        {
            simManager.audioManager.PlayDynamicSound("LeaderSpawn", soundOrigin.position, frequencyOverride: 400f);
        }
    }

    private PlanetTile GetSpeciesSoundOrigin(SpeciesData species)
    {
        return simManager.GetPlanetGraph().Values
            .Where(t => t.populations.ContainsKey(species))
            .OrderByDescending(t => t.populations[species])
            .FirstOrDefault();
    }
}
