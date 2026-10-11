using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum BloomCompanionId { None, Whisperseed, Dryad, Petalborn }

/// <summary>Remembers recruitment across deaths in this run; physical followers belong to each level.</summary>
public static class BloomCompanionSession
{
    private struct Recruitment
    {
        public BloomCompanionId id;
        public BloomPetFollower prefab;
    }

    private static readonly List<Recruitment> collected = new List<Recruitment>();
    public static int Count => collected.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        collected.Clear();
        SceneManager.sceneLoaded -= RestoreFollowers;
        SceneManager.sceneLoaded += RestoreFollowers;
    }

    public static bool IsCollected(BloomCompanionId id) => collected.Exists(item => item.id == id);

    internal static bool Remember(BloomPetFollower pet)
    {
        // Preview scenes retain their original local recruitment behavior.
        if (!IsGameplayScene(pet.gameObject.scene) || pet.CompanionId == BloomCompanionId.None) return true;
        if (pet.CompanionPrefab == null)
        {
            Debug.LogError("Assign the Bloom companion's source prefab before recruiting it.", pet);
            return false;
        }
        if (IsCollected(pet.CompanionId)) return false;
        collected.Add(new Recruitment { id = pet.CompanionId, prefab = pet.CompanionPrefab });
        return true;
    }

    public static void ResetCollected()
    {
        collected.Clear();
        foreach (var pet in Object.FindObjectsByType<BloomPetFollower>(FindObjectsSortMode.None))
            if (pet.IsRecruited) pet.RemoveFollower();
    }

    private static bool IsGameplayScene(Scene scene) => scene.path.StartsWith("Assets/Scenes/GameLoop/") &&
        scene.name != TitleMenu.TitleSceneName;

    private static void RestoreFollowers(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == TitleMenu.TitleSceneName) { ResetCollected(); return; }
        if (!IsGameplayScene(scene) || collected.Count == 0) return;
        Movement player = null;
        var scenePets = new List<BloomPetFollower>();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (player == null) player = root.GetComponentInChildren<Movement>();
            scenePets.AddRange(root.GetComponentsInChildren<BloomPetFollower>());
        }
        if (player == null || player.TryGetComponent<PlayerHealth>(out var health) && health.IsDead) return;
        var world = PlatformNavigation2D.GetOrCreate(scene);
        var occupied = new List<Vector2>();
        foreach (var item in collected)
        {
            BloomPetFollower follower = null;
            foreach (var pet in scenePets)
            {
                if (pet.CompanionId != item.id) continue;
                if (follower == null) follower = pet;
                else pet.RemoveFollower();
            }
            if (follower == null)
            {
                if (item.prefab == null) continue;
                follower = Object.Instantiate(item.prefab, player.transform.position, Quaternion.identity);
                SceneManager.MoveGameObjectToScene(follower.gameObject, scene);
            }
            var feet = ArrivalFeet(player, follower, world, occupied);
            occupied.Add(feet);
            follower.PlaceAtFeet(feet);
            follower.AttachToOwner(player, false);
        }
    }

    private static Vector2 ArrivalFeet(Movement player, BloomPetFollower pet, PlatformNavigation2D world,
        List<Vector2> occupied)
    {
        var playerBounds = player.GetComponent<Collider2D>().bounds;
        var feet = new Vector2(playerBounds.center.x, playerBounds.min.y);
        var support = world.SupportAt(feet, 4f);
        if (support == null) return feet;
        Vector2 size = pet.GetComponent<BoxCollider2D>().bounds.size;
        var bounds = support.bounds;
        float left = bounds.min.x + size.x * .5f + .08f;
        float right = bounds.max.x - size.x * .5f - .08f;
        if (left > right) return feet;
        // Search the entrance platform first; avoid arriving inside a wall or hazard.
        for (int i = 0; i < 32; i++)
        {
            float distance = 1.5f + (i / 2) * .85f;
            float direction = (i % 2 == 0 ? -1 : 1) * player.FacingDirection;
            var candidate = new Vector2(Mathf.Clamp(feet.x + direction * distance, left, right), bounds.max.y + .04f);
            if (occupied.Exists(position => Vector2.Distance(position, candidate) < size.x + .1f)) continue;
            bool blocked = false;
            foreach (var collider in Physics2D.OverlapBoxAll(candidate + Vector2.up * size.y * .5f,
                size - Vector2.one * .02f, 0))
            {
                if (collider.GetComponentInParent<Level02ThornHazard>() != null ||
                    collider.GetComponentInParent<BlightBossHazard>() != null ||
                    !collider.isTrigger && (collider.attachedRigidbody == null ||
                    collider.attachedRigidbody.bodyType == RigidbodyType2D.Static))
                { blocked = true; break; }
            }
            if (!blocked) return candidate;
        }
        // A player's safe entrance also fits the smaller companion hull on tiny platforms.
        return new Vector2(feet.x, bounds.max.y + .04f);
    }
}
