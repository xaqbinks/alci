using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Manages the main menu UI, allowing the player to select game parameters
/// and launch the simulation.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Data Sources")]
    [Tooltip("A list of all possible planets the player can choose from.")]
    public List<PlanetData> availablePlanets;
    [Tooltip("A list of all possible species archetypes the player can choose from.")]
    public List<SpeciesData> availableSpecies;

    [Header("UI Prefabs & Parents")]
    [Tooltip("The prefab for a generic selection button.")]
    public Button selectionButtonPrefab;
    [Tooltip("The parent transform for planet selection buttons.")]
    public Transform planetSelectionParent;
    [Tooltip("The parent transform for player species selection buttons.")]
    public Transform playerSpeciesSelectionParent;
    [Tooltip("The parent transform for opponent species selection toggles.")]
    public Transform opponentSpeciesSelectionParent;

    [Header("UI References")]
    [Tooltip("The button to launch the simulation.")]
    public Button launchButton;

    [Header("Visual Feedback")]
    public Color defaultColor = Color.white;
    public Color selectedColor = Color.cyan;

    private GameSetupManager setupManager;
    private Button selectedPlanetButton;
    private Button selectedPlayerSpeciesButton;
    private Dictionary<SpeciesData, Button> opponentButtons = new Dictionary<SpeciesData, Button>();

    void Start()
    {
        // Find the persistent GameSetupManager instance.
        setupManager = GameSetupManager.instance;
        if (setupManager == null)
        {
            Debug.LogError("GameSetupManager instance not found! Make sure it exists in the startup scene.");
            return;
        }

        PopulatePlanetSelections();
        PopulatePlayerSpeciesSelections();
        PopulateOpponentSpeciesSelections();

        launchButton.onClick.AddListener(LaunchSimulation);
    }

    private void PopulatePlanetSelections()
    {
        foreach (var planet in availablePlanets)
        {
            Button button = Instantiate(selectionButtonPrefab, planetSelectionParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = planet.name;
            button.onClick.AddListener(() => SelectPlanet(planet, button));
        }
    }

    private void PopulatePlayerSpeciesSelections()
    {
        foreach (var species in availableSpecies)
        {
            Button button = Instantiate(selectionButtonPrefab, playerSpeciesSelectionParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = species.speciesName;
            button.onClick.AddListener(() => SelectPlayerSpecies(species, button));
        }
    }

    private void PopulateOpponentSpeciesSelections()
    {
        foreach (var species in availableSpecies)
        {
            Button button = Instantiate(selectionButtonPrefab, opponentSpeciesSelectionParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = species.speciesName;
            button.onClick.AddListener(() => ToggleOpponentSpecies(species, button));
            opponentButtons[species] = button;
        }
    }

    public void SelectPlanet(PlanetData planet, Button clickedButton)
    {
        if (selectedPlanetButton != null)
        {
            selectedPlanetButton.GetComponent<Image>().color = defaultColor;
        }
        selectedPlanetButton = clickedButton;
        selectedPlanetButton.GetComponent<Image>().color = selectedColor;

        setupManager.selectedPlanet = planet;
        Debug.Log($"Planet selected: {planet.name}");
    }

    public void SelectPlayerSpecies(SpeciesData species, Button clickedButton)
    {
        if (selectedPlayerSpeciesButton != null)
        {
            selectedPlayerSpeciesButton.GetComponent<Image>().color = defaultColor;
        }
        selectedPlayerSpeciesButton = clickedButton;
        selectedPlayerSpeciesButton.GetComponent<Image>().color = selectedColor;

        setupManager.playerSpecies = species;
        Debug.Log($"Player species selected: {species.speciesName}");
    }

    public void ToggleOpponentSpecies(SpeciesData species, Button clickedButton)
    {
        if (setupManager.opponentSpecies.Contains(species))
        {
            setupManager.opponentSpecies.Remove(species);
            clickedButton.GetComponent<Image>().color = defaultColor;
            Debug.Log($"Opponent deselected: {species.speciesName}");
        }
        else
        {
            setupManager.opponentSpecies.Add(species);
            clickedButton.GetComponent<Image>().color = selectedColor;
            Debug.Log($"Opponent selected: {species.speciesName}");
        }
    }

    public void SetSandboxMode(bool isSandbox)
    {
        setupManager.isSandboxMode = isSandbox;
        Debug.Log($"Sandbox mode set to: {isSandbox}");
    }

    public void LaunchSimulation()
    {
        if (setupManager.selectedPlanet == null || setupManager.playerSpecies == null)
        {
            Debug.LogError("Cannot launch simulation: A planet and player species must be selected!");
            return;
        }

        // Tell the GameManager to start the next phase, which will handle loading the scene.
        GameManager.instance.StartInterventionPhase();
    }
}
