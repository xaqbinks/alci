using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[RequireComponent(typeof(GeologyManager), typeof(GreatLeaderManager), typeof(CelestialEventManager), typeof(EvolutionManager), typeof(CivilizationManager), typeof(DiplomacyManager), typeof(ConflictManager), typeof(PlanetVisualizer), typeof(SpeciesVisualizer), typeof(AudioManager), typeof(MusicManager))]
public class SimulationManager : MonoBehaviour
{
    [Header("Data & Configuration")]
    [Tooltip("Reference to the ScriptableObject holding the player's choices.")]
    public PlayerSelections playerSelections;
    [Tooltip("A list of all possible technologies in the game.")]
    public List<Technology> masterTechnologyList;
    [Tooltip("A list of all possible procedural traits.")]
    public List<ProceduralTrait> masterTraitList;
    [Tooltip("The planet data asset for the current game.")]
    public PlanetData activePlanet;

    [Header("Manager & UI References")]
    public PlanetViewController planetViewController;
    [HideInInspector] public AudioManager audioManager;
    private MusicManager musicManager;
    private PlanetVisualizer planetVisualizer;
    private SpeciesVisualizer speciesVisualizer;

    public long currentTick { get; private set; }
    public bool isRunning { get; private set; }
    private bool isSandbox;
    private List<SpeciesData> activeSpeciesList;
    private Dictionary<int, PlanetTile> planetGraph;
    private List<PlanetTile> activeTiles = new List<PlanetTile>();
    private GeologyManager geologyManager;
    private GreatLeaderManager leaderManager;
    private CelestialEventManager celestialEventManager;
    private EvolutionManager evolutionManager;
    private CivilizationManager civilizationManager;
    private DiplomacyManager diplomacyManager;
    private ConflictManager conflictManager;
    private List<AIPlayerController> aiControllers = new List<AIPlayerController>();
    private HashSet<SpeciesData> speciesInCombat = new HashSet<SpeciesData>();

    void Start()
    {
        // 1. Generate unique species for this match
        activeSpeciesList = SpeciesGenerator.GenerateSpeciesForMatch(playerSelections, masterTraitList, activePlanet);
        foreach(var species in activeSpeciesList) { species.InitializeRuntimeData(); }

        // 2. Generate Planet Graph
        planetGraph = PlanetMeshGenerator.GeneratePlanetGraph(GetComponentInChildren<MeshFilter>(), activePlanet);

        // 3. Initialize Managers
        geologyManager = GetComponent<GeologyManager>();
        geologyManager.Initialize(this, planetGraph, activePlanet);

        leaderManager = GetComponent<GreatLeaderManager>();
        leaderManager.Initialize(this, activeSpeciesList);

        celestialEventManager = GetComponent<CelestialEventManager>();
        celestialEventManager.Initialize(this, activePlanet);

        evolutionManager = GetComponent<EvolutionManager>();
        evolutionManager.Initialize(this, activeSpeciesList);

        civilizationManager = GetComponent<CivilizationManager>();
        civilizationManager.Initialize(this, activeSpeciesList, masterTechnologyList);

        diplomacyManager = GetComponent<DiplomacyManager>();
        diplomacyManager.Initialize(this, activeSpeciesList, planetGraph);

        conflictManager = GetComponent<ConflictManager>();
        conflictManager.Initialize(this, activeTiles);

        audioManager = GetComponent<AudioManager>();
        audioManager.Initialize();

        musicManager = GetComponent<MusicManager>();
        musicManager.Initialize();

        // Create and initialize AI controllers for non-player species
        // For now, assumes species[0] is the player. A real implementation would have a player selection screen.
        for (int i = 1; i < activeSpeciesList.Count; i++)
        {
            var aiSpecies = activeSpeciesList[i];
            var aiController = gameObject.AddComponent<AIPlayerController>();
            // TODO: Assign a personality. For now, create a default one.
            var personality = ScriptableObject.CreateInstance<AIPersonality>();
            aiController.Initialize(aiSpecies, this, masterTechnologyList, personality);
            aiControllers.Add(aiController);
        }

        // 4. Initialize Visualizers
        planetVisualizer = GetComponent<PlanetVisualizer>();
        planetVisualizer.DrawPlanet(planetGraph); // Draw the planet once at the start

        speciesVisualizer = GetComponent<SpeciesVisualizer>();
        speciesVisualizer.Initialize(this);

        // 5. Spawn initial populations
        SpawnInitialPopulations();

        // 6. Initialize UI and start the simulation
        isSandbox = playerSelections.isSandboxMode;
        isRunning = true;
    }

