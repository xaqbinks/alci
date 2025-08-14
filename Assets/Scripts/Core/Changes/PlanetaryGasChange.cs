using UnityEngine;

/// <summary>
/// A minimal change that alters the concentration of a gas in the planet's atmosphere.
/// </summary>
[CreateAssetMenu(fileName = "New PlanetaryGasChange", menuName = "Alien Civilizations/Minimal Changes/Planetary Gas Change")]
public class PlanetaryGasChange : MinimalChangePreset
{
    [Header("Planetary Change Parameters")]
    [Tooltip("The name of the gas to alter (e.g., 'Oxygen', 'CO2').")]
    public string gasName;

    [Tooltip("The amount of gas to add to the atmosphere. Can be negative.")]
    public float amountToAdd;

    public override void Apply(SimulationManager simulationManager, SpeciesData targetSpecies)
    {
        if (simulationManager == null)
        {
            Debug.LogError("PlanetaryGasChange requires a reference to the SimulationManager!");
            return;
        }

        simulationManager.AddAtmosphericGas(gasName, amountToAdd);
        Debug.Log($"Applied planetary change: Added {amountToAdd} of {gasName} to the atmosphere.");
    }
}
