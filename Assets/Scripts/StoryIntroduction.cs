using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class StoryIntroduction : MonoBehaviour
{
    public enum IntroductionState { Waiting, Dialogue, Walking, Complete }
    public const string OpeningLine = "I have to find all six amulet pieces to save my friend T.K.!";
    public Movement player;
    public StoryDialogue dialogue;
    [Min(0f)] public float walkDistance = 2f;
    [Min(.01f)] public float walkDuration = 1f;
    public IntroductionState State { get; private set; }
    private StoryGameplayPause pause;
    private Rigidbody2D body;
    private Vector2 walkStart;
    private float elapsed;
    private bool waitingForKeyRelease;

    private IEnumerator Start()
    {
        // The persistent menu finishes hiding its own UI before we acquire the story pause.
        var menu = FindAnyObjectByType<TitleMenu>();
        while (menu != null && menu.IsLoading) yield return null;
        StoryRunSession.EnsureStoryRun();
        if (StoryRunSession.IntroductionPlayed) { State = IntroductionState.Complete; yield break; }
        StoryRunSession.MarkIntroductionPlayed();
        body = player.GetComponent<Rigidbody2D>();
        pause = new StoryGameplayPause(player);
        State = IntroductionState.Dialogue;
        waitingForKeyRelease = true;
        dialogue.ShowIntroduction(OpeningLine);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (State == IntroductionState.Dialogue && keyboard != null)
        {
            if (!keyboard.enterKey.isPressed && !keyboard.spaceKey.isPressed) waitingForKeyRelease = false;
            if (!waitingForKeyRelease && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                DismissDialogue();
        }
        AdvanceWalk(Time.unscaledDeltaTime);
    }

    public void DismissDialogue()
    {
        if (State != IntroductionState.Dialogue) return;
        dialogue.Hide();
        walkStart = body.position;
        elapsed = 0;
        State = IntroductionState.Walking;
    }

    private void AdvanceWalk(float deltaTime)
    {
        if (State != IntroductionState.Walking) return;
        elapsed += deltaTime;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(.01f, walkDuration));
        Vector2 position = walkStart + Vector2.right * walkDistance * progress;
        body.position = position;
        player.transform.position = new Vector3(position.x, position.y, player.transform.position.z);
        if (progress < 1) return;
        State = IntroductionState.Complete;
        Physics2D.SyncTransforms();
        pause.Dispose();
        pause = null;
    }

    private void OnDisable()
    {
        pause?.Dispose();
        pause = null;
        if (dialogue != null) dialogue.Hide();
    }
}
