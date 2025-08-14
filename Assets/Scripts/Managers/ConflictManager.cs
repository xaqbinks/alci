using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ConflictManager : MonoBehaviour
{
    private SimulationManager simManager;
    private List<PlanetTile> activeTiles;

    // The disposition level at which species are considered "at war" and will fight.
    private const float WAR_THRESHOLD = -50f;
    // A base casualty rate for combat.
    private const float BASE_CASUALTY_RATE = 0.1f;

    public void Initialize(SimulationManager manager, List<PlanetTile> allActiveTiles)
    {
        simManager = manager;
        activeTiles = allActiveTiles;
    }

    public void ProcessConflictTick()
    {
        if (activeTiles == null) return;

        // Iterate over a copy since the underlying collection of populations might change.
        foreach (var tile in new List<PlanetTile>(activeTiles))
        {
            // A conflict requires at least two species on the same tile.
            if (tile.populations.Count < 2) continue;

            var combatants = tile.populations.Keys.ToList();
            // Check every pair of species on the tile for conflict.
            for (int i = 0; i < combatants.Count; i++)
            {
                for (int j = i + 1; j < combatants.Count; j++)
                {
                    SpeciesData s1 = combatants[i];
                    SpeciesData s2 = combatants[j];

                    // Check if their disposition is low enough to be at war.
                    if (s1.dispositions.ContainsKey(s2) && s1.dispositions[s2] < WAR_THRESHOLD)
                    {
                        ResolveCombat(tile, s1, s2);
                        CheckForDefensivePacts(s1, s2);
                        CheckForDefensivePacts(s2, s1);
                    }
                }
            }
        }
    }

    private void CheckForDefensivePacts(SpeciesData attacker, SpeciesData defender)
    {
        // Find if the defender has any allies who should join the war.
        foreach (var treaty in defender.activeTreaties)
        {
            if (treaty.type == TreatyType.DefensivePact)
            {
                // Find the ally (the other member of the pact)
                SpeciesData ally = treaty.members.FirstOrDefault(m => m != defender);
                if (ally != null && ally != attacker)
                {
                    // If the ally is not already at war with the attacker, pull them in.
                    if (ally.dispositions[attacker] > WAR_THRESHOLD)
                    {
                        ally.dispositions[attacker] = WAR_THRESHOLD - 1;
                        attacker.dispositions[ally] = WAR_THRESHOLD - 1;
                        Debug.Log($"<color=orange>{ally.proceduralName} has joined the war against {attacker.proceduralName} due to a defensive pact with {defender.proceduralName}!</color>");
                    }
                }
            }
        }
    }

    private void ResolveCombat(PlanetTile tile, SpeciesData s1, SpeciesData s2)
    {
        if (!tile.populations.ContainsKey(s1) || !tile.populations.ContainsKey(s2)) return;

        long pop1 = tile.populations[s1];
        long pop2 = tile.populations[s2];

        if (pop1 <= 0 || pop2 <= 0) return;

        // Get the current, fully modified combat strength from the species data.
        float s1Strength = s1.GetCurrentCombatStrength();
        float s2Strength = s2.GetCurrentCombatStrength();

        // Apply terrain modifiers. Mountains provide a defensive bonus.
        if (tile.terrainType == TerrainType.Mountain)
        {
            // For simplicity, we give the bonus to both, representing difficult terrain.
            s1Strength *= 1.25f; // 25% bonus
            s2Strength *= 1.25f;
        }

        // Report that these species were in combat for ethic calculations
        simManager.ReportCombat(s1, s2);

        // --- Trigger Dynamic Combat Sound ---
        // The sound becomes deeper and more chaotic (more FM) as the battle size increases.
        long totalPopInvolved = pop1 + pop2;
        // Normalize total pop to a 0-1 range (assuming a max practical battle size of ~5000 for this calculation)
        float battleScale = Mathf.Clamp01((float)totalPopInvolved / 5000f);
        float frequency = Mathf.Lerp(300f, 80f, battleScale); // Lower frequency for larger battles
        float fmAmount = Mathf.Lerp(50f, 400f, battleScale); // More FM for larger battles
        simManager.audioManager.PlayDynamicSound(GameConstants.SOUND_BATTLE_IMPACT, tile.position, frequencyOverride: frequency, fmAmountOverride: fmAmount);


        // Calculate the total combat power of each side on the tile.
        float power1 = pop1 * s1Strength;
        float power2 = pop2 * s2Strength;

        float totalPower = power1 + power2;
        if (totalPower == 0) return;

        // Calculate losses for each side. Losses are proportional to the enemy's power.
        long losses1 = (long)(pop1 * (power2 / totalPower) * BASE_CASUALTY_RATE);
        long losses2 = (long)(pop2 * (power1 / totalPower) * BASE_CASUALTY_RATE);

        // Apply losses.
        tile.populations[s1] -= losses1;
        tile.populations[s2] -= losses2;

        // Record the deaths for the evolutionary engine.
        s1.deathTolls["Predation"] += (int)losses1;
        s2.deathTolls["Predation"] += (int)losses2;

        Debug.Log($"<color=red>Combat on tile {tile.id} between {s1.proceduralName} and {s2.proceduralName}. Losses: {losses1} vs {losses2}.</color>");
    }
}
