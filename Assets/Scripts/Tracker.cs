using Unity.VisualScripting;
using UnityEngine;

public class Tracker : MonoBehaviour
{
    [SerializeField] private GameDataSO gameData;
    private Rigidbody _rigidbody;
    
    void Start()
    {
        gameData.ResetData();
        gameData.maxLap = 3;
       _rigidbody = gameObject.GetComponent<Rigidbody>();
    }
    
    void Update()
    {
        gameData.currentTotalTime += Time.deltaTime;
        gameData.currentLapTime += Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // TODO: probably need checkpoints to actually make sure player drove the whole track
        if (LayerMask.LayerToName(other.gameObject.layer) == "Finish")
        {
            gameData.lastLapTime = gameData.currentLapTime;
            Vector3 finishDirection = other.gameObject.transform.forward.normalized;
            Vector3 playerDirection = _rigidbody.linearVelocity.normalized;
            
            // Check if player is coming from right direction
            float dot = Vector3.Dot(finishDirection, playerDirection);
            if (dot > 0.5)
            {
                if (gameData.currentLapTime < gameData.bestLapTime || gameData.bestLapTime == 0)
                {
                    Debug.Log("Fastest lap!");
                    gameData.bestLapTime = gameData.currentLapTime;
                }
            
                if (gameData.currentLap == gameData.maxLap)
                {
                    Debug.Log("Finish");
                    gameData.totalTime = gameData.currentTotalTime;

                    if (HighScoreManager.Instance.IsEligibleForLeaderboard(gameData.totalTime))
                    {
                        Debug.Log("Eligible for leaderboard");
                        HighScoreManager.Instance.AddNewScore("", gameData.totalTime);
                    }
                    return;
                }
            
                gameData.currentLap++;
                Debug.Log($"Lap {gameData.currentLap}/{gameData.maxLap} | {gameData.currentLapTime}");
                gameData.currentLapTime = 0;
            }
            else
            {
                Debug.Log("Wrong direction!"); 
            }
        }
    }
}
