using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public ItemObject[] items;

    public static ItemObject GetItemByID(int id)
    {
        foreach (var item in GameManager.Instance.ItemDatabaseInstance.items)
        {
            if (item.itemID == id)
                return item;
        }

        Debug.LogError("Item ID not found: " + id);
        return null;
    }
}
