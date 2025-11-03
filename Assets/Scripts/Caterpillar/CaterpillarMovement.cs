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

  
   
    private float leftLimit;
    private float rightLimit;

    private int currentDirection;

    private float halfWidth;

    private Vector2 movement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        leftLimit = transform.position.x - 5;
        rightLimit = transform.position.x + 5;
        halfWidth = spriteRenderer.bounds.extents.x;
        currentDirection = startDirection;
    }

    // Update is called once per frame
    private void Update()
    {
        SetDirection();
        movement.x = speed * currentDirection;
        movement.y = rigidBody.linearVelocity.y;
        rigidBody.linearVelocity = movement;
    }

    private void SetDirection()
    {
        bool hitGroundRight = Physics2D.Raycast(transform.position, Vector2.right, halfWidth + 0.1f, LayerMask.GetMask("Ground"));
        bool hitGroundLeft = Physics2D.Raycast(transform.position, Vector2.left, halfWidth + 0.1f, LayerMask.GetMask("Ground"));

        // Check both ground collision and position limits for right movement
        if ((hitGroundRight || transform.position.x >= rightLimit) && currentDirection > 0)
        {
            currentDirection = -1;
            spriteRenderer.flipX = true;
        }
        // Check both ground collision and position limits for left movement
        else if ((hitGroundLeft || transform.position.x <= leftLimit) && currentDirection < 0)
        {
            currentDirection = 1;
            spriteRenderer.flipX = false;
        }


    }
}
