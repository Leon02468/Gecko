using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public Transform player; // Assign this in the inspector or find at runtime

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Face the player
        if (player != null)
        {
            Vector3 scale = transform.localScale;
            if (player.position.x < transform.position.x)
                scale.x = Mathf.Abs(scale.x); // Face left
            else
                scale.x = -Mathf.Abs(scale.x);  // Face right
            transform.localScale = scale;
        }
    }

    public void Shoot()
    {
        if (player == null) return;
        Vector2 direction = (player.position - firePoint.position).normalized;
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        var projectile = proj.GetComponent<EnemyProjectile>();
        if (projectile != null)
            projectile.SetDirection(direction);

        if (animator != null)
            animator.SetTrigger("Shoot"); // Optional: trigger shoot animation if needed
    }
}
