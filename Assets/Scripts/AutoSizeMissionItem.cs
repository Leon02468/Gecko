using UnityEngine;
using UnityEngine.UI;
using System.Reflection; // Added for reflection support

/// <summary>
/// Automatically resizes the mission item based on its content.
/// Attach this to your MissionItem prefab.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[ExecuteInEditMode]
public class AutoSizeMissionItem : MonoBehaviour
{
    [Header("References")]
    public Text paragraphText; // Use Unity's standard Text component for compatibility
    public Image missionImage;
    public Button buttonIcon;

    [Header("Size Settings")]
    [Tooltip("Padding around the text content")]
    public float topPadding = 10f;
    public float bottomPadding = 10f;
    public float leftPadding = 10f;
    public float rightPadding = 10f;

    [Tooltip("Minimum height of the mission item")]
    public float minHeight = 60f;

    [Tooltip("Maximum height of the mission item")]
    public float maxHeight = 200f;

    [Tooltip("Image width (if image is present)")]
    public float imageWidth = 80f;

    [Tooltip("Button width")]
    public float buttonWidth = 60f;

    [Tooltip("Spacing between elements")]
    public float elementSpacing = 10f;

    private RectTransform rectTransform;
    private LayoutElement layoutElement;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Add or get LayoutElement component
        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        // Auto-find references if not assigned
        if (paragraphText == null)
        {
            // Try to find TextMeshPro component first (without direct reference)
            var tmpText = GetComponentInChildren(System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro"));
            if (tmpText != null)
            {
                paragraphText = tmpText as Text; // Won't work, but will be handled below
            }
            
            if (paragraphText == null)
                paragraphText = GetComponentInChildren<Text>();
        }
        if (missionImage == null)
            missionImage = GetComponentInChildren<Image>();
    }

    void Start()
    {
        // Initial resize
        ResizeToContent();
    }

    void OnEnable()
    {
        // Resize when enabled
        ResizeToContent();
    }

    void LateUpdate()
    {
        // Continuously resize in editor for live preview
        if (!Application.isPlaying)
        {
            ResizeToContent();
        }
    }

    /// <summary>
    /// Call this after updating the text to resize the item
    /// </summary>
    public void ResizeToContent()
    {
        if (layoutElement == null)
            return;

        // Force the text to update its layout
        Canvas.ForceUpdateCanvases();

        float textPreferredHeight = 0f;

        // Try to get preferred height using reflection to support both Text and TextMeshPro
        if (paragraphText != null)
        {
            // Check if it's TextMeshPro using reflection
            var textComponent = paragraphText.GetComponent(System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro"));
            if (textComponent != null)
            {
                // Force mesh update for TMP
                var forceMethod = textComponent.GetType().GetMethod("ForceMeshUpdate", 
                    BindingFlags.Public | BindingFlags.Instance);
                if (forceMethod != null)
                {
                    forceMethod.Invoke(textComponent, new object[] { true, true });
                }

                // Get preferred height
                var preferredProperty = textComponent.GetType().GetProperty("preferredHeight");
                if (preferredProperty != null)
                {
                    textPreferredHeight = (float)preferredProperty.GetValue(textComponent);
                }
            }
            else if (paragraphText is Text standardText)
            {
                // Standard Text component
                textPreferredHeight = standardText.preferredHeight;
            }
        }

        // Calculate total content height
        float contentHeight = topPadding + textPreferredHeight + bottomPadding;

        // Clamp to min/max
        contentHeight = Mathf.Clamp(contentHeight, minHeight, maxHeight);

        // Apply to LayoutElement
        layoutElement.minHeight = contentHeight;
        layoutElement.preferredHeight = contentHeight;
        layoutElement.flexibleHeight = 0; // Don't let it expand beyond preferred

        // Log for debugging (remove in production)
        if (Application.isPlaying)
        {
            Debug.Log($"[AutoSizeMissionItem] Resized to {contentHeight}px (text: {textPreferredHeight}px)");
        }
    }

    /// <summary>
    /// Force update the layout after text changes
    /// </summary>
    public void UpdateLayout()
    {
        ResizeToContent();
        
        // Force parent layout group to rebuild
        if (transform.parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
        }
    }

#if UNITY_EDITOR
    // Update in editor when values change
    void OnValidate()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (layoutElement == null)
            layoutElement = GetComponent<LayoutElement>();
        
        ResizeToContent();
    }
#endif
}
