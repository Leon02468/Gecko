using UnityEngine;

public class InteractableObj : MonoBehaviour, IInteractable
{
    public MantisBossController boss;
    public BoxCollider2D boxCollider;

    void Awake()
    {
        if(boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }
    }

    public bool CanInteract() => true;

    public void Interact()
    {
        Debug.Log("Interacted with Mantis Boss Intro Trigger");
        boss.StartIntro();
        boxCollider.enabled = false;
    }
}
