using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MantisHitbox : MonoBehaviour
{
    public float damage = 1f;
    public float knockback = 6f;

    private void Awake()
    {
        Debug.Log("MantisHitbox Awake");
    }

    private void Update()
    {
        Debug.Log("MantisHitbox Update: " + Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("MantisHitbox triggered by " + other.gameObject.name);
        if (!enabled) return;
        Debug.Log("MantisHitbox enabled");
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            var ph = other.GetComponent<PlayerHealth>();
            ph?.TakeDamage(damage, (ph.transform.position - transform.position).normalized * knockback, null);
        }
    }
}
