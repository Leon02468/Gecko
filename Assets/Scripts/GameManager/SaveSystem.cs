using System;
using UnityEngine;
using System.IO;

public static class SaveSystem
{
    const string filePrefix = "save_slot_";
    const string fileExt = ".json";

    static string GetPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, filePrefix + slot + fileExt);
    }

    public static void SaveSlot(int slot, SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetPath(slot), json);
            Debug.Log($"Saved slot {slot} to {GetPath(slot)}");
        }
        catch (Exception e)
        {
            Debug.LogError("Save failed: " + e);
        }
    }

    public static bool SlotExists(int slot)
    {
        return File.Exists(GetPath(slot));
    }

    public static SaveData LoadSlot(int slot)
    {
        try
        {
            string path = GetPath(slot);
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"Loaded slot {slot} from {path}");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError("Load failed: " + e);
            return null;
        }
    }

    public static void DeleteSlot(int slot)
    {
        string path = GetPath(slot);
        if (File.Exists(path)) File.Delete(path);
    }
}
