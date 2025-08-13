using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DiplomacyManager : MonoBehaviour
{
    private SimulationManager simManager;
    private List<SpeciesData> activeSpecies;
    private Dictionary<int, PlanetTile> planetGraph;

    private const float TREATY_PROPOSAL_THRESHOLD = 50f;
    private const int TREATY_DURATION = 1000; // Ticks

    public void Initialize(SimulationManager manager, List<SpeciesData> speciesList, Dictionary<int, PlanetTile> graph)
    {
        simManager = manager;
        activeSpecies = speciesList;
        planetGraph = graph;

        if (activeSpecies == null) return;
        foreach (var s1 in activeSpecies)
        {
            if (s1.dispositions == null) s1.dispositions = new Dictionary<SpeciesData, float>();
            foreach (var s2 in activeSpecies)
            {
                if (s1 != s2) s1.dispositions[s2] = 0;
            }
        }
    }

    public void ProcessDiplomacyTick()
    {
        if (activeSpecies == null) return;

        // First, remove any expired treaties
        foreach(var species in activeSpecies)
        {
            species.activeTreaties.RemoveAll(treaty => treaty.IsExpired(simManager.currentTick));
        }

        // Update dispositions and propose new treaties
        for (int i = 0; i < activeSpecies.Count; i++)
        {
            for (int j = i + 1; j < activeSpecies.Count; j++)
            {
                SpeciesData s1 = activeSpecies[i];
                SpeciesData s2 = activeSpecies[j];

                UpdateBilateralRelations(s1, s2);
                ProposeTreaties(s1, s2);
            }
        }
    }

    private void UpdateBilateralRelations(SpeciesData s1, SpeciesData s2)
    {
        float dispositionChange = 0;
        if (HasBorderFriction(s1, s2))
        {
            dispositionChange -= (s1.aggressionFactor + s2.aggressionFactor) * 0.05f;
        }
        else
        {
            dispositionChange += 0.02f;
        }
        s1.dispositions[s2] = Mathf.Clamp(s1.dispositions[s2] + dispositionChange, -100f, 100f);
        s2.dispositions[s1] = s1.dispositions[s2];
    }

    private void ProposeTreaties(SpeciesData s1, SpeciesData s2)
    {
        if (s1.dispositions[s2] < TREATY_PROPOSAL_THRESHOLD) return;

        // Check if they already have a treaty of a certain type
        bool hasResearchPact = s1.activeTreaties.Any(t => t.type == TreatyType.ResearchAgreement && t.members.Contains(s2));
        bool hasDefensivePact = s1.activeTreaties.Any(t => t.type == TreatyType.DefensivePact && t.members.Contains(s2));

        // Propose a Research Agreement
        if (!hasResearchPact && s1.ethicScores[Ethic.Erudite] > 0.3f && s2.ethicScores[Ethic.Erudite] > 0.3f)
        {
            FormTreaty(s1, s2, TreatyType.ResearchAgreement);
        }

        // Propose a Defensive Pact
        if (!hasDefensivePact && s1.ethicScores[Ethic.Pacifist] > 0.3f && s2.ethicScores[Ethic.Pacifist] > 0.3f)
        {
            FormTreaty(s1, s2, TreatyType.DefensivePact);
        }
    }

    private void FormTreaty(SpeciesData s1, SpeciesData s2, TreatyType type)
    {
        var newTreaty = new Treaty(type, s1, s2, simManager.currentTick, TREATY_DURATION);
        s1.activeTreaties.Add(newTreaty);
        s2.activeTreaties.Add(newTreaty);
        Debug.Log($"<color=cyan>{s1.proceduralName} and {s2.proceduralName} have formed a {type}!</color>");
    }

    private bool HasBorderFriction(SpeciesData s1, SpeciesData s2)
    {
        foreach (var tile in planetGraph.Values)
        {
            if (tile.populations.ContainsKey(s1) && tile.populations[s1] > 0)
            {
                foreach (int neighborId in tile.neighbours)
                {
                    if (planetGraph.ContainsKey(neighborId) && planetGraph[neighborId].populations.ContainsKey(s2) && planetGraph[neighborId].populations[s2] > 0)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}
