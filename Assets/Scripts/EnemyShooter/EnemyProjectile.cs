using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1; // 1 heart
    //public float time = 5f;
    private Vector2 moveDirection = Vector2.right; // Default direction

    //private void Start()
    //{
    //    while (time >= 0f)
    //        time -= Time.deltaTime;
    //    Destroy(gameObject);
    //}

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;
    }

    private void Update()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        else if (!collision.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
