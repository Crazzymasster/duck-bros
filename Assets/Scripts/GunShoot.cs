using UnityEngine;

public class GunShoot : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public Transform rightHand;
    public Transform leftHand;
    public float bulletSpeed = 12f;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Shoot();
        }
    }
    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Bullet2d bulletScript = bullet.GetComponent<Bullet2d>();

        if (bulletScript != null)
        {
            // If the gun is parented under one of the hand empty objects, use that to determine firing direction.
            Vector3 dir = firePoint.right;

            bool inRight = (rightHand != null) && (transform.IsChildOf(rightHand) || transform.parent == rightHand);
            bool inLeft = (leftHand != null) && (transform.IsChildOf(leftHand) || transform.parent == leftHand);

            if (inLeft)
            {
                dir = -dir;
            }
            else if (!inRight && !inLeft)
            {
                // fallback to flipping based on scale if hand parenting isn't set up
                float signX = Mathf.Sign(firePoint.lossyScale.x);
                dir = Vector3.Scale(dir, new Vector3(signX, 1f, 1f));
            }

            bulletScript.SetDirection(dir, bulletSpeed);
        }
    }
}
