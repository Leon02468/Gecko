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

    private AudioSource audioSource;


    void Awake()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) playerMovement = playerObj.GetComponent<PlayerMovement>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
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
            AudioManager.Instance?.PlayShopMusic(); //Play shop music
        }
        else
        {
            shopUI.CloseShop();
            shopOpen = false;
            AudioManager.Instance?.RestorePreviousMusic(); // Restore previous music
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
        // stop previous voice when advancing
        if (audioSource.isPlaying)
            audioSource.Stop();

        isTyping = true;
        dialogueText.SetText("");

        // Play the full voice clip for this sentence
        AudioClip voiceClip = null;
        float pitch = 1f;

        if (dialogueData.voiceSounds != null && dialogueIndex < dialogueData.voiceSounds.Length)
            voiceClip = dialogueData.voiceSounds[dialogueIndex];

        if (voiceClip != null)
        {
            audioSource.pitch = pitch;
            audioSource.clip = voiceClip;
            audioSource.Play();
        }

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
