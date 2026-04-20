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
    [SerializeField] private float maxRunSpeed = 0f;
    [SerializeField] private float groundAcceleration = 60f;
    [SerializeField] private float airAcceleration = 30f;
    [SerializeField] private float groundFriction = 40f;

    [Header("Falling")]
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private float FastFallGravityScale = 5f;
    [SerializeField] private float maxFallSpeed = -20f;
    [SerializeField] private float fastFallThreshold = -2f;

    [Header("Wall Jump")]
    [SerializeField] private float wallCheckDistance = 0.3f;
    [SerializeField] private float wallSlideMaxSpeed = -2f;
    [SerializeField] private float wallJumpHorizontalVelocity = 10f;
    [SerializeField] private float wallJumpVerticalVelocity = 14f;
    [SerializeField] private float sideContactThreshold = 0.65f;

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

    public Transform Weapon;
    private SpriteRenderer GunSprites;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; 
        rb.gravityScale = gravityScale;
        mySpriteRenderer = GetComponent<SpriteRenderer>();
        GunSprites = Weapon.GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");
        
        jumpPressed = Input.GetButtonDown("Jump");
        if (jumpPressed)
            lastJumpPressedTime = Time.time;
        jumpHeld = Input.GetButton("Jump");

        if(Input.GetKeyDown(KeyCode.A))
        {
            mySpriteRenderer.flipX = true;
            GunSprites.flipX = true;
            // Weapon.position.x = rb.position.x - 0.5f; // Adjust the offset as needed
        }
        else if(Input.GetKeyDown(KeyCode.D))
        {
            mySpriteRenderer.flipX = false;
            GunSprites.flipX = false;
        }
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        HandleHorizontalMovement();
        HandleJump();
        HandleFall();
        HandleWallSlide();

        jumpPressed = false;
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

        if (jumpPressed && isOnWall && !isGrounded)
        {
            v.x = wallJumpHorizontalVelocity * -wallDirX;
            v.y = wallJumpVerticalVelocity;

            isOnWall = false;
            jumpsUsed = 1;
            rb.linearVelocity = v;
            return;
        }

        bool coyoteOK = (Time.time - lastGroundedTime) <= coyoteTime;
        bool bufferedJump = (Time.time - lastJumpPressedTime) <= jumpBufferTime;

        if (bufferedJump && (isGrounded || coyoteOK || jumpsUsed < maxJumps))
        {
            v.y = jumpVelocity;
            jumpsUsed++;
            lastJumpPressedTime = -10f;
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

        if (!isGrounded && inputY < -0.5f && v.y < 0f)
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
        if (isOnWall && !isGrounded)
            return;

        float targetSpeed = inputX * maxRunSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        float accel = isGrounded ? groundAcceleration : airAcceleration;
        float movement = speedDiff * accel * Time.fixedDeltaTime;

        rb.linearVelocity += new Vector2(movement, 0f);

        if (isGrounded && Mathf.Approximately(inputX, 0f))
        {
            float newX = Mathf.MoveTowards(
                rb.linearVelocity.x,
                0f,
                groundFriction * Time.fixedDeltaTime
            );
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }

        // optional: clamp to maxRunSpeed
        float clampedX = Mathf.Clamp(rb.linearVelocity.x, -maxRunSpeed, maxRunSpeed);
        rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);
    }

    // Contact-normal based wall detection (works even if ground and walls are the same GameObject)
    private void OnCollisionStay2D(Collision2D col)
    {
        // if grounded then prefer ground state over wall
        if (isGrounded)
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
                wallDirX = (cp.point.x < transform.position.x) ? -1 : 1;
                isOnWall = true;
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        // leaving contacts, clear wall state (simple approach)
        isOnWall = false;
        wallDirX = 0;
    }

    private void HandleWallSlide()
    {
        if (!isOnWall || isGrounded)
            return;

        Vector2 v = rb.linearVelocity;
        v.x = 0f;

        if (v.y < wallSlideMaxSpeed)
        {
            v.y = wallSlideMaxSpeed;
        }

        rb.linearVelocity = v;
    }
}