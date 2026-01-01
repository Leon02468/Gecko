using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays player health as hearts in the top-left corner of the screen.
/// Supports full hearts, empty hearts, and blinking low health animation.
/// </summary>
public class HeartHealthUI : MonoBehaviour
{
    [Header("Heart Sprites")]
    [Tooltip("Sprite for a full heart")]
    public Sprite fullHeartSprite;
    
    [Tooltip("Sprite for an empty heart")]
    public Sprite emptyHeartSprite;
    
    [Header("Low Health Animation")]
    [Tooltip("Enable blinking animation when health is low")]
    public bool enableLowHealthBlink = true;
    
    [Tooltip("Health threshold for low health animation (e.g., 1 means 1 HP or less)")]
    public float lowHealthThreshold = 1f;
    
    [Tooltip("Speed of the blinking animation")]
    public float blinkSpeed = 2f;
    
    [Header("Heart Layout")]
    [Tooltip("Horizontal spacing between hearts")]
    public float heartSpacing = 10f;
    
    [Tooltip("Size of each heart (width and height)")]
    public float heartSize = 50f;
    
    [Tooltip("Padding from the top-left corner of the screen")]
    public Vector2 cornerPadding = new Vector2(20f, 20f);
    
    [Header("References")]
    [Tooltip("Parent object to hold all heart images (will be created if not assigned)")]
    public Transform heartsContainer;
    
    [Tooltip("Prefab for heart UI element (Image component). If null, will create default.")]
    public GameObject heartPrefab;
    
    private PlayerHealth playerHealth;
    private Image[] heartImages;
    private Animator[] heartAnimators; // Optional: if you want to use Animator for blinking
    private float blinkTimer = 0f;
    private bool blinkState = true;
    
    void Start()
    {
        // Find the player health component
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("HeartHealthUI: PlayerHealth component not found in scene!");
            enabled = false;
            return;
        }
        
        // Create hearts container if not assigned
        if (heartsContainer == null)
        {
            GameObject container = new GameObject("HeartsContainer");
            container.transform.SetParent(transform, false);
            heartsContainer = container.transform;
            
            // Set up RectTransform for top-left positioning
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.pivot = new Vector2(0, 1);
            containerRect.anchoredPosition = cornerPadding;
        }
        
        InitializeHearts();
    }
    
    void InitializeHearts()
    {
        int maxHearts = Mathf.CeilToInt(playerHealth.CurrentHP);
        
        // Clear existing hearts if any
        foreach (Transform child in heartsContainer)
        {
            Destroy(child.gameObject);
        }
        
        heartImages = new Image[maxHearts];
        heartAnimators = new Animator[maxHearts];
        
        // Create heart UI elements
        for (int i = 0; i < maxHearts; i++)
        {
            GameObject heartObj;
            
            if (heartPrefab != null)
            {
                heartObj = Instantiate(heartPrefab, heartsContainer);
            }
            else
            {
                // Create default heart
                heartObj = new GameObject($"Heart_{i}");
                heartObj.transform.SetParent(heartsContainer, false);
                heartImages[i] = heartObj.AddComponent<Image>();
            }
            
            // Get or add Image component
            if (heartImages[i] == null)
            {
                heartImages[i] = heartObj.GetComponent<Image>();
                if (heartImages[i] == null)
                {
                    heartImages[i] = heartObj.AddComponent<Image>();
                }
            }
            
            // Set up RectTransform for positioning
            RectTransform heartRect = heartObj.GetComponent<RectTransform>();
            if (heartRect == null)
            {
                heartRect = heartObj.AddComponent<RectTransform>();
            }
            
            heartRect.anchorMin = new Vector2(0, 1);
            heartRect.anchorMax = new Vector2(0, 1);
            heartRect.pivot = new Vector2(0, 1);
            heartRect.sizeDelta = new Vector2(heartSize, heartSize);
            heartRect.anchoredPosition = new Vector2(i * (heartSize + heartSpacing), 0);
            
            // Set initial sprite
            heartImages[i].sprite = fullHeartSprite;
            
            // Check for Animator component (optional)
            heartAnimators[i] = heartObj.GetComponent<Animator>();
        }
    }
    
    void Update()
    {
        if (playerHealth == null || heartImages == null)
            return;
        
        UpdateHearts();
        
        // Handle low health blinking
        if (enableLowHealthBlink && playerHealth.CurrentHP <= lowHealthThreshold && playerHealth.CurrentHP > 0)
        {
            HandleLowHealthBlink();
        }
        else
        {
            // Reset blink state when not in low health
            blinkState = true;
            blinkTimer = 0f;
            SetHeartsAlpha(1f);
        }
    }
    
    void UpdateHearts()
    {
        float currentHP = playerHealth.CurrentHP;
        
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;
            
            // Determine if this heart should be full or empty
            if (i < Mathf.Floor(currentHP))
            {
                // Full heart
                heartImages[i].sprite = fullHeartSprite;
                heartImages[i].enabled = true;
            }
            else if (i < Mathf.Ceil(currentHP) && currentHP % 1 != 0)
            {
                // Half heart (if you want to support it, otherwise it will be empty)
                // For now, treat fractional HP as empty heart
                heartImages[i].sprite = emptyHeartSprite;
                heartImages[i].enabled = true;
            }
            else
            {
                // Empty heart
                heartImages[i].sprite = emptyHeartSprite;
                heartImages[i].enabled = true;
            }
        }
    }
    
    void HandleLowHealthBlink()
    {
        blinkTimer += Time.deltaTime * blinkSpeed;
        
        if (blinkTimer >= 1f)
        {
            blinkTimer = 0f;
            blinkState = !blinkState;
            
            // Toggle visibility of hearts
            SetHeartsAlpha(blinkState ? 1f : 0.3f);
        }
    }
    
    void SetHeartsAlpha(float alpha)
    {
        foreach (var heartImage in heartImages)
        {
            if (heartImage != null)
            {
                Color color = heartImage.color;
                color.a = alpha;
                heartImage.color = color;
            }
        }
    }
    
    /// <summary>
    /// Call this if the player's max health changes to reinitialize hearts
    /// </summary>
    public void RefreshHearts()
    {
        InitializeHearts();
    }
}
