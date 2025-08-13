using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AIPlayerController : MonoBehaviour
{
    [Tooltip("The personality driving this AI's decisions. Should be assigned in the Editor.")]
    public AIPersonality personality;

    private SpeciesData controlledSpecies;
    private SimulationManager simManager;
    private List<Technology> masterTechList;

    public void Initialize(SpeciesData species, SimulationManager manager, List<Technology> allTech, AIPersonality pers)
    {
        controlledSpecies = species;
        simManager = manager;
        masterTechList = allTech;
        personality = pers;
    }

    /// <summary>
    /// The main decision-making function for the AI, called each tick.
    /// </summary>
    public void ProcessAIDecisions()
    {
        if (controlledSpecies == null || simManager.GetTotalPopulation(controlledSpecies) <= 0) return;

        // 1. Choose a technology to research if we don't have a goal.
        if (controlledSpecies.targetTechnology == null)
        {
            ChooseNextTechnology();
        }

        // 2. Assess all tiles and plan migrations if necessary.
        AssessAndPlanMigration();
    }

    private void ChooseNextTechnology()
    {
        var availableTechs = masterTechList.Where(tech =>
            !controlledSpecies.discoveredTechnologies.Contains(tech) &&
            tech.prerequisites.All(p => controlledSpecies.discoveredTechnologies.Contains(p))
        ).ToList();

        if (availableTechs.Count == 0) return;

        // --- Enhanced Tech Selection ---
        // 1. Check for pressing needs based on death tolls
        var primaryPressure = controlledSpecies.deathTolls
            .Where(kvp => kvp.Value > 0)
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault().Key;

        Technology chosenTech = null;

        if (primaryPressure != null)
        {
            switch (primaryPressure)
            {
                case "Predation":
                case "Geology": // Combat strength helps survive geological events in this simplified model
                    chosenTech = availableTechs.FirstOrDefault(t => t.bonusType == TechBonusType.CombatBonus);
                    break;
                case "Starvation":
                    chosenTech = availableTechs.FirstOrDefault(t => t.bonusType == TechBonusType.FoodGatheringEfficiency);
                    break;
                case "Environment":
                    chosenTech = availableTechs.FirstOrDefault(t => t.bonusType == TechBonusType.TemperatureTolerance);
                    break;
            }
        }

        // 2. If no pressing need, use personality
        if (chosenTech == null)
        {
            switch (personality.researchFocus)
            {
                case AIPersonality.ResearchPriority.Military:
                    chosenTech = availableTechs.FirstOrDefault(t => t.bonusType == TechBonusType.CombatBonus);
                    break;
                case AIPersonality.ResearchPriority.Growth:
                    chosenTech = availableTechs.FirstOrDefault(t => t.bonusType == TechBonusType.FoodGatheringEfficiency);
                    break;
                case AIPersonality.ResearchPriority.Exploration:
                    chosenTech = availableTechs.FirstOrDefault(t => t.bonusType == TechBonusType.NewHabitableTerrain);
                    break;
            }
        }

        // 3. If still no specific tech, just pick the cheapest
        if (chosenTech == null)
        {
            chosenTech = availableTechs.OrderBy(t => t.knowledgeCost).First();
        }

        controlledSpecies.targetTechnology = chosenTech;
        Debug.Log($"<color=orange>{controlledSpecies.proceduralName} AI has set a new research goal: {controlledSpecies.targetTechnology.techName}</color>");
    }

    private void AssessAndPlanMigration()
    {
        controlledSpecies.migrationIntents.Clear();
        var ownedTiles = simManager.GetPlanetGraph().Values.Where(t => t.populations.ContainsKey(controlledSpecies)).ToList();

        foreach (var tile in ownedTiles)
        {
            long pop = tile.populations[controlledSpecies];
            bool isOvercrowded = pop > 200;
            bool isStarving = !tile.resources.ContainsKey(controlledSpecies.primaryFoodSource) && controlledSpecies.diet != DietType.Carnivore;

            if (isOvercrowded || isStarving)
            {
                PlanetTile bestNeighbor = FindBestMigrationTarget(tile);
                if (bestNeighbor != null)
                {
                    controlledSpecies.migrationIntents[tile] = bestNeighbor;
                }
            }
        }
    }

    private PlanetTile FindBestMigrationTarget(PlanetTile fromTile)
    {
        PlanetTile bestTarget = null;
        float bestScore = float.MinValue;

        foreach (int neighborId in fromTile.neighbours)
        {
            PlanetTile neighbor = simManager.GetPlanetGraph()[neighborId];

            if (!controlledSpecies.GetCurrentValidTerrainTypes().Contains(neighbor.terrainType)) continue;

            float score = 0;

            // Score based on food availability
            if (controlledSpecies.diet == DietType.Carnivore)
            {
                score += neighbor.populations.Where(p => p.Key.diet != DietType.Carnivore).Sum(p => p.Value) * 0.1f;
            }
            else
            {
                score += neighbor.resources.ContainsKey(controlledSpecies.primaryFoodSource) ? neighbor.resources[controlledSpecies.primaryFoodSource] : 0;
            }

            // Negative score for existing populations (prefer empty tiles)
            score -= neighbor.populations.Sum(kvp => (long)kvp.Value);

            // Penalty for hostile neighbors (even if not at war)
            foreach (var enemy in neighbor.populations.Keys)
            {
                if (controlledSpecies.dispositions.ContainsKey(enemy) && controlledSpecies.dispositions[enemy] < 0)
                {
                    score += controlledSpecies.dispositions[enemy]; // Add negative disposition
                }
            }

            // Bonus for defensible terrain if militaristic
            if (controlledSpecies.government == GovernmentType.Autocracy && neighbor.terrainType == TerrainType.Mountain)
            {
                score += 50; // Arbitrary bonus
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = neighbor;
            }
        }
        return bestTarget;
    }
}
