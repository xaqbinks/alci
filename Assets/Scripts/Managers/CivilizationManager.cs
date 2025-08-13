using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CivilizationManager : MonoBehaviour
{
    private SimulationManager simManager;
    private List<SpeciesData> activeSpecies;
    private List<Technology> masterTechList;
    private const string INDUSTRIAL_MINERAL = "Silicon"; // The resource needed for industry

    public void Initialize(SimulationManager manager, List<SpeciesData> speciesList, List<Technology> allTech)
    {
        simManager = manager;
        activeSpecies = speciesList;
        masterTechList = allTech;
    }

    public void ProcessCivilizationTick()
    {
        if (activeSpecies == null) return;

        float totalPollutionGenerated = 0;

        foreach (var species in activeSpecies)
        {
            long totalPopulation = simManager.GetTotalPopulation(species);
            if (totalPopulation <= 0) continue;

            // --- 1. Industrialization ---
            // Check if the species has the "Industrialization" tech to build industry
            bool canIndustrialize = species.discoveredTechnologies.Any(t => t.techName == "Industrialization");
            if (canIndustrialize)
            {
                // Find a tile with minerals and population to build industry
                var industrialTile = simManager.GetPlanetGraph().Values.FirstOrDefault(t =>
                    t.populations.ContainsKey(species) &&
                    t.resources.ContainsKey(INDUSTRIAL_MINERAL) &&
                    t.resources[INDUSTRIAL_MINERAL] > 1f
                );

                if (industrialTile != null)
                {
                    float mineralsConsumed = industrialTile.resources[INDUSTRIAL_MINERAL] * species.industrializationRate;
                    industrialTile.resources[INDUSTRIAL_MINERAL] -= mineralsConsumed;
                    species.industryLevel += mineralsConsumed;
                }
            }

            // --- 2. Knowledge Generation ---
            float knowledgeGain = 1.0f + (totalPopulation / 500f);
            knowledgeGain += species.industryLevel * 0.1f; // Industry provides a knowledge bonus

            // Apply government bonuses
            if (species.government == GovernmentType.Technocracy)
                knowledgeGain *= 1.25f;

            // Apply treaty bonuses
            if (species.activeTreaties.Any(t => t.type == TreatyType.ResearchAgreement))
            {
                knowledgeGain *= 1.15f; // 15% bonus from research agreements
            }

            species.currentKnowledge += knowledgeGain;

            // --- 3. Pollution ---
            totalPollutionGenerated += species.industryLevel * species.pollutionFactor;

            // --- 4. Evolving Ethics ---
            UpdateEthics(species);

            // --- 5. Technology Discovery ---
            ProcessTechnology(species);
        }

        // --- Apply Pollution to Planet ---
        if (totalPollutionGenerated > 0)
        {
            simManager.AddAtmosphericGas("Pollution", totalPollutionGenerated * 0.001f);
        }
    }

    private void ProcessTechnology(SpeciesData species)
    {
        if (species.targetTechnology != null &&
            species.currentKnowledge >= species.targetTechnology.knowledgeCost &&
            !species.discoveredTechnologies.Contains(species.targetTechnology) &&
            species.targetTechnology.prerequisites.All(p => species.discoveredTechnologies.Contains(p)))
        {
            Technology discoveredTech = species.targetTechnology;

            species.currentKnowledge -= discoveredTech.knowledgeCost;
            species.discoveredTechnologies.Add(discoveredTech);

            // Find a tile to play the sound from (e.g., the most populous one)
            PlanetTile soundOrigin = simManager.GetPlanetGraph().Values
                .Where(t => t.populations.ContainsKey(species))
                .OrderByDescending(t => t.populations[species])
                .FirstOrDefault();

            if (soundOrigin != null)
            {
                // More expensive techs get a higher pitch and more "sci-fi" sound
                float techScale = Mathf.Clamp01(discoveredTech.knowledgeCost / 2000f); // Assuming max cost of 2000
                float frequency = Mathf.Lerp(440f, 880f, techScale);
                float fmAmount = Mathf.Lerp(100f, 500f, techScale);
                simManager.audioManager.PlayDynamicSound("TechDiscovery", soundOrigin.position, frequency, fmAmount);
            }

            Debug.Log($"<color=blue>{species.proceduralName} has discovered its target technology: {discoveredTech.techName}!</color>");
            species.targetTechnology = null;

            if (discoveredTech.techName == "Social Cohesion" && species.government == GovernmentType.None)
            {
                species.government = DetermineGovernment(species);
                if (soundOrigin != null)
                {
                    // Different governments get different sounds
                    float govFrequency = 400f;
                    if (species.government == GovernmentType.Autocracy) govFrequency = 250f; // Lower, more imposing
                    if (species.government == GovernmentType.Technocracy) govFrequency = 600f; // Higher, more cerebral
                    simManager.audioManager.PlayDynamicSound("GovernmentFormed", soundOrigin.position, govFrequency);
                }
                Debug.Log($"<color=yellow>{species.proceduralName} has formed a {species.government}!</color>");
            }
        }
    }

    private GovernmentType DetermineGovernment(SpeciesData species)
    {
        if (species.ethicScores[Ethic.Militarist] >= species.ethicScores[Ethic.Erudite] && species.ethicScores[Ethic.Militarist] >= species.ethicScores[Ethic.Pacifist])
            return GovernmentType.Autocracy;
        if (species.ethicScores[Ethic.Erudite] >= species.ethicScores[Ethic.Militarist] && species.ethicScores[Ethic.Erudite] >= species.ethicScores[Ethic.Pacifist])
            return GovernmentType.Technocracy;
        return GovernmentType.Collective;
    }

    private void UpdateEthics(SpeciesData species)
    {
        if (simManager.WasInCombat(species))
        {
            species.ethicScores[Ethic.Militarist] += 0.1f;
            species.ethicScores[Ethic.Pacifist] -= 0.05f;
        }
        else
        {
            species.ethicScores[Ethic.Pacifist] += 0.05f;
        }

        if (species.targetTechnology != null)
        {
            species.ethicScores[Ethic.Erudite] += 0.05f;
        }

        float totalScore = species.ethicScores.Values.Sum();
        if (totalScore > 0)
        {
            var keys = species.ethicScores.Keys.ToList();
            foreach (var key in keys)
            {
                species.ethicScores[key] = Mathf.Clamp01(species.ethicScores[key] / totalScore);
            }
        }
    }
}