    void Update()
    {
        // For testing, process one tick per second.
        if (isRunning && Time.frameCount % 60 == 0)
        {
             ProcessTick();
        }
    }

    public void ProcessTick()
    {
        if (!isRunning) return;
        currentTick++;
        Debug.Log($"--- Processing Tick {currentTick} ---");

        // Clear per-tick data
        speciesInCombat.Clear();

        // --- Order of Operations for a Single Tick ---
        // 1. Abiotic Foundation
        geologyManager.ProcessGeologyTick();
        celestialEventManager.ProcessCelestialEvents();

        // 2. Biotic Layer
        ProcessBioticTick();

        // 3. Evolutionary Engine
        evolutionManager.ProcessEvolutionTick();

        // 4. Cognition & Society
        civilizationManager.ProcessCivilizationTick();

        // 5. Inter-Civilization Systems
        diplomacyManager.ProcessDiplomacyTick();
        conflictManager.ProcessConflictTick();

        // 6. Great Leader Lifecycle
        leaderManager.ProcessLeaderLifecycle();

        // 7. AI Decision Making
        foreach (var ai in aiControllers)
        {
            ai.ProcessAIDecisions();
        }

        // 8. Update Visuals
        speciesVisualizer.UpdateVisuals(planetGraph);

        // 9. Update Music State
        UpdateMusicalState();
    }

    /// <summary>
    /// Finds valid starting tiles for each species and places an initial population.
    /// </summary>
    void SpawnInitialPopulations()
    {
        Debug.Log("Spawning initial populations...");
        List<PlanetTile> potentialSpawns = planetGraph.Values
            .Where(t => t.terrainType != TerrainType.Ocean && t.populations.Count == 0)
            .ToList();

        foreach (var species in activeSpeciesList)
        {
            var validTilesForSpecies = potentialSpawns
                .Where(t => species.validTerrainTypes.Contains(t.terrainType))
                .ToList();

            if (validTilesForSpecies.Count > 0)
            {
                PlanetTile spawnTile = validTilesForSpecies[Random.Range(0, validTilesForSpecies.Count)];
                spawnTile.populations[species] = 100;
                if (!activeTiles.Contains(spawnTile))
                {
                    activeTiles.Add(spawnTile);
                }
                potentialSpawns.Remove(spawnTile);
                Debug.Log($"{species.proceduralName} has spawned on tile {spawnTile.id} ({spawnTile.terrainType}).");
            }
            else
            {
                Debug.LogWarning($"Could not find a valid spawn location for {species.proceduralName}! They may not survive.");
            }
        }
    }

