using UnityEngine;

// A separate asset keeps prefab references pointing to assets rather than
// being remapped to each instantiated companion's own scene component.
[CreateAssetMenu(menuName = "Lost in the Forest/Bloom Companion Catalog")]
public sealed class BloomCompanionCatalog : ScriptableObject
{
    [SerializeField] private BloomPetFollower whisperseed;
    [SerializeField] private BloomPetFollower dryad;
    [SerializeField] private BloomPetFollower petalborn;

    public BloomPetFollower PrefabFor(BloomCompanionId id)
    {
        switch (id)
        {
            case BloomCompanionId.Whisperseed: return whisperseed;
            case BloomCompanionId.Dryad: return dryad;
            case BloomCompanionId.Petalborn: return petalborn;
            default: return null;
        }
    }
}
