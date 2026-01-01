using System;
using System.Collections.Generic;
using UnityEngine;

public class SaveData
{
    public string saveName;

    // Serialize a primitive (ticks) — JsonUtility handles this fine
    public long savedAtTicks;

    // Example gameplay fields
    public int sceneBuildIndex; // optional
    public float playerX;
    public float playerY;
    public float playerHealth; // Current health
    public float playerMaxHealth; // Maximum health capacity
    public int level;

    [System.Serializable]
    public class InventorySlotData
    {
        public int itemID;
        public int quantity;
    }

    public List<InventorySlotData> inventory;
    public int money;

    /// <summary>
    /// Mission progress data for saving/loading
    /// </summary>
    [System.Serializable]
    public class MissionData
    {
        /// <summary>Mission ID</summary>
        public string id;
        /// <summary>Current kill count/progress</summary>
        public int currentCount;
        /// <summary>Saved progress when mission was cancelled (for switching missions)</summary>
        public int savedProgress;
        /// <summary>Mission status: 0=Available, 1=Active, 2=Completed, 3=Claimed</summary>
        public int status;
    }

    /// <summary>
    /// List of all mission progress data
    /// </summary>
    public List<MissionData> missions;

    public DateTime SavedAtUtc
    {
        get => DateTime.SpecifyKind(new DateTime(savedAtTicks), DateTimeKind.Utc);
        set => savedAtTicks = value.ToUniversalTime().Ticks;
    }

    public static SaveData CreateDefault()
    {
        var s = new SaveData();
        s.saveName = "New Game";
        s.SavedAtUtc = DateTime.UtcNow;
        s.sceneBuildIndex = 2;
        s.playerX = 123.18f;
        s.playerY = -122.75f;
        s.playerHealth = 5;
        s.playerMaxHealth = 5; // Default max health
        s.level = 1;
        s.missions = new List<MissionData>(); // Initialize empty mission list
        return s;
    }
}
