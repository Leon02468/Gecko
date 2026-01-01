using UnityEngine;

public enum ItemType { Consumable, Material, Weapon, Misc, Money }

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemObject : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public ItemType type;
    public string description;
    public int maxStack = 1;
    
    // Only used for consumables
    public float healAmount = 0f;

    [Tooltip("A unique ID used for save/load")]
    public int itemID;
}
