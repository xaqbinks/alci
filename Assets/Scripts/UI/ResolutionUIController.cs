using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages the end-game resolution screen, displaying the outcome of the match.
/// </summary>
public class ResolutionUIController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The text element to display the final game outcome message.")]
    public TextMeshProUGUI outcomeText;
    [Tooltip("The button to return to the main menu.")]
    public Button returnToMenuButton;

    void Start()
    {
        returnToMenuButton.onClick.AddListener(ReturnToMenu);
        // Start with the screen disabled.
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Activates the resolution screen and displays the final outcome.
    /// </summary>
    /// <param name="message">The reason the game ended.</param>
    public void ShowResolutionScreen(string message)
    {
        outcomeText.text = message;
        gameObject.SetActive(true);
    }

    private void ReturnToMenu()
    {
        // This might require destroying the persistent GameManager and GameSetupManager
        // to ensure a clean start, but for now, a simple scene load is fine.
        if (GameManager.instance != null) Destroy(GameManager.instance.gameObject);
        if (GameSetupManager.instance != null) Destroy(GameSetupManager.instance.gameObject);

        SceneManager.LoadScene("MainMenu");
    }
}
