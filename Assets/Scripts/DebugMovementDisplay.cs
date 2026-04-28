using UnityEngine;

public class DebugMovementDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerAttacks playerAttacks;
    [SerializeField] private Rigidbody2D rb;

    [Header("Display Settings")]
    [SerializeField] private bool enableDebugDisplay = true;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int fontSize = 14;
    [SerializeField] private float displayX = 10f;
    [SerializeField] private float displayY = 10f;
    [SerializeField] private float displayWidth = 550f;
    [SerializeField] private float displayHeight = 700f;

    [Header("Console Logging")]
    [SerializeField] private bool logToConsole = true;
    [SerializeField] private float logInterval = 0.5f;

    private float lastLogTime;
    private GUIStyle debugStyle;
    private Vector2 scrollPosition = Vector2.zero;

    private void Start()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerAttacks == null)
            playerAttacks = GetComponent<PlayerAttacks>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (logToConsole && Time.time - lastLogTime >= logInterval)
        {
            LogDebugInfo();
            lastLogTime = Time.time;
        }
    }

    private void OnGUI()
    {
        if (!enableDebugDisplay || playerMovement == null)
            return;

        // Lazy initialize GUI style on first OnGUI call
        if (debugStyle == null)
        {
            debugStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = textColor },
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };
        }

        string debugText = GetDebugText();
        
        // Draw background box
        GUI.Box(new Rect(displayX - 5, displayY - 5, displayWidth + 10, displayHeight + 10), "");
        
        // Draw scrollable content
        scrollPosition = GUI.BeginScrollView(new Rect(displayX, displayY, displayWidth, displayHeight), scrollPosition, new Rect(0, 0, displayWidth - 20, 1200));
        GUI.Label(new Rect(5, 5, displayWidth - 30, 1200), debugText, debugStyle);
        GUI.EndScrollView();
    }

    private string GetDebugText()
    {
        // Input
        float inputX = Input.GetAxis("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");
        bool jumpPressed = Input.GetButtonDown("Jump");
        bool jumpHeld = Input.GetButton("Jump");
        bool attackPressed = Input.GetKeyDown(KeyCode.Space);

        // Movement state
        bool isGrounded = playerMovement.IsGrounded;
        bool isDashing = playerMovement.IsDashing;
        int facingDir = playerMovement.FacingDirection;

        // Velocity
        Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;

        // Combat state
        bool isAttacking = playerAttacks != null && playerAttacks.IsAttacking;
        var currentAttack = playerAttacks != null ? playerAttacks.CurrentAttack : AttackType.None;

        string text = "<b>== DUCK BROS DEBUG ==</b>\n\n";

        text += "<b>INPUT:</b>\n";
        text += $"  Horizontal: {inputX:F2}\n";
        text += $"  Vertical: {inputY:F2}\n";
        text += $"  Jump Pressed: {(jumpPressed ? "YES" : "NO")}\n";
        text += $"  Jump Held: {(jumpHeld ? "YES" : "NO")}\n";
        text += $"  Attack Pressed: {(attackPressed ? "YES" : "NO")}\n";
        text += $"  Facing: {(facingDir > 0 ? "RIGHT" : "LEFT")}\n\n";

        text += "<b>MOVEMENT STATE:</b>\n";
        text += $"  Grounded: {(isGrounded ? "YES" : "NO")}\n";
        text += $"  Dashing: {(isDashing ? "YES" : "NO")}\n";
        text += $"  Velocity: ({velocity.x:F2}, {velocity.y:F2})\n\n";

        text += "<b>WALL STATE:</b>\n";
        text += $"  On Wall: {GetWallState()}\n";
        text += $"  Wall Dir: {GetWallDir()}\n";
        text += $"  Wall Detach Timer: {GetWallDetachTimer():F2}s\n";
        text += $"  Wall ReEngage Timer: {GetWallReEngageTimer():F2}s\n\n";

        text += "<b>JUMP STATE:</b>\n";
        int jumpsUsed = GetJumpsUsed();
        int maxJumps = GetMaxJumps();
        float coyoteRemaining = GetCoyoteRemaining();
        float bufferRemaining = GetBufferRemaining();
        text += $"  Jumps Used: {jumpsUsed}/{maxJumps}\n";
        text += $"  Can Air Jump: {(jumpsUsed < maxJumps ? "YES" : "NO")}\n";
        text += $"  Coyote Time: {coyoteRemaining:F2}s\n";
        text += $"  Buffer Time: {bufferRemaining:F2}s\n";
        text += $"  Can Jump Now: {(GetCanJump() ? "YES ✓" : "NO ✗")}\n\n";

        text += "<b>COMBAT STATE:</b>\n";
        text += $"  Attacking: {(isAttacking ? "YES" : "NO")}\n";
        text += $"  Current Attack: {currentAttack}\n";

        return text;
    }

    private void LogDebugInfo()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");
        bool isGrounded = playerMovement.IsGrounded;
        bool isOnWall = GetIsOnWall();
        int jumpsUsed = GetJumpsUsed();
        int maxJumps = GetMaxJumps();
        Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;

        Debug.Log($"[MOVEMENT] Input:({inputX:F2},{inputY:F2}) | Grounded:{isGrounded} | OnWall:{isOnWall} | Jumps:{jumpsUsed}/{maxJumps} | Vel:({velocity.x:F2},{velocity.y:F2}) | Dashing:{playerMovement.IsDashing}");
    }

    // Reflection-based accessors for private fields (for debug purposes)
    private bool GetIsOnWall()
    {
        var field = typeof(PlayerMovement).GetField("isOnWall", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (bool)field.GetValue(playerMovement);
        return false;
    }

    private int GetWallDir()
    {
        var field = typeof(PlayerMovement).GetField("wallDirX", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (int)field.GetValue(playerMovement);
        return 0;
    }

    private string GetWallState()
    {
        bool isOnWall = GetIsOnWall();
        int wallDir = GetWallDir();
        if (!isOnWall)
            return "NO";
        return wallDir > 0 ? "YES (RIGHT)" : "YES (LEFT)";
    }

    private float GetWallDetachTimer()
    {
        var field = typeof(PlayerMovement).GetField("wallDetachTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (float)field.GetValue(playerMovement);
        return 0f;
    }

    private float GetWallReEngageTimer()
    {
        var field = typeof(PlayerMovement).GetField("wallReEngageTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (float)field.GetValue(playerMovement);
        return 0f;
    }

    private int GetJumpsUsed()
    {
        var field = typeof(PlayerMovement).GetField("jumpsUsed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (int)field.GetValue(playerMovement);
        return 0;
    }

    private float GetTimeSinceGround()
    {
        var field = typeof(PlayerMovement).GetField("lastGroundedTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            float lastGroundedTime = (float)field.GetValue(playerMovement);
            return Time.time - lastGroundedTime;
        }
        return 999f;
    }

    private float GetTimeSinceJumpPress()
    {
        var field = typeof(PlayerMovement).GetField("lastJumpPressedTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            float lastJumpPressedTime = (float)field.GetValue(playerMovement);
            return Time.time - lastJumpPressedTime;
        }
        return 999f;
    }

    private int GetMaxJumps()
    {
        var field = typeof(PlayerMovement).GetField("maxJumps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (int)field.GetValue(playerMovement);
        return 0;
    }

    private float GetCoyoteRemaining()
    {
        var coyoteTimeField = typeof(PlayerMovement).GetField("coyoteTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var lastGroundedField = typeof(PlayerMovement).GetField("lastGroundedTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (coyoteTimeField != null && lastGroundedField != null)
        {
            float coyoteTime = (float)coyoteTimeField.GetValue(playerMovement);
            float lastGroundedTime = (float)lastGroundedField.GetValue(playerMovement);
            float timeSinceGround = Time.time - lastGroundedTime;
            return Mathf.Max(0f, coyoteTime - timeSinceGround);
        }
        return 0f;
    }

    private float GetBufferRemaining()
    {
        var bufferTimeField = typeof(PlayerMovement).GetField("jumpBufferTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return bufferTimeField != null ? Mathf.Max(0f, (float)bufferTimeField.GetValue(playerMovement) - GetTimeSinceJumpPress()) : 0f;
    }

    private bool GetCanJump()
    {
        bool isGrounded = playerMovement.IsGrounded;
        float coyoteRemaining = GetCoyoteRemaining();
        int jumpsUsed = GetJumpsUsed();
        int maxJumps = GetMaxJumps();
        float bufferRemaining = GetBufferRemaining();

        return bufferRemaining > 0f && (isGrounded || coyoteRemaining > 0f || jumpsUsed < maxJumps);
    }
}
