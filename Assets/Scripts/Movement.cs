using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class Movement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 6f;
    [SerializeField, Min(0f)] private float jumpSpeed = 10f;
    [Tooltip("Upward speed of the wing-assisted airborne jump; independent of the first jump.")]
    [SerializeField, Min(0f)] private float doubleJumpSpeed = 14f;
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Placeholder visuals")]
    [SerializeField] private Transform facingMarker;
    [SerializeField] private Transform aimMarker;
    [SerializeField] private Transform wings;
    [SerializeField, Min(0.01f)] private float wingDisplayTime = 0.35f;

    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private Rigidbody2D body;
    private CapsuleCollider2D bodyCollider;
    private ContactFilter2D groundFilter;
    private float horizontalInput;
    private bool jumpRequested;
    private bool airJumpAvailable = true;
    private float wingTimeRemaining;

    public int FacingDirection { get; private set; } = 1;
    public Vector2 AimDirection { get; private set; } = Vector2.right;
    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();
        groundFilter = new ContactFilter2D { useTriggers = false };
        groundFilter.SetLayerMask(groundLayers);
        if (wings != null) wings.gameObject.SetActive(false);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        horizontalInput = keyboard == null ? 0f :
            (keyboard.rightArrowKey.isPressed ? 1f : 0f) -
            (keyboard.leftArrowKey.isPressed ? 1f : 0f);

        if (horizontalInput != 0f)
            FacingDirection = horizontalInput > 0f ? 1 : -1;

        float verticalAim = keyboard == null ? 0f :
            (keyboard.upArrowKey.isPressed ? 1f : 0f) -
            (keyboard.downArrowKey.isPressed ? 1f : 0f);
        AimDirection = verticalAim == 0f
            ? new Vector2(FacingDirection, 0f)
            : new Vector2(0f, verticalAim);

        if (keyboard != null && keyboard.zKey.wasPressedThisFrame)
            jumpRequested = true;

        if (facingMarker != null)
        {
            var position = facingMarker.localPosition;
            position.x = Mathf.Abs(position.x) * FacingDirection;
            facingMarker.localPosition = position;
        }

        if (aimMarker != null)
            aimMarker.localRotation = Quaternion.FromToRotation(Vector3.right, AimDirection);

        if (wings != null)
        {
            wingTimeRemaining = Mathf.Max(0f, wingTimeRemaining - Time.deltaTime);
            wings.gameObject.SetActive(wingTimeRemaining > 0f);
            wings.localScale = new Vector3(1f,
                0.75f + Mathf.Abs(Mathf.Sin(wingTimeRemaining * 24f)) * 0.5f, 1f);
        }
    }

    private void FixedUpdate()
    {
        IsGrounded = CheckGrounded();
        if (IsGrounded) airJumpAvailable = true;

        var velocity = body.linearVelocity;
        velocity.x = horizontalInput * moveSpeed;
        if (jumpRequested)
        {
            if (IsGrounded)
            {
                velocity.y = jumpSpeed;
                IsGrounded = false;
            }
            else if (airJumpAvailable)
            {
                velocity.y = doubleJumpSpeed;
                airJumpAvailable = false;
                wingTimeRemaining = wingDisplayTime;
                if (wings != null) wings.gameObject.SetActive(true);
            }
        }
        jumpRequested = false;
        body.linearVelocity = velocity;
    }

    private bool CheckGrounded()
    {
        // Rising past the platform must not replenish the airborne jump.
        if (body.linearVelocity.y > 0.05f) return false;
        int count = bodyCollider.Cast(Vector2.down, groundFilter, groundHits, 0.05f);
        for (int i = 0; i < count; i++)
            if (groundHits[i].normal.y > 0.5f) return true;
        return false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) ClearInput();
    }

    private void OnDisable()
    {
        ClearInput();
        wingTimeRemaining = 0f;
        if (wings != null) wings.gameObject.SetActive(false);
    }

    private void ClearInput()
    {
        horizontalInput = 0f;
        jumpRequested = false;
    }
}
