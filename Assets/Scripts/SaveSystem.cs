using System.IO;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public static class SaveSystem
{
    public static void SaveGame(SaveData saveData, string slot = "1")
    {
        string path = Path.Combine(Application.persistentDataPath, $"save_{slot}.doggo");
        string json = JsonUtility.ToJson(saveData);
        
        File.WriteAllText(path, json);
        Debug.Log("Game saved");
    }

    public static SaveData LoadGame(string slot = "1")
    {
        string path = Path.Combine(Application.persistentDataPath, $"save_{slot}.doggo");
        if (!File.Exists(path))
        {
            Debug.Log("No save file found, creating new one with defaults.");
            return new SaveData();
        }
        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SaveData>(json);
    }
}

[Serializable]
public class SaveData
{
    public HighScoreData highScores;

    public SaveData()
    {
        highScores = new HighScoreData(true);
    }
    
    public SaveData(HighScoreData data) {
        highScores = data;
    }
}


