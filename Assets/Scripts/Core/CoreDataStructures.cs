// ====================================================================================
// FILE: CoreDataStructures.cs
// ====================================================================================
using UnityEngine;
using System.Collections.Generic;

// --- Enums used across multiple scripts ---
public enum StarSystemType { SingleStar, BinaryStar_Erratic, DecayingOrbit }
public enum BiologicalClass { Carbon_Mammalian, Carbon_Insectoid, Silicon_Crystalline, Flora_Sentient, Carbon_Aquatic, Aetherial_Hivemind }
public enum Ethic { Militarist, Pacifist, Erudite }
public enum GovernmentType { None, Autocracy, Collective, Technocracy }
public enum TechBonusType { FoodGatheringEfficiency, TemperatureTolerance, NewHabitableTerrain, GeologicalDamageResistance, VolcanoEnergyHarnessing, TerraformLand, TerraformMountain, CombatBonus, KnowledgeBonus }
public enum TerrainType { Ocean, Land, Mountain }
public enum EventType { Standard, Significant }
public enum DietType { Herbivore, Carnivore, Omnivore, Photosynthetic, Lithovore }
public enum TreatyType { ResearchAgreement, DefensivePact }

[System.Serializable]
public struct AtmosphericGas
{
    public string gasName;
    public float concentration; // As a percentage, e.g., 0.78 for 78%
}

// --- PlanetData.cs ---
[CreateAssetMenu(fileName = "New PlanetData", menuName = "Alien Civilizations/Planet Data")]
public class PlanetData : ScriptableObject
{
    [Header("Celestial Configuration")]
    public StarSystemType systemType = StarSystemType.SingleStar;

    [Header("Procedural Terrain Generation")]
    public float planetRadius = 10f;
    public float noiseScale = 1.5f;
    public float noiseStrength = 1.2f;
    public float seaLevel = 10.2f;

    [Header("Climate System")]
    public float baseSurfaceTemperature = 15f;
    [System.NonSerialized] public float currentSurfaceTemperature;

    // Using a simple list of serializable structs to make it editable in the Inspector.
    public List<AtmosphericGas> initialAtmosphere;
    [System.NonSerialized] public Dictionary<string, float> atmosphericComposition;

    [Tooltip("Gases in this list will contribute to the greenhouse effect.")]
    public List<string> greenhouseGases;
    public float greenhouseEffectFactor = 0.01f;


    [Header("Orbital Dynamics (Decaying Orbit)")]
    public float moonInitialDistance = 1000f;
    [System.NonSerialized] public float moonCurrentDistance;
    public float moonDecayRate = 1f;

    [Header("Dynamic Geology")]
    public float tectonicActivityChance = 0.2f;
    public int ticksPerGeologicalEvent = 200;
}

// --- SpeciesData.cs ---
[CreateAssetMenu(fileName = "New SpeciesData (Archetype)", menuName = "Alien Civilizations/Species Archetype")]
public class SpeciesData : ScriptableObject
{
    [Header("Core Identity")]
    public string speciesName;
    public GameObject speciesPrefab;
    public BiologicalClass biology = BiologicalClass.Carbon_Mammalian;

    [Header("Diet & Reproduction")]
    public DietType diet;
    public string primaryFoodSource = "Flora"; // Ignored by Carnivores
    public float baseFoodConsumptionRate = 0.1f;
    public float baseReproductionRate = 0.05f;

    [Header("Habitat Preferences")]
    public List<TerrainType> baseValidTerrainTypes;
    public float idealTemperature = 15f;
    public float baseTemperatureTolerance = 10f;
    [Header("Atmospheric Preferences")]
    public string requiredGas = "O2";
    public float idealGasConcentration = 0.21f;
    public float baseGasConcentrationTolerance = 0.05f;

    [Header("Diplomacy & Conflict")]
    [Range(0f, 1f)] public float aggressionFactor = 0.5f;
    [Range(0f, 1f)] public float cooperationFactor = 0.2f;
    public float baseCombatStrength = 1.0f;

    [Header("Industrialization")]
    public float industrializationRate = 0.05f; // Rate of converting minerals to industry
    public float pollutionFactor = 0.1f; // How much pollution is generated per industry level

    // --- Runtime Data (Non-Serialized) ---
    public string proceduralName;
    public float currentKnowledge;
    public List<Technology> discoveredTechnologies;
    public GovernmentType government;
    public Dictionary<Ethic, float> ethicScores;
    public Dictionary<SpeciesData, float> dispositions;
    public List<GreatLeader> activeLeaders;
    public Dictionary<long, int> populationHistory;
    public Dictionary<string, int> deathTolls;
    public Technology targetTechnology;
    public Dictionary<PlanetTile, PlanetTile> migrationIntents;
    public float industryLevel;
    public List<Treaty> activeTreaties;

