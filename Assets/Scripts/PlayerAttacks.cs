using UnityEngine;
using System.Collections;

public enum AttackType
{
    None,

    Jab,
    ForwardTilt,
    UpTilt,
    DownTilt,
    DashAttack,

    ForwardSmash,
    UpSmash,
    DownSmash,

    NeutralAir,
    ForwardAir,
    BackAir,
    UpAir,
    DownAir
}

[System.Serializable]
public class AttackData
{
    public AttackType type;
    public Sprite sprite;
    public Sprite[] animationFrames;
    public float animationFps = 16f;
    public float damage = 10f;
    public float knockback = 5f;
    public float duration = 0.3f;
    public Vector2 hitboxSize = new Vector2(1f, 1f);
    public Vector2 hitboxOffset = new Vector2(0.5f, 0f);

    public float startupTime = 0.05f;

    public float endLag = 0.1f;
}

public class PlayerAttacks : MonoBehaviour
{
    [Header("Attack Definitions")]
    public AttackData[] attacks;

    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D rb;
    public LayerMask hittableLayers;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Movement Sync")]
    public PlayerMovement movementController;

    [Header("Attack Key")]
    public KeyCode attackKey = KeyCode.Space;

    [Header("Smash Attack")]
    public float smashInputWindow = 0.12f;

    private bool isAttacking;
    private bool isGrounded;
    private bool isDashing;
    private AttackType currentAttack;
    private float attackTimer;
    private float endLagTimer;
    private bool hitboxActive;
    private float wPressTime = -999f;
    private float aPressTime = -999f;
    private float sPressTime = -999f;
    private float dPressTime = -999f;

    private AttackData currentAttackData;
    private Sprite defaultSprite;
    private Coroutine attackAnimationRoutine;

    public bool IsAttacking => isAttacking || endLagTimer > 0f;
    public AttackType CurrentAttack => currentAttack;

