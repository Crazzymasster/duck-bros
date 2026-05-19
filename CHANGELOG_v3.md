# Combat & Movement System Overhaul - Changelog v3

## Summary
Implemented categorized attack system (Light/Heavy) and stamina-based sprint momentum to improve game feel and reduce input complexity.

---

## File: Assets/Scripts/PlayerAttacks.cs

### Change 1: Added AttackCategory Enum (Lines 26-30)
**NEW - Add after line 23:**
```csharp
public enum AttackCategory
{
    Light,      // Jabs and Tilts - fast, low knockback
    Heavy,      // Smashes - slow, high knockback
    Aerial,     // Air attacks
    Special     // For future special moves
}
```

### Change 2: Modified AttackData Class (Line 34)
**MODIFY - Add this field to AttackData class:**
```csharp
public AttackCategory category;
```
**Location:** In the `[System.Serializable] public class AttackData` block, add after `public AttackType type;`

### Change 3: Updated Attack Input Headers (Lines 56-62)
**REPLACE:**
```csharp
    [Header("Attack Key")]
    public KeyCode attackKey = KeyCode.Space;

    [Header("Smash Attack")]
    public float smashInputWindow = 0.12f;

    [Header("Dash Attack")]
    public float dashAttackTapWindow = 0.16f;
    public float dashAttackMinSpeed = 2.2f;
```

**WITH:**
```csharp
    [Header("Attack Input - New Categorized System")]
    [SerializeField] private KeyCode lightAttackKey = KeyCode.X;      // Jabs & Tilts
    [SerializeField] private KeyCode heavyAttackKey = KeyCode.C;      // Smashes & Power moves
    [SerializeField] private KeyCode specialAttackKey = KeyCode.V;    // Reserved for future

    [Header("Heavy Attack (Smash) Timing")]
    public float smashChargeWindow = 0.12f;      // Hold to build power

    [Header("Dash Attack")]
    public float dashAttackMinSpeed = 2.2f;
    [Tooltip("Enable velocity-based dash attack (no tap timing required)")]
    public bool useVelocityDashAttack = true;
```

### Change 4: Added CurrentCategory Property (After IsAttacking property)
**NEW - Add after line ~110:**
```csharp
    public AttackCategory CurrentCategory => currentCategory;
```

### Change 5: Added Private Variable for Category (In private variables section)
**NEW - Add after `private AttackType currentAttack;` around line 87:**
```csharp
    private AttackCategory currentCategory;
```

### Change 6: Rewrote HandleAttackInput() Method
**REPLACE entire HandleAttackInput() method (lines ~143-148):**

**OLD:**
```csharp
    private void HandleAttackInput()
    {
        if (!Input.GetKeyDown(attackKey)) return;
        if (isAttacking || endLagTimer > 0f) return;

        AttackType attackToPerform = DetermineAttackType();
        PerformAttack(attackToPerform);
    }
```

**WITH:**
```csharp
    private void HandleAttackInput()
    {
        if (isAttacking || endLagTimer > 0f) return;

        // Light Attack: Jabs and Tilts
        if (Input.GetKeyDown(lightAttackKey))
        {
            AttackType attackToPerform = DetermineLightAttack();
            PerformAttack(attackToPerform, AttackCategory.Light);
            return;
        }

        // Heavy Attack: Smashes and power moves
        if (Input.GetKeyDown(heavyAttackKey))
        {
            AttackType attackToPerform = DetermineHeavyAttack();
            PerformAttack(attackToPerform, AttackCategory.Heavy);
            return;
        }

        // Special Attack: Reserved for future special move system
        if (Input.GetKeyDown(specialAttackKey))
        {
            // TODO: Implement special moves in future
            return;
        }
    }
```

### Change 7: Remove Old DetermineAttackType() Method
**DELETE the entire method that contains:**
```csharp
    private AttackType DetermineAttackType()
    {
        if (isGrounded)
            return DetermineGroundAttack();
        else
            return DetermineAerialAttack();
    }
```

### Change 8: Replace DetermineGroundAttack() with DetermineLightAttack()
**REPLACE:**
```csharp
    private AttackType DetermineGroundAttack()
    {
        bool holdingLeft = Input.GetKey(KeyCode.A);
        bool holdingRight = Input.GetKey(KeyCode.D);
        bool holdingUp = Input.GetKey(KeyCode.W);
        bool holdingDown = Input.GetKey(KeyCode.S);
        bool holdingHorizontal = holdingLeft || holdingRight;

        if (holdingHorizontal && ShouldDashAttack(holdingLeft, holdingRight))
            return AttackType.DashAttack;

        if (holdingHorizontal && WasRecentlyPressed(holdingLeft ? aPressTime : dPressTime))
        {
            if(isDashing)
                return AttackType.DashAttack;
            
            return AttackType.ForwardSmash;
        }

        if (holdingUp && WasRecentlyPressed(wPressTime))
            return AttackType.UpSmash;

        if (holdingDown && WasRecentlyPressed(sPressTime))
            return AttackType.DownSmash;
        
        if (holdingHorizontal)
        {
            if (isDashing)
                return AttackType.DashAttack;

            return AttackType.ForwardTilt;
        }

        if (holdingUp)
            return AttackType.UpTilt;
        
        if (holdingDown)
            return AttackType.DownTilt;
        
        return AttackType.Jab;
    }
```