    public void InitializeRuntimeData()
    {
        proceduralName = speciesName;
        currentKnowledge = 0;
        discoveredTechnologies = new List<Technology>();
        government = GovernmentType.None;
        ethicScores = new Dictionary<Ethic, float> { { Ethic.Militarist, 0f }, { Ethic.Pacifist, 0f }, { Ethic.Erudite, 0f } };
        dispositions = new Dictionary<SpeciesData, float>();
        activeLeaders = new List<GreatLeader>();
        populationHistory = new Dictionary<long, int>();
        deathTolls = new Dictionary<string, int> { {"Starvation", 0}, {"Environment", 0}, {"Predation", 0}, {"Geology", 0} };
        targetTechnology = null;
        migrationIntents = new Dictionary<PlanetTile, PlanetTile>();
        industryLevel = 0f;
        activeTreaties = new List<Treaty>();
    }

    // --- On-the-Fly Stat Calculation ---

    public float GetCurrentCombatStrength()
    {
        float current = baseCombatStrength;
        // Additive bonuses first
        current += discoveredTechnologies.Where(t => t.bonusType == TechBonusType.CombatBonus).Sum(t => t.bonusValue);
        current += activeLeaders.Where(l => l.ability.bonusType == TechBonusType.CombatBonus && !l.ability.isMultiplier).Sum(l => l.ability.bonusValue);

        // Multiplicative bonuses last
        if (government == GovernmentType.Autocracy) current *= 1.2f;
        foreach(var leader in activeLeaders.Where(l => l.ability.bonusType == TechBonusType.CombatBonus && l.ability.isMultiplier))
        {
            current *= leader.ability.bonusValue;
        }
        return current;
    }

    public float GetCurrentFoodConsumptionRate()
    {
        float current = baseFoodConsumptionRate;
        // Multiplicative bonuses (efficiency)
        float efficiencyBonus = discoveredTechnologies.Where(t => t.bonusType == TechBonusType.FoodGatheringEfficiency).Sum(t => t.bonusValue);
        efficiencyBonus += activeLeaders.Where(l => l.ability.bonusType == TechBonusType.FoodGatheringEfficiency).Sum(l => l.ability.bonusValue);
        current *= (1 - efficiencyBonus);

        if (government == GovernmentType.Collective) current *= 0.8f;
        return Mathf.Max(0.01f, current);
    }

    public float GetCurrentTemperatureTolerance()
    {
        float current = baseTemperatureTolerance;
        current += discoveredTechnologies.Where(t => t.bonusType == TechBonusType.TemperatureTolerance).Sum(t => t.bonusValue);
        current += activeLeaders.Where(l => l.ability.bonusType == TechBonusType.TemperatureTolerance).Sum(l => l.ability.bonusValue);
        return current;
    }

    public List<TerrainType> GetCurrentValidTerrainTypes()
    {
        var current = new List<TerrainType>(baseValidTerrainTypes);
        var newTerrains = discoveredTechnologies.Where(t => t.bonusType == TechBonusType.NewHabitableTerrain).Select(t => t.newTerrain);
        current.AddRange(newTerrains);
        return current.Distinct().ToList();
    }
}

// --- Technology.cs ---
[CreateAssetMenu(fileName = "New Technology", menuName = "Alien Civilizations/Technology")]
public class Technology : ScriptableObject
{
    public string techName;
    [TextArea] public string description;
    public float knowledgeCost;
    public List<Technology> prerequisites;
    public TechBonusType bonusType;
    public float bonusValue;
    public TerrainType newTerrain;
}

// --- ProceduralTrait.cs ---
[CreateAssetMenu(fileName = "New ProceduralTrait", menuName = "Alien Civilizations/Procedural Trait")]
public class ProceduralTrait : ScriptableObject
{
    public string traitName;
    [TextArea] public string description;
    public enum ModifierType { Additive, Multiplicative }
    public enum SpawnCondition { HotPlanet, ColdPlanet, ScarceResources, HighGravity, VolatileGeology }
    public SpawnCondition condition;
    public string targetFieldName;
    public float modifierValue;
    public ModifierType modifierType;
}

// --- Fully Implemented Data-only Classes ---

/// <summary>
/// Represents a single tile on the planet's surface, the fundamental unit of the simulation grid.
/// </summary>
public class PlanetTile
{
    public readonly int id; // Corresponds to a vertex index in the planet mesh
    public readonly Vector3 position;
    public List<int> neighbours; // List of neighbour tile IDs

    public TerrainType terrainType;
    public float baseTemperature; // The tile's temperature without any global effects
    public float currentTemperature; // The actual temperature after all modifiers

    // Resources available on the tile, e.g., "Flora": 100, "Silicon": 50
    public Dictionary<string, float> resources;
    // Populations of different species living on this tile
    public Dictionary<SpeciesData, long> populations;

    public PlanetTile(int id, Vector3 position)
    {
        this.id = id;
        this.position = position;
        this.neighbours = new List<int>();
        this.resources = new Dictionary<string, float>();
        this.populations = new Dictionary<SpeciesData, long>();
    }
}

