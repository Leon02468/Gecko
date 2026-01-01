using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this to your OptionCanvas prefab. It will find the Shop and Missions buttons
/// and wire them to the NPC that spawned this panel.
/// </summary>
public class NPCChoicePanel : MonoBehaviour
{
    [Header("Button References (auto-find if empty)")]
    public Button shopButton;
    public Button missionsButton;

    private NPC sourceNPC; // Changed from NPCShopMission to NPC
    private bool initialized = false;

    /// <summary>
    /// Called by NPC when instantiating this prefab
    /// </summary>
    public void Initialize(NPC npc) // Changed parameter type
    {
        if (initialized)
        {
            Debug.LogWarning("NPCChoicePanel: Already initialized!");
            return;
        }

        sourceNPC = npc;
        initialized = true;

        Debug.Log("NPCChoicePanel: Starting initialization...");

        // Auto-find buttons if not assigned
        if (shopButton == null)
        {
            shopButton = FindButtonByName("ShopButton") ?? FindButtonByLabel("Shop");
            Debug.Log($"NPCChoicePanel: Shop button auto-find result: {(shopButton != null ? shopButton.name : "null")}");
        }

        if (missionsButton == null)
        {
            missionsButton = FindButtonByName("MissionsButton") ?? FindButtonByLabel("Missions");
            Debug.Log($"NPCChoicePanel: Missions button auto-find result: {(missionsButton != null ? missionsButton.name : "null")}");
        }

        // IMPORTANT: Clear any selected GameObject to prevent auto-submit
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // Wire up the buttons AFTER clearing selection
        if (shopButton != null)
        {
            // Remove ALL existing listeners first
            shopButton.onClick.RemoveAllListeners();
            // Disable interactable temporarily to prevent immediate trigger
            bool wasInteractable = shopButton.interactable;
            shopButton.interactable = false;
            
            // Add our listener
            shopButton.onClick.AddListener(OnShopClicked);
            
            // Re-enable after a frame
            StartCoroutine(EnableButtonNextFrame(shopButton, wasInteractable));
            
            Debug.Log("NPCChoicePanel: Shop button wired");
        }
        else
        {
            Debug.LogWarning("NPCChoicePanel: Could not find Shop button");
        }

        if (missionsButton != null)
        {
            // Remove ALL existing listeners first
            missionsButton.onClick.RemoveAllListeners();
            // Disable interactable temporarily to prevent immediate trigger
            bool wasInteractable = missionsButton.interactable;
            missionsButton.interactable = false;
            
            // Add our listener
            missionsButton.onClick.AddListener(OnMissionsClicked);
            
            // Re-enable after a frame
            StartCoroutine(EnableButtonNextFrame(missionsButton, wasInteractable));
            
            Debug.Log("NPCChoicePanel: Missions button wired");
        }
        else
        {
            Debug.LogWarning("NPCChoicePanel: Could not find Missions button");
        }
    }

    private System.Collections.IEnumerator EnableButtonNextFrame(Button button, bool shouldBeInteractable)
    {
        yield return null; // Wait one frame
        if (button != null)
        {
            button.interactable = shouldBeInteractable;
            Debug.Log($"NPCChoicePanel: Button {button.name} re-enabled");
        }
    }

    private void OnShopClicked()
    {
        if (!initialized)
        {
            Debug.LogWarning("NPCChoicePanel: OnShopClicked called before initialization!");
            return;
        }

        Debug.Log("NPCChoicePanel: Shop button clicked!");
        if (sourceNPC != null)
        {
            sourceNPC.OnShopSelected(); // Call NPC method
        }
        else
        {
            Debug.LogError("NPCChoicePanel: sourceNPC is null!");
        }
    }

    private void OnMissionsClicked()
    {
        if (!initialized)
        {
            Debug.LogWarning("NPCChoicePanel: OnMissionsClicked called before initialization!");
            return;
        }

        Debug.Log("NPCChoicePanel: Missions button clicked!");
        if (sourceNPC != null)
        {
            sourceNPC.OnMissionsSelected(); // Call NPC method
        }
        else
        {
            Debug.LogError("NPCChoicePanel: sourceNPC is null!");
        }
    }

    private Button FindButtonByName(string name)
    {
        // First try direct child search
        Transform directChild = transform.Find(name);
        if (directChild != null)
        {
            Button btn = directChild.GetComponent<Button>();
            if (btn != null) return btn;
        }

        // Then search all children (including inactive)
        foreach (Button btn in GetComponentsInChildren<Button>(true))
        {
            if (btn.gameObject.name == name)
                return btn;
        }
        
        return null;
    }

    private Button FindButtonByLabel(string label)
    {
        foreach (var btn in GetComponentsInChildren<Button>(true))
        {
            var txt = btn.GetComponentInChildren<Text>(true); // include inactive
            if (txt != null && txt.text.Trim().Equals(label, System.StringComparison.OrdinalIgnoreCase))
                return btn;
        }
        return null;
    }

    void OnDestroy()
    {
        // Clean up listeners when destroyed
        if (shopButton != null)
            shopButton.onClick.RemoveListener(OnShopClicked);
        if (missionsButton != null)
            missionsButton.onClick.RemoveListener(OnMissionsClicked);
    }
}
