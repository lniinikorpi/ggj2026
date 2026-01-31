using Unity.Properties;
using UnityEngine;

[CreateAssetMenu(fileName = "GameDataSO", menuName = "ScriptableObjects/GameDataSO")]
public class GameDataSO : ScriptableObject
{
    public int score;
    public int currentLap = 1;
    public int maxLap;
    public float bestLapTime;
    public float bestTotalTime;
    public float totalTime;
    public float lastLapTime;
    public float currentTotalTime;
    public float currentLapTime;
    
    [CreateProperty] private string LapString => $"{currentLap}/{maxLap}";

    [CreateProperty]
    private string LapTime =>
        $"{Mathf.FloorToInt(currentLapTime % 60):00}:{Mathf.RoundToInt((currentLapTime % 1f) * 1000f):00}";

    public void ResetData()
    {
        score = 0;
        currentLap = 1;
        currentTotalTime = 0;
        currentLapTime = 0;
        bestLapTime = 0;
        bestTotalTime = 0;
        lastLapTime = 0;
    }
}
