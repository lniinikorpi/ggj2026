using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;

[CreateAssetMenu(fileName = "GameDataSO", menuName = "ScriptableObjects/GameDataSO")]
public class GameDataSO : ScriptableObject
{
    // Laps & Time
    public int currentLap = 1;
    public int maxLap;
    public float bestLapTime;
    public float bestTotalTime;
    public float totalTime;
    public float lastLapTime;
    public float currentTotalTime;
    public float currentLapTime;
    
    // Trick
    public int score;
    public List<string> trickNames;
    public int trickMultiplier = 1;
    public float trickPointPool = 0;
    private const float TrickBaseScore = 100;
    private string _lastTrickName = "none";
    private int _consecutiveTricks = 1;
    
    public void DoTrick(TrickData trick)
    {
        //Debug.Log($"DoTrick {trick.name}");
        trickNames.Add(trick.name);
        // If new trick - increase multi
        if(trick.name != _lastTrickName)
        {
            trickMultiplier++;
            trickPointPool += TrickBaseScore;
            _lastTrickName = trick.name;
            _consecutiveTricks = 1;
        }else
        {
            _consecutiveTricks++;
            // Reduce added base score if consecutive tricks
            trickPointPool += TrickBaseScore / _consecutiveTricks;
            _lastTrickName = trick.name;
        }
    }
    // Calculate score after enough time elapsed after last trick
    public void CalculateTrickScore()
    {
       score += Mathf.FloorToInt(trickPointPool * trickMultiplier);
       trickNames.Clear();
       trickPointPool = 0;
       trickMultiplier = 1;
       _consecutiveTricks = 1;
       _lastTrickName = "none";
    }
    
    // String properties to get formatted string for UI
    [CreateProperty] private string LapString => $"{currentLap}/{maxLap}";
    [CreateProperty]
    private string LapTime =>
        $"{Mathf.FloorToInt(currentLapTime % 60):00}:{Mathf.RoundToInt((currentLapTime % 1f) * 1000f):000}";
    [CreateProperty] private string BestLap => $"Best {Mathf.FloorToInt(bestLapTime % 60):00}:{Mathf.RoundToInt((bestLapTime % 1f) * 1000f):000}";
    [CreateProperty] private string LastLap => $"Last {Mathf.FloorToInt(lastLapTime % 60):00}:{Mathf.RoundToInt((lastLapTime % 1f) * 1000f):000}";

    public void ResetData()
    {
        score = 0;
        currentLap = 1;
        currentTotalTime = 0;
        currentLapTime = 0;
        bestLapTime = 0;
        bestTotalTime = 0;
        lastLapTime = 0;
        trickMultiplier = 1;
        trickPointPool = 0;
        trickNames.Clear();
    }
}
