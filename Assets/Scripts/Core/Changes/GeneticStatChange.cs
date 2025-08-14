using UnityEngine;
using System.Reflection;

public enum ModifierType { Additive, Multiplicative }

/// <summary>
/// A minimal change that modifies a single float stat on a species.
/// </summary>
[CreateAssetMenu(fileName = "New GeneticStatChange", menuName = "Alien Civilizations/Minimal Changes/Genetic Stat Change")]
public class GeneticStatChange : MinimalChangePreset
{
    [Header("Genetic Change Parameters")]
    [Tooltip("The exact name of the public float variable to modify on SpeciesData (e.g., 'baseReproductionRate').")]
    public string targetFieldName;

    [Tooltip("How the modifier is applied. This determines which value below is used.")]
    public ModifierType modifierType;

    [Tooltip("The small amount to ADD to the stat. Used if Modifier Type is Additive.")]
    [Range(-0.1f, 0.1f)]
    public float additiveValue = 0.01f;

    [Tooltip("The small percentage to MULTIPLY the stat by. Used if Modifier Type is Multiplicative.")]
    [Range(0.9f, 1.1f)]
    public float multiplicativeValue = 1.01f;


    public override void Apply(SimulationManager simulationManager, SpeciesData targetSpecies)
    {
        if (targetSpecies == null)
        {
            Debug.LogError("GeneticStatChange requires a target species!");
            return;
        }

        FieldInfo field = typeof(SpeciesData).GetField(targetFieldName);
        if (field != null && field.FieldType == typeof(float))
        {
            float currentValue = (float)field.GetValue(targetSpecies);
            if (modifierType == ModifierType.Additive)
            {
                field.SetValue(targetSpecies, currentValue + additiveValue);
            }
            else // Multiplicative
            {
                field.SetValue(targetSpecies, currentValue * multiplicativeValue);
            }
            Debug.Log($"Applied genetic change: {targetSpecies.speciesName}'s {targetFieldName} changed to {field.GetValue(targetSpecies)}.");
        }
        else
        {
            Debug.LogWarning($"GeneticStatChange Error: Could not find public float field '{targetFieldName}' on SpeciesData.");
        }
    }
}
