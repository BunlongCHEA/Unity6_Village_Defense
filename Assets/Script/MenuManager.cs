using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Main Menu UI References")]
    public Button playButton;
    public Button loadButton;
    public Button quitButton;

    [Header("In-Game Pause Menu")]
    public GameObject pauseMenuPanel;
    public Button resumeButton;
    public Button pauseSaveButton;
    public Button exitToMenuButton;
    public TextMeshProUGUI pauseMenuTitle;

    [Header("Death/Restart UI")]
    public GameObject restartCanvas;
    public Button restartButton;
    public Button exitToMenuOnDeathButton;

    [Header("Save Settings")]
    public string defaultSaveFileName = "village_defense";

    [Header("Settings")]
    public string worldSceneName = "WorldScene";
    public string mainMenuSceneName = "MenuScene";

    [Header("Fade Settings")]
    public float sceneTransitionDelay = 0.5f;

    // Constants
    public string CURRENT_USER = "BunlongCHEA";
    private const string SAVE_FOLDER = "GameSaves";

    // State Management
    private bool isInMainMenu = true;
    private bool isPaused = false;
    private string lastSaveFileName = "";

    // Save directory path
    private string SaveDirectory => Path.Combine(Application.persistentDataPath, SAVE_FOLDER);

    void Start()
    {
        InitializeMenu();
        SetupButtons();

        // Determine if we're in main menu or world scene
        string currentScene = SceneManager.GetActiveScene().name;
        isInMainMenu = (currentScene != worldSceneName);

        SetupMenuForCurrentScene();
    }

    // Public method for PlayerMovement to check if we're in main menu
    public bool IsInMainMenu()
    {
        return isInMainMenu;
    }

    // Public method called by PlayerMovement when ESC is pressed
    public void TogglePauseMenuFromPlayer()
    {
        if (isInMainMenu)
            return;

        TogglePauseMenu();
    }

    // Initialization
    private void InitializeMenu()
    {
        // Create save directory if it doesn't exist
        if (!Directory.Exists(SaveDirectory))
            Directory.CreateDirectory(SaveDirectory);
    }

    private void SetupMenuForCurrentScene()
    {
        if (isInMainMenu)
        {
            // Main Menu Setup
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            if (restartCanvas != null)
                restartCanvas.SetActive(false);

            UpdateLoadButtonState();
        }
        else
        {
            // World Scene Setup
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            if (restartCanvas != null)
                restartCanvas.SetActive(false);

            // Hide main menu buttons if they exist
            if (playButton != null) playButton.gameObject.SetActive(false);
            if (loadButton != null) loadButton.gameObject.SetActive(false);
            if (quitButton != null) quitButton.gameObject.SetActive(false);
        }
    }

    private void SetupButtons()
    {
        // Main Menu Buttons
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(OnLoadClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        // Pause Menu Buttons
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeClicked);
        }

        if (pauseSaveButton != null)
        {
            pauseSaveButton.onClick.RemoveAllListeners();
            pauseSaveButton.onClick.AddListener(OnPauseSaveClicked);
        }

        if (exitToMenuButton != null)
        {
            exitToMenuButton.onClick.RemoveAllListeners();
            exitToMenuButton.onClick.AddListener(OnExitToMenuClicked);
        }

        // Restart UI Buttons
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (exitToMenuOnDeathButton != null)
        {
            exitToMenuOnDeathButton.onClick.RemoveAllListeners();
            exitToMenuOnDeathButton.onClick.AddListener(OnExitToMenuClicked);
        }
    }

    // Pause Menu System
    private void TogglePauseMenu()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    private void PauseGame()
    {
        if (isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f; // Pause the game

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        // Disable player controls
        DisablePlayerControls();
    }

    private void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f; // Resume the game

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        // Re-enable player controls
        EnablePlayerControls();
    }

    private void DisablePlayerControls()
    {
        // Disable inventory
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnPlayerDeath(); // This closes inventory

        // Disable player movement
        PlayerMovement playerMovement = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).FirstOrDefault();
        if (playerMovement != null)
            playerMovement.SetInventoryOpen(true); // This disables movement
    }

    private void EnablePlayerControls()
    {
        // Re-enable player movement
        PlayerMovement playerMovement = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).FirstOrDefault();
        if (playerMovement != null)
            playerMovement.SetInventoryOpen(false); // This enables movement
    }

    // Pause Menu Button Events
    public void OnResumeClicked()
    {
        ResumeGame();
    }

    public void OnPauseSaveClicked()
    {
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeToBlack(() => {
                string fileName = GetSaveFileName();
                SaveGame(fileName);
                lastSaveFileName = fileName;

                FadeManager.Instance.FadeFromBlack();
            });
        }
        else
        {
            string fileName = GetSaveFileName();
            SaveGame(fileName);
            lastSaveFileName = fileName;
        }
    }

    public void OnExitToMenuClicked()
    {
        StartCoroutine(ExitToMainMenu());

        //if (FadeManager.Instance != null)
        //{
            //FadeManager.Instance.FadeTransition(() => {
                //StartCoroutine(ExitToMainMenu());
            //}, sceneTransitionDelay);
        //}
        //else
        //{
            //StartCoroutine(ExitToMainMenu());
        //}
    }

    private IEnumerator ExitToMainMenu()
    {
        // Resume time before changing scenes
        Time.timeScale = 1f;
        isPaused = false;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainMenuSceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    // Called by PlayerMovement when death animation ends
    public void ShowRestartUI()
    {
        if (restartCanvas != null)
        {
            restartCanvas.SetActive(true);
            Time.timeScale = 0f; // Pause the game when showing restart UI
            Debug.Log("[MENU] Showing restart UI after player death");
        }
        else
        {
            Debug.LogError("[MENU] Restart Canvas not assigned in MenuManager!");
        }
    }

    // Called by restart button
    public void OnRestartClicked()
    {
        // Resume time scale first
        Time.timeScale = 1f;

        // Hide restart UI
        if (restartCanvas != null)
            restartCanvas.SetActive(false);

        // Find and reset the player
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.ResetAfterRestart();
                Debug.Log("[MENU] Player reset after restart button clicked");
            }
            else
            {
                Debug.LogError("[MENU] PlayerMovement component not found on Player!");
            }
        }
        else
        {
            Debug.LogError("[MENU] Player object not found!");
        }
    }

    // Main Menu Button Events (Updated with Fade)
    public void OnPlayClicked()
    {
        if (FadeManager.Instance != null && !FadeManager.Instance.IsFading())
        {
            SetButtonsInteractable(false);

            FadeManager.Instance.FadeTransition(() => {
                StartCoroutine(LoadWorldSceneWithFade());
            }, sceneTransitionDelay);
        }
        else
        {
            SetButtonsInteractable(false);
            StartCoroutine(LoadWorldSceneWithFade());
        }
    }

    public void OnLoadClicked()
    {
        bool hasSaveFiles = HasAnySaveFiles();
        Debug.Log("[MENU] hasSaveFiles: " + hasSaveFiles);

        if (hasSaveFiles)
        {
            SetButtonsInteractable(false);
            Debug.Log("[MENU] Buttons disabled, preparing to load game...");

            if (FadeManager.Instance != null && !FadeManager.Instance.IsFading())
            {
                Debug.Log("[MENU] Using FadeManager transition");
                FadeManager.Instance.FadeTransition(() => {
                    Debug.Log("[MENU] Fade transition complete, starting LoadGameWithFade coroutine");
                    StartCoroutine(LoadGameWithFade());
                }, sceneTransitionDelay);
            }
            else
            {
                Debug.Log("[MENU] No FadeManager or already fading, directly loading game");
                StartCoroutine(LoadGameWithFade());
            }
        }
        else
        {
            Debug.LogWarning("[MENU] No save files found!");
        }
    }

    public void OnSaveClicked()
    {
        if (IsInWorldScene())
        {
            if (FadeManager.Instance != null)
            {
                FadeManager.Instance.FadeToBlack(() => {
                    string fileName = GetSaveFileName();
                    SaveGame(fileName);

                    FadeManager.Instance.FadeFromBlack();
                });
            }
            else
            {
                string fileName = GetSaveFileName();
                SaveGame(fileName);
            }
        }
    }

    public void OnQuitClicked()
    {
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeToBlack(() => {
                QuitGame();
            });
        }
        else
        {
            QuitGame();
        }
    }

    private void UpdateLoadButtonState()
    {
        if (loadButton != null)
        {
            bool hasSaves = HasAnySaveFiles();
            loadButton.interactable = hasSaves;
        }
    }

    // Fade-Enhanced Scene Loading
    private IEnumerator LoadWorldSceneWithFade()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(worldSceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator LoadGameWithFade()
    {
        Debug.Log("[MENU] LoadGameWithFade started");

        string latestSaveFile = GetLatestSaveFile();

        // Debug the path
        Debug.Log($"[MENU] Looking for save file at path: {SaveDirectory}");
        Debug.Log($"[MENU] Latest save file found: {latestSaveFile}");

        if (string.IsNullOrEmpty(latestSaveFile))
        {
            Debug.LogError("[MENU] No save file found!");
            SetButtonsInteractable(true);
            yield break;
        }

        InventoryData saveData = null;
        string fileName = string.Empty;
        bool loadSuccess = false;

        try
        {
            Debug.Log("[MENU] Reading save file...");
            string jsonData = File.ReadAllText(latestSaveFile);
            Debug.Log($"[MENU] JSON data length: {jsonData.Length}");

            Debug.Log("[MENU] Parsing JSON to InventoryData...");
            saveData = InventoryData.FromJsonString(jsonData);
            if (saveData == null)
            {
                Debug.LogError("[MENU] Failed to parse save data - result is null");
                SetButtonsInteractable(true);
                yield break;
            }

            Debug.Log($"[MENU] Save data parsed successfully. Items: {(saveData.inventoryItems?.Count ?? 0)}");
            fileName = Path.GetFileName(latestSaveFile);
            loadSuccess = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[MENU] Error loading save file: " + e.Message + "\n" + e.StackTrace);
            SetButtonsInteractable(true);
            yield break;
        }

        if (!loadSuccess || saveData == null)
        {
            Debug.LogError("[MENU] Load unsuccessful or saveData is null");
            SetButtonsInteractable(true);
            yield break;
        }

        Debug.Log("[MENU] Loading world scene...");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(worldSceneName);
        asyncLoad.allowSceneActivation = true; // Ensure scene activates immediately when ready

        //yield return new WaitForSeconds(10.0f); // Increased wait time significantly

        Debug.Log("[MENU] Start Apply ApplyGameDataWithDelay(saveData)");
        // Apply data
        yield return StartCoroutine(ApplyGameDataWithDelay(saveData));
    }

    // Apply saved game data with proper delays and initialization
    private IEnumerator ApplyGameDataWithDelay(InventoryData saveData)
    {
        Debug.Log("[MENU] === ApplyGameDataWithDelay: START ===");
        if (saveData == null)
        {
            Debug.LogError("[MENU] Save data is null, aborting.");
            yield break;
        }

        Debug.Log("[MENU] InventoryManager.Instance is: " + InventoryManager.Instance);
        if (InventoryManager.Instance == null)
            Debug.Log("[MENU] InventoryManager.Instance is NULL");
        else
        {
            Debug.Log("[MENU] InventoryManager.Instance name: " + InventoryManager.Instance.gameObject.name);
            Debug.Log("[MENU] InventoryManager.Instance isActiveAndEnabled: " + InventoryManager.Instance.isActiveAndEnabled);
            Debug.Log("[MENU] InventoryManager.Instance destroyed? " + (InventoryManager.Instance == null));
        }

        try
        {
            Debug.Log("[MENU] About to call LoadInventoryData...");
            InventoryManager.Instance.LoadInventoryData(saveData);
            ApplyPlayerData(saveData);
            Debug.Log("[MENU] Inventory data loaded successfully.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[MENU] Exception in LoadInventoryData: " + ex);
        }

        Debug.Log("[MENU] === ApplyGameDataWithDelay: END ===");
        yield return null;
    }

    // Helper method for applying player data
    private void ApplyPlayerData(InventoryData saveData)
    {
        try
        {
            Debug.Log("[MENU] Finding player object...");
            GameObject player = GameObject.FindWithTag("Player");

            if (player != null)
            {
                Debug.Log("[MENU] Player found, setting position and health");
                //player.transform.position = saveData.playerPosition;
                PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
                if (playerMovement != null)
                {
                    playerMovement.currentHealth = saveData.playerHealth;
                    Debug.Log($"[MENU] Player health set to {saveData.playerHealth}");
                    if (playerMovement.healthBarSlider != null)
                    {
                        playerMovement.healthBarSlider.maxValue = playerMovement.maxHealth;
                        playerMovement.healthBarSlider.value = playerMovement.currentHealth;
                    }
                }
                else
                {
                    Debug.LogWarning("[MENU] PlayerMovement component not found!");
                }

                playerMovement.name = saveData.playerName;
            }
            else
            {
                Debug.LogError("[MENU] Failed to find Player object!");

                // Try alternative search methods
                player = GameObject.Find("Player");
                if (player != null)
                {
                    Debug.Log("[MENU] Found player using GameObject.Find instead of tag");
                    player.transform.position = saveData.playerPosition;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[MENU] Error applying player data: " + e.Message);
        }
    }

    // Scene Management
    private bool IsInWorldScene()
    {
        return SceneManager.GetActiveScene().name == worldSceneName;
    }

    // Save System
    private void SaveGame(string fileName)
    {
        try
        {
            // Get the complete inventory data (now includes player info)
            InventoryData saveData = null;

            if (InventoryManager.Instance != null)
            {
                saveData = InventoryManager.Instance.GetInventoryData();
            }
            else
            {
                saveData = new InventoryData();
            }

            // Update filename
            saveData.fileName = fileName;

            // Date and user info
            saveData.saveDateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            saveData.playerName = CURRENT_USER;

            // Get player position and health if not already set
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                saveData.playerPosition = player.transform.position;

                PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
                if (playerMovement != null)
                {
                    saveData.playerHealth = playerMovement.currentHealth;
                }
            }

            string dateStamp = GetDateStamp();
            string fullFileName = $"{fileName}_{dateStamp}.json";
            string filePath = Path.Combine(SaveDirectory, fullFileName);
            Debug.Log("[MENU] Save File at: " + filePath);

            // If this is an overwrite save, use the same filename
            if (!string.IsNullOrEmpty(lastSaveFileName))
            {
                string[] existingFiles = Directory.GetFiles(SaveDirectory, $"{lastSaveFileName}_*.json");
                if (existingFiles.Length > 0)
                {
                    filePath = existingFiles[0]; // Overwrite the most recent file with this name
                    fullFileName = Path.GetFileName(filePath);
                }
            }

            string jsonData = saveData.ToJsonString();
            File.WriteAllText(filePath, jsonData);

            UpdateLoadButtonState();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error saving game: " + e.Message);
        }
    }

    // File Management
    private string GetSaveFileName()
    {
        string fileName = defaultSaveFileName;

        if (string.IsNullOrEmpty(fileName))
        {
            fileName = defaultSaveFileName;
        }

        return fileName;
    }

    private string GetDateStamp()
    {
        return System.DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
    }

    private bool HasAnySaveFiles()
    {
        if (!Directory.Exists(SaveDirectory))
        {
            return false;
        }

        string[] saveFiles = Directory.GetFiles(SaveDirectory, "*.json");
        return saveFiles.Length > 0;
    }

    private string GetLatestSaveFile()
    {
        if (!Directory.Exists(SaveDirectory))
        {
            return null;
        }

        string[] saveFiles = Directory.GetFiles(SaveDirectory, "*.json");

        if (saveFiles.Length == 0)
        {
            return null;
        }

        string latestFile = saveFiles[0];
        System.DateTime latestTime = File.GetLastWriteTime(latestFile);

        foreach (string file in saveFiles)
        {
            System.DateTime fileTime = File.GetLastWriteTime(file);
            if (fileTime > latestTime)
            {
                latestTime = fileTime;
                latestFile = file;
            }
        }

        return latestFile;
    }

    // UI Control Methods
    private void SetButtonsInteractable(bool interactable)
    {
        if (playButton != null) playButton.interactable = interactable;
        if (loadButton != null) loadButton.interactable = interactable;
        if (quitButton != null) quitButton.interactable = interactable;
    }

    // Utility Methods
    private string GetCurrentDateTime()
    {
        return "2025-06-29 17:13:18";
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}