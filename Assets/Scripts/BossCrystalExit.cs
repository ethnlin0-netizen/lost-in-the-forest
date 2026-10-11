using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class BossCrystalExit : MonoBehaviour
{
    [SerializeField] private BossCrystalReward reward;
    private bool returningToMenu;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        reward = GetComponentInParent<BossCrystalReward>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (returningToMenu || reward == null || !reward.Revealed) return;
        var player = other.GetComponentInParent<Movement>();
        if (player == null || !player.isActiveAndEnabled) return;
        var health = player.GetComponent<PlayerHealth>();
        if (health != null && health.IsDead) return;

        var menu = FindAnyObjectByType<TitleMenu>();
        if (menu != null)
        {
            returningToMenu = menu.ReturnToMainMenu();
            if (returningToMenu) GameAudioController.Instance?.PlayTransition(GameSound.CrystalTouch, this);
            return;
        }

        // Also support testing Boss directly, without the persistent title menu.
        if (!Application.CanStreamedLevelBeLoaded(TitleMenu.TitleSceneName))
        {
            Debug.LogError("TitleScreen is missing from the build scene list.", this);
            return;
        }
        returningToMenu = true;
        GameAudioController.Instance?.PlayTransition(GameSound.CrystalTouch, this);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (SceneManager.LoadSceneAsync(TitleMenu.TitleSceneName, LoadSceneMode.Single) == null)
            returningToMenu = false;
    }
}
