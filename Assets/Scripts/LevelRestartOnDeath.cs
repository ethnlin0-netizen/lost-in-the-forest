using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Reloads saved levels for both health deaths and lethal hazards.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Level02ThornRespawn))]
public sealed class LevelRestartOnDeath : MonoBehaviour
{
    private Level02ThornRespawn acidRespawn;
    private bool restarting;
    private static Scene loadingScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLoadingState() { loadingScene = default; }

    private void Awake()
    {
        acidRespawn = GetComponent<Level02ThornRespawn>();
    }

    private void OnEnable()
    {
        acidRespawn.ReturnedToEntrance += Restart;
    }

    private void OnDisable()
    {
        acidRespawn.ReturnedToEntrance -= Restart;
    }

    private void Restart()
    {
        if (restarting) return;
        restarting = RestartScene(gameObject.scene, this);
    }

    public static bool RestartScene(Scene scene, Object context)
    {
        if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
        {
            Debug.LogError("Cannot restart an unsaved or unloaded level.", context);
            return false;
        }
        // Health death and repeated hazard contacts can arrive in the same frame.
        if (loadingScene == scene) return true;
        bool inBuild = Application.CanStreamedLevelBeLoaded(scene.path);
#if !UNITY_EDITOR
        if (!inBuild)
        {
            Debug.LogError("Cannot restart: add " + scene.path + " to the build scene list.", context);
            return false;
        }
#endif
        loadingScene = scene;
        var menu = FindAnyObjectByType<TitleMenu>();
        if (menu != null && menu.IsPaused) menu.StartGame();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        // Reload the saved scene, not just the player: enemies and pickups are recreated.
        AsyncOperation operation;
#if UNITY_EDITOR
        operation = inBuild
            ? SceneManager.LoadSceneAsync(scene.path, LoadSceneMode.Single)
            : UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        operation = SceneManager.LoadSceneAsync(scene.path, LoadSceneMode.Single);
#endif
        if (operation == null)
        {
            loadingScene = default;
            Debug.LogError("Could not reload " + scene.path, context);
            return false;
        }
        Scene requestedScene = scene;
        operation.completed += _ =>
        {
            if (loadingScene == requestedScene) loadingScene = default;
        };
        return true;
    }
}
