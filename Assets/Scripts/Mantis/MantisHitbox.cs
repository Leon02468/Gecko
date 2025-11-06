using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MantisHitbox : MonoBehaviour
{
    public float damage = 1f;
    public float knockback = 6f;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        enabled = false; // default off
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled) return;
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            var ph = other.GetComponent<PlayerHealth>();
            ph?.TakeDamage(damage, (Vector2)transform.position, knockback);
        }
    }
}
