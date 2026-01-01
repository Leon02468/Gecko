using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemObject itemObject;
    public int quantity = 1;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            int leftover = GameManager.Instance.InventoryInstance.AddItem(itemObject, quantity);
            if (leftover <= 0) Destroy(gameObject);
            else quantity = leftover;
        }
    }

}
