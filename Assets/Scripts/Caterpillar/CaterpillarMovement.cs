using Unity.VisualScripting;
using UnityEngine;

public class CaterpillarMovement : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D rigidBody;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private float speed = 3f;

    [SerializeField]
    private int startDirection = 1;

    private int currentDirection;

    private float halfWidth;

    private Vector2 movement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        halfWidth = spriteRenderer.bounds.extents.x;
        currentDirection = startDirection;
    }

    // Update is called once per frame
    private void Update()
    {
        movement.x = speed * currentDirection;
        movement.y = rigidBody.linearVelocity.y;
        rigidBody.linearVelocity = movement;
        SetDirection();
    }

    private void SetDirection()
    {
        if (Physics2D.Raycast(transform.position, Vector2.right, halfWidth + 0.1f, LayerMask.GetMask("Ground")) 
            && rigidBody.linearVelocity.x > 0)
        {
            currentDirection *= -1;
            spriteRenderer.flipX = true;
        }
        else if (Physics2D.Raycast(transform.position, Vector2.left, halfWidth + 0.1f, LayerMask.GetMask("Ground"))
            && rigidBody.linearVelocity.x < 0)
        {
            currentDirection *= -1;
            spriteRenderer.flipX = false;
        }
    }
}
