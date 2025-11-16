using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public float interactRange = 1.5f;
    public LayerMask interactLayer;
    private IInteractable currentInteractable;

    void Update()
    {
        // Detect interactable in front of player (simple circle check)
        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRange, interactLayer);
        if (hit != null)
        {
            currentInteractable = hit.GetComponent<IInteractable>();
        }
        else
        {
            currentInteractable = null;
        }

        // Optional: show UI prompt if currentInteractable != null
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && currentInteractable != null && currentInteractable.CanInteract())
        {
            currentInteractable.Interact();
        }
    }
}
