using UnityEngine;

public class InteractableObj : MonoBehaviour, IInteractable
{
    public MantisBossController boss;
    public BoxCollider2D boxCollider;
    public GameObject bossArenaMusic;

    void Awake()
    {
        bossArenaMusic.SetActive(false);
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
        bossArenaMusic?.SetActive(true);
    }
}
