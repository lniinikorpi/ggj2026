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
    public const float TrickBaseScore = 100;
    private string _lastTrickName = "none";
    private int _trickMultiplier = 1;
    private float _trickPointPool = 0;
    private int _consecutiveTricks = 1;
    
    public void DoTrick(TrickData trick)
    {
        // If new trick - increase multi
        if(trick.name != _lastTrickName)
        {
            _trickMultiplier++;
            _trickPointPool += TrickBaseScore;
            _lastTrickName = trick.name;
            _consecutiveTricks = 1;
        }else
        {
            _consecutiveTricks++;
            // Reduce added base score if consecutive tricks
            _trickPointPool += TrickBaseScore / _consecutiveTricks;
            _lastTrickName = trick.name;
        }
    }
    // Calculate score after enough time elapsed after last trick
    public void CalculateTrickScore()
    {
       score += Mathf.FloorToInt(_trickPointPool * _trickMultiplier);
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
        _trickMultiplier = 1;
        _trickPointPool = 0;
    }
}
