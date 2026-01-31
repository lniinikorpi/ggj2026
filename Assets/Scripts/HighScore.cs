using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class HighScoreManager : MonoBehaviour
{
    private const int MAX_SCORES = 10;
    private HighScoreData data;
    
    public static HighScoreManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        LoadHighscores();
    }
    
    private void LoadHighscores()
    {
        data = SaveSystem.LoadGame().highScores;
        Debug.Log("Loaded high scores");
        Debug.Log(data);
    }
    
    private void SortScores() => data.highScores = data.highScores.OrderBy(e => e.score).ToList();
    
    // is higher than last one in the leaderboards
    public bool IsEligibleForLeaderboard(float score) => score < data.highScores[^1].score;

    public void AddNewScore(string playerName, float score)
    {
        if(string.IsNullOrWhiteSpace(playerName)) playerName = "Anonymous";
        
        data.highScores.Add(new HighScoreEntry(playerName, score));
        SortScores();
        Debug.Log($"Added new high score {data}");
        
        if (data.highScores.Count > MAX_SCORES) data.highScores = data.highScores.Take(MAX_SCORES).ToList();
        
        SaveSystem.SaveGame(new SaveData(data));
    }
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

    public override string ToString() => $"{playerName} : {score}";
}

[Serializable]
public class HighScoreData
{
    public List<HighScoreEntry> highScores = new List<HighScoreEntry>();
    
    public HighScoreData(bool dummy = false)
    {
        if (dummy)
        {
            for (int i = 0; i < 10; ++i)
                highScores.Add(new HighScoreEntry());
        }
    }

    public override string ToString()
    {
        String str = "Highscores: \n";
        
        for(int i = 0; i < highScores.Count; ++i) str +=  $"{highScores[i]}\n";
        
        return str;
    }
}
