using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public GameObject slotPanel;
    public GameObject slotPrefab;
    public Transform slotListParent;    

    int slotCount = 3;

    void Start()
    {
        slotCount = GameManager.Instance != null ? GameManager.Instance.slotCount : 3;
    }

    public void OnStartButton()
    {
        PopulateSlots();
    }

    void PopulateSlots()
    {
        if (slotPrefab == null)
        {
            Debug.LogError("Slot Prefab is not assigned in MainMenuController.");
            return;
        }

        if (slotListParent == null)
        {
            Debug.LogError("slotListParent is not assigned in MainMenuController!");
            return;
        }

        for (int i = slotListParent.childCount - 1; i >= 0; i--)
        {
            Transform child = slotListParent.GetChild(i);
            if (child == null) continue;
            // Only destroy generated slot entries (which should have SlotUI component)
            if (child.GetComponent<SlotUI>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        for (int i = 0; i < slotCount; i++)
        {
            GameObject go = Instantiate(slotPrefab, slotListParent);
            go.name = $"Slot_{i + 1}";
            go.SetActive(true);

            SlotUI ui = go.GetComponent<SlotUI>();
            if (ui != null)
            {
                ui.Setup(i);
            }
            else
            {
                Debug.LogWarning("Instantiated slotPrefab does not contain SlotUI component!");
            }
        }
    }

    public void OnQuit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
