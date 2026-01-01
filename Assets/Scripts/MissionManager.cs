using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple MissionManager: holds a manually editable list of missions (description, target count, enemy type, reward)
/// and populates a ScrollView with MissionItem prefab instances.
/// </summary>
public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject missionCanvas;
    public ScrollRect missionListScrollView;
    public GameObject missionItemPrefab; // Prefab for individual mission items (assign prefab asset)
    public Transform missionListContent; // Content transform of ScrollView

    [Header("Layout Settings")]
    [Tooltip("Spacing between mission items (in pixels)")]
    public float itemSpacing = 5f;
    [Tooltip("Padding: Left")]
    public int paddingLeft = 10;
    [Tooltip("Padding: Right")]
    public int paddingRight = 10;
    [Tooltip("Padding: Top")]
    public int paddingTop = 10;
    [Tooltip("Padding: Bottom")]
    public int paddingBottom = 10;

    [Header("Enemy Types (optional)")]
    public List<EnemyType> availableEnemyTypes = new List<EnemyType>(); // assign ScriptableObjects in inspector

    [Header("Dropdown Options")]
    public List<string> availableDescriptions = new List<string>();
    public List<int> availableTargetCounts = new List<int>() { 1, 5, 10 };
    // Available reward items (use ItemObject assets instead of free-form strings)
    public List<ItemObject> availableRewardItems = new List<ItemObject>();

    [Header("Mission Data")]
    public List<Mission> allMissions = new List<Mission>(); // edit in inspector

    private List<GameObject> missionItemObjects = new List<GameObject>();

    void Awake()
    {
        // Check if this GameObject has a parent
        if (transform.parent != null)
        {
            Debug.LogWarning("[MissionManager] MissionManager is a child object! Moving to root before applying DontDestroyOnLoad.");
            transform.SetParent(null);
        }

        if (Instance == null)
        {
            Instance = this;
            
            // Only apply DontDestroyOnLoad if not already applied
            if (gameObject.scene.name != "DontDestroyOnLoad")
            {
                DontDestroyOnLoad(gameObject);
                Debug.Log("[MissionManager] Applied DontDestroyOnLoad to MissionManager");
            }
            else
            {
                Debug.Log("[MissionManager] GameObject is already in DontDestroyOnLoad scene");
            }
        }
        else
        {
            Debug.LogWarning($"[MissionManager] Duplicate MissionManager detected! Destroying {gameObject.name}");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (missionCanvas != null)
            missionCanvas.SetActive(false);
            
        // Configure ScrollRect for free scrolling
        ConfigureScrollRect();
        
        // Configure Content layout
        ConfigureContentLayout();
    }

    private void ConfigureScrollRect()
    {
        if (missionListScrollView == null)
        {
            Debug.LogWarning("MissionManager: missionListScrollView is null! Cannot configure scroll.");
            return;
        }

        // Ensure free vertical scrolling
        missionListScrollView.horizontal = false;
        missionListScrollView.vertical = true;
        missionListScrollView.movementType = ScrollRect.MovementType.Clamped;
        missionListScrollView.inertia = true;
        missionListScrollView.scrollSensitivity = 20f;
        missionListScrollView.elasticity = 0.1f;
        
        Debug.Log("MissionManager: ScrollRect configured for free scrolling");
    }

    private void ConfigureContentLayout()
    {
        if (missionListContent == null)
        {
            Debug.LogWarning("MissionManager: missionListContent is null! Cannot configure layout.");
            return;
        }

        // Get or add VerticalLayoutGroup
        VerticalLayoutGroup layoutGroup = missionListContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = missionListContent.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        // Create RectOffset at runtime (not in field initializer)
        RectOffset contentPadding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);

        // Configure layout group with minimal spacing
        layoutGroup.spacing = itemSpacing;
        layoutGroup.padding = contentPadding;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = false; // DON'T control child width - let items size themselves
        layoutGroup.childControlHeight = false; // Let items control their own height
        layoutGroup.childForceExpandWidth = false; // DON'T force expand width
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childScaleWidth = false;
        layoutGroup.childScaleHeight = false;

        // Get or add ContentSizeFitter
        ContentSizeFitter sizeFitter = missionListContent.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = missionListContent.gameObject.AddComponent<ContentSizeFitter>();
        }

        // Configure size fitter
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Debug.Log($"MissionManager: Content layout configured - Spacing: {itemSpacing}px, Padding: {paddingTop}/{paddingBottom}, Child Width Control: OFF");
    }

    /// <summary>
    /// Show mission UI and populate list
    /// </summary>
    public void ShowMissionUI()
    {
        if (missionCanvas != null)
            missionCanvas.SetActive(true);

        PopulateMissionList();
    }

    /// <summary>
    /// Hide mission UI
    /// </summary>
    public void HideMissionUI()
    {
        if (missionCanvas != null)
            missionCanvas.SetActive(false);
    }

    private void PopulateMissionList()
    {
        Debug.Log("MissionManager: PopulateMissionList called");

        // Clear existing items
        foreach (var it in missionItemObjects)
        {
            if (it != null)
                Destroy(it);
        }
        missionItemObjects.Clear();

        if (missionItemPrefab == null || missionListContent == null)
        {
            Debug.LogError("MissionManager: missionItemPrefab or missionListContent is null!");
            return;
        }

        // Get RectTransform of content
        RectTransform contentRect = missionListContent as RectTransform;
        if (contentRect == null)
        {
            Debug.LogError("MissionManager: missionListContent is not a RectTransform!");
            return;
        }

        // IMPORTANT: Reset content position to prevent shifting
        contentRect.anchoredPosition = new Vector2(0, 0);
        contentRect.localPosition = Vector3.zero;
        contentRect.localScale = Vector3.one;
        contentRect.localRotation = Quaternion.identity;

        Debug.Log($"MissionManager: Content position reset. Creating {allMissions.Count} mission items...");

        // Create new items from allMissions (editable in Inspector)
        foreach (var m in allMissions)
        {
            if (m == null)
            {
                Debug.LogWarning("MissionManager: Null mission in allMissions list!");
                continue;
            }

            var go = Instantiate(missionItemPrefab, missionListContent);
            
            // Ensure proper local positioning
            RectTransform itemRect = go.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                itemRect.localScale = Vector3.one;
                itemRect.localRotation = Quaternion.identity;
            }

            var ui = go.GetComponent<MissionItemUI>();
            if (ui != null)
            {
                ui.Setup(m, this);
                ui.SetEnemySprite(m.enemyType != null ? m.enemyType.sprite : null);
            }
            else
            {
                Debug.LogWarning($"MissionManager: Instantiated prefab {go.name} has no MissionItemUI component!");
            }
            missionItemObjects.Add(go);
        }

        // Force immediate layout rebuild
        Canvas.ForceUpdateCanvases();
        
        // Rebuild content layout
        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        // Configure content size for proper scrolling
        ConfigureContentSize();

        // Reset scroll position to top
        ResetScrollPosition();

        Debug.Log($"MissionManager: Populated {missionItemObjects.Count} mission items with {itemSpacing}px spacing");
    }

    private void ConfigureContentSize()
    {
        if (missionListScrollView == null || missionListContent == null)
            return;

        RectTransform contentRect = missionListContent as RectTransform;
        RectTransform viewportRect = missionListScrollView.viewport;

        if (contentRect == null || viewportRect == null)
            return;

        // Set content width to match viewport exactly
        float viewportWidth = viewportRect.rect.width;
        contentRect.sizeDelta = new Vector2(viewportWidth, contentRect.sizeDelta.y);

        Debug.Log($"MissionManager: Content configured - Width: {contentRect.sizeDelta.x}, Height: {contentRect.sizeDelta.y}");
    }

    private void ResetScrollPosition()
    {
        if (missionListScrollView != null)
        {
            missionListScrollView.verticalNormalizedPosition = 1f;
            Debug.Log("MissionManager: Scroll position reset to top");
        }
    }

    /// <summary>
    /// Adds a new enemy type at runtime (optional). You can also create EnemyType assets via CreateAssetMenu.
    /// </summary>
    public void RegisterEnemyType(EnemyType et)
    {
        if (et == null) return;
        if (!availableEnemyTypes.Contains(et)) availableEnemyTypes.Add(et);
    }

    public EnemyType GetEnemyTypeById(string id)
    {
        return availableEnemyTypes.Find(e => e != null && e.id == id);
    }

    /// <summary>
    /// Update mission progress by id and refresh corresponding UI item.
    /// </summary>
    public void UpdateMissionProgress(string missionId, int increase)
    {
        var mission = allMissions.Find(m => m.id == missionId);
        if (mission == null)
        {
            Debug.LogWarning($"MissionManager: Mission '{missionId}' not found!");
            return;
        }

        // Only update progress for ACTIVE missions
        if (mission.status != Mission.MissionStatus.Active)
        {
            Debug.Log($"MissionManager: Mission '{missionId}' is not active (status: {mission.status}). Skipping progress update.");
            return;
        }

        mission.currentCount += increase;
        if (mission.currentCount >= mission.targetCount)
        {
            mission.currentCount = mission.targetCount;
            mission.status = Mission.MissionStatus.Completed;
            Debug.Log($"MissionManager: Mission '{missionId}' completed! ({mission.currentCount}/{mission.targetCount})");
            
            // AUTO-CLAIM: Automatically grant reward when mission completes
            // Use a separate GameObject to start the coroutine if this GameObject might be inactive
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(AutoClaimReward(mission));
            }
            else
            {
                // If MissionManager GameObject is inactive, use a helper
                Debug.LogWarning("[MissionManager] MissionManager GameObject is inactive, using CoroutineHelper");
                CoroutineHelper.Instance.StartHelperCoroutine(AutoClaimReward(mission));
            }
        }
        else
        {
            Debug.Log($"MissionManager: Mission '{missionId}' progress: {mission.currentCount}/{mission.targetCount}");
        }

        // Update UI item immediately
        RefreshMissionItem(mission);
        
        // IMPORTANT: Force overlay to update immediately
        if (MissionOverlayUI.Instance != null)
        {
            MissionOverlayUI.Instance.ForceUpdate();
        }
        
        // IMPORTANT: Auto-save mission progress
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGameEvent($"Mission progress updated: {missionId}");
        }
    }

    /// <summary>
    /// Automatically claim reward when mission completes
    /// </summary>
    private System.Collections.IEnumerator AutoClaimReward(Mission mission)
    {
        // Show "Completed!" message in overlay
        Debug.Log("[MissionManager] Mission completed! Waiting for player to return to NPC...");
        
        // DON'T auto-claim immediately - require NPC interaction
        // Just mark as completed and wait for player to talk to NPC
        mission.status = Mission.MissionStatus.Completed;
        
        // Keep overlay showing "Completed!"
        if (MissionOverlayUI.Instance != null)
        {
            MissionOverlayUI.Instance.ForceUpdate();
        }
        
        // Refresh UI to show mission is completed
        RefreshMissionItem(mission);
        
        yield break; // Stop here - reward will be claimed when player talks to NPC
    }

    /// <summary>
    /// Accept a mission (only one can be active at a time)
    /// </summary>
    public bool AcceptMission(string missionId)
    {
        // Check if there's already an active mission
        var activeMission = allMissions.Find(m => m.status == Mission.MissionStatus.Active);
        if (activeMission != null)
        {
            Debug.LogWarning($"MissionManager: Cannot accept mission '{missionId}'. Mission '{activeMission.id}' is already active!");
            return false;
        }

        var mission = allMissions.Find(m => m.id == missionId);
        if (mission == null)
        {
            Debug.LogWarning($"MissionManager: Mission '{missionId}' not found!");
            return false;
        }

        if (mission.status != Mission.MissionStatus.Available)
        {
            Debug.LogWarning($"MissionManager: Mission '{missionId}' is not available (status: {mission.status})!");
            return false;
        }

        // Accept the mission
        mission.status = Mission.MissionStatus.Active;
        
        // RESTORE SAVED PROGRESS: If player switches back to this mission
        if (mission.savedProgress > 0)
        {
            mission.currentCount = mission.savedProgress;
            Debug.Log($"MissionManager: Restored saved progress for mission '{missionId}': {mission.currentCount}/{mission.targetCount}");
        }
        else
        {
            mission.currentCount = 0; // Start fresh if no saved progress
        }
        
        Debug.Log($"MissionManager: Accepted mission '{missionId}' with progress {mission.currentCount}/{mission.targetCount}");

        // Refresh UI
        RefreshAllMissionItems();
        
        // Show mission overlay
        if (MissionOverlayUI.Instance != null)
        {
            MissionOverlayUI.Instance.RefreshDisplay();
        }
        
        // Auto-save when mission is accepted
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGameEvent($"Mission accepted: {missionId}");
        }

        return true;
    }

    /// <summary>
    /// Deny/Cancel a mission (keeps it in the list as Available)
    /// </summary>
    public void DenyMission(string missionId)
    {
        var mission = allMissions.Find(m => m.id == missionId);
        if (mission == null)
        {
            Debug.LogWarning($"MissionManager: Mission '{missionId}' not found!");
            return;
        }

        if (mission.status == Mission.MissionStatus.Active)
        {
            // SAVE PROGRESS: Store current progress before cancelling
            mission.savedProgress = mission.currentCount;
            Debug.Log($"MissionManager: Saved progress for mission '{missionId}': {mission.savedProgress}/{mission.targetCount}");
            
            // Cancel active mission - reset to Available
            mission.status = Mission.MissionStatus.Available;
            // DON'T reset currentCount here - it's saved in savedProgress
            
            Debug.Log($"MissionManager: Cancelled mission '{missionId}'. Progress saved, mission set to Available.");
            
            // Hide mission overlay when cancelling
            if (MissionOverlayUI.Instance != null)
            {
                MissionOverlayUI.Instance.HideOverlay();
            }
        }
        else
        {
            // Just keep it as Available
            Debug.Log($"MissionManager: Denied mission '{missionId}'. Keeping as Available.");
        }

        // Refresh UI
        RefreshAllMissionItems();
        
        // Auto-save when mission is denied/cancelled
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGameEvent($"Mission cancelled: {missionId}");
        }
    }

    /// <summary>
    /// Get the currently active or completed mission (for overlay display)
    /// </summary>
    public Mission GetActiveMission()
    {
        // Return Active OR Completed missions (so overlay stays visible)
        return allMissions.Find(m => m.status == Mission.MissionStatus.Active || m.status == Mission.MissionStatus.Completed);
    }

    /// <summary>
    /// Refresh all mission items in the UI
    /// </summary>
    private void RefreshAllMissionItems()
    {
        foreach (var itemObj in missionItemObjects)
        {
            if (itemObj == null) continue;
            
            var ui = itemObj.GetComponent<MissionItemUI>();
            if (ui != null)
            {
                ui.UpdateDisplay();
            }
        }
    }

    /// <summary>
    /// Refresh a specific mission item in the UI
    /// </summary>
    private void RefreshMissionItem(Mission mission)
    {
        for (int i = 0; i < missionItemObjects.Count; i++)
        {
            if (missionItemObjects[i] == null) continue;
            
            var ui = missionItemObjects[i].GetComponent<MissionItemUI>();
            if (ui != null && ui.mission == mission)
            {
                ui.UpdateDisplay();
                Debug.Log($"MissionManager: Refreshed UI for mission '{mission.id}'");
                break;
            }
        }
    }

    // Grant reward for a mission (called by MissionItemUI when accept clicked)
    public void GrantReward(Mission mission, NPC npc = null)
    {
        if (mission == null || mission.status != Mission.MissionStatus.Completed)
        {
            Debug.LogWarning($"MissionManager: Cannot grant reward - mission is not completed (status: {mission?.status})");
            return;
        }

        // Show NPC reward dialogue if NPC is provided
        if (npc != null)
        {
            // Use CoroutineHelper if GameObject is inactive
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(ShowRewardDialogue(npc, mission));
            }
            else
            {
                Debug.LogWarning("[MissionManager] GameObject is inactive, using CoroutineHelper for reward dialogue");
                CoroutineHelper.Instance.StartHelperCoroutine(ShowRewardDialogue(npc, mission));
            }
        }
        else
        {
            // Grant reward immediately if no NPC
            GrantRewardImmediate(mission);
        }
    }

    private System.Collections.IEnumerator ShowRewardDialogue(NPC npc, Mission mission)
    {
        // Build reward message
        string rewardMessage = "Here's your reward: ";
        
        switch (mission.rewardType)
        {
            case Mission.RewardType.Leaf:
                rewardMessage += $"{mission.rewardAmount} Leaves!";
                break;
            case Mission.RewardType.Cherries:
            case Mission.RewardType.Item:
                string itemName = mission.rewardItem != null ? mission.rewardItem.itemName : "Item";
                rewardMessage += $"{mission.rewardAmount}x {itemName}!";
                break;
            default:
                rewardMessage = "Thank you for your help!";
                break;
        }

        // Show dialogue in NPC UI (overlay still visible showing "Completed!")
        if (npc != null)
        {
            npc.ShowRewardMessage(rewardMessage);
            Debug.Log($"[MissionManager] NPC showing reward: {rewardMessage}");
        }
        
        // Wait for NPC message to display (overlay still shows "Completed!")
        yield return new WaitForSeconds(2.5f);
        
        // Grant reward
        GrantRewardImmediate(mission);
        
        // NOW hide overlay after reward is granted
        Debug.Log("[MissionManager] Reward granted, hiding overlay now");
        if (MissionOverlayUI.Instance != null)
        {
            MissionOverlayUI.Instance.HideOverlay();
        }
    }

    private void GrantRewardImmediate(Mission mission)
    {
        Debug.Log($"[MissionManager] ==================== GRANTING REWARD ====================");
        Debug.Log($"[MissionManager] Mission ID: '{mission.id}'");
        Debug.Log($"[MissionManager] Reward Type: {mission.rewardType}");
        Debug.Log($"[MissionManager] Reward Amount: {mission.rewardAmount}");
        Debug.Log($"[MissionManager] Reward Item: {(mission.rewardItem != null ? mission.rewardItem.itemName : "null")}");
        
        bool rewardGranted = false;
        
        // Grant rewards based on type
        switch (mission.rewardType)
        {
            case Mission.RewardType.Leaf:
                if (mission.rewardAmount > 0)
                {
                    if (MoneyManager.Instance == null)
                    {
                        Debug.LogError("[MissionManager] MoneyManager.Instance is NULL! Cannot grant leaf reward.");
                        break;
                    }
                    
                    int moneyBefore = MoneyManager.Instance.Money;
                    Debug.Log($"[MissionManager] Money before: {moneyBefore}");
                    
                    MoneyManager.Instance.AddMoney(mission.rewardAmount);
                    
                    int moneyAfter = MoneyManager.Instance.Money;
                    Debug.Log($"[MissionManager] Money after: {moneyAfter}");
                    Debug.Log($"[MissionManager] ? Successfully granted {mission.rewardAmount} leaves!");
                    
                    rewardGranted = true;
                }
                else
                {
                    Debug.LogWarning($"[MissionManager] Mission {mission.id} has Leaf reward but amount <= 0");
                }
                break;

            case Mission.RewardType.Cherries:
                if (mission.rewardItem != null && mission.rewardAmount > 0)
                {
                    if (InventoryManager.Instance == null)
                    {
                        Debug.LogError("[MissionManager] InventoryManager.Instance is NULL! Cannot grant cherry reward.");
                        break;
                    }
                    
                    Debug.Log($"[MissionManager] Adding {mission.rewardAmount}x {mission.rewardItem.itemName} to inventory...");
                    Debug.Log($"[MissionManager] Item ID: {mission.rewardItem.itemID}");
                    
                    int leftover = InventoryManager.Instance.AddItem(mission.rewardItem, mission.rewardAmount);
                    
                    if (leftover == 0)
                    {
                        Debug.Log($"[MissionManager] ? Successfully added ALL {mission.rewardAmount}x {mission.rewardItem.itemName} to inventory!");
                        rewardGranted = true;
                    }
                    else
                    {
                        Debug.LogWarning($"[MissionManager] ?? Added {mission.rewardAmount - leftover}x {mission.rewardItem.itemName}, but {leftover} items couldn't fit (inventory full)");
                        rewardGranted = leftover < mission.rewardAmount; // Partial success
                    }
                    
                    // Force inventory save
                    Debug.Log("[MissionManager] Forcing inventory save...");
                    InventoryManager.Instance.SaveInventory();
                }
                else
                {
                    Debug.LogWarning($"[MissionManager] Mission {mission.id} Cherries reward misconfigured");
                    Debug.LogWarning($"[MissionManager] - Item exists: {mission.rewardItem != null}");
                    Debug.LogWarning($"[MissionManager] - Amount: {mission.rewardAmount}");
                }
                break;

            case Mission.RewardType.Item:
                if (mission.rewardItem != null && mission.rewardAmount > 0)
                {
                    if (InventoryManager.Instance == null)
                    {
                        Debug.LogError("[MissionManager] InventoryManager.Instance is NULL! Cannot grant item reward.");
                        break;
                    }
                    
                    Debug.Log($"[MissionManager] Adding {mission.rewardAmount}x {mission.rewardItem.itemName} to inventory...");
                    Debug.Log($"[MissionManager] Item ID: {mission.rewardItem.itemID}");
                    
                    int leftover = InventoryManager.Instance.AddItem(mission.rewardItem, mission.rewardAmount);
                    
                    if (leftover == 0)
                    {
                        Debug.Log($"[MissionManager] ? Successfully added ALL {mission.rewardAmount}x {mission.rewardItem.itemName} to inventory!");
                        rewardGranted = true;
                    }
                    else
                    {
                        Debug.LogWarning($"[MissionManager] ?? Added {mission.rewardAmount - leftover}x {mission.rewardItem.itemName}, but {leftover} items couldn't fit (inventory full)");
                        rewardGranted = leftover < mission.rewardAmount; // Partial success
                    }
                    
                    // Force inventory save
                    Debug.Log("[MissionManager] Forcing inventory save...");
                    InventoryManager.Instance.SaveInventory();
                }
                else
                {
                    Debug.LogWarning($"[MissionManager] Mission {mission.id} Item reward misconfigured");
                    Debug.LogWarning($"[MissionManager] - Item exists: {mission.rewardItem != null}");
                    Debug.LogWarning($"[MissionManager] - Amount: {mission.rewardAmount}");
                }
                break;

            default:
                Debug.LogWarning($"[MissionManager] Mission {mission.id} has no reward configured (RewardType: {mission.rewardType})");
                break;
        }

        if (rewardGranted)
        {
            Debug.Log($"[MissionManager] ? REWARD SUCCESSFULLY GRANTED ?");
        }
        else
        {
            Debug.LogError($"[MissionManager] ? REWARD WAS NOT GRANTED - Check logs above for reason");
        }

        // Mark mission as claimed
        mission.status = Mission.MissionStatus.Claimed;
        Debug.Log($"[MissionManager] Mission '{mission.id}' status ? Claimed");

        // Update UI item
        RefreshMissionItem(mission);
        
        // Auto-save after claiming reward
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGameEvent($"Mission reward claimed: {mission.id}");
        }
        
        Debug.Log($"[MissionManager] ==================== REWARD COMPLETE ====================");
    }

    /// <summary>
    /// Get a snapshot of all mission progress for saving
    /// </summary>
    public List<SaveData.MissionData> GetMissionSnapshot()
    {
        var list = new List<SaveData.MissionData>();

        foreach (var mission in allMissions)
        {
            if (mission == null) continue;

            list.Add(new SaveData.MissionData
            {
                id = mission.id,
                currentCount = mission.currentCount,
                savedProgress = mission.savedProgress,
                status = (int)mission.status
            });
        }

        Debug.Log($"[MissionManager] Created mission snapshot with {list.Count} missions");
        return list;
    }

    /// <summary>
    /// Apply saved mission progress from a save file
    /// </summary>
    public void ApplyMissionSnapshot(List<SaveData.MissionData> data)
    {
        if (data == null || data.Count == 0)
        {
            Debug.Log("[MissionManager] No mission data to apply");
            return;
        }

        Debug.Log($"[MissionManager] Applying mission snapshot with {data.Count} missions");

        // Create a dictionary for quick lookup
        var savedMissions = new Dictionary<string, SaveData.MissionData>();
        foreach (var saved in data)
        {
            if (saved != null && !string.IsNullOrEmpty(saved.id))
            {
                savedMissions[saved.id] = saved;
            }
        }

        // Apply saved data to existing missions
        foreach (var mission in allMissions)
        {
            if (mission == null || string.IsNullOrEmpty(mission.id)) continue;

            if (savedMissions.TryGetValue(mission.id, out var savedMission))
            {
                mission.currentCount = savedMission.currentCount;
                mission.savedProgress = savedMission.savedProgress;
                mission.status = (Mission.MissionStatus)savedMission.status;

                Debug.Log($"[MissionManager] Restored mission '{mission.id}': status={mission.status}, currentCount={mission.currentCount}, savedProgress={mission.savedProgress}");
            }
            else
            {
                // Mission exists in game but not in save file - reset to defaults
                mission.currentCount = 0;
                mission.savedProgress = 0;
                mission.status = Mission.MissionStatus.Available;
                Debug.Log($"[MissionManager] Mission '{mission.id}' not in save file, reset to defaults");
            }
        }

        // Update UI if it's visible
        if (missionCanvas != null && missionCanvas.activeSelf)
        {
            RefreshAllMissionItems();
        }

        // Update overlay if there's an active mission
        if (MissionOverlayUI.Instance != null)
        {
            MissionOverlayUI.Instance.RefreshDisplay();
        }

        Debug.Log("[MissionManager] Mission snapshot applied successfully");
    }
}

[System.Serializable]
public class Mission
{
    public enum RewardType { None, Leaf, Cherries, Item }
    public enum MissionStatus { Available, Active, Completed, Claimed }

    public string id;
    public string description;   // e.g. "Kill enemies"
    public int targetCount;      // e.g. 5
    public int currentCount;     // editable or tracked at runtime
    public EnemyType enemyType;  // reference to EnemyType ScriptableObject (selectable in inspector)

    // New structured reward fields
    public RewardType rewardType = RewardType.None;
    public int rewardAmount = 0; // for money/stackable items
    public ItemObject rewardItem; // for item-type rewards (e.g. cherries revive item)

    // Mission status tracking
    public MissionStatus status = MissionStatus.Available;

    // SAVED PROGRESS: Store progress when switching missions
    [System.NonSerialized]
    public int savedProgress = 0; // Progress saved when cancelling/switching

    // DEPRECATED: Use status instead
    [System.Obsolete("Use status instead")]
    public bool isCompleted => status == MissionStatus.Completed || status == MissionStatus.Claimed;
    [System.Obsolete("Use status instead")]
    public bool isClaimed => status == MissionStatus.Claimed;
}