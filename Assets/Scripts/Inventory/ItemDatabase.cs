using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    public ItemObject[] items;

    private void Awake()
    {
        Instance = this;
    }

    public static ItemObject GetItemByID(int id)
    {
        foreach (var item in Instance.items)
        {
            if (item.itemID == id)
                return item;
        }

        Debug.LogError("Item ID not found: " + id);
        return null;
    }
}