    private void ProcessBioticTick()
    {
        if (activeTiles.Count == 0) return;

        var tilesToProcess = new List<PlanetTile>(activeTiles);
        var popChanges = new Dictionary<PlanetTile, Dictionary<SpeciesData, long>>();

        // --- Pass 1: Calculate all deaths and growth based on the start-of-tick state ---
        foreach (var tile in tilesToProcess)
        {
            popChanges[tile] = new Dictionary<SpeciesData, long>();

            // Resource Regeneration
            if (tile.resources.ContainsKey("Flora"))
                tile.resources["Flora"] = Mathf.Min(tile.resources["Flora"] + 0.25f, 200f);

            if (tile.populations.Count == 0) continue;

            var speciesOnTile = tile.populations.Keys.ToList();

            // --- Sub-pass 1.1: Environmental Damage ---
            foreach (var species in speciesOnTile)
            {
                if (!popChanges[tile].ContainsKey(species)) popChanges[tile][species] = 0;
                long popLoss = CalculateEnvironmentalDamage(species, tile);
                popChanges[tile][species] -= popLoss;
                species.deathTolls["Environment"] += (int)popLoss;
            }

            // --- Sub-pass 1.2: Predation ---
            // Carnivores hunt, causing deaths for prey. This is calculated before prey consumption/growth.
            var carnivores = speciesOnTile.Where(s => s.diet == DietType.Carnivore).ToList();
            var prey = speciesOnTile.Where(s => s.diet != DietType.Carnivore).ToList();
            foreach (var carnivore in carnivores)
            {
                if (tile.populations[carnivore] <= 0) continue;
                ProcessPredation(carnivore, tile, prey, popChanges);
            }

            // --- Sub-pass 1.3: Consumption & Growth ---
            // All species now try to eat and reproduce based on remaining populations.
            foreach (var species in speciesOnTile)
            {
                if (!popChanges[tile].ContainsKey(species)) popChanges[tile][species] = 0;
                long growth = ProcessFeeding(species, tile);
                popChanges[tile][species] += growth;
            }
        }

        // --- Pass 2: Apply all calculated changes simultaneously ---
        foreach (var tileChanges in popChanges)
        {
            var tile = tileChanges.Key;
            foreach (var speciesChange in tileChanges.Value)
            {
                var species = speciesChange.Key;
                var change = speciesChange.Value;
                if (!tile.populations.ContainsKey(species)) tile.populations[species] = 0;
                tile.populations[species] = (long)Mathf.Max(0, tile.populations[species] + change);
            }
        }

        // --- Pass 3: Migration ---
        foreach(var species in activeSpeciesList)
        {
            if(species.migrationIntents.Count > 0)
            {
                foreach(var intent in species.migrationIntents)
                {
                    PlanetTile fromTile = intent.Key;
                    PlanetTile toTile = intent.Value;
                    long popToMove = fromTile.populations[species] / 10; // Move 10%

                    if(popToMove > 0)
                    {
                        fromTile.populations[species] -= popToMove;
                        if (!toTile.populations.ContainsKey(species)) toTile.populations[species] = 0;
                        toTile.populations[species] += popToMove;

                        if (!activeTiles.Contains(toTile)) activeTiles.Add(toTile);
                    }
                }
                species.migrationIntents.Clear();
            }
        }

        // --- Pass 4: Cleanup ---
        foreach(var tile in tilesToProcess)
        {
            var extinct = tile.populations.Where(kvp => kvp.Value <= 0).Select(kvp => kvp.Key).ToList();
            foreach(var s in extinct) tile.populations.Remove(s);

            if(tile.populations.Count == 0) activeTiles.Remove(tile);
        }
    }

    private long CalculateEnvironmentalDamage(SpeciesData species, PlanetTile tile)
    {
        long pop = tile.populations[species];
        long totalDamage = 0;

        // Temperature damage
        float tempDiff = Mathf.Abs(tile.currentTemperature - species.idealTemperature);
        if (tempDiff > species.GetCurrentTemperatureTolerance())
        {
            float severity = (tempDiff - species.GetCurrentTemperatureTolerance()) / 10f;
            totalDamage += (long)(pop * severity * 0.01f);
        }

        // Atmosphere damage
        float idealGasConc = species.idealGasConcentration;
        float currentGasConc = activePlanet.atmosphericComposition.ContainsKey(species.requiredGas) ? activePlanet.atmosphericComposition[species.requiredGas] : 0f;
        float gasDiff = Mathf.Abs(currentGasConc - idealGasConc);
        // TODO: Refactor gas tolerance to use the getter system
        if(gasDiff > species.baseGasConcentrationTolerance)
        {
            float severity = (gasDiff - species.baseGasConcentrationTolerance) / (species.baseGasConcentrationTolerance + 0.01f);
            totalDamage += (long)(pop * severity * 0.01f);
        }
        return totalDamage;
    }

