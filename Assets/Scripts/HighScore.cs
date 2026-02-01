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

        // Backwards-compatibility: older versions created dummy entries as "John Doe" with a score of 100s,
        // which incorrectly surfaced as a real best time (01:40). If we detect that legacy placeholder data,
        // replace it with the current baseline times.
        if (data.highScores.Count > 0 && data.highScores.All(e => e.playerName == "John Doe" && Mathf.Approximately(e.score, 100f)))
            data = new HighScoreData(true);

        SortScores();
        Debug.Log("Loaded high scores");
        Debug.Log(data);
    }
    
    private void SortScores() => data.highScores = data.highScores.OrderBy(e => e.score).ToList();

    private static bool IsBaselinePlaceholderEntry(HighScoreEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.playerName))
            return false;

        // Baseline entries are seeded as "TIME 1..10".
        return entry.playerName.StartsWith("TIME ", StringComparison.OrdinalIgnoreCase);
    }

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

    /// <summary>
    /// Returns the player's best time (lowest) excluding baseline placeholder entries.
    /// </summary>
    public bool TryGetBestPlayerScore(out float bestPlayerScore)
    {
        bestPlayerScore = 0f;
        if (data == null || data.highScores == null || data.highScores.Count == 0)
            return false;

        float best = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < data.highScores.Count; i++)
        {
            var e = data.highScores[i];
            if (IsBaselinePlaceholderEntry(e))
                continue;

            if (!found || e.score < best)
            {
                best = e.score;
                found = true;
            }
        }

        if (!found)
            return false;

        bestPlayerScore = best;
        return true;
    }

    /// <summary>
    /// Returns the worst (highest) baseline time. Used as default display when no player time exists.
    /// </summary>
    public bool TryGetWorstBaselineScore(out float worstBaselineScore)
    {
        worstBaselineScore = 0f;
        if (data == null || data.highScores == null || data.highScores.Count == 0)
            return false;

        float worst = float.NegativeInfinity;
        bool found = false;
        for (int i = 0; i < data.highScores.Count; i++)
        {
            var e = data.highScores[i];
            if (!IsBaselinePlaceholderEntry(e))
                continue;

            if (!found || e.score > worst)
            {
                worst = e.score;
                found = true;
            }
        }

        if (!found)
            return false;

        worstBaselineScore = worst;
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
        // Default ctor is used by `HighScoreData(true)` when creating placeholder leaderboard entries.
        // Keep it as a safe fallback, but the actual baseline times are defined in `HighScoreData`.
        this.playerName = "TIME";
        this.score = 9999f;
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
            // Baseline leaderboard times (seconds). Lower is better.
            // Keep this in sync with UI baseline times so that when there is no player best,
            // the displayed BEST TIME matches the fastest baseline entry.
            float[] baselineTimesSeconds =
            {
                165f,
                195f,
                225f,
                255f,
                285f,
                315f,
                336f,
                360f,
                390f,
                420f
            };

            for (int i = 0; i < baselineTimesSeconds.Length; ++i)
                highScores.Add(new HighScoreEntry($"TIME {i + 1}", baselineTimesSeconds[i]));
        }
    }

    public override string ToString()
    {
        String str = "";
        
        for(int i = 0; i < highScores.Count; ++i) str +=  $"{highScores[i]}\n";
        
        return str;
    }
}
