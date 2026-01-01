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

    bool shopWasOpened = false;
    bool shopWasClosed = false;

    public void Interact()
    {
        if (isDialogueActive)
        {
            NextLine();
            return;
        }

        if (missionDialogueActive)
        {
            NextMissionLine();
            return;
        }

        // If the shop is open, always close it first before anything else
        if (shopOpen)
        {
            ToggleShop();
            // Mark that the shop was closed for the first time if this is the first close after opening
            if (dialogueFinished && shopWasOpened && !shopWasClosed)
            {
                shopWasClosed = true;
            }
            return;
        }

        // After first dialogue, open shop ONCE
        if (dialogueFinished && !shopWasOpened)
        {
            ToggleShop();
            shopWasOpened = true;
            return;
        }

        // After shop is closed for the first time, start mission dialogue ONCE
        if (dialogueFinished && shopWasOpened && shopWasClosed && !missionDialoguePlayed)
        {
            StartMissionDialogue();
            return;
        }

        // After mission dialogue, allow normal shop toggling (mission dialogue will not play again)
        if (dialogueFinished && shopWasOpened && shopWasClosed && missionDialoguePlayed)
        {
            ToggleShop();
            return;
        }

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
            // Set volume from AudioManager before playing/stopping
            audioSource.volume = AudioManager.GlobalSFXVolume;
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

    //Mission part
    bool missionDialogueActive = false;
    int missionDialogueIndex = 0;
    bool missionDialoguePlayed = false; // <-- Add this flag

    public void StartMissionDialogue()
    {
        // Only allow mission dialogue to play once
        if (missionDialoguePlayed)
            return;

        if (dialogueData.missionLines == null || dialogueData.missionLines.Length == 0)
            return;

        missionDialogueActive = true;
        missionDialogueIndex = 0;
        nameText.SetText(dialogueData.npcName);
        portraitImage.sprite = dialogueData.npcSprite;
        dialoguePanel.SetActive(true);

        if (playerMovement)
            playerMovement.canMove = false;

        missionDialoguePlayed = true; // <-- Set flag so it can't play again
        StartCoroutine(TypeMissionLine());
    }
    IEnumerator TypeMissionLine()
    {
        isTyping = true;
        dialogueText.SetText("");

        AudioClip voiceClip = null;
        float pitch = 1f;

        if (dialogueData.missionVoiceSounds != null && missionDialogueIndex < dialogueData.missionVoiceSounds.Length)
            voiceClip = dialogueData.missionVoiceSounds[missionDialogueIndex];

        if (voiceClip != null)
        {
            audioSource.volume = AudioManager.GlobalSFXVolume;
            audioSource.pitch = pitch;
            audioSource.clip = voiceClip;
            audioSource.Play();
        }

        foreach (char letter in dialogueData.missionLines[missionDialogueIndex])
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(dialogueData.typingSpeed);
        }

        isTyping = false;
    }

    void NextMissionLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.SetText(dialogueData.missionLines[missionDialogueIndex]);
            isTyping = false;
            return;
        }

        if (++missionDialogueIndex < dialogueData.missionLines.Length)
        {
            StartCoroutine(TypeMissionLine());
        }
        else
        {
            EndMissionDialogue();
        }
    }

    void EndMissionDialogue()
    {
        StopAllCoroutines();
        missionDialogueActive = false;
        dialogueText.SetText("");
        dialoguePanel.SetActive(false);

        if (playerMovement) playerMovement.canMove = true;
    }

}
