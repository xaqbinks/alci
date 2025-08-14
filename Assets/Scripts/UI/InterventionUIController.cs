using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// Manages the UI for the Intervention Phase, where the player makes their
/// single "Minimal Change".
/// </summary>
public class InterventionUIController : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("A list of all possible interventions the player can choose from.")]
    public List<MinimalChangePreset> availableChanges;

    [Header("UI References")]
    [Tooltip("The parent transform for the category buttons.")]
    public Transform categoryParent;
    [Tooltip("The parent transform for the change option buttons within a category.")]
    public Transform changeParent;
    [Tooltip("The button prefab for UI elements.")]
    public Button buttonPrefab;
    [Tooltip("The button to commit the selected change.")]
    public Button commitButton;
    [Tooltip("Text to display the description of the selected change.")]
    public TextMeshProUGUI descriptionText;

    private MinimalChangePreset selectedChange;
    private SimulationManager simManager;

    void Start()
    {
        // This controller would be in the MainSimulationScene, active only during the Intervention state.
        simManager = FindObjectOfType<SimulationManager>();

        // Initially hide this UI until the correct game state.
        gameObject.SetActive(GameManager.instance.currentState == GameState.Intervention);

        PopulateCategories();
        commitButton.onClick.AddListener(CommitChange);
        commitButton.interactable = false;
    }

    private void PopulateCategories()
    {
        var categories = availableChanges.Select(c => c.category).Distinct();
        foreach (var category in categories)
        {
            Button catButton = Instantiate(buttonPrefab, categoryParent);
            catButton.GetComponentInChildren<TextMeshProUGUI>().text = category.ToString();
            catButton.onClick.AddListener(() => PopulateChangesForCategory(category));
        }
    }

    private void PopulateChangesForCategory(ChangeCategory category)
    {
        // Clear previous change buttons
        foreach (Transform child in changeParent)
        {
            Destroy(child.gameObject);
        }

        var changesInCategory = availableChanges.Where(c => c.category == category);
        foreach (var change in changesInCategory)
        {
            Button changeButton = Instantiate(buttonPrefab, changeParent);
            changeButton.GetComponentInChildren<TextMeshProUGUI>().text = change.changeName;
            changeButton.onClick.AddListener(() => SelectChange(change));
        }
    }

    private void SelectChange(MinimalChangePreset change)
    {
        selectedChange = change;
        descriptionText.text = selectedChange.description;
        commitButton.interactable = true;
    }

    private void CommitChange()
    {
        if (selectedChange == null || simManager == null)
        {
            Debug.LogError("Cannot commit change: No change selected or SimulationManager not found.");
            return;
        }

        // Apply the change to the player's species (assuming species[0] is the player)
        SpeciesData playerSpecies = simManager.GetPlayerSpecies();
        selectedChange.Apply(simManager, playerSpecies);

        Debug.Log($"Change '{selectedChange.changeName}' committed. Starting simulation.");

        // Deactivate this UI and tell the GameManager to start the simulation phase.
        gameObject.SetActive(false);
        GameManager.instance.BeginSimulation();
    }
}
