using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarUI : MonoBehaviour
{
    [Header("Hotbar Settings")]
    public Transform hotbarContainer;
    public GameObject hotbarSlotPrefab; // Prefab with Image + TMP_Text for icon and quantity
    public float slotSpacing = 10f;
    public float slotSize = 60f;
    public int hotbarSlots = 5;

    private InventoryManager inventoryManager;

    void Start()
    {
        inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null || inventoryManager.itemSlot == null)
        {
            Debug.LogError("HotbarUI: InventoryManager or itemSlot not found!");
            enabled = false;
            return;
        }

        if (hotbarContainer == null)
        {
            GameObject container = new GameObject("HotbarContainer");
            container.transform.SetParent(transform, false);
            hotbarContainer = container.transform;

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0); // Center bottom
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 40f); // 40px from bottom
        }

        InitializeHotbar();
    }

    void InitializeHotbar()
    {
        foreach (Transform child in hotbarContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < hotbarSlots; i++)
        {
            GameObject slotObj = hotbarSlotPrefab != null
                ? Instantiate(hotbarSlotPrefab, hotbarContainer)
                : new GameObject($"HotbarSlot_{i}", typeof(RectTransform), typeof(Image));

            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(slotSize, slotSize);
            slotRect.anchorMin = slotRect.anchorMax = slotRect.pivot = new Vector2(0, 0.5f);
            slotRect.anchoredPosition = new Vector2(i * (slotSize + slotSpacing), 0);

            // Add TMP_Text if prefab is not used
            if (hotbarSlotPrefab == null)
            {
                var textObj = new GameObject("QtyText", typeof(RectTransform), typeof(TMP_Text));
                textObj.transform.SetParent(slotObj.transform, false);
                var qtyText = textObj.GetComponent<TMP_Text>();
                qtyText.fontSize = 24;
                qtyText.alignment = TextAlignmentOptions.BottomRight;
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(1, 0);
                textRect.sizeDelta = new Vector2(slotSize, slotSize);
                textRect.anchoredPosition = Vector2.zero;
            }
        }
    }

    void Update()
    {
        UpdateHotbar();
    }

    void UpdateHotbar()
    {
        if (inventoryManager == null || inventoryManager.itemSlot == null) return;

        for (int i = 0; i < hotbarSlots; i++)
        {
            if (i >= hotbarContainer.childCount) break;
            var slot = inventoryManager.itemSlot[i];
            var slotObj = hotbarContainer.GetChild(i).gameObject;
            var icon = slotObj.GetComponent<Image>();
            var qtyText = slotObj.GetComponentInChildren<TMP_Text>();

            if (slot != null && slot.item != null && slot.quantity > 0)
            {
                icon.enabled = true;
                icon.sprite = slot.item.icon;
                qtyText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
            }
            else
            {
                icon.enabled = false;
                qtyText.text = "";
            }
        }
    }
}
