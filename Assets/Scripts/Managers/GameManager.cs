using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Defines the high-level state of the game session.
/// </summary>
public enum GameState { MainMenu, Setup, Intervention, Simulating, Paused, Resolution }

/// <summary>
/// The master controller for the entire game. This is a persistent singleton
/// that manages the overall game state and the flow between different phases.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public GameState currentState { get; private set; }

    // A reference to the resolution screen UI controller.
    // This will be null until the simulation scene is loaded.
    public ResolutionUIController resolutionUIController;

    private void Awake()
    {
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

    private void Start()
    {
        // The game starts in the main menu.
        currentState = GameState.MainMenu;
    }

    /// <summary>
    /// Called by the MainMenuController to transition into the game setup.
    /// </summary>
    public void StartGameSetup()
    {
        // This could eventually load a "Galaxy View" scene.
        // For now, it just changes the state.
        ChangeState(GameState.Setup);
    }

    /// <summary>
    /// Called after players have made their selections.
    /// </summary>
    public void StartInterventionPhase()
    {
        // This would load the main simulation scene.
        SceneManager.LoadScene("MainSimulationScene");
        ChangeState(GameState.Intervention);
    }

    /// <summary>
    /// Called by the Intervention UI once a player has made their "Minimal Change".
    /// </summary>
    public void BeginSimulation()
    {
        if (currentState != GameState.Intervention)
        {
            Debug.LogWarning($"GameManager: BeginSimulation called from unexpected state: {currentState}");
        }
        ChangeState(GameState.Simulating);
    }

    /// <summary>
    /// Toggles the simulation between running and paused states.
    /// </summary>
    public void TogglePause()
    {
        if (currentState == GameState.Simulating)
        {
            ChangeState(GameState.Paused);
        }
        else if (currentState == GameState.Paused)
        {
            ChangeState(GameState.Simulating);
        }
    }

    /// <summary>
    /// Called when a win/loss condition is met.
    /// </summary>
    public void EndGame(string reason)
    {
        if (currentState == GameState.Resolution) return; // Prevent multiple game-end calls

        Debug.Log($"Game Over: {reason}");
        ChangeState(GameState.Resolution);

        if (resolutionUIController != null)
        {
            resolutionUIController.ShowResolutionScreen(reason);
        }
        else
        {
            // This is a common issue if the resolution UI is not in the active scene.
            Debug.LogError("ResolutionUIController not found! Cannot display final outcome screen.");
        }
    }

    private void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"GameManager: State changed to {currentState}");

        // This is where you could fire off events that other managers listen to.
        // e.g., OnGameStateChanged?.Invoke(currentState);
    }
}
