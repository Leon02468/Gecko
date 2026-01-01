using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for individual mission items in the mission list
/// </summary>
public class MissionItemUI : MonoBehaviour
{
    public Mission mission;
    private MissionManager missionManager;

    [Header("UI Elements")]
    public Image missionImage; // optional image on left
    public TextMeshProUGUI paragraphText; // single paragraph combining mission description/enemy/reward

    // Single icon button that alternates between accept and deny
    public Button buttonIcon;
    public Image buttonIconImage; // image component on the button
    public Sprite acceptSprite;
    public Sprite denySprite;

    // Auto-resize component
    private AutoSizeMissionItem autoSize;

    void Awake()
    {
        // Get auto-size component if present
        autoSize = GetComponent<AutoSizeMissionItem>();
    }

    void Start()
    {
        if (buttonIcon != null)
        {
            // ensure we have the image reference
            if (buttonIconImage == null)
                buttonIconImage = buttonIcon.GetComponent<Image>();

            // clear and set unified handler
            buttonIcon.onClick.RemoveAllListeners();
            buttonIcon.onClick.AddListener(OnButtonClicked);
        }
    }

    /// <summary>
    /// Sets up the mission item with data
    /// </summary>
    public void Setup(Mission missionData, MissionManager manager)
    {
        mission = missionData;
        missionManager = manager;
        
        // DEBUG: Log mission data when setting up
        if (mission != null)
        {
            Debug.Log($"[MissionItemUI] Setup called for mission '{mission.id}':");
            Debug.Log($"  - Description: {mission.description}");
            Debug.Log($"  - Progress: {mission.currentCount}/{mission.targetCount}");
            Debug.Log($"  - EnemyType: {(mission.enemyType != null ? mission.enemyType.displayName : "NULL")}");
            Debug.Log($"  - RewardType: {mission.rewardType}");
            Debug.Log($"  - RewardAmount: {mission.rewardAmount}");
        }
        else
        {
            Debug.LogError("[MissionItemUI] Setup called with NULL mission data!");
        }
        
        UpdateDisplay();
    }

    /// <summary>
    /// Updates the display based on current mission data
    /// </summary>
    public void UpdateDisplay()
    {
        if (mission == null)
        {
            Debug.LogError("[MissionItemUI] UpdateDisplay called but mission is NULL!");
            if (paragraphText != null)
                paragraphText.text = "[ERROR: No mission data]";
            return;
        }

        // Show enemy sprite from Mission.enemyType if available
        if (missionImage != null)
        {
            Sprite s = mission.enemyType != null ? mission.enemyType.sprite : null;
            missionImage.sprite = s;
            missionImage.gameObject.SetActive(s != null);
            
            if (s == null)
                Debug.LogWarning($"[MissionItemUI] Mission '{mission.id}' has no enemy sprite!");
        }

        if (paragraphText != null)
        {
            string formattedText = FormatParagraph(mission);
            paragraphText.text = formattedText;
            Debug.Log($"[MissionItemUI] Updated text for mission '{mission.id}': {formattedText}");
            
            // IMPORTANT: Trigger auto-resize after text changes
            if (autoSize != null)
            {
                // Delay resize by one frame to ensure text is fully updated
                StartCoroutine(DelayedResize());
            }
        }
        else
        {
            Debug.LogError("[MissionItemUI] paragraphText is NULL! Cannot display mission text.");
        }

        // Update button based on mission status
        if (buttonIcon != null && buttonIconImage != null)
        {
            switch (mission.status)
            {
                case Mission.MissionStatus.Available:
                    // Show Accept icon (green checkmark)
                    buttonIcon.gameObject.SetActive(true);
                    if (acceptSprite != null) buttonIconImage.sprite = acceptSprite;
                    break;

                case Mission.MissionStatus.Active:
                    // Show Deny/Cancel icon (red X)
                    buttonIcon.gameObject.SetActive(true);
                    if (denySprite != null) buttonIconImage.sprite = denySprite;
                    break;

                case Mission.MissionStatus.Completed:
                    // Show Claim button (green checkmark) - player must click to claim
                    buttonIcon.gameObject.SetActive(true);
                    if (acceptSprite != null) buttonIconImage.sprite = acceptSprite;
                    Debug.Log($"[MissionItemUI] Mission '{mission.id}' completed - showing Claim button");
                    break;

                case Mission.MissionStatus.Claimed:
                    // Hide button after claiming
                    buttonIcon.gameObject.SetActive(false);
                    break;
            }
        }

        transform.localScale = Vector3.one;
    }

    private System.Collections.IEnumerator DelayedResize()
    {
        // Wait for text to fully update
        yield return null;
        
        if (autoSize != null)
        {
            autoSize.UpdateLayout();
            Debug.Log($"[MissionItemUI] Auto-resized mission item '{mission.id}'");
        }
    }

