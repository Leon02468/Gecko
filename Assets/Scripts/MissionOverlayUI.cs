using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

/// <summary>
/// Displays the active mission objective as an overlay on the game screen
/// Simple design: Enemy sprite at top, progress text at bottom
/// Uses TextMeshPro only
/// </summary>
public class MissionOverlayUI : MonoBehaviour
{
    public static MissionOverlayUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject overlayPanel; // The panel to show/hide
    public Image enemyIcon; // Enemy sprite - displayed at top
    
    [Header("Text Component")]
    [Tooltip("Assign your TextMeshPro - Text (UI) component here")]
    public Component progressText; // TextMeshPro component

    [Header("Settings")]
    public bool showAutomatically = true; // Auto-show when mission is active
    public float updateInterval = 0.5f; // How often to refresh (in seconds)

    [Header("Completion Settings")]
    public string completedText = "Completed!";
    public Color completedColor = Color.green;
    public Color inProgressColor = Color.white;

    private object currentMission;
    private float updateTimer;

    void Awake()
    {
        Debug.Log("[MissionOverlay] ========== AWAKE START ==========");
        Debug.Log($"[MissionOverlay] GameObject name: {gameObject.name}");
        Debug.Log($"[MissionOverlay] GameObject active: {gameObject.activeInHierarchy}");
        
        // Singleton pattern with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[MissionOverlay] Set as Instance");
            
            // IMPORTANT: Find the root Canvas to apply DontDestroyOnLoad to
            // If this GameObject is a child of a Canvas, we need to move the Canvas, not just this GameObject
            Transform rootTransform = transform;
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            
            if (parentCanvas != null)
            {
                Debug.Log($"[MissionOverlay] Found parent Canvas: {parentCanvas.gameObject.name}");
                rootTransform = parentCanvas.transform;
            }
            
            // Move to root if parented (but not if parented to Canvas, which is our root)
            if (rootTransform.parent != null)
            {
                Debug.LogWarning($"[MissionOverlay] Moving '{rootTransform.name}' from parent '{rootTransform.parent.name}' to root");
                rootTransform.SetParent(null);
            }
            else
            {
                Debug.Log($"[MissionOverlay] {rootTransform.name} is already at root level");
            }
            
            // Apply DontDestroyOnLoad to the root (Canvas if it exists, otherwise this GameObject)
            if (rootTransform.gameObject.scene.name != "DontDestroyOnLoad")
            {
                DontDestroyOnLoad(rootTransform.gameObject);
                Debug.Log($"[MissionOverlay] Applied DontDestroyOnLoad to {rootTransform.gameObject.name}");
            }
            else
            {
                Debug.Log($"[MissionOverlay] {rootTransform.gameObject.name} is already in DontDestroyOnLoad scene");
            }
        }
        else
        {
            Debug.LogWarning($"[MissionOverlay] Duplicate detected! Instance={Instance.gameObject.name}, This={gameObject.name}");
            Destroy(gameObject);
            return;
        }

        // Check UI references
        Debug.Log($"[MissionOverlay] overlayPanel assigned: {overlayPanel != null}");
        if (overlayPanel != null)
        {
            Debug.Log($"[MissionOverlay] overlayPanel name: {overlayPanel.name}");
            Debug.Log($"[MissionOverlay] overlayPanel active: {overlayPanel.activeSelf}");
        }
        
        Debug.Log($"[MissionOverlay] enemyIcon assigned: {enemyIcon != null}");
        Debug.Log($"[MissionOverlay] progressText assigned: {progressText != null}");

        // Try to get TextMeshPro component if assigned
        if (progressText != null)
        {
            Debug.Log($"[MissionOverlay] progressText type: {progressText.GetType().Name}");
            
            var tmpType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
            var tmpUIType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            
            if (tmpType != null && tmpType.IsInstanceOfType(progressText))
            {
                Debug.Log($"[MissionOverlay] ? progressText is already TextMeshPro");
            }
            else if (progressText is Component)
            {
                var comp = progressText as Component;
                if (comp != null && comp.gameObject != null)
                {
                    var tmpComponent = comp.gameObject.GetComponent(tmpUIType);
                    if (tmpComponent != null)
                    {
                        progressText = tmpComponent;
                        Debug.Log("[MissionOverlay] ? Extracted TextMeshProUGUI from GameObject");
                    }
                    else
                    {
                        Debug.LogError($"[MissionOverlay] ? GameObject has no TextMeshProUGUI component!");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[MissionOverlay] progressText is not a Component!");
            }
        }
        else
        {
            Debug.LogError("[MissionOverlay] ? progressText is NOT assigned in inspector!");
        }

        // Start hidden
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(false);
            Debug.Log("[MissionOverlay] Set overlayPanel to inactive (starting hidden)");
        }
        else
        {
            Debug.LogError("[MissionOverlay] ? Cannot hide overlay - overlayPanel is null!");
        }
        
        Debug.Log("[MissionOverlay] ========== AWAKE END ==========");
    }

