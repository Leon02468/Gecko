using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public GameObject interactBtn;
    public float interactRange = 1.5f;
    public LayerMask interactLayer;
    private IInteractable currentInteractable;
    private GameObject lastDetectedObject; // Track the last detected interactable

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
            
            // Only log when a NEW interactable is detected (optional debug)
            if (hit.gameObject != lastDetectedObject)
            {
                lastDetectedObject = hit.gameObject;
            }
        }
        else
        {
            currentInteractable = null;
            
            // Clear the last detected object when no interactable is in range
            if (lastDetectedObject != null)
            {
                lastDetectedObject = null;
            }
        }

        if (currentInteractable != null) interactBtn.SetActive(true);
        else interactBtn.SetActive(false);
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed && currentInteractable != null && currentInteractable.CanInteract())
        {
            // Keep the interaction log as it's useful for debugging interactions
            Debug.Log("Interacting with: " + ((MonoBehaviour)currentInteractable).name);
            currentInteractable.Interact();
        }
    }
}
