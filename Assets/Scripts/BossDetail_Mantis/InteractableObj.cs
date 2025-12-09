using UnityEngine;

public class InteractableObj : MonoBehaviour, IInteractable
{
    public bool CanInteract() => true;

    public void Interact()
    {
        // Do something
        // In this situation, do the boss intro --> handled by a separate BossIntro script
    }
}
