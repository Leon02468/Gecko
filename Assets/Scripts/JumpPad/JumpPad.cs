using UnityEngine;

public class JumpPad : MonoBehaviour
{
    private float bounce = 20f;
    private Animator animator;
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<Rigidbody2D>().AddForce(Vector2.up * bounce, ForceMode2D.Impulse);
            animator.SetTrigger("Bounce");
            if (GameManager.Instance.AudioInstance != null)
            {
                GameManager.Instance.AudioInstance.PlayJumpPad();
            }
        }
    }
}
