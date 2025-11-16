using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class VineProjectile : MonoBehaviour
{
    public float speed = 6f;
    public float life = 4f;
    public float damage = 1f;
    public float knockback = 6f;
    Rigidbody2D rb;
        
    void Awake() => rb = GetComponent<Rigidbody2D>();

    public void Launch(float angleDegrees)
    {
        float r = angleDegrees * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        rb.linearVelocity = dir.normalized * speed;
        Destroy(gameObject, life);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            var ph = other.GetComponent<PlayerHealth>();
            ph?.TakeDamage(damage, (ph.transform.position - transform.position).normalized * knockback, null);
            Debug.Log("VineProjectile hit player for " + damage + " damage.");
            Destroy(gameObject);
            Debug.Log("VineProjectile destroyed after hitting player.");
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
            Debug.Log("VineProjectile destroyed after hitting ground.");
        }
    }
}