    private void ProcessPredation(SpeciesData carnivore, PlanetTile tile, List<SpeciesData> preyList, Dictionary<PlanetTile, Dictionary<SpeciesData, long>> popChanges)
    {
        // Find the most populous valid prey on the tile
        SpeciesData targetPrey = preyList
            .Where(p => tile.populations.ContainsKey(p) && tile.populations[p] > 0)
            .OrderByDescending(p => tile.populations[p])
            .FirstOrDefault();

        if (targetPrey == null) return; // No prey available

        long carnivorePop = tile.populations[carnivore];
        long preyPop = tile.populations[targetPrey];

        // Carnivores hunt a percentage of the prey population
        long huntedAmount = (long)(preyPop * 0.1f);

        if (!popChanges[tile].ContainsKey(targetPrey)) popChanges[tile][targetPrey] = 0;
        popChanges[tile][targetPrey] -= huntedAmount;
        targetPrey.deathTolls["Predation"] += (int)huntedAmount;
    }

    private long ProcessFeeding(SpeciesData species, PlanetTile tile)
    {
        long pop = tile.populations[species];
        if (pop <= 0) return 0;

        float consumptionRate = species.GetCurrentFoodConsumptionRate();
        float foodNeeded = pop * consumptionRate;
        float growthRate = species.baseReproductionRate;

        // Carnivores get food from predation, which was already calculated.
        // We assume 1 prey pop = 1 food unit.
        // This is a simplification; a more complex model could use biomass.
        if (species.diet == DietType.Carnivore)
        {
            long preyDeaths = species.deathTolls["Predation"]; // This is a rough proxy for food intake
            if (preyDeaths >= foodNeeded)
            {
                return (long)(pop * growthRate);
            }
        }
        // Non-carnivores eat from tile resources
        else if (tile.resources.ContainsKey(species.primaryFoodSource) && tile.resources[species.primaryFoodSource] >= foodNeeded)
        {
            tile.resources[species.primaryFoodSource] -= foodNeeded;
            return (long)(pop * growthRate);
        }

        // Starvation for all diet types if food needs aren't met
        long popLoss = (long)(pop * 0.1f);
        species.deathTolls["Starvation"] += (int)popLoss;
        return -popLoss;
    }

    // --- PUBLIC GETTERS FOR MANAGERS ---
    public Dictionary<int, PlanetTile> GetPlanetGraph() => planetGraph;
    public long GetTotalPopulation(SpeciesData species)
    {
        long total = 0;
        foreach(var tile in planetGraph.Values)
        {
            if(tile.populations.ContainsKey(species))
            {
                total += tile.populations[species];
            }
        }
        return total;
    }

    public void UpdateAllTileTemperatures(float globalTemperature)
    {
        if (planetGraph == null) return;
        foreach (var tile in planetGraph.Values)
        {
            // The current temperature is the global baseline adjusted for the tile's own properties (e.g., elevation).
            float elevationEffect = (tile.position.magnitude - activePlanet.seaLevel) * 0.5f;
            tile.currentTemperature = globalTemperature - elevationEffect;
        }
    }

    public bool WasInCombat(SpeciesData species) => speciesInCombat.Contains(species);
    public void ReportCombat(SpeciesData s1, SpeciesData s2)
    {
        speciesInCombat.Add(s1);
        speciesInCombat.Add(s2);
    }

    public PlanetTile GetClosestTile(Vector3 worldPosition)
    {
        if (planetGraph == null || planetGraph.Count == 0) return null;

        return planetGraph.Values.OrderBy(tile => Vector3.Distance(tile.position, worldPosition)).First();
    }

    private void UpdateMusicalState()
    {
        if (activeSpeciesList == null || activeSpeciesList.Count == 0) return;

        // Determine state based on the player species (assumed to be the first one)
        var playerSpecies = activeSpeciesList[0];

        // Is the player at war with anyone?
        if (playerSpecies.atWarWith.Any())
        {
            musicManager.SetMusicalState(MusicalState.War);
            return;
        }

        // Is the player in a tense "cold war" state with anyone?
        bool isTense = activeSpeciesList.Any(other =>
            other != playerSpecies &&
            playerSpecies.dispositions.ContainsKey(other) &&
            playerSpecies.dispositions[other] < -25f // A threshold for tension, less severe than war
        );

        if (isTense)
        {
            musicManager.SetMusicalState(MusicalState.Tension);
            return;
        }

        // Otherwise, we are at peace.
        musicManager.SetMusicalState(MusicalState.Peace);
    }

