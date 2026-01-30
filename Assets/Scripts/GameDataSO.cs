using UnityEngine;

[CreateAssetMenu(fileName = "GameDataSO", menuName = "ScriptableObjects/GameDataSO")]
public class GameDataSO : ScriptableObject
{
    public int score;
    public int currentLap;
    public int maxLap;
    public float bestLapTime;
    public float bestTotalTime;
    
    
    public void ResetData()
    {
        score = 0;
        currentLap = 0;
        bestLapTime = 0;
        bestTotalTime = 0;
    }
}
