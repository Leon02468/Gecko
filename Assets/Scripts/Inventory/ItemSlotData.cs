using System;
using UnityEngine;


[Serializable]
public class ItemSlotData
{
    public string itemName;
    public int quantity;
    public string itemSpriteName; // Save sprite name, not the Sprite object
    public bool isFull;
    public string itemDescription;
}