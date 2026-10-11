using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class StoryEnding : MonoBehaviour
{
    public enum EndingState { WaitingForBoss, Waking, Celebrating, AwaitingReturn, ReturningToTitle }
    public BlightBossHealth boss;
    public Movement player;
    public StoryDialogue dialogue;
    [Min(.01f)] public float wakeDuration = .5f;
    [Min(.01f)] public float celebrationDuration = 2f;
    [Min(1)] public int bounceCount = 3;
    [Min(0f)] public float bounceHeight = 60f;
    public EndingState State { get; private set; }
    private StoryGameplayPause pause;
    private float elapsed;
    private Vector2 tkRest, lennyRest;
    private bool waitingForKeyRelease;

    private void OnEnable()
    {
        if (boss == null) return;
        boss.Died += BeginEnding;
        if (boss.IsDead) BeginEnding();
    }

    private void BeginEnding()
    {
        if (boss == null || !boss.IsDead || State != EndingState.WaitingForBoss) return;
        StoryRunSession.EnsureStoryRun();
        pause = new StoryGameplayPause(player, dialogue.canvas);
        tkRest = dialogue.sleepingTK.anchoredPosition;
        lennyRest = dialogue.celebratingLenny.anchoredPosition;
        dialogue.ShowCelebration();
        elapsed = 0;
        State = EndingState.Waking;
    }

    private void Update()
    {
        AdvanceAnimation(Time.unscaledDeltaTime);
        if (State != EndingState.AwaitingReturn) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (!keyboard.enterKey.isPressed) waitingForKeyRelease = false;
        if (!waitingForKeyRelease && keyboard.enterKey.wasPressedThisFrame) ReturnToTitle();
    }

    private void AdvanceAnimation(float deltaTime)
    {
        if (State == EndingState.Waking)
        {
            elapsed += deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(.01f, wakeDuration));
            dialogue.sleepingTK.localRotation = Quaternion.Euler(0, 0, 90 * (1 - Mathf.SmoothStep(0, 1, progress)));
            if (progress < 1) return;
            State = EndingState.Celebrating;
            elapsed = 0;
        }
        else if (State == EndingState.Celebrating)
        {
            elapsed += deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(.01f, celebrationDuration));
            float height = Mathf.Abs(Mathf.Sin(progress * Mathf.PI * bounceCount)) * bounceHeight;
            if (progress >= 1) height = 0;
            dialogue.sleepingTK.anchoredPosition = tkRest + Vector2.up * height;
            dialogue.celebratingLenny.anchoredPosition = lennyRest + Vector2.up * height;
            if (progress < 1) return;
            State = EndingState.AwaitingReturn;
            waitingForKeyRelease = true;
            dialogue.ShowText("Lenny", "T.K. is awake!", "Enter: return to title");
        }
    }

    public void ReturnToTitle()
    {
        if (State != EndingState.AwaitingReturn) return;
        if (!Application.CanStreamedLevelBeLoaded(TitleMenu.TitleSceneName))
        {
            Debug.LogError("TitleScreen is missing from the build scene list.", this);
            return;
        }
        var menu = FindAnyObjectByType<TitleMenu>();
        if (menu != null)
        {
            if (!menu.ReturnToMainMenu()) return;
        }
        else
        {
            StoryRunSession.EndRun();
            Time.timeScale = 1;
            AudioListener.pause = false;
            if (SceneManager.LoadSceneAsync(TitleMenu.TitleSceneName, LoadSceneMode.Single) == null) return;
        }
        State = EndingState.ReturningToTitle;
    }

    private void OnDisable()
    {
        if (boss != null) boss.Died -= BeginEnding;
        pause?.Dispose();
        pause = null;
        if (dialogue != null) dialogue.Hide();
    }
}
