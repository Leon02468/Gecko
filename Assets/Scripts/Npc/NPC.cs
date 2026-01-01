using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class NPC : MonoBehaviour, IInteractable
{
    public NPCDialogue dialogueData;
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public Image portraitImage;

    int dialogueIndex;
    bool isTyping, isDialogueActive;
    PlayerMovement playerMovement;

    [Header("Post-Dialogue Options")]
    public ShopUI shopUI;
    public MissionManager missionManager;
    public GameObject choicePanelPrefab; // Assign your OptionCanvas prefab here
    
    private GameObject activeChoicePanel;
    private bool dialogueFinished = false;
    private bool shopOrMissionOpen = false; // Track if shop or mission is currently open
    private bool isGrantingReward = false; // Prevent reward spam

    private AudioSource audioSource;
    private Coroutine typeLineCoroutine;

    void Awake()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) playerMovement = playerObj.GetComponent<PlayerMovement>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Auto-assign MissionManager if not set
        if (missionManager == null)
        {
            missionManager = MissionManager.Instance;
            if (missionManager == null)
                missionManager = FindObjectOfType<MissionManager>();
        }

        // Ensure EventSystem exists
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // IMPORTANT: Ensure dialogueFinished starts as false
        dialogueFinished = false;
        
        // IMPORTANT: Deactivate choice panel prefab at start
        if (choicePanelPrefab != null && choicePanelPrefab.activeSelf)
        {
            choicePanelPrefab.SetActive(false);
            Debug.Log("NPC: Choice panel prefab deactivated at start");
        }
    }

    void Update()
    {
        // Handle ESC key press
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapeKey();
        }
    }

    void HandleEscapeKey()
    {
        Debug.Log("NPC: ESC key pressed");

        // Priority 1: If shop or missions are open, close them and show choice panel
        if (shopOrMissionOpen)
        {
            CloseShopAndMissions();
            return;
        }

        // Priority 2: If choice panel is open, close it and re-enable movement
        if (activeChoicePanel != null)
        {
            Debug.Log("NPC: Closing choice panel via ESC");
            CloseChoicePanel();
            
            // Re-enable movement so player can walk away
            if (playerMovement)
                playerMovement.canMove = true;
            
            return;
        }
    }

    // IMPORTANT: Block interaction when UI is open
    public bool CanInteract()
    {
        // Don't allow interaction if reward is being granted
        if (isGrantingReward)
        {
            Debug.Log("NPC: Cannot interact - reward is being granted");
            return false;
        }

        // Don't allow interaction if choice panel is open
        if (activeChoicePanel != null)
        {
            Debug.Log("NPC: Cannot interact - choice panel is open");
            return false;
        }

        // Don't allow interaction if shop is open
        if (shopUI != null && shopUI.shopPanel != null && shopUI.shopPanel.activeSelf)
        {
            Debug.Log("NPC: Cannot interact - shop is open");
            return false;
        }

        // Don't allow interaction if missions are open
        var mm = missionManager != null ? missionManager : MissionManager.Instance;
        if (mm != null && mm.missionCanvas != null && mm.missionCanvas.activeSelf)
        {
            Debug.Log("NPC: Cannot interact - missions are open");
            return false;
        }

        // Allow interaction otherwise
        return true;
    }

    public void Interact()
    {
        Debug.Log($"NPC Interact called - isDialogueActive: {isDialogueActive}, dialogueFinished: {dialogueFinished}, isGrantingReward: {isGrantingReward}");

        // Skip interaction if reward is being granted
        if (isGrantingReward)
        {
            Debug.Log("[NPC] Reward is being granted, ignoring interaction");
            return;
        }

        // -----------------------------------------
        // 0. CHECK FOR COMPLETED MISSIONS FIRST - ALWAYS
        // -----------------------------------------
        var mm = missionManager != null ? missionManager : MissionManager.Instance;
        if (mm != null)
        {
            var completedMission = mm.allMissions.Find(m => m.status == Mission.MissionStatus.Completed);
            if (completedMission != null)
            {
                Debug.Log($"[NPC] Found completed mission: {completedMission.id}. Auto-claiming reward!");
                
                // Set flag IMMEDIATELY to prevent spam
                isGrantingReward = true;
                
                // Reset dialogue states FIRST
                if (typeLineCoroutine != null)
                {
                    StopCoroutine(typeLineCoroutine);
                    typeLineCoroutine = null;
                }
                isDialogueActive = false;
                isTyping = false;
                dialogueFinished = false;
                
                // Close any open panels
                CloseChoicePanel();
                if (dialoguePanel != null)
                    dialoguePanel.SetActive(false);
                
                // Grant reward immediately
                mm.GrantReward(completedMission, this);
                return; // Exit early, reward dialogue will be shown
            }
        }

        // -----------------------------------------
        // 1. If dialogue is happening ? continue it
        // -----------------------------------------
        if (isDialogueActive)
        {
            NextLine();
            return;
        }

        // ------------------------------------------------
        // 2. If dialogue is done ? show choice panel
        // ------------------------------------------------
        if (dialogueFinished)
        {
            ShowChoicePanel();
            return;
        }

        // -----------------------------------------
        // 3. Otherwise ? start dialogue normally
        // -----------------------------------------
        StartDialogue();
    }

    void ShowChoicePanel()
    {
        if (activeChoicePanel != null)
        {
            Debug.Log("NPC: Choice panel already active");
            return;
        }

        Debug.Log("NPC: Showing choice popup AFTER dialogue");

        if (choicePanelPrefab != null)
        {
            // Instantiate the choice panel
            activeChoicePanel = Instantiate(choicePanelPrefab);
            activeChoicePanel.name = "NPC_ChoicePanel_Runtime";
            activeChoicePanel.SetActive(true); // Explicitly activate it
            DontDestroyOnLoad(activeChoicePanel);

            // Ensure Canvas is on top
            var canvas = activeChoicePanel.GetComponentInChildren<Canvas>();
            if (canvas != null) canvas.sortingOrder = 1000;

            // Check if it has NPCChoicePanel component
            var choicePanel = activeChoicePanel.GetComponent<NPCChoicePanel>();
            if (choicePanel != null)
            {
                // Initialize using the NPCChoicePanel component (preferred method)
                choicePanel.Initialize(this);
                Debug.Log("NPC: Using NPCChoicePanel component for button wiring");
            }
            else
            {
                // Fallback: manual button wiring
                Debug.Log("NPC: No NPCChoicePanel component found, using fallback button wiring");
                WireButtonsManually();
            }

            // Clear EventSystem selection to prevent auto-submit
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                StartCoroutine(ClearSelectionNextFrame());
            }

            // Disable movement while choice is shown
            if (playerMovement)
                playerMovement.canMove = false;
        }
        else
        {
            Debug.LogWarning("NPC: No choicePanelPrefab assigned. Cannot show choice.");
        }
    }

    IEnumerator ClearSelectionNextFrame()
    {
        // Wait one frame then clear selection again to ensure no auto submit
        yield return null;
        if (EventSystem.current != null) 
            EventSystem.current.SetSelectedGameObject(null);
    }

    void WireButtonsManually()
    {
        // Fallback: search for buttons by name
        var allButtons = activeChoicePanel.GetComponentsInChildren<Button>(true);
        
        foreach (var btn in allButtons)
        {
            string btnName = btn.gameObject.name.ToLower();
            
            if (btnName.Contains("shop"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnShopSelected);
                Debug.Log("NPC: Shop button wired (fallback)");
            }
            else if (btnName.Contains("mission"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnMissionsSelected);
                Debug.Log("NPC: Missions button wired (fallback)");
            }
        }
    }

    // Called when player clicks Shop button
    public void OnShopSelected()
    {
        Debug.Log("NPC: Shop selected!");
        CloseChoicePanel(); // Close choice panel when shop opens

        if (shopUI != null)
        {
            // Close missions if open
            var mm = missionManager != null ? missionManager : MissionManager.Instance;
            if (mm != null && mm.missionCanvas != null && mm.missionCanvas.activeSelf)
                mm.HideMissionUI();

            shopUI.OpenShop();
            shopOrMissionOpen = true; // Mark shop as open
            // Movement stays disabled while shop is open
            
            // Play shop toggle sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayShopToggle();
        }
        else
        {
            Debug.LogWarning("NPC: ShopUI not assigned!");
            // Re-enable movement if shop can't open
            if (playerMovement)
                playerMovement.canMove = true;
        }
    }

    // Called when player clicks Missions button
    public void OnMissionsSelected()
    {
        Debug.Log("NPC: Missions selected!");
        CloseChoicePanel(); // Close choice panel when missions open

        var mm = missionManager != null ? missionManager : MissionManager.Instance;
        if (mm != null)
        {
            // Close shop if it's open
            if (shopUI != null && shopUI.shopPanel != null && shopUI.shopPanel.activeSelf)
                shopUI.CloseShop();

            mm.ShowMissionUI();
            shopOrMissionOpen = true; // Mark missions as open
            // Movement stays disabled while mission UI is open
        }
        else
        {
            Debug.LogWarning("NPC: MissionManager not found!");
            // Re-enable movement if missions can't open
            if (playerMovement)
                playerMovement.canMove = true;
        }
    }

    void CloseShopAndMissions()
    {
        Debug.Log("NPC: Closing shop and missions (ESC pressed)");

        bool somethingClosed = false;

        // Close shop if open
        if (shopUI != null && shopUI.shopPanel != null && shopUI.shopPanel.activeSelf)
        {
            shopUI.CloseShop();
            somethingClosed = true;
            Debug.Log("NPC: Shop closed via ESC");
        }

        // Close missions if open
        var mm = missionManager != null ? missionManager : MissionManager.Instance;
        if (mm != null && mm.missionCanvas != null && mm.missionCanvas.activeSelf)
        {
            mm.HideMissionUI();
            somethingClosed = true;
            Debug.Log("NPC: Missions closed via ESC");
        }

        if (somethingClosed)
        {
            shopOrMissionOpen = false;
            
            // Show choice panel again after closing shop/missions
            if (dialogueFinished)
            {
                ShowChoicePanel();
            }
        }
    }

    void CloseChoicePanel()
    {
        if (activeChoicePanel != null)
        {
            Destroy(activeChoicePanel);
            activeChoicePanel = null;
            Debug.Log("NPC: Choice panel closed");
        }
    }

    void StartDialogue()
    {
        Debug.Log("NPC: Starting dialogue");

        // Safety check: make sure we have dialogue data
        if (dialogueData == null || dialogueData.dialogueLines == null || dialogueData.dialogueLines.Length == 0)
        {
            Debug.LogError("NPC: No dialogue data or empty dialogue lines! Skipping to choice panel.");
            dialogueFinished = true;
            // Automatically show choice panel
            ShowChoicePanel();
            return;
        }

        isDialogueActive = true;
        dialogueIndex = 0;

        nameText.SetText(dialogueData.npcName);
        portraitImage.sprite = dialogueData.npcSprite;
        dialoguePanel.SetActive(true);

        if (playerMovement)
        {
            playerMovement.canMove = false;
        }

        typeLineCoroutine = StartCoroutine(TypeLine());
    }

    void NextLine()
    {
        if (isTyping)
        {
            if (typeLineCoroutine != null)
            {
                StopCoroutine(typeLineCoroutine);
                typeLineCoroutine = null;
            }

            dialogueText.SetText(dialogueData.dialogueLines[dialogueIndex]);
            isTyping = false;
            return;
        }

        if (++dialogueIndex < dialogueData.dialogueLines.Length)
        {
            typeLineCoroutine = StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();

        isTyping = true;
        dialogueText.SetText("");

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
        typeLineCoroutine = null;

        if (dialogueData.autoProgressLines.Length > dialogueIndex &&
            dialogueData.autoProgressLines[dialogueIndex])
        {
            yield return new WaitForSeconds(dialogueData.autoProgressDelay);
            NextLine();
        }
    }

    public void EndDialogue()
    {
        Debug.Log("NPC: Ending dialogue - setting dialogueFinished = true");

        if (typeLineCoroutine != null)
        {
            StopCoroutine(typeLineCoroutine);
            typeLineCoroutine = null;
        }

        isDialogueActive = false;
        isTyping = false;
        dialogueText.SetText("");
        dialoguePanel.SetActive(false);

        dialogueFinished = true;

        // Automatically show choice panel after dialogue ends
        Debug.Log("NPC: Auto-showing choice panel after dialogue");
        ShowChoicePanel();
    }

    public void ResetDialogue()
    {
        Debug.Log("NPC: Resetting dialogue");
        dialogueFinished = false;
        dialogueIndex = 0;
    }

    /// <summary>
    /// Show a reward message in the dialogue panel
    /// Called by MissionManager when claiming rewards
    /// </summary>
    public void ShowRewardMessage(string message)
    {
        Debug.Log($"[NPC] ShowRewardMessage called: {message}");

        // Stop any ongoing dialogue first
        if (typeLineCoroutine != null)
        {
            StopCoroutine(typeLineCoroutine);
            typeLineCoroutine = null;
        }

        // Reset all dialogue flags
        isDialogueActive = false;
        isTyping = false;

        // Show dialogue panel
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        // Set NPC info if available
        if (dialogueData != null)
        {
            if (nameText != null)
                nameText.SetText(dialogueData.npcName);
            if (portraitImage != null)
                portraitImage.sprite = dialogueData.npcSprite;
        }

        // Disable player movement during reward message
        if (playerMovement)
        {
            playerMovement.canMove = false;
        }

        // Show message with typing effect
        if (dialogueText != null)
        {
            typeLineCoroutine = StartCoroutine(TypeRewardMessage(message));
        }
    }

    private IEnumerator TypeRewardMessage(string message)
    {
        Debug.Log("[NPC] TypeRewardMessage started");
        
        isTyping = true;
        dialogueText.SetText("");

        // Stop any playing audio
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        // Play voice sound if available
        if (audioSource != null && dialogueData != null && dialogueData.voiceSounds != null && dialogueData.voiceSounds.Length > 0)
        {
            audioSource.pitch = 1f;
            audioSource.clip = dialogueData.voiceSounds[0];
            audioSource.Play();
        }

        // Type out the message
        foreach (char letter in message)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(dialogueData != null ? dialogueData.typingSpeed : 0.05f);
        }

        isTyping = false;
        Debug.Log("[NPC] Reward message typing complete");

        // Auto-hide after 2 seconds
        yield return new WaitForSeconds(2f);
        
        Debug.Log("[NPC] Hiding reward dialogue");
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            dialogueText.SetText("");
        }

        // Re-enable player movement
        if (playerMovement)
        {
            playerMovement.canMove = true;
        }

        // IMPORTANT: Reset flag to allow future reward claims
        isGrantingReward = false;
        Debug.Log("[NPC] Reset isGrantingReward flag - ready for next interaction");

        typeLineCoroutine = null;
        
        Debug.Log("[NPC] Reward message complete");
    }

    void OnDisable()
    {
        if (typeLineCoroutine != null)
        {
            StopCoroutine(typeLineCoroutine);
            typeLineCoroutine = null;
        }

        CloseChoicePanel();

        // Re-enable movement if it was disabled by this NPC
        if (playerMovement && (isDialogueActive || activeChoicePanel != null))
            playerMovement.canMove = true;
    }
}
