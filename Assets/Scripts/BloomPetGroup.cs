using System.Collections.Generic;
using UnityEngine;

// Added to the recruiting player at runtime; no shared player prefab changes.
[DefaultExecutionOrder(30)]
public sealed class BloomPetGroup : MonoBehaviour
{
    private readonly List<BloomPetFollower> pets = new List<BloomPetFollower>();
    private Rigidbody2D ownerBody;
    private float previousVerticalSpeed;
    public int Count => pets.Count;
    public int JumpRevision { get; private set; }
    public float LatestJumpSpeed { get; private set; }

    private void Awake()
    {
        ownerBody = GetComponent<Rigidbody2D>();
        previousVerticalSpeed = ownerBody == null ? 0f : ownerBody.linearVelocity.y;
    }

    private void FixedUpdate()
    {
        if (ownerBody == null) return;
        float speed = ownerBody.linearVelocity.y;
        // Observe the existing player's impulses after Movement.FixedUpdate,
        // including the wing jump, without changing its input or jump logic.
        if (speed > 1f && speed > previousVerticalSpeed + 2f)
        {
            LatestJumpSpeed = speed;
            JumpRevision++;
        }
        previousVerticalSpeed = speed;
    }

    public int Register(BloomPetFollower pet)
    {
        int index = pets.IndexOf(pet);
        if (index >= 0) return index;
        pets.Add(pet);
        return pets.Count - 1;
    }

    public int SlotOf(BloomPetFollower pet) => pets.IndexOf(pet);
    public void Unregister(BloomPetFollower pet) => pets.Remove(pet);
}
