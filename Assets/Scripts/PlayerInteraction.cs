using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public GameObject interactBtn;
    public float interactRange = 1.5f;
    public LayerMask interactLayer;
    private IInteractable currentInteractable;

    private void Start()
    {
        interactBtn.SetActive(false);
    }

    void Update()
    {
        // Detect interactable in front of player (simple circle check)
        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRange, interactLayer);
        if (hit != null)
        {
            currentInteractable = hit.GetComponent<IInteractable>();
            Debug.Log("Interactable found: " + hit.name);
        }
        else
        {
            currentInteractable = null;
        }

        if (currentInteractable != null) interactBtn.SetActive(true);
        else interactBtn.SetActive(false);
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && currentInteractable != null && currentInteractable.CanInteract())
        {
            currentInteractable.Interact();
        }
    }
}
