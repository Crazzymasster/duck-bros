using UnityEngine;

public class Bullet2d : MonoBehaviour
{
    private Vector3 moveDirection;
    private float moveSpeed;
    private Rigidbody2D rb;
    private Collider2D col;
    [Tooltip("Tag to match for platforms/ground that destroy the bullet")]
    public string destroyOnTag = "ground";
    public void SetDirection(Vector3 direction, float speed)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        // Orient the bullet to face the movement direction (works for sprites pointing "up" by default)
        transform.up = moveDirection;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Ensure we have a Rigidbody2D and a trigger collider so bullets don't apply physics forces
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = true;
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            Vector2 delta = new Vector2(moveDirection.x, moveDirection.y) * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + delta);
        }
        else
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }

    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("ground"))
        {
            moveSpeed = 0f;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
