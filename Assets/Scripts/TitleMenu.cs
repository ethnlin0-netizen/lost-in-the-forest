using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One persistent title/pause menu. Run progress exists only in memory.</summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class TitleMenu : MonoBehaviour
{
    public const string TitleSceneName = "TitleScreen";
    public const string CreditsText =
        "Caleb Kimondo - Gameplay Programmer\n\n" +
        "Ryan Rothmund - Artist/UI Programmer\n\n" +
        "Matthew Mirfield - AI Programmer\n\n" +
        "Ethan Lin - Level Designer";

    public const string ControlsText =
        "Left Arrow - Move Left\n" +
        "Right Arrow - Move Right\n" +
        "Up Arrow - Aim Above\n" +
        "Down Arrow - Aim Below\n" +
        "Z - Jump\n" +
        "Double tap Z - Double Jump\n" +
        "X: tail sweep\n" +
        "F - Ranged Attack";

    [SerializeField] private string firstBiomeScene = "PrismbloomLevel01";
    [SerializeField] private GameObject menuView;
    [SerializeField] private GameObject mainButtons;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button creditsBackButton;
    [SerializeField] private Button controlsBackButton;

    private static TitleMenu instance;
    private readonly List<Behaviour> pausedInput = new List<Behaviour>();
    private bool loading;
    private float previousTimeScale = 1f;
    private bool previousAudioPause;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    public bool HasStartedRun { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsLoading => loading;
    public bool IsMenuVisible => menuView != null && menuView.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        instance = null;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        // The Canvas and EventSystem survive level loads as children of this root.
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        menuView.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        BackToMenu();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame || loading) return;

        if (IsMenuVisible && (creditsPanel.activeSelf || controlsPanel.activeSelf))
            BackToMenu();
        else if (HasStartedRun && !IsMenuVisible)
            PauseGame();
    }

    public void StartGame()
    {
        if (loading) return;
        if (IsPaused)
        {
            ResumeGame();
            return;
        }
        if (!HasStartedRun) StartCoroutine(StartFreshRun());
    }

    public bool ReturnToMainMenu()
    {
        if (loading) return false;
        if (!Application.CanStreamedLevelBeLoaded(TitleSceneName))
        {
            Debug.LogError("TitleScreen is missing from the build scene list.", this);
            return false;
        }
        StartCoroutine(ReturnToTitleScreen());
        return true;
    }

    private IEnumerator ReturnToTitleScreen()
    {
        RestorePausedState();
        loading = true;
        HasStartedRun = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        menuView.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        BackToMenu();
        playButton.interactable = false;

        var operation = SceneManager.LoadSceneAsync(TitleSceneName, LoadSceneMode.Single);
        if (operation != null)
            while (!operation.isDone) yield return null;

        loading = false;
        playButton.interactable = true;
        BackToMenu();
    }

    private IEnumerator StartFreshRun()
    {
        if (!Application.CanStreamedLevelBeLoaded(firstBiomeScene))
        {
            Debug.LogError($"First biome '{firstBiomeScene}' is missing from the build scene list.", this);
            yield break;
        }

        loading = true;
        playButton.interactable = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        var operation = SceneManager.LoadSceneAsync(firstBiomeScene, LoadSceneMode.Single);
        if (operation == null)
        {
            loading = false;
            playButton.interactable = true;
            yield break;
        }
        while (!operation.isDone) yield return null;

        HasStartedRun = true;
        loading = false;
        playButton.interactable = true;
        HideMenu();
    }

    public void PauseGame()
    {
        if (!HasStartedRun || IsPaused || loading) return;
        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;

        // Stop input as well as physics, preventing attacks and buffered jumps
        // while the player navigates the menu with the arrow keys.
        pausedInput.Clear();
        foreach (var movement in FindObjectsByType<Movement>())
            SuspendInput(movement);
        foreach (var combat in FindObjectsByType<PlayerCombat>())
            SuspendInput(combat);

        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        menuView.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        BackToMenu();
    }

    private void SuspendInput(Behaviour input)
    {
        if (!input.enabled) return;
        pausedInput.Add(input);
        input.enabled = false;
    }

    private void ResumeGame()
    {
        RestorePausedState();
        HideMenu();
    }

    private void RestorePausedState()
    {
        if (!IsPaused) return;
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        foreach (var input in pausedInput)
            if (input != null) input.enabled = true;
        pausedInput.Clear();
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        IsPaused = false;
    }

    public void ShowCredits()
    {
        if (!IsMenuVisible || loading) return;
        mainButtons.SetActive(false);
        controlsPanel.SetActive(false);
        creditsPanel.SetActive(true);
        Select(creditsBackButton);
    }

    public void ShowControls()
    {
        if (!IsMenuVisible || loading) return;
        mainButtons.SetActive(false);
        creditsPanel.SetActive(false);
        controlsPanel.SetActive(true);
        Select(controlsBackButton);
    }

    public void BackToMenu()
    {
        creditsPanel.SetActive(false);
        controlsPanel.SetActive(false);
        mainButtons.SetActive(true);
        Select(playButton);
    }

    private void HideMenu()
    {
        BackToMenu();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        menuView.SetActive(false);
    }

    private static void Select(Button button)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(button == null ? null : button.gameObject);
    }

    public void QuitGame()
    {
        RestorePausedState();
        HasStartedRun = false;
        // No save file or PlayerPrefs progress is written when leaving a run.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        RestorePausedState();
        instance = null;
    }
}