    void Start()
    {
        if(spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if(rb == null)
            rb = GetComponent<Rigidbody2D>();

        if(movementController == null)
            movementController = GetComponent<PlayerMovement>();

        if(spriteRenderer != null)
            defaultSprite = spriteRenderer.sprite; 
    }

    void Update()
    {
        CheckGrounded();
        TrackDirectionKeyPresses();
        HandleAttackInput();
        UpdateAttackTimers();
    }

    private void TrackDirectionKeyPresses()
    {
        if (Input.GetKeyDown(KeyCode.W)) wPressTime = Time.time;
        if (Input.GetKeyDown(KeyCode.A)) aPressTime = Time.time;  
        if (Input.GetKeyDown(KeyCode.S)) sPressTime = Time.time;
        if (Input.GetKeyDown(KeyCode.D)) dPressTime = Time.time;
    }

    private void HandleAttackInput()
    {
        if (!Input.GetKeyDown(attackKey)) return;
        if (isAttacking || endLagTimer > 0f) return;

        AttackType attackToPerform = DetermineAttackType();
        PerformAttack(attackToPerform);
    }

    private AttackType DetermineAttackType()
    {
        if (isGrounded)
            return DetermineGroundAttack();
        else
            return DetermineAerialAttack();
    }

    private AttackType DetermineGroundAttack()
    {
        bool holdingLeft = Input.GetKey(KeyCode.A);
        bool holdingRight = Input.GetKey(KeyCode.D);
        bool holdingUp = Input.GetKey(KeyCode.W);
        bool holdingDown = Input.GetKey(KeyCode.S);
        bool holdingHorizontal = holdingLeft || holdingRight;

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

    private AttackType DetermineAerialAttack()
    {
        bool holdingLeft = Input.GetKey(KeyCode.A);
        bool holdingRight = Input.GetKey(KeyCode.D);
        bool holdingUp = Input.GetKey(KeyCode.W);
        bool holdingDown = Input.GetKey(KeyCode.S);

        if (holdingLeft || holdingRight)
        {
            float facingDir = GetFacingDirection();
            float inputDir = holdingRight ? 1f : -1f;

            return (inputDir == facingDir) ? AttackType.ForwardAir : AttackType.BackAir;
        }

        if (holdingUp)
            return AttackType.UpAir;
        
        if (holdingDown)
            return AttackType.DownAir;

        return AttackType.NeutralAir;
    }
    
    private bool WasRecentlyPressed(float pressTime)
    {
        return Time.time - pressTime <= smashInputWindow;
    }

    private void PerformAttack(AttackType type)
    {
        currentAttackData = GetAttackData(type);

        if (currentAttackData == null)
        {
            type = isGrounded ? AttackType.Jab : AttackType.NeutralAir;
            currentAttackData = GetAttackData(type);
        }

        if (currentAttackData == null) return;

        isAttacking = true;
        currentAttack = type;
        attackTimer = currentAttackData.duration;
        hitboxActive = false;

        if (attackAnimationRoutine != null)
        {
            StopCoroutine(attackAnimationRoutine);
            attackAnimationRoutine = null;
        }

        if (HasAnimationFrames(currentAttackData))
        {
            attackAnimationRoutine = StartCoroutine(PlayAttackAnimation(currentAttackData));
        }
        else if (currentAttackData.sprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = currentAttackData.sprite;
        }

        OnAttackStarted(type);
    }

    private void UpdateAttackTimers()
    {
        if(!isAttacking && endLagTimer > 0f)
        {
            endLagTimer -= Time.deltaTime;
            return;
        }

        if (!isAttacking)
        {
            endLagTimer = 0f;
            return;
        }

        attackTimer -= Time.deltaTime;

        if(!hitboxActive && currentAttackData != null && 
        (currentAttackData.duration - attackTimer) >= currentAttackData.startupTime)
        {
            hitboxActive = true;
            CheckHit();
        }

        if(attackTimer <= 0f)
        {
            EndAttack();
        }
    }

    private void EndAttack()
    {
        float endLag = currentAttackData != null ? currentAttackData.endLag : 0f;
        isAttacking = false;
        hitboxActive = false;
        currentAttack = AttackType.None;
        endLagTimer = endLag;

        if(spriteRenderer != null && defaultSprite != null)
            spriteRenderer.sprite = defaultSprite;

        currentAttackData = null;

        if (attackAnimationRoutine != null)
        {
            StopCoroutine(attackAnimationRoutine);
            attackAnimationRoutine = null;
        }

        OnAttackEnded();
    }

    private bool HasAnimationFrames(AttackData data)
    {
        return data != null && data.animationFrames != null && data.animationFrames.Length > 0;
    }

    private IEnumerator PlayAttackAnimation(AttackData data)
    {
        if (data == null || spriteRenderer == null || !HasAnimationFrames(data))
            yield break;

        int frameIndex = 0;
        float fps = Mathf.Max(1f, data.animationFps);
        float frameTime = 1f / fps;
        WaitForSeconds wait = new WaitForSeconds(frameTime);

        while (isAttacking && currentAttackData == data)
        {
            Sprite frame = data.animationFrames[Mathf.Min(frameIndex, data.animationFrames.Length - 1)];
            if (frame != null)
                spriteRenderer.sprite = frame;

            if (frameIndex < data.animationFrames.Length - 1)
                frameIndex++;

            yield return wait;
        }
    }

    private void CheckHit()
    {
        if(currentAttackData == null) return;

        float facingDir = GetFacingDirection();
        Vector2 offset = new Vector2(
            currentAttackData.hitboxOffset.x * facingDir,
            currentAttackData.hitboxOffset.y
        );

        Vector2 center = (Vector2)transform.position + offset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, currentAttackData.hitboxSize, 0f, hittableLayers);

        foreach (Collider2D hit in hits)
        {
            if (hit.transform == transform) continue;
            if (hit.transform.IsChildOf(transform)) continue;

            OnHitTarget(hit, currentAttackData);
        }
    }

    private void CheckGrounded()
    {
        if (movementController != null)
        {
            isGrounded = movementController.IsGrounded;
            return;
        }

        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
    }

    private float GetFacingDirection()
    {
        if (movementController != null)
            return movementController.FacingDirection;

        if (spriteRenderer != null)
            return spriteRenderer.flipX ? -1f : 1f;

        return transform.localScale.x >= 0f ? 1f : -1f;
    }

    private AttackData GetAttackData(AttackType type)
    {
        if(attacks == null) return null;

        foreach (AttackData data in attacks)
        {
            if (data.type == type)
                return data;
        }
        return null;
    }

    public void SetDashing(bool dashing)
    {
        isDashing = dashing;
    }

    public void SetGrounded(bool grounded)
    {
        isGrounded = grounded;
    }

    protected virtual void OnAttackStarted(AttackType type)
    {
        
    }

    protected virtual void OnAttackEnded()
    {
        
    }

    protected virtual void OnHitTarget(Collider2D target, AttackData attackData)
    {
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null)
        {
            Vector2 dir = (target.transform.position - transform.position).normalized;
            targetRb.AddForce(dir * attackData.knockback, ForceMode2D.Impulse);
        }

        Debug.Log($"Hit {target.name} with {attackData.type} for {attackData.damage} damage!");
    }

    void OnDrawGizmosSelected()
    {
        if (currentAttackData != null && hitboxActive)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        }

        AttackData dataToShow = currentAttackData;

        if (dataToShow == null && attacks != null && attacks.Length > 0)
            dataToShow = attacks[0];

        if (dataToShow != null)
        {
            float facingDir = GetFacingDirection();
            Vector2 offset = new Vector2(
                dataToShow.hitboxOffset.x * facingDir,
                dataToShow.hitboxOffset.y
            );

            Vector2 center = (Vector2)transform.position + offset;

            Gizmos.DrawWireCube(center, dataToShow.hitboxSize);
        }

        if(groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}