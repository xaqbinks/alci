using UnityEngine;

/// <summary>
/// Defines the high-level category of a minimal change intervention.
/// </summary>
public enum ChangeCategory
{
    Planetary,
    Genetic,
    Sensory,
    Cognitive,
    Celestial
}

/// <summary>
/// An abstract ScriptableObject that represents the base for any single,
/// minimal intervention a player can make in the game.
/// </summary>
public abstract class MinimalChangePreset : ScriptableObject
{
    [Header("Intervention Metadata")]
    [Tooltip("The user-facing name of this change.")]
    public string changeName;

    [Tooltip("A detailed description of what this change does, shown in the UI.")]
    [TextArea(3, 5)]
    public string description;

    [Tooltip("The category this change belongs to.")]
    public ChangeCategory category;

    /// <summary>
    /// The core method that applies this intervention's logic to the simulation.
    /// This must be implemented by all derived classes.
    /// </summary>
    /// <param name="simulationManager">A reference to the main simulation manager.</param>
    /// <param name="targetSpecies">The species this change is being applied to (can be null for planetary changes).</param>
    public abstract void Apply(SimulationManager simulationManager, SpeciesData targetSpecies);
}
