using System;
using System.Collections.Generic;
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
        if (data == null)
            data = new HighScoreData(true);

        if (data.highScores == null)
            data.highScores = new List<HighScoreEntry>();

        if (data.highScores.Count == 0)
            data = new HighScoreData(true);

        SortScores();
        Debug.Log("Loaded high scores");
        Debug.Log(data);
    }
    
    private void SortScores() => data.highScores = data.highScores.OrderBy(e => e.score).ToList();

    /// <summary>
    /// Returns the leaderboard index a score would have in the current leaderboard.
    /// 0 = best (lowest time). Clamped to last index.
    /// </summary>
    public int GetLeaderboardIndexForScore(float score)
    {
        if (data == null || data.highScores == null || data.highScores.Count == 0)
            return 0;

        SortScores();
        for (int i = 0; i < data.highScores.Count; i++)
        {
            if (score <= data.highScores[i].score)
                return i;
        }

        return data.highScores.Count - 1;
    }
    
    // is higher than last one in the leaderboards
    public bool IsEligibleForLeaderboard(float score) => score < data.highScores[^1].score;

    public bool TryGetBestScore(out float bestScore)
    {
        bestScore = 0;
        if (data == null || data.highScores == null || data.highScores.Count == 0)
            return false;

        // Data is kept sorted ascending (lower time is better).
        bestScore = data.highScores[0].score;
        return true;
    }

    public void AddNewScore(string playerName, float score)
    {
        if(string.IsNullOrWhiteSpace(playerName)) playerName = "Anonymous";

        var entry = new HighScoreEntry(playerName, score);
        data.highScores.Add(entry);
        SortScores();
        Debug.Log($"Added new high score {data}");
        
        if (data.highScores.Count > MAX_SCORES) data.highScores = data.highScores.Take(MAX_SCORES).ToList();

        // Persist highscores AND the player's placement without wiping other save fields.
        SaveData save = SaveSystem.LoadGame();
        save.highScores = data;

        int index = data.highScores.IndexOf(entry);
        // In case the entry was trimmed out for any reason, fall back to last.
        if (index < 0) index = Mathf.Max(0, data.highScores.Count - 1);
        save.playerLeaderboardIndex = index;

        SaveSystem.SaveGame(save);
    }

    public string GetHighScores()
    {
        return data.ToString();
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
        String str = "";
        
        for(int i = 0; i < highScores.Count; ++i) str +=  $"{highScores[i]}\n";
        
        return str;
    }
}