    public void AddAtmosphericGas(string gasName, float amount)
    {
        if (activePlanet.atmosphericComposition == null) return;
        if (!activePlanet.atmosphericComposition.ContainsKey(gasName))
        {
            activePlanet.atmosphericComposition[gasName] = 0;
        }
        activePlanet.atmosphericComposition[gasName] += amount;
    }
}

public static class SpeciesGenerator
{
    /// <summary>
    /// Creates unique, procedurally modified instances of species for a new game.
    /// </summary>
    public static List<SpeciesData> GenerateSpeciesForMatch(PlayerSelections selections, List<ProceduralTrait> masterTraitList, PlanetData planet)
    {
        var generatedSpeciesList = new List<SpeciesData>();
        if (selections == null) return generatedSpeciesList;

        var chosenArchetypes = selections.GetAllSelectedArchetypes();

        foreach (var archetype in chosenArchetypes)
        {
            // Create an instance to avoid modifying the base ScriptableObject asset
            SpeciesData newSpecies = Object.Instantiate(archetype);
            newSpecies.name = archetype.name + "_Generated"; // Avoids "(Clone)" name

            // Determine planetary conditions to select appropriate traits
            var conditions = new List<ProceduralTrait.SpawnCondition>();
            if (planet.surfaceTemperature > 25f) conditions.Add(ProceduralTrait.SpawnCondition.HotPlanet);
            if (planet.surfaceTemperature < 5f) conditions.Add(ProceduralTrait.SpawnCondition.ColdPlanet);
            if (planet.tectonicActivityChance > 0.5f) conditions.Add(ProceduralTrait.SpawnCondition.VolatileGeology);
            // ... (add other condition checks like ScarceResources, HighGravity)

            // Find all traits that match the current planetary conditions
            var applicableTraits = masterTraitList.Where(t => conditions.Contains(t.condition)).ToList();

            // Apply 1-2 matching traits randomly
            int traitsToApply = Random.Range(1, 3);
            for (int i = 0; i < traitsToApply && applicableTraits.Count > 0; i++)
            {
                var trait = applicableTraits[Random.Range(0, applicableTraits.Count)];
                ApplyTrait(newSpecies, trait);
                applicableTraits.Remove(trait); // Ensure a trait is not applied more than once
            }

            generatedSpeciesList.Add(newSpecies);
        }
        return generatedSpeciesList;
    }

    /// <summary>
    /// Applies a single procedural trait to a species instance using reflection.
    /// </summary>
    private static void ApplyTrait(SpeciesData species, ProceduralTrait trait)
    {
        // Using reflection to find and modify the target field.
        // Note: Reflection can have performance implications, but is flexible.
        FieldInfo field = typeof(SpeciesData).GetField(trait.targetFieldName);
        if (field != null && field.FieldType == typeof(float))
        {
            float currentValue = (float)field.GetValue(species);
            if (trait.modifierType == ProceduralTrait.ModifierType.Additive)
            {
                field.SetValue(species, currentValue + trait.modifierValue);
            }
            else // Multiplicative
            {
                field.SetValue(species, currentValue * trait.modifierValue);
            }
            // Append the trait name to the procedurally generated name.
            species.proceduralName = $"{species.proceduralName} ({trait.traitName})";
        }
        else
        {
            Debug.LogWarning($"ProceduralTrait Error: Could not find public float field '{trait.targetFieldName}' on SpeciesData to apply trait '{trait.traitName}'.");
        }
    }
}

/// <summary>
/// Handles the procedural generation of the planet's tile graph from a 3D mesh.
/// </summary>
public static class PlanetMeshGenerator
{
    /// <summary>
    /// Generates a graph of PlanetTiles from a mesh, assigning terrain and resources.
    /// </summary>
    public static Dictionary<int, PlanetTile> GeneratePlanetGraph(MeshFilter meshFilter, PlanetData planetData)
    {
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("PlanetMeshGenerator: MeshFilter or sharedMesh is null! Cannot generate planet.");
            return new Dictionary<int, PlanetTile>();
        }