/// <summary>
/// Represents a diplomatic agreement between two species.
/// </summary>
public class Treaty
{
    public TreatyType type;
    public List<SpeciesData> members = new List<SpeciesData>(2);
    public long startTick;
    public int durationTicks;

    public Treaty(TreatyType type, SpeciesData s1, SpeciesData s2, long currentTick, int duration)
    {
        this.type = type;
        this.members.Add(s1);
        this.members.Add(s2);
        this.startTick = currentTick;
        this.durationTicks = duration;
    }

    public bool IsExpired(long currentTick)
    {
        return currentTick > startTick + durationTicks;
    }
}

/// <summary>
/// Represents a unique individual with powerful, species-wide effects.
/// </summary>
public class GreatLeader
{
    public string leaderName;
    public GreatLeaderAbility ability;
    public SpeciesData species;
    public long birthTick;
    public int ageInTicks;
    public int maxAgeInTicks;

    public GreatLeader(string name, GreatLeaderAbility ability, SpeciesData species, long birthTick, int lifespan)
    {
        this.leaderName = name;
        this.ability = ability;
        this.species = species;
        this.birthTick = birthTick;
        this.ageInTicks = 0;
        this.maxAgeInTicks = lifespan;
    }
}

/// <summary>
/// A ScriptableObject defining a specific Great Leader ability.
/// </summary>
[CreateAssetMenu(fileName = "New Leader Ability", menuName = "Alien Civilizations/Great Leader Ability")]
public class GreatLeaderAbility : ScriptableObject
{
    public string abilityName;
    [TextArea] public string description;
    public TechBonusType bonusType;
    public float bonusValue;
    public bool isMultiplier; // Is the bonus multiplicative or additive?
}

/// <summary>
/// Represents a noteworthy event to be displayed on the game's timeline.
/// </summary>
public class TimelineEvent
{
    public long tick;
    public EventType eventType;
    public string description;

    public TimelineEvent(long tick, string description, EventType type = EventType.Standard)
    {
        this.tick = tick;
        this.description = description;
        this.eventType = type;
    }
}

/// <summary>
/// A ScriptableObject to define different AI behaviors and priorities.
/// </summary>
[CreateAssetMenu(fileName = "New AIPersonality", menuName = "Alien Civilizations/AI Personality")]
public class AIPersonality : ScriptableObject
{
    public string personalityName;
    [Range(0.5f, 2.0f)] public float aggressionMultiplier = 1.0f;
    [Range(0.5f, 2.0f)] public float expansionism = 1.0f;

    public enum ResearchPriority { Balanced, Military, Growth, Exploration, Defense }
    public ResearchPriority researchFocus = ResearchPriority.Balanced;
}

/// <summary>
/// A ScriptableObject defining a type of celestial event that can occur.
/// </summary>
[CreateAssetMenu(fileName = "New CelestialEvent", menuName = "Alien Civilizations/Celestial Event")]
public class CelestialEvent : ScriptableObject
{
    public string eventName;
    [TextArea] public string description;
    public float globalTemperatureChange;
    public float radiationIncrease;
    public float chancePerTick; // The chance this event occurs on any given tick
}


// --- Save Game Data Structures ---

/// <summary>
/// A container for all data needed to save and load a game state.
/// </summary>
[System.Serializable]
public class SaveData
{
    public long currentTick;
    public PlanetDataSave savedPlanetData;
    public List<SpeciesSaveData> savedSpecies;
    public List<PlanetTileSaveData> savedTiles;
}

[System.Serializable]
public class PlanetDataSave
{
    public float surfaceTemperature;
    public float atmosphericCO2;
    public float moonCurrentDistance;
}

[System.Serializable]
public class SpeciesSaveData
{
    public string speciesArchetypeName; // Used to look up the base ScriptableObject
    public string proceduralName;
    public float currentKnowledge;
    public List<string> discoveredTechnologyNames;
    public GovernmentType government;
    // Dictionaries are not directly serializable by Unity's default serializer.
    // This would require a more complex solution (e.g., custom serialization or converting to lists).
}

[System.Serializable]
public class PlanetTileSaveData
{
    public int id;
    // ... other tile properties to save
}

/// <summary>
/// A container for data that persists between game sessions (e.g., achievements, unlocks).
/// </summary>
[System.Serializable]
public class LegacyData
{
    public List<string> unlockedSpeciesArchetypes;
    public List<string> achievedVictoryTypes;
    public int totalGamesPlayed;
}


// This class is required by SimulationManager but was not defined in the initial prompt's data structures.
// Creating a placeholder ScriptableObject for it.
[CreateAssetMenu(fileName = "New PlayerSelections", menuName = "Alien Civilizations/Player Selections")]
public class PlayerSelections : ScriptableObject
{
    public bool isSandboxMode;
    public List<SpeciesData> selectedArchetypes;

    public List<SpeciesData> GetAllSelectedArchetypes()
    {
        return selectedArchetypes ?? new List<SpeciesData>();
    }
}
