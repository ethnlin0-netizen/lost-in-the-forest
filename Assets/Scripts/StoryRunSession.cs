using UnityEngine;

/// <summary>Story progress belongs to a new game, not an individual scene load.</summary>
public static class StoryRunSession
{
    public const string IntroductionScene = "PrismbloomLevel01Story";
    public const string EndingScene = "BossStory";
    public const string IntroductionScenePath = "Assets/Scenes/GameLoop/PrismbloomLevel01Story.unity";
    public const string EndingScenePath = "Assets/Scenes/GameLoop/BossStory.unity";
    public static bool Active { get; private set; }
    public static bool IntroductionPlayed { get; private set; }
    private static int gameplayLocks;
    public static bool BlocksGameplay => gameplayLocks > 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        Active = IntroductionPlayed = false;
        gameplayLocks = 0;
    }

    public static string FirstScene(string original)
    {
        return System.IO.Path.GetFileNameWithoutExtension(original) == "PrismbloomLevel01" &&
            Application.CanStreamedLevelBeLoaded(IntroductionScenePath) ? IntroductionScenePath : original;
    }

    public static void BeginNewGame(bool storyEnabled)
    {
        Active = storyEnabled;
        IntroductionPlayed = false;
    }

    public static void EnsureStoryRun() { Active = true; }
    public static void MarkIntroductionPlayed() { IntroductionPlayed = true; }
    public static void EndRun() { Active = false; }
    internal static void LockGameplay() { ++gameplayLocks; }
    internal static void UnlockGameplay() { gameplayLocks = Mathf.Max(0, gameplayLocks - 1); }

    public static string Destination(string original)
    {
        if (!Active || string.IsNullOrWhiteSpace(original)) return original;
        // Exact paths keep the story on the finished GameLoop scenes, even when
        // older scenes with the same name still exist elsewhere in Assets.
        switch (System.IO.Path.GetFileNameWithoutExtension(original))
        {
            case "GloamthorneLevel02":
            case "Ashpetal Level 03":
            case "Wraithglass Barrens Level 05":
                return "Assets/Scenes/GameLoop/" + System.IO.Path.GetFileNameWithoutExtension(original) + ".unity";
            case "Level04":
            case "Veinroot Level04":
                return "Assets/Scenes/GameLoop/Veinroot Level04.unity";
            case "Boss":
                return EndingScenePath;
            default:
                return original;
        }
    }
}