**WITH:**
```csharp
    /// <summary>
    /// Light Attacks - Fast attacks with low knockback
    /// Neutral = Jab
    /// Forward = Forward Tilt
    /// Up = Up Tilt
    /// Down = Down Tilt
    /// Dash Attack = Moving fast + any direction
    /// </summary>
    private AttackType DetermineLightAttack()
    {
        if (!isGrounded)
            return DetermineAerialAttack();

        bool holdingLeft = Input.GetKey(KeyCode.A);
        bool holdingRight = Input.GetKey(KeyCode.D);
        bool holdingUp = Input.GetKey(KeyCode.W);
        bool holdingDown = Input.GetKey(KeyCode.S);
        bool holdingHorizontal = holdingLeft || holdingRight;

        // Dash attack if moving fast (velocity-based, no timing needed!)
        if (holdingHorizontal && IsMovingFastEnough())
            return AttackType.DashAttack;

        // Forward Tilt
        if (holdingHorizontal)
            return AttackType.ForwardTilt;

        // Up Tilt
        if (holdingUp)
            return AttackType.UpTilt;

        // Down Tilt
        if (holdingDown)
            return AttackType.DownTilt;

        // Neutral Jab (default)
        return AttackType.Jab;
    }

    /// <summary>
    /// Heavy Attacks - Powerful attacks with high knockback
    /// Neutral = Neutral (nothing happens - can implement later)
    /// Forward = Forward Smash
    /// Up = Up Smash
    /// Down = Down Smash
    /// </summary>
    private AttackType DetermineHeavyAttack()
    {
        if (!isGrounded)
            return DetermineAerialAttack();

        bool holdingLeft = Input.GetKey(KeyCode.A);
        bool holdingRight = Input.GetKey(KeyCode.D);
        bool holdingUp = Input.GetKey(KeyCode.W);
        bool holdingDown = Input.GetKey(KeyCode.S);

        // Forward Smash
        if (holdingLeft || holdingRight)
            return AttackType.ForwardSmash;

        // Up Smash
        if (holdingUp)
            return AttackType.UpSmash;

        // Down Smash
        if (holdingDown)
            return AttackType.DownSmash;

        // Neutral: Use Forward Smash as default since we have no neutral heavy
        return AttackType.ForwardSmash;
    }
```

### Change 9: Replace ShouldDashAttack() with IsMovingFastEnough()
**REPLACE:**
```csharp
    private bool ShouldDashAttack(bool holdingLeft, bool holdingRight)
    {
        float inputDir = holdingRight ? 1f : -1f;
        float pressTime = holdingRight ? dPressTime : aPressTime;
        bool tappedForward = WasRecentlyPressed(pressTime, dashAttackTapWindow);

        bool movingForwardFast = false;
        if (rb != null)
        {
            movingForwardFast = Mathf.Abs(rb.linearVelocity.x) >= dashAttackMinSpeed && Mathf.Sign(rb.linearVelocity.x) == inputDir;
        }

        return tappedForward || movingForwardFast || isDashing;
    }
```

**WITH:**
```csharp
    private bool IsMovingFastEnough()
    {
        if (rb == null)
            return false;

        return Mathf.Abs(rb.linearVelocity.x) >= dashAttackMinSpeed;
    }
```

### Change 10: Update PerformAttack() Signature
**MODIFY the PerformAttack() method signature:**

**OLD:**
```csharp
    private void PerformAttack(AttackType type)
```

**NEW:**
```csharp
    private void PerformAttack(AttackType type, AttackCategory category = AttackCategory.Light)
```

**And add this line inside PerformAttack() after `isAttacking = true;`:**
```csharp
        currentCategory = category;
```

### Change 11: Update EndAttack() Method
**In the EndAttack() method, add this line after `currentAttack = AttackType.None;`:**
```csharp
        currentCategory = AttackCategory.Light;
```

---

## File: Assets/Scripts/PlayerMovement.cs

### Change 1: Added Sprint/Stamina Header (After Wall Jump header, around line 36)
**NEW - Add after the Wall Jump section:**
```csharp
    [Header("Sprint / Momentum")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private float maxSprintStamina = 2.5f;     // Total sprint duration in seconds
    [SerializeField] private float sprintStaminaDrainRate = 1f; // Stamina lost per second while sprinting
    [SerializeField] private float sprintStaminaRechargeRate = 0.8f; // Stamina gained per second while NOT sprinting
    [SerializeField] private float sprintSpeedMultiplier = 1.5f; // How much faster while sprinting (1.5x = 50% faster)
```

