using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
public class AmuletPickup : MonoBehaviour
{
    [Range(1, 6)] public int amuletNumber = 1;
    [Tooltip("Exact scene name, without .unity. Add the scene to the build list.")]
    public string nextScene = "Level02";
    private bool loading;

    private void Reset() { GetComponent<BoxCollider2D>().isTrigger = true; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (loading || other.attachedRigidbody == null ||
            other.attachedRigidbody.GetComponent<Movement>() == null) return;
        // Keep the amulet available while the next level is still being built.
        string destination = StoryRunSession.Destination(nextScene);
        if (string.IsNullOrWhiteSpace(destination) || !Application.CanStreamedLevelBeLoaded(destination))
        {
            Debug.LogWarning("Amulet " + amuletNumber + ": add " + destination +
                " to the build scene list to enable progression.", this);
            return;
        }
        loading = true;
        GameAudioController.Instance?.PlayTransition(GameSound.AmuletCollect, this);
        var operation = SceneManager.LoadSceneAsync(destination, LoadSceneMode.Single);
        if (operation == null) loading = false;
    }
}
