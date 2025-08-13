using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CelestialEventManager : MonoBehaviour
{
    private SimulationManager simManager;
    private PlanetData planetData;

    public void Initialize(SimulationManager manager, PlanetData data)
    {
        simManager = manager;
        planetData = data;

        if (planetData != null)
        {
            // Initialize runtime variables from the planet data asset
            planetData.moonCurrentDistance = planetData.moonInitialDistance;
            planetData.currentSurfaceTemperature = planetData.baseSurfaceTemperature;

            // Convert the initial list of gases into a runtime dictionary
            planetData.atmosphericComposition = new Dictionary<string, float>();
            if (planetData.initialAtmosphere != null)
            {
                foreach (var gas in planetData.initialAtmosphere)
                {
                    planetData.atmosphericComposition[gas.gasName] = gas.concentration;
                }
            }

            // Ensure Pollution is treated as a greenhouse gas if it's not already in the list.
            if (planetData.greenhouseGases != null && !planetData.greenhouseGases.Contains("Pollution"))
            {
                planetData.greenhouseGases.Add("Pollution");
            }
        }
    }

    [Tooltip("A list of all possible celestial events that can be triggered. Should be assigned in the Unity Editor.")]
    public List<CelestialEvent> possibleEvents;

    public void ProcessCelestialEvents()
    {
        if (planetData == null) return;

        // --- 1. Process Climate Model ---
        ProcessClimateModel();

        // --- 2. Handle passive, continuous effects based on the star system type. ---
        switch (planetData.systemType)
        {
            case StarSystemType.DecayingOrbit:
                planetData.moonCurrentDistance -= planetData.moonDecayRate;
                if (planetData.moonCurrentDistance < 0) planetData.moonCurrentDistance = 0;
                break;
        }

        // --- 3. Handle random, active events from the list of possibilities. ---
        if (possibleEvents != null)
        {
            foreach (var celestialEvent in possibleEvents)
            {
                if (Random.value < celestialEvent.chancePerTick)
                {
                    TriggerCelestialEvent(celestialEvent);
                }
            }
        }
    }

    private void ProcessClimateModel()
    {
        // TODO: In the future, species industrial activity could add gases.
        // For now, the atmosphere is static unless changed by an event.

        // Calculate total greenhouse effect
        float totalGreenhouseConcentration = 0f;
        if (planetData.greenhouseGases != null)
        {
            foreach (string gasName in planetData.greenhouseGases)
            {
                if (planetData.atmosphericComposition.ContainsKey(gasName))
                {
                    totalGreenhouseConcentration += planetData.atmosphericComposition[gasName];
                }
            }
        }

        // Calculate the global temperature based on the total greenhouse effect.
        float tempIncreaseFromGases = totalGreenhouseConcentration * planetData.greenhouseEffectFactor;
        planetData.currentSurfaceTemperature = planetData.baseSurfaceTemperature + tempIncreaseFromGases;

        // Propagate this change to all tiles via the SimulationManager
        simManager.UpdateAllTileTemperatures(planetData.currentSurfaceTemperature);
    }

    private void TriggerCelestialEvent(CelestialEvent cEvent)
    {
        Debug.Log($"A celestial event has occurred: {cEvent.eventName}!");

        // Apply global effects defined in the ScriptableObject.
        planetData.baseSurfaceTemperature += cEvent.globalTemperatureChange;

        if(cEvent.globalTemperatureChange != 0)
            Debug.Log($"Global base temperature changed by {cEvent.globalTemperatureChange}.");
        if(cEvent.radiationIncrease > 0)
            Debug.Log($"Radiation levels increased by {cEvent.radiationIncrease}.");
    }
}
