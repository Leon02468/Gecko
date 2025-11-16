using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MantisHitbox : MonoBehaviour
{
    public float damage = 1f;
    public float knockback = 6f;


    public void ApplyHit()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;

        Vector2 center = col.bounds.center;
        float radius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y);

        // Prefer OverlapBox for box-like colliders; use OverlapCircle as fallback
        Collider2D hit = Physics2D.OverlapBox(center, col.bounds.size, 0f, LayerMask.GetMask("Player"));
        if (hit == null)
            hit = Physics2D.OverlapCircle(center, radius, LayerMask.GetMask("Player"));

        if (hit != null)
        {
            var ph = hit.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage, (hit.transform.position - transform.position).normalized * knockback, null);
            }
        }
    }
}
