using UnityEngine;

/// <summary>Animates Lenny's artwork from gameplay events; never applies movement or damage.</summary>
[RequireComponent(typeof(Movement), typeof(PlayerCombat), typeof(SpriteRenderer))]
public class LennyAnimation : MonoBehaviour
{
    [Header("Artwork")]
    [SerializeField] private Transform facingRoot;
    [SerializeField] private Transform leftWing;
    [SerializeField] private Transform rightWing;
    [SerializeField] private Transform tail;

    [Header("Wing flap")]
    [SerializeField, Min(0.01f)] private float wingFlapDuration = 0.35f;
    [SerializeField, Range(1, 4)] private int wingFlaps = 2;
    [SerializeField, Range(0f, 70f)] private float wingFlapAngle = 35f;

    [Header("Tail sweep")]
    [Tooltip("Visible tail length from its joint to its tip in the resting pose, in local units.")]
    [SerializeField, Min(0.01f)] private float restingTailReach = 0.6f;
    [Tooltip("Extra arc after the contact pose; duration comes from Player Combat's Swing Duration.")]
    [SerializeField, Range(0f, 80f)] private float sweepArc = 35f;

    private Movement movement;
    private PlayerCombat combat;
    private SpriteRenderer bodyVisual;
    private SpriteRenderer[] partVisuals;
    private Quaternion leftRestRotation;
    private Quaternion rightRestRotation;
    private Quaternion tailRestRotation;
    private Vector3 facingRestScale;
    private Vector3 tailRestPosition;
    private Vector3 tailRestScale;
    private SpriteRenderer tailVisual;
    private int tailRestOrder;
    private float wingElapsed;
    private bool flapping;
    private float tailElapsed;
    private float tailDuration;
    private float tailReach;
    private int tailDirection;
    private bool sweeping;

    public bool IsFlapping => flapping;
    public bool IsSweeping => sweeping;

    private void Awake()
    {
        movement = GetComponent<Movement>();
        combat = GetComponent<PlayerCombat>();
        bodyVisual = GetComponent<SpriteRenderer>();
        partVisuals = facingRoot != null ? facingRoot.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        if (facingRoot != null) facingRestScale = facingRoot.localScale;
        if (leftWing != null) leftRestRotation = leftWing.localRotation;
        if (rightWing != null) rightRestRotation = rightWing.localRotation;
        if (tail != null)
        {
            tailRestRotation = tail.localRotation;
            tailRestPosition = tail.localPosition;
            tailRestScale = tail.localScale;
            tailVisual = tail.GetComponent<SpriteRenderer>();
            if (tailVisual != null) tailRestOrder = tailVisual.sortingOrder;
        }
    }

    private void OnEnable()
    {
        movement.DoubleJumped += BeginWingFlap;
        combat.MeleePerformed += BeginMelee;
        combat.VisualsReset += ResetPoses;
    }

    private void LateUpdate()
    {
        int facing = movement.FacingDirection;
        bodyVisual.flipX = facing < 0;
        if (facingRoot != null)
            facingRoot.localScale = new Vector3(facingRestScale.x * facing, facingRestScale.y, facingRestScale.z);
        foreach (var renderer in partVisuals)
            if (renderer != null) renderer.color = bodyVisual.color;

        if (!movement.enabled || !combat.enabled)
        {
            ResetPoses();
            return;
        }
        if (flapping)
        {
            wingElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(wingElapsed / Mathf.Max(0.01f, wingFlapDuration));
            float angle = Mathf.Sin(progress * Mathf.PI * 2f * wingFlaps) * wingFlapAngle * (1f - progress);
            if (leftWing != null) leftWing.localRotation = leftRestRotation * Quaternion.Euler(0, 0, angle);
            if (rightWing != null) rightWing.localRotation = rightRestRotation * Quaternion.Euler(0, 0, -angle);
            if (progress >= 1f) ResetWings();
        }
        if (sweeping)
        {
            tailElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(tailElapsed / tailDuration);
            if (progress >= 1f) ResetTail();
            else ApplyTailPose(progress);
        }
    }

    private void BeginWingFlap()
    {
        wingElapsed = 0f;
        flapping = true;
    }

    private void BeginMelee(bool charged, int direction, float duration, float reach)
    {
        ResetTail();
        if (!charged || tail == null) return;
        tailDirection = direction;
        tailDuration = Mathf.Max(0.01f, duration);
        tailReach = reach;
        tailElapsed = 0f;
        sweeping = true;
        ApplyTailPose(0f);
    }

    private void ApplyTailPose(float progress)
    {
        // Contact is immediate on release, matching the existing OverlapBox and knockback.
        // Stretch along the tail's length to match the existing reach, then sweep back to rest.
        float recovery = Mathf.SmoothStep(0f, 1f, progress);
        float localDirection = tailDirection * movement.FacingDirection;
        float contactAngle = localDirection > 0f ? 180f : 0f;
        float angle = Mathf.Lerp(contactAngle, 0f, recovery) +
            Mathf.Sin(progress * Mathf.PI) * sweepArc * localDirection;
        float reachFromJoint = Mathf.Max(0f, tailReach - tailRestPosition.x * localDirection);
        float stretch = Mathf.Lerp(Mathf.Max(1f, reachFromJoint / restingTailReach), 1f, recovery);
        // Keep the tail attached to the hip throughout the sweep.
        tail.localPosition = tailRestPosition;
        tail.localRotation = tailRestRotation * Quaternion.Euler(0, 0, angle);
        tail.localScale = new Vector3(tailRestScale.x * stretch, tailRestScale.y, tailRestScale.z);
        if (tailVisual != null) tailVisual.sortingOrder = bodyVisual.sortingOrder + 2;
    }

    private void ResetWings()
    {
        flapping = false;
        if (leftWing != null) leftWing.localRotation = leftRestRotation;
        if (rightWing != null) rightWing.localRotation = rightRestRotation;
    }

    private void ResetTail()
    {
        sweeping = false;
        if (tail == null) return;
        tail.localPosition = tailRestPosition;
        tail.localRotation = tailRestRotation;
        tail.localScale = tailRestScale;
        if (tailVisual != null) tailVisual.sortingOrder = tailRestOrder;
    }

    private void ResetPoses()
    {
        ResetWings();
        ResetTail();
    }

    private void OnDisable()
    {
        movement.DoubleJumped -= BeginWingFlap;
        combat.MeleePerformed -= BeginMelee;
        combat.VisualsReset -= ResetPoses;
        ResetPoses();
    }
}
