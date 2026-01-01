using UnityEngine;

public enum ItemType { Consumable, Material, Weapon, Misc }

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemObject : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public ItemType type;
    public string description;
    public int maxStack = 1;
    // In ItemObject.cs
    public float healAmount = 0f; // Only used for consumables
    public bool isUnknownFruit = false; // unknow fruit check

    public bool isSpeedFruit = false; // Mark this fruit as a speed buff
    public float speedBuffAmount = 2f; // How much to increase speed
    public float speedBuffDuration = 5f; // Duration in seconds


    [Tooltip("A unique ID used for save/load")]
    public int itemID;
}