### Change 2: Added Sprint Private Variables (In private variables section)
**NEW - Add after `private int lastWallJumpDir;`:**
```csharp
    private bool isSprinting;
    private float currentSprintStamina;
    private bool sprintPressed;
```

### Change 3: Added Sprint Properties
**NEW - Replace the existing public properties:**

**OLD:**
```csharp
    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public int FacingDirection => (mySpriteRenderer != null && mySpriteRenderer.flipX) ? -1 : 1;
```

**NEW:**
```csharp
    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public bool IsSprinting => isSprinting;
    public float SprintStaminaPercent => currentSprintStamina / maxSprintStamina;
    public int FacingDirection => (mySpriteRenderer != null && mySpriteRenderer.flipX) ? -1 : 1;
```

### Change 4: Initialize Sprint Stamina in Awake()
**ADD to the end of Awake() method:**
```csharp
        currentSprintStamina = maxSprintStamina;
        isSprinting = false;
```

### Change 5: Update Update() Method - Add Sprint Input Handling
**REPLACE the Update() method:**

**OLD:**
```csharp
    private void Update()
    {
        inputX = Input.GetAxis("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");
        
        jumpPressed = Input.GetButtonDown("Jump");
        if (jumpPressed)
            lastJumpPressedTime = Time.time;
        jumpHeld = Input.GetButton("Jump");

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
```

**NEW:**
```csharp
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
```

### Change 6: Add UpdateSprintStamina() to FixedUpdate()
**MODIFY FixedUpdate() - add this call after CheckGrounded():**
```csharp
        UpdateSprintStamina();
```

### Change 7: Add New UpdateSprintStamina() Method
**NEW - Add this new method (after FixedUpdate()):**
```csharp
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
```

### Change 8: Update UpdateLocomotionStateForCombat()
**REPLACE the entire UpdateLocomotionStateForCombat() method:**

**OLD:**
```csharp
    private void UpdateLocomotionStateForCombat()
    {
        bool hasDashInput = Mathf.Abs(inputX) >= dashInputThreshold;
        bool movingFastEnough = Mathf.Abs(rb.linearVelocity.x) >= dashSpeedThreshold;
        bool movingInInputDir = hasDashInput && Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(inputX);

        isDashing = isGrounded && movingFastEnough && movingInInputDir;

        if (playerAttacks != null)
        {
            playerAttacks.SetGrounded(isGrounded);
            playerAttacks.SetDashing(isDashing);
        }
    }
```

**NEW:**
```csharp
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
```

### Change 9: Update HandleHorizontalMovement() for Sprint Speed
**MODIFY HandleHorizontalMovement() - add sprint multiplier:**

**Find this section:**
```csharp
        float attackMultiplier = 1f;
        if (playerAttacks != null && playerAttacks.IsAttacking && isGrounded)
            attackMultiplier = attackMoveMultiplier;

        float targetSpeed = inputX * maxRunSpeed * attackMultiplier;
```

**REPLACE WITH:**
```csharp
        float attackMultiplier = 1f;
        if (playerAttacks != null && playerAttacks.IsAttacking && isGrounded)
            attackMultiplier = attackMoveMultiplier;

        // Apply sprint multiplier to max speed
        float speedMultiplier = isSprinting && isGrounded ? sprintSpeedMultiplier : 1f;
        float targetSpeed = inputX * maxRunSpeed * speedMultiplier * attackMultiplier;
```

**And find this section near the end:**
```csharp
        // Clamp to maxRunSpeed
        float clampedX = Mathf.Clamp(rb.linearVelocity.x, -maxRunSpeed, maxRunSpeed);
        rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);
```

**REPLACE WITH:**
```csharp
        // Clamp to maxRunSpeed (accounting for sprint multiplier)
        float maxSpeed = maxRunSpeed * speedMultiplier;
        float clampedX = Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed);
        rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);
```

---

## File: CONTROLS.md

This file was created/updated with full documentation. See the existing file for reference.

---

## Summary of Key Behavioral Changes

1. **Attack System:** 
   - Old: One attack button (Space) modified by direction keys
   - New: Separate Light (X), Heavy (C), and Special (V) buttons

2. **Dash Attack:**
   - Old: Required tap timing (dashAttackTapWindow) + holding direction + movement
   - New: Just need velocity while moving in a direction

3. **Sprint System:**
   - Old: Instant toggle with Shift key
   - New: Press Shift to activate momentum-based sprint (drains stamina), press again to stop early

4. **Momentum Control:**
   - Old: Had to hold sprint through attacks
   - New: Press sprint once, let momentum carry you, attack while coasting

---

## Testing Checklist

- [ ] Light attacks (X) work in all directions
- [ ] Heavy attacks (C) work in all directions  
- [ ] Dash attacks trigger when moving fast + pressing X
- [ ] Sprint activates with Shift, stops with Shift again
- [ ] Sprint stamina drains while active, recharges while not sprinting
- [ ] Sprint momentum carries into attacks
- [ ] Aerial attacks work correctly
- [ ] Special button (V) is reserved (does nothing currently)
