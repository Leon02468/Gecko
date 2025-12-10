using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour
{
    public TMP_Text slotTitle;
    public TMP_Text slotDetails;
    public Button playButton;    // single primary action
    public TMP_Text playButtonText;  // label on the play button (child Text)
    public Button deleteButton;

    int slotIndex;

    public void Setup(int index)
    {
        slotIndex = index;
        slotTitle.text = $"Slot {index + 1}";

        // Clear previous listeners to avoid duplicate calls
        playButton.onClick.RemoveAllListeners();
        deleteButton.onClick.RemoveAllListeners();

        if (SaveSystem.SlotExists(index))
        {
            var save = SaveSystem.LoadSlot(index);

            // safe guard: save might be null if load failed
            if (save != null)
            {
                // show name + local time
                DateTime local = save.SavedAtUtc.ToLocalTime();
                slotDetails.text = $"Saved: {local}";
            }
            else
            {
                slotDetails.text = "Corrupt save (will start new if played)";
            }

            playButtonText.text = "Confirm";
            playButton.interactable = true;
            playButton.onClick.AddListener(() => OnPlayExisting());

            deleteButton.interactable = true;
            deleteButton.onClick.AddListener(() => OnDeleteClicked());
        }
        else
        {
            slotDetails.text = "Empty";
            playButtonText.text = "Sign";
            playButton.interactable = true;
            playButton.onClick.AddListener(() => OnPlayNew());

            deleteButton.interactable = false; // nothing to delete
        }
    }

    // If there is a save: load it and go to gameplay (or intro depending on GameManager)
    void OnPlayExisting()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager missing in scene.");
            return;
        }
        
        GameManager.Instance.LoadGame(slotIndex);
    }

    // If empty: create default save, save it, then proceed (StartNewGame)
    void OnPlayNew()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager missing in scene.");
            return;
        }

        GameManager.Instance.StartNewGame(slotIndex);
    }

    void OnDeleteClicked()
    {
        SaveSystem.DeleteSlot(slotIndex);
        Setup(slotIndex); // refresh UI
    }
}
