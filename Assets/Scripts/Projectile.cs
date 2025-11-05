using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour, IProjectile
{
    public int damage = 1;
    public float lifetime = 6f;
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(int damage, Vector2 velocity)
    {
        this.damage = damage;
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col == null) return;
        // attempt to damage an IDamageable on the hit object or its parents
        var dmg = col.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            Vector2 kb = rb != null ? (rb.linearVelocity.normalized * 2f) : Vector2.zero;
            dmg.TakeDamage(damage, kb);
            Destroy(gameObject);
            return;
        }

        // optional: destroy on hitting world geometry (layer-based check could be added)
    }
}
