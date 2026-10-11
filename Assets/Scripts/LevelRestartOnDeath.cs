using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Reloads the encounter on acid deaths. PlayerHealth handles health deaths.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Level02ThornRespawn))]
public sealed class LevelRestartOnDeath : MonoBehaviour
{
    private Level02ThornRespawn acidRespawn;
    private bool restarting;

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
        var scene = gameObject.scene;
        if (scene.buildIndex < 0)
        {
            Debug.LogError("Cannot restart: add this level to the build scene list.", this);
            return;
        }
        restarting = true;
        // Loading from the saved scene restores defeated enemies, health, and spawn positions.
        SceneManager.LoadSceneAsync(scene.buildIndex, LoadSceneMode.Single);
    }
}
