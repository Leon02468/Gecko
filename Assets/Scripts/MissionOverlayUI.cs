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
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Try to get TextMeshPro component if assigned GameObject or Component
        if (progressText != null)
        {
            Debug.Log($"[MissionOverlay] progressText assigned type: {progressText.GetType().Name}");
            
            // Check if we need to extract the TextMeshPro component
            var tmpType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
            var tmpUIType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            
            // If it's already the correct type, we're good
            if (tmpType != null && tmpType.IsInstanceOfType(progressText))
            {
                Debug.Log($"[MissionOverlay] Using TextMeshPro component: {progressText.GetType().Name}");
            }
            // If it's some other component, try to get TextMeshProUGUI from its GameObject
            else if (progressText is Component)
            {
                var comp = progressText as Component;
                if (comp != null && comp.gameObject != null)
                {
                    var tmpComponent = comp.gameObject.GetComponent(tmpUIType);
                    if (tmpComponent != null)
                    {
                        progressText = tmpComponent;
                        Debug.Log("[MissionOverlay] Found Component, extracted TextMeshProUGUI from its GameObject");
                    }
                    else
                    {
                        Debug.LogError($"[MissionOverlay] Component's GameObject does not have TextMeshProUGUI! Type was: {progressText.GetType().Name}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[MissionOverlay] progressText is not a Component! Type: {progressText.GetType().Name}");
            }
            
            // Final verification
            if (tmpType != null && tmpType.IsInstanceOfType(progressText))
            {
                // Verify properties exist
                var textProp = progressText.GetType().GetProperty("text");
                var colorProp = progressText.GetType().GetProperty("color");
                Debug.Log($"[MissionOverlay] Has 'text' property: {textProp != null}");
                Debug.Log($"[MissionOverlay] Has 'color' property: {colorProp != null}");
            }
            else
            {
                Debug.LogError($"[MissionOverlay] Final check: progressText is still not a TextMeshPro component! Type: {progressText.GetType().Name}");
            }
        }
        else
        {
            Debug.LogError("[MissionOverlay] progressText is not assigned!");
        }

        // Start hidden
        if (overlayPanel != null)
            overlayPanel.SetActive(false);
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
        // Use reflection to access MissionManager
        var missionManagerType = System.Type.GetType("MissionManager");
        if (missionManagerType == null)
        {
            HideOverlay();
            return;
        }

        var instanceProp = missionManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceProp == null)
        {
            HideOverlay();
            return;
        }

        var missionManagerInstance = instanceProp.GetValue(null);
        if (missionManagerInstance == null)
        {
            HideOverlay();
            return;
        }

        // Get the active mission
        var getActiveMissionMethod = missionManagerType.GetMethod("GetActiveMission");
        if (getActiveMissionMethod == null)
        {
            HideOverlay();
            return;
        }

        var activeMission = getActiveMissionMethod.Invoke(missionManagerInstance, null);

        if (activeMission == null)
        {
            // No active mission - hide overlay
            HideOverlay();
            return;
        }

        // Show overlay with mission info
        currentMission = activeMission;
        ShowOverlay();
        UpdateMissionInfo();
    }

    private void ShowOverlay()
    {
        if (overlayPanel != null && !overlayPanel.activeSelf)
        {
            overlayPanel.SetActive(true);
            Debug.Log("[MissionOverlay] Showing mission overlay");
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
        if (currentMission == null)
            return;

        var missionType = currentMission.GetType();

        // Get mission fields using reflection
        var currentCountField = missionType.GetField("currentCount");
        var targetCountField = missionType.GetField("targetCount");
        var enemyTypeField = missionType.GetField("enemyType");
        var statusField = missionType.GetField("status");

        int currentCount = (int)(currentCountField?.GetValue(currentMission) ?? 0);
        int targetCount = (int)(targetCountField?.GetValue(currentMission) ?? 1);
        var enemyType = enemyTypeField?.GetValue(currentMission);

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
                }
                else
                {
                    enemyIcon.gameObject.SetActive(false);
                }
            }
            else
            {
                enemyIcon.gameObject.SetActive(false);
            }
        }

        // Update progress text at bottom
        // Check if mission is completed
        bool isCompleted = false;
        if (statusField != null)
        {
            var statusValue = statusField.GetValue(currentMission);
            string statusName = statusValue?.ToString() ?? "";
            isCompleted = (statusName == "Completed" || currentCount >= targetCount);
        }
        else
        {
            isCompleted = (currentCount >= targetCount);
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

        // Set text using TextMeshPro
        SetProgressText(textToDisplay, colorToUse);
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
}
