using System.IO;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public static class SaveSystem
{
    public static void SaveGame(SaveData saveData, string slot = "save1")
    {
        string path = Path.Combine(Application.persistentDataPath, $"{slot}.doggo");
        string json = JsonUtility.ToJson(saveData);
        
        File.WriteAllText(json, path);
        Debug.Log("Game saved");
    }

    public static SaveData LoadGame(string slot = "save1")
    {
        string path = Path.Combine(Application.persistentDataPath, $"{slot}.doggo");
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
}

[Serializable]
public class HighScoreEntry
{
    public string playerName;
    public float score;
    
    public HighScoreEntry(string playerName, float score)
    {
        this.playerName = playerName;
        this.score = score;
    }

    public HighScoreEntry()
    {
        this.playerName = "John Doe";
        this.score = 100;
    }
}

[Serializable]
public class HighScoreData
{
    private const int MAX_SCORES = 10;
    private List<HighScoreEntry> _highScores = new List<HighScoreEntry>();

    // is higher than last one in the leaderboards
    public bool IsEligibleForLeaderboard(float score) => score > _highScores[^1].score;

    public void AddNewScore(string playerName, float score)
    {
        if(string.IsNullOrWhiteSpace(playerName)) playerName = "Anonymous";
        
        _highScores.Add(new HighScoreEntry(playerName, score));

        if (_highScores.Count > MAX_SCORES) _highScores = _highScores.Take(MAX_SCORES).ToList();
        
        //TODO: save highScores after adding new entry
    }
    
    public List<HighScoreEntry> GetHighScores => _highScores;

    private void SortScores() => _highScores = _highScores.OrderByDescending(e => e.score).ToList();
    
}

