using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPC : MonoBehaviour, IInteractable
{
    public NPCDialogue dialogueData;
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public Image portraitImage;

    int dialogueIndex;
    bool isTyping, isDialogueActive;
    PlayerMovement playerMovement;

    public ShopUI shopUI;
    bool shopOpen = false;
    bool dialogueFinished = false;

    void Awake()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) playerMovement = playerObj.GetComponent<PlayerMovement>();
    }

    public bool CanInteract() => true;

    public void Interact()
    {
        // -----------------------------------------
        // 1. If dialogue is happening Å® continue it
        // -----------------------------------------
        if (isDialogueActive)
        {
            NextLine();
            return;
        }

        // ------------------------------------------------
        // 2. If dialogue is done Å® toggle shop open/close
        // ------------------------------------------------
        if (dialogueFinished)
        {
            ToggleShop();
            return;
        }

        // -----------------------------------------
        // 3. Otherwise Å® start dialogue normally
        // -----------------------------------------
        StartDialogue();
    }

    void ToggleShop()
    {
        if (shopUI == null)
            return;

        if (!shopOpen)
        {
            shopUI.OpenShop();
            shopOpen = true;
        }
        else
        {
            shopUI.CloseShop();
            shopOpen = false;
        }
    }

    void StartDialogue()
    {
        isDialogueActive = true;
        dialogueIndex = 0;

        nameText.SetText(dialogueData.npcName);
        portraitImage.sprite = dialogueData.npcSprite;
        dialoguePanel.SetActive(true);

        if (playerMovement)
        {
            playerMovement.canMove = false; //use flag to disable movement
            if (playerMovement.rb != null)
                playerMovement.rb.linearVelocity = Vector2.zero;
        }

        StartCoroutine(TypeLine());
    }

    void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.SetText(dialogueData.dialogueLines[dialogueIndex]);
            isTyping = false;
            return;
        }

        if (++dialogueIndex < dialogueData.dialogueLines.Length)
        {
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.SetText("");

        foreach (char letter in dialogueData.dialogueLines[dialogueIndex])
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(dialogueData.typingSpeed);
        }

        isTyping = false;

        if (dialogueData.autoProgressLines.Length > dialogueIndex &&
            dialogueData.autoProgressLines[dialogueIndex])
        {
            yield return new WaitForSeconds(dialogueData.autoProgressDelay);
            NextLine();
        }
    }

    public void EndDialogue()
    {
        StopAllCoroutines();
        isDialogueActive = false;
        dialogueText.SetText("");
        dialoguePanel.SetActive(false);

        dialogueFinished = true;  // Shop mode activates after this

        if (playerMovement) playerMovement.canMove = true; // re-enable movement after close shop
    }
}