    /// <summary>
    /// Called from MissionManager to explicitly set the enemy sprite
    /// </summary>
    public void SetEnemySprite(Sprite s)
    {
        if (missionImage != null)
        {
            missionImage.sprite = s;
            missionImage.gameObject.SetActive(s != null);
        }
    }

    private string FormatParagraph(Mission m)
    {
        if (m == null)
        {
            Debug.LogError("[MissionItemUI] FormatParagraph called with NULL mission!");
            return "[ERROR: No mission]";
        }
        
        string enemyName = m.enemyType != null ? m.enemyType.displayName : "[No Enemy]";
        
        Debug.Log($"[MissionItemUI] Formatting mission '{m.id}':");
        Debug.Log($"  - Enemy: {enemyName}");
        Debug.Log($"  - Progress: {m.currentCount}/{m.targetCount}");
        Debug.Log($"  - Status: {m.status}");

        // Build reward description depending on structured reward fields
        string rewardDesc = "None"; // default

        if (m.rewardType == Mission.RewardType.Leaf)
        {
            rewardDesc = $"Leaf x{m.rewardAmount}";
        }
        else if (m.rewardType == Mission.RewardType.Cherries)
        {
            string itemName = m.rewardItem != null ? m.rewardItem.itemName : "Cherries";
            rewardDesc = $"{itemName} x{m.rewardAmount}";
        }
        else if (m.rewardType == Mission.RewardType.Item)
        {
            string itemName = m.rewardItem != null ? m.rewardItem.itemName : "Item";
            rewardDesc = $"{itemName} x{m.rewardAmount}";
        }
        
        Debug.Log($"  - Reward: {rewardDesc}");

        // Add status indicator to text
        string statusText = "";
        string progressText = "";
        
        switch (m.status)
        {
            case Mission.MissionStatus.Available:
                statusText = "[Available]";
                // Show saved progress if exists
                if (m.savedProgress > 0)
                {
                    progressText = $"{m.savedProgress}/{m.targetCount} (Saved)";
                }
                else
                {
                    progressText = $"0/{m.targetCount}";
                }
                break;
            case Mission.MissionStatus.Active:
                statusText = "[ACTIVE]";
                progressText = $"{m.currentCount}/{m.targetCount}";
                break;
            case Mission.MissionStatus.Completed:
                statusText = "[Completed!]";
                progressText = $"{m.targetCount}/{m.targetCount}";
                break;
            case Mission.MissionStatus.Claimed:
                statusText = "[Claimed]";
                progressText = $"{m.targetCount}/{m.targetCount}";
                break;
        }

        string result = $"{statusText} {m.description} • {progressText} • Enemy: {enemyName} • Reward: {rewardDesc}";
        Debug.Log($"  - Final text: {result}");
        
        return result;
    }

    private void OnButtonClicked()
    {
        if (mission == null || missionManager == null) return;

        switch (mission.status)
        {
            case Mission.MissionStatus.Available:
                // Accept the mission
                Debug.Log($"[MissionItemUI] Attempting to accept mission '{mission.id}'");
                bool accepted = missionManager.AcceptMission(mission.id);
                if (!accepted)
                {
                    Debug.LogWarning($"[MissionItemUI] Failed to accept mission '{mission.id}' - probably another mission is active");
                    // TODO: Show "Already have an active mission" popup
                }
                break;

            case Mission.MissionStatus.Active:
                // Cancel/Deny the mission (saves progress)
                Debug.Log($"[MissionItemUI] Cancelling active mission '{mission.id}'");
                missionManager.DenyMission(mission.id);
                break;

            case Mission.MissionStatus.Completed:
                // Claim reward - find NPC and show dialogue
                Debug.Log($"[MissionItemUI] Claiming reward for mission '{mission.id}'");
                NPC npc = FindNearestNPC();
                if (npc != null)
                {
                    // Grant reward with NPC dialogue
                    missionManager.GrantReward(mission, npc);
                }
                else
                {
                    Debug.LogWarning($"[MissionItemUI] No NPC found! Please talk to an NPC to claim reward.");
                    // Still grant reward even if no NPC found
                    missionManager.GrantReward(mission, null);
                }
                break;

            case Mission.MissionStatus.Claimed:
                // Do nothing - mission already claimed
                Debug.Log($"[MissionItemUI] Mission '{mission.id}' already claimed");
                break;
        }

        UpdateDisplay();
    }

    /// <summary>
    /// Try to find the nearest NPC for reward dialogue
    /// </summary>
    private NPC FindNearestNPC()
    {
        // Try to find NPC that's currently interacting with player
        NPC[] npcs = FindObjectsOfType<NPC>();
        
        if (npcs.Length == 0)
            return null;

        // Return first NPC (could be enhanced to find closest one)
        return npcs[0];
    }
}