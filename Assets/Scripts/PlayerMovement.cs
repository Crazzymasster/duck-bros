using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Jumping")]
    [SerializeField] private float jumpVelocity = 14f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float coyoteTime = 0.08f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("GroundCheck")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Horizontal Movement")]
    [SerializeField] private float maxRunSpeed = 12f;
    [SerializeField] private float groundAcceleration = 90f;
    [SerializeField] private float groundReverseBoost = 1.3f;
    [SerializeField] private float airAcceleration = 40f;
    [SerializeField] private float groundFriction = 50f;

    [Header("Falling")]
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private float FastFallGravityScale = 5f;
    [SerializeField] private float maxFallSpeed = -20f;
    [SerializeField] private float fastFallThreshold = -2f;

    [Header("Wall Jump")]
    [SerializeField] private float wallSlideMaxSpeed = -3f;
    [SerializeField] private float wallJumpHorizontalVelocity = 11f;
    [SerializeField] private float wallJumpVerticalVelocity = 15f;
    [SerializeField] private float sideContactThreshold = 0.65f;
    [SerializeField] private float wallDetachLockTime = 0.4f;
    [SerializeField] private float wallReEngageDelay = 0.15f;

    [Header("Sprint / Momentum")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private float maxSprintStamina = 2.5f;     // Total sprint duration in seconds
    [SerializeField] private float sprintStaminaDrainRate = 1f; // Stamina lost per second while sprinting
    [SerializeField] private float sprintStaminaRechargeRate = 0.8f; // Stamina gained per second while NOT sprinting
    [SerializeField] private float sprintSpeedMultiplier = 1.5f; // How much faster while sprinting (1.5x = 50% faster)

    [Header("Attack Sync")]
    [SerializeField] private PlayerAttacks playerAttacks;
    [SerializeField] private float attackMoveMultiplier = 0.8f;
    [SerializeField] private float dashSpeedThreshold = 7f;
    [SerializeField] private float dashInputThreshold = 0.7f;

    private float inputY;
    private bool fastFalling;
    private bool isOnWall;
    private int wallDirX;
    private int jumpsUsed;
    private bool isGrounded;
    private bool jumpHeld;
    private Rigidbody2D rb;
    private float inputX;
    private bool jumpPressed;
    private float lastJumpPressedTime = -10f;
    private float lastGroundedTime = -10f;
    private SpriteRenderer mySpriteRenderer;
    private bool isDashing;
    private float wallDetachTimer;
    private float wallReEngageTimer;
    private int lastWallJumpDir;  // Track which wall direction we jumped from

    private bool isSprinting;
    private float currentSprintStamina;
    private bool sprintPressed;

    public Transform Weapon;
    private SpriteRenderer GunSprites;

    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public bool IsSprinting => isSprinting;
    public float SprintStaminaPercent => currentSprintStamina / maxSprintStamina;
    public int FacingDirection => (mySpriteRenderer != null && mySpriteRenderer.flipX) ? -1 : 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; 
        rb.gravityScale = gravityScale;
        mySpriteRenderer = GetComponent<SpriteRenderer>();

        if (playerAttacks == null)
            playerAttacks = GetComponent<PlayerAttacks>();

        if (Weapon != null)
            GunSprites = Weapon.GetComponent<SpriteRenderer>();

        currentSprintStamina = maxSprintStamina;
        isSprinting = false;
    }

    private void Update()
    {
        inputX = Input.GetAxis("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");
        
        jumpPressed = Input.GetButtonDown("Jump");
        if (jumpPressed)
            lastJumpPressedTime = Time.time;
        jumpHeld = Input.GetButton("Jump");

        // Sprint input: press to toggle, press again to stop early
        sprintPressed = Input.GetKeyDown(sprintKey);
        if (sprintPressed && isGrounded)
        {
            if (isSprinting)
            {
                isSprinting = false;  // Press again to stop early
            }
            else if (currentSprintStamina > 0f)
            {
                isSprinting = true;   // Start sprinting if we have stamina
            }
        }

        if (inputX < -0.01f)
        {
            mySpriteRenderer.flipX = true;
            if (GunSprites != null)
                GunSprites.flipX = true;
        }
        else if (inputX > 0.01f)
        {
            mySpriteRenderer.flipX = false;
            if (GunSprites != null)
                GunSprites.flipX = false;
        }
    }

    private void FixedUpdate()
    {
        if (wallDetachTimer > 0f)
            wallDetachTimer -= Time.fixedDeltaTime;

        if (wallReEngageTimer > 0f)
            wallReEngageTimer -= Time.fixedDeltaTime;

        CheckGrounded();
        UpdateSprintStamina();
        UpdateLocomotionStateForCombat();
        HandleHorizontalMovement();
        HandleJump();
        HandleFall();
        HandleWallSlide();

        jumpPressed = false;
    }

    private void UpdateSprintStamina()
    {
        if (isSprinting && isGrounded)
        {
            // Drain stamina while sprinting
            currentSprintStamina -= sprintStaminaDrainRate * Time.fixedDeltaTime;
            
            // Stop sprinting if out of stamina
            if (currentSprintStamina <= 0f)
            {
                currentSprintStamina = 0f;
                isSprinting = false;
            }
        }
        else
        {
            // Recharge stamina when not sprinting
            currentSprintStamina += sprintStaminaRechargeRate * Time.fixedDeltaTime;
            currentSprintStamina = Mathf.Min(currentSprintStamina, maxSprintStamina);
        }

        // Auto-stop sprint if airborne
        if (!isGrounded)
        {
            isSprinting = false;
        }
    }

    private void UpdateLocomotionStateForCombat()
    {
        // Check if we're moving fast enough and in the right direction to trigger dash attack
        bool hasDashInput = Mathf.Abs(inputX) >= dashInputThreshold;
        bool movingFastEnough = Mathf.Abs(rb.linearVelocity.x) >= dashSpeedThreshold;
        bool movingInInputDir = hasDashInput && Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(inputX);

        // Dash is triggered by sprint momentum or moving fast enough while sprinting
        isDashing = isGrounded && movingFastEnough && movingInInputDir && isSprinting;

        if (playerAttacks != null)
        {
            playerAttacks.SetGrounded(isGrounded);
            playerAttacks.SetDashing(isDashing);
        }
    }

    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;

        isGrounded = Physics2D.OverlapCircle(
            groundCheckPoint.position,
            groundCheckRadius,
            groundLayer
        );

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }

        if (isGrounded && !wasGrounded)
        {
            jumpsUsed = 0;
            isOnWall = false;
            wallDirX = 0;
        }
    }

    private void HandleJump()
    {
        Vector2 v = rb.linearVelocity;

        bool coyoteOK = (Time.time - lastGroundedTime) <= coyoteTime;
        bool bufferedJump = (Time.time - lastJumpPressedTime) <= jumpBufferTime;

        // Wall jump (uses bufferedJump, not just jumpPressed, so you can hold space from ground jump and still wall jump)
        if (bufferedJump && isOnWall && !isGrounded)
        {
            v.x = wallJumpHorizontalVelocity * -wallDirX;
            v.y = wallJumpVerticalVelocity;

            lastWallJumpDir = wallDirX;  // Remember which wall we jumped from
            isOnWall = false;
            wallDirX = 0;
            wallDetachTimer = wallDetachLockTime;
            wallReEngageTimer = wallReEngageDelay;
            jumpsUsed = 0;  // Wall jump resets air jumps (Smash Bros behavior)
            lastJumpPressedTime = -10f;
            rb.linearVelocity = v;
            return;
        }

        if (bufferedJump && (isGrounded || coyoteOK || jumpsUsed < maxJumps))
        {
            v.y = jumpVelocity;
            jumpsUsed++;
            lastJumpPressedTime = -10f;
            if (isGrounded)
                isGrounded = false;
        }

        if (!jumpHeld && v.y > 0f)
        {
            v.y *= jumpCutMultiplier;
        }

        rb.linearVelocity = v;
    }

    private void HandleFall()
    {
        Vector2 v = rb.linearVelocity;

        if (!isGrounded && inputY < -0.5f && v.y < fastFallThreshold)
        {
            fastFalling = true;
        }

        rb.gravityScale = fastFalling ? FastFallGravityScale : gravityScale;

        if (v.y < maxFallSpeed)
        {
            v.y = maxFallSpeed;
        }

        rb.linearVelocity = v;

        if (isGrounded)
        {
            fastFalling = false;
        }
    }

    private void HandleHorizontalMovement()
    {
        // Only allow wall stick if: on wall, airborne, past detach grace, inputting into wall, and NOT in re-engage grace period
        bool isWallSticking = isOnWall && !isGrounded && wallDetachTimer <= 0f && wallReEngageTimer <= 0f && 
                              Mathf.Abs(inputX) > 0.05f && Mathf.Sign(inputX) == wallDirX;
        if (isWallSticking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float attackMultiplier = 1f;
        if (playerAttacks != null && playerAttacks.IsAttacking && isGrounded)
            attackMultiplier = attackMoveMultiplier;

        // Apply sprint multiplier to max speed
        float speedMultiplier = isSprinting && isGrounded ? sprintSpeedMultiplier : 1f;
        float targetSpeed = inputX * maxRunSpeed * speedMultiplier * attackMultiplier;
        float currentSpeed = rb.linearVelocity.x;
        float speedDiff = targetSpeed - currentSpeed;

        // Apply direction-reversal boost for snappier feel
        float accel = groundAcceleration;
        if (isGrounded && Mathf.Abs(inputX) > 0.05f && Mathf.Sign(inputX) != Mathf.Sign(currentSpeed))
        {
            accel *= groundReverseBoost;  // 1.3x faster when turning
        }
        
        if (!isGrounded)
            accel = airAcceleration;

        float movement = speedDiff * accel * Time.fixedDeltaTime;
        rb.linearVelocity += new Vector2(movement, 0f);

        // Smooth friction when not inputting on ground
        if (isGrounded && Mathf.Approximately(inputX, 0f))
        {
            float newX = Mathf.MoveTowards(
                rb.linearVelocity.x,
                0f,
                groundFriction * Time.fixedDeltaTime
            );
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }

        // Clamp to maxRunSpeed (accounting for sprint multiplier)
        float maxSpeed = maxRunSpeed * speedMultiplier;
        float clampedX = Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed);
        rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);
    }

    // Contact-normal based wall detection (works even if ground and walls are the same GameObject)
    private void OnCollisionStay2D(Collision2D col)
    {
        // if grounded or in detach grace, skip wall detection
        if (isGrounded || wallDetachTimer > 0f)
            return;

        foreach (ContactPoint2D cp in col.contacts)
        {
            Vector2 n = cp.normal.normalized;

            // top contact -> treat as ground (avoid wall state)
            if (n.y > 0.65f)
            {
                isOnWall = false;
                wallDirX = 0;
                return;
            }

            // side contact -> wall
            if (Mathf.Abs(n.x) > sideContactThreshold && n.y < 0.65f)
            {
                // determine wall side using contact point relative to player position
                int newWallDir = (cp.point.x < transform.position.x) ? -1 : 1;
                
                // Only block re-engagement if we're trying to stick to the same wall we just jumped from
                bool isReEngage = wallReEngageTimer > 0f && newWallDir == lastWallJumpDir;
                
                if (!isReEngage)
                {
                    wallDirX = newWallDir;
                    isOnWall = true;
                    return;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        // leaving contacts, clear wall state (simple approach)
        if (wallDetachTimer <= 0f)
        {
            isOnWall = false;
            wallDirX = 0;
            lastWallJumpDir = 0;  // Clear the jump wall memory when leaving all contact
        }
    }

    private void HandleWallSlide()
    {
        if (!isOnWall || isGrounded)
            return;

        // Skip slide only if we're still in detach grace on the SAME wall we just jumped from
        bool isBlockedBySameWall = wallDetachTimer > 0f && wallDirX == lastWallJumpDir;
        if (isBlockedBySameWall)
            return;

        // Don't stick if holding away from wall
        if (Mathf.Abs(inputX) > 0.05f && Mathf.Sign(inputX) != wallDirX)
            return;

        Vector2 v = rb.linearVelocity;
        v.x = 0f;  // Kill horizontal movement completely

        // Cap downward velocity smoothly when wall sliding
        if (v.y < wallSlideMaxSpeed)
        {
            // Lerp towards max speed for smooth deceleration
            v.y = Mathf.Lerp(v.y, wallSlideMaxSpeed, Time.fixedDeltaTime * 8f);
        }

        rb.linearVelocity = v;
    }
}