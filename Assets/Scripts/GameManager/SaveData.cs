using System;
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
    public float playerHealth;
    public int level;

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
        s.sceneBuildIndex = 0;
        s.playerX = 0f;
        s.playerY = 0f;
        s.playerHealth = 5;
        s.level = 1;
        return s;
    }
}
