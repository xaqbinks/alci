using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A persistent singleton that holds the player's selections from the main menu
/// and carries them over into the main simulation scene.
/// </summary>
public class GameSetupManager : MonoBehaviour
{
    public static GameSetupManager instance;

    [Header("Player Selections")]
    public PlanetData selectedPlanet;
    public SpeciesData playerSpecies;
    public List<SpeciesData> opponentSpecies = new List<SpeciesData>();
    public bool isSandboxMode = false;

    private void Awake()
    {
        // Singleton pattern: Ensure only one instance of this manager exists.
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Gathers all selected species (player and opponents) into a single list.
    /// </summary>
    /// <returns>A list of all SpeciesData archetypes for the match.</returns>
    public List<SpeciesData> GetAllSelectedSpecies()
    {
        var allSpecies = new List<SpeciesData>();
        if (playerSpecies != null)
        {
            allSpecies.Add(playerSpecies);
        }
        allSpecies.AddRange(opponentSpecies);
        return allSpecies;
    }
}