        Debug.Log("Starting planet generation...");

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        var planetGraph = new Dictionary<int, PlanetTile>(vertices.Length);

        // 1. Create a PlanetTile for each vertex in the mesh
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = meshFilter.transform.TransformPoint(vertices[i]);
            planetGraph[i] = new PlanetTile(i, worldPos);
        }

        // 2. Determine tile neighbors by iterating through the mesh's triangles
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            // Add each vertex to the others' neighbor lists, ensuring no duplicates
            if (!planetGraph[v1].neighbours.Contains(v2)) planetGraph[v1].neighbours.Add(v2);
            if (!planetGraph[v1].neighbours.Contains(v3)) planetGraph[v1].neighbours.Add(v3);

            if (!planetGraph[v2].neighbours.Contains(v1)) planetGraph[v2].neighbours.Add(v1);
            if (!planetGraph[v2].neighbours.Contains(v3)) planetGraph[v2].neighbours.Add(v3);

            if (!planetGraph[v3].neighbours.Contains(v1)) planetGraph[v3].neighbours.Add(v1);
            if (!planetGraph[v3].neighbours.Contains(v2)) planetGraph[v3].neighbours.Add(v2);
        }

        // 3. Assign terrain, temperature, and initial resources based on procedural noise
        foreach (var tile in planetGraph.Values)
        {
            // Use the tile's world position (normalized to a unit sphere) to sample 3D noise
            Vector3 p = tile.position.normalized;
            float noiseValue = CalculateNoise(p, planetData.noiseScale); // Returns a value in [-1, 1]

            // Apply noise strength to the base planet radius to get elevation
            float elevation = planetData.planetRadius + noiseValue * planetData.noiseStrength;

            // Determine TerrainType based on elevation relative to sea level
            if (elevation < planetData.seaLevel)
            {
                tile.terrainType = TerrainType.Ocean;
            }
            else if (elevation > planetData.seaLevel + (planetData.noiseStrength * 0.7f)) // Mountains are the top 30% of land
            {
                tile.terrainType = TerrainType.Mountain;
            }
            else
            {
                tile.terrainType = TerrainType.Land;
            }

            // Set initial temperature and resources
            tile.baseTemperature = planetData.surfaceTemperature - (elevation - planetData.seaLevel) * 0.5f; // Cooler at high elevations
            tile.currentTemperature = tile.baseTemperature;

            if (tile.terrainType == TerrainType.Land)
            {
                tile.resources["Flora"] = Random.Range(80f, 120f);
            }
            else if (tile.terrainType == TerrainType.Mountain)
            {
                tile.resources["Silicon"] = Random.Range(40f, 60f);
            }
             else if (tile.terrainType == TerrainType.Ocean)
            {
                tile.resources["Marine Life"] = Random.Range(50f, 150f);
            }
        }

        Debug.Log($"Planet generation complete. Created {planetGraph.Count} tiles.");
        return planetGraph;
    }

    /// <summary>
    /// Calculates a pseudo-3D noise value by sampling 2D Perlin noise on 3 axes and averaging.
    /// This avoids the polar distortion of sampling a sphere with a 2D texture.
    /// </summary>
    private static float CalculateNoise(Vector3 point, float scale)
    {
        // Remap Perlin noise from [0, 1] to [-1, 1]
        float xy = Mathf.PerlinNoise(point.x * scale, point.y * scale) * 2f - 1f;
        float yz = Mathf.PerlinNoise(point.y * scale, point.z * scale) * 2f - 1f;
        float xz = Mathf.PerlinNoise(point.x * scale, point.z * scale) * 2f - 1f;

        float yx = Mathf.PerlinNoise(point.y * scale, point.x * scale) * 2f - 1f;
        float zy = Mathf.PerlinNoise(point.z * scale, point.y * scale) * 2f - 1f;
        float zx = Mathf.PerlinNoise(point.z * scale, point.x * scale) * 2f - 1f;

        // Average the 6 samples to get a more uniform noise value
        return (xy + yz + xz + yx + zy + zx) / 6.0f;
    }
}
