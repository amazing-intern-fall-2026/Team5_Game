using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [Header("UI References")]
    public GameObject pauseMenuUI;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";

    void Update()
    {
        // Toggle pause menu with the Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    // Connect this to your "Resume" button's OnClick event
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // Unfreeze time
        GameIsPaused = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; // Freeze time
        GameIsPaused = true;
    }

    // Connect this to your "Main Menu" button's OnClick event
    public void LoadMainMenu()
    {
        // Ensure time is unfrozen before leaving the scene
        Time.timeScale = 1f; 
        GameIsPaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Connect this to your "Quit game" button's OnClick event
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}