    void Update()
    {
        if (!showAutomatically)
            return;

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            RefreshDisplay();
        }
    }

    /// <summary>
    /// Manually refresh the overlay display
    /// </summary>
    public void RefreshDisplay()
    {
        Debug.Log("[MissionOverlay] ========== REFRESH DISPLAY ==========");
        
        // Use reflection to access MissionManager
        var missionManagerType = System.Type.GetType("MissionManager");
        if (missionManagerType == null)
        {
            Debug.LogError("[MissionOverlay] ? MissionManager type not found!");
            HideOverlay();
            return;
        }
        Debug.Log("[MissionOverlay] ? MissionManager type found");

        var instanceProp = missionManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceProp == null)
        {
            Debug.LogError("[MissionOverlay] ? MissionManager.Instance property not found!");
            HideOverlay();
            return;
        }
        Debug.Log("[MissionOverlay] ? MissionManager.Instance property found");

        var missionManagerInstance = instanceProp.GetValue(null);
        if (missionManagerInstance == null)
        {
            Debug.LogWarning("[MissionOverlay] MissionManager.Instance is null (not spawned yet?)");
            HideOverlay();
            return;
        }
        Debug.Log("[MissionOverlay] ? MissionManager.Instance exists");

        // Get the active mission
        var getActiveMissionMethod = missionManagerType.GetMethod("GetActiveMission");
        if (getActiveMissionMethod == null)
        {
            Debug.LogError("[MissionOverlay] ? GetActiveMission method not found!");
            HideOverlay();
            return;
        }
        Debug.Log("[MissionOverlay] ? GetActiveMission method found");

        var activeMission = getActiveMissionMethod.Invoke(missionManagerInstance, null);

        if (activeMission == null)
        {
            Debug.Log("[MissionOverlay] No active mission - hiding overlay");
            HideOverlay();
            return;
        }

        Debug.Log($"[MissionOverlay] ? Active mission found: {activeMission.GetType().Name}");

        // Show overlay with mission info
        currentMission = activeMission;
        ShowOverlay();
        UpdateMissionInfo();
        
        Debug.Log("[MissionOverlay] ========== REFRESH COMPLETE ==========");
    }

    private void ShowOverlay()
    {
        Debug.Log($"[MissionOverlay] ShowOverlay called - overlayPanel={overlayPanel != null}");
        
        if (overlayPanel != null)
        {
            bool wasActive = overlayPanel.activeSelf;
            overlayPanel.SetActive(true);
            Debug.Log($"[MissionOverlay] overlayPanel.SetActive(true) - was:{wasActive}, now:{overlayPanel.activeSelf}");
            
            // Check Canvas and GraphicRaycaster
            var canvas = overlayPanel.GetComponentInParent<Canvas>();
            Debug.Log($"[MissionOverlay] Parent Canvas found: {canvas != null}");
            if (canvas != null)
            {
                Debug.Log($"[MissionOverlay] Canvas enabled: {canvas.enabled}, renderMode: {canvas.renderMode}");
            }
        }
        else
        {
            Debug.LogError("[MissionOverlay] ? Cannot show overlay - overlayPanel is null!");
        }
    }

    /// <summary>
    /// Hide the overlay (can be called internally or externally)
    /// </summary>
    public void HideOverlay()
    {
        if (overlayPanel != null && overlayPanel.activeSelf)
        {
            overlayPanel.SetActive(false);
            currentMission = null;
            Debug.Log("[MissionOverlay] Hiding mission overlay");
        }
    }

    private void UpdateMissionInfo()
    {
        Debug.Log("[MissionOverlay] ========== UPDATE MISSION INFO ==========");
        
        if (currentMission == null)
        {
            Debug.LogError("[MissionOverlay] ? currentMission is null!");
            return;
        }

        var missionType = currentMission.GetType();
        Debug.Log($"[MissionOverlay] Mission type: {missionType.Name}");

        // Get mission fields using reflection
        var currentCountField = missionType.GetField("currentCount");
        var targetCountField = missionType.GetField("targetCount");
        var enemyTypeField = missionType.GetField("enemyType");
        var statusField = missionType.GetField("status");

        int currentCount = (int)(currentCountField?.GetValue(currentMission) ?? 0);
        int targetCount = (int)(targetCountField?.GetValue(currentMission) ?? 1);
        var enemyType = enemyTypeField?.GetValue(currentMission);

        Debug.Log($"[MissionOverlay] Progress: {currentCount}/{targetCount}");
        Debug.Log($"[MissionOverlay] Enemy type: {(enemyType != null ? enemyType.GetType().Name : "null")}");

        // Update enemy icon at top
        if (enemyIcon != null)
        {
            if (enemyType != null)
            {
                var spriteField = enemyType.GetType().GetField("sprite");
                var sprite = spriteField?.GetValue(enemyType) as Sprite;
                
                if (sprite != null)
                {
                    enemyIcon.sprite = sprite;
                    enemyIcon.gameObject.SetActive(true);
                    Debug.Log($"[MissionOverlay] ? Set enemy icon sprite: {sprite.name}");
                }
                else
                {
                    enemyIcon.gameObject.SetActive(false);
                    Debug.LogWarning("[MissionOverlay] Enemy type has no sprite");
                }
            }
            else
            {
                enemyIcon.gameObject.SetActive(false);
                Debug.LogWarning("[MissionOverlay] No enemy type assigned to mission");
            }
        }
        else
        {
            Debug.LogWarning("[MissionOverlay] enemyIcon is not assigned!");
        }

        // Check if mission is completed
        bool isCompleted = false;
        if (statusField != null)
        {
            var statusValue = statusField.GetValue(currentMission);
            string statusName = statusValue?.ToString() ?? "";
            isCompleted = (statusName == "Completed" || currentCount >= targetCount);
            Debug.Log($"[MissionOverlay] Mission status: {statusName}, isCompleted: {isCompleted}");
        }
        else
        {
            isCompleted = (currentCount >= targetCount);
            Debug.Log($"[MissionOverlay] No status field, checking count: isCompleted={isCompleted}");
        }

        string textToDisplay;
        Color colorToUse;

        if (isCompleted)
        {
            textToDisplay = completedText;
            colorToUse = completedColor;
        }
        else
        {
            textToDisplay = $"{currentCount}/{targetCount}";
            colorToUse = inProgressColor;
        }

        Debug.Log($"[MissionOverlay] Displaying: '{textToDisplay}' in color {colorToUse}");

        // Set text using TextMeshPro
        SetProgressText(textToDisplay, colorToUse);
        
        Debug.Log("[MissionOverlay] ========== UPDATE COMPLETE ==========");
    }

    private void SetProgressText(string text, Color color)
    {
        if (progressText == null)
        {
            Debug.LogWarning("[MissionOverlay] progressText is not assigned!");
            return;
        }

        Debug.Log($"[MissionOverlay] Setting text to: '{text}' with color: {color}");
        Debug.Log($"[MissionOverlay] progressText type: {progressText.GetType().FullName}");

        // Get the actual type
        var componentType = progressText.GetType();
        
        // List all properties for debugging
        var allProperties = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Debug.Log($"[MissionOverlay] Available properties: {string.Join(", ", System.Array.ConvertAll(allProperties, p => p.Name))}");

        // Use reflection to set TextMeshPro properties
        var textProperty = componentType.GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
        if (textProperty != null)
        {
            textProperty.SetValue(progressText, text);
            Debug.Log($"[MissionOverlay] Text property set successfully");
        }
        else
        {
            Debug.LogError("[MissionOverlay] Could not find 'text' property on progressText component!");
        }

        var colorProperty = componentType.GetProperty("color", BindingFlags.Public | BindingFlags.Instance);
        if (colorProperty != null)
        {
            colorProperty.SetValue(progressText, color);
            Debug.Log($"[MissionOverlay] Color property set successfully");
        }
        else
        {
            Debug.LogError("[MissionOverlay] Could not find 'color' property on progressText component!");
        }

        // Force TextMeshPro to update
        var updateMethod = componentType.GetMethod("SetAllDirty", BindingFlags.Public | BindingFlags.Instance);
        if (updateMethod != null)
        {
            updateMethod.Invoke(progressText, null);
            Debug.Log("[MissionOverlay] Forced TextMeshPro update");
        }
    }

    /// <summary>
    /// Force update the overlay (call when mission progress changes)
    /// </summary>
    public void ForceUpdate()
    {
        RefreshDisplay();
    }

    /// <summary>
    /// Manually show/hide the overlay
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (visible)
        {
            RefreshDisplay();
        }
        else
        {
            HideOverlay();
        }
    }

    /// <summary>
    /// Ensures the Canvas is either on this GameObject or is a child of it.
    /// This is critical for DontDestroyOnLoad to work properly.
    /// </summary>
    private void EnsureCanvasIsChild()
    {
        // Check if there's a Canvas on this GameObject
        var canvas = GetComponent<Canvas>();
        
        if (canvas != null)
        {
            Debug.Log("[MissionOverlay] ? Canvas is on the same GameObject");
            return;
        }

        // Check if there's a Canvas in children
        canvas = GetComponentInChildren<Canvas>();
        
        if (canvas != null)
        {
            Debug.Log($"[MissionOverlay] ? Canvas found in children: {canvas.gameObject.name}");
            
            // Make sure the Canvas GameObject is a direct child of this GameObject
            if (canvas.transform.parent != transform)
            {
                Debug.LogWarning($"[MissionOverlay] Canvas is not a direct child, reparenting...");
                canvas.transform.SetParent(transform, true);
                Debug.Log($"[MissionOverlay] ? Reparented Canvas to {gameObject.name}");
            }
            return;
        }

        Debug.LogWarning("[MissionOverlay] ? No Canvas found! The overlay may not display correctly. Consider adding an EnsureMissionOverlayCanvas component.");
    }
}
