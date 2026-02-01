using System.Collections.Generic;
using UnityEngine;

public class Tracker : MonoBehaviour
{
    [SerializeField] private GameDataSO gameData;
    [SerializeField] private UI_Updater uiUpdater;
    [Header("Lap validation")]
    [SerializeField] private List<Collider> requiredCheckpoints = new();

    [Header("Respawn")]
    [Tooltip("Updated automatically when a checkpoint collider is triggered.")]
    [SerializeField] private bool debugLogCheckpointUpdates;

    private Rigidbody _rigidbody;
    private PlayerController _playerController;

    private HashSet<Collider> _requiredCheckpointSet;
    private readonly HashSet<Collider> _triggeredCheckpoints = new();

    private bool _hasLastCheckpoint;
    private Vector3 _lastCheckpointPosition;
    private Quaternion _lastCheckpointRotation;

    public bool TryGetLastCheckpointPose(out Vector3 position, out Quaternion rotation)
    {
        position = _lastCheckpointPosition;
        rotation = _lastCheckpointRotation;
        return _hasLastCheckpoint;
    }
    
    void Start()
    {
        gameData.ResetData();
        gameData.maxLap = 3;
        _rigidbody = gameObject.GetComponent<Rigidbody>();
        _playerController = gameObject.GetComponent<PlayerController>();
        if (uiUpdater == null)
            uiUpdater = FindAnyObjectByType<UI_Updater>();

        _requiredCheckpointSet = new HashSet<Collider>();
        if (requiredCheckpoints != null)
        {
            foreach (var checkpoint in requiredCheckpoints)
            {
                if (checkpoint != null)
                    _requiredCheckpointSet.Add(checkpoint);
            }
        }
    }
    
    void Update()
    {
        if (gameData.isGameOver)
            return;
        gameData.currentTotalTime += Time.deltaTime;
        gameData.currentLapTime += Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gameData.isGameOver)
            return;

        if (_requiredCheckpointSet != null && _requiredCheckpointSet.Contains(other))
        {
            // Ignore duplicate triggers in the same lap.
            _triggeredCheckpoints.Add(other);

            _hasLastCheckpoint = true;
            _lastCheckpointPosition = other.transform.position;
            _lastCheckpointRotation = other.transform.rotation;
            if (debugLogCheckpointUpdates)
            {
                Debug.Log($"[Tracker] Last checkpoint updated: {other.name} @ {_lastCheckpointPosition}");
            }
            return;
        }

        if (LayerMask.LayerToName(other.gameObject.layer) == "Finish")
        {
            Vector3 finishDirection = other.gameObject.transform.forward.normalized;
            Vector3 playerDirection = _rigidbody.linearVelocity.normalized;
            
            // Check if player is coming from right direction
            float dot = Vector3.Dot(finishDirection, playerDirection);
            if (dot > 0.5)
            {
                if (!HasTriggeredAllRequiredCheckpoints())
                {
                    Debug.Log("Finish crossed, but not all checkpoints were triggered for this lap.");
                    return;
                }

                gameData.lastLapTime = gameData.currentLapTime;
                if (gameData.currentLapTime < gameData.bestLapTime || gameData.bestLapTime == 0)
                {
                    Debug.Log("Fastest lap!");
                    gameData.bestLapTime = gameData.currentLapTime;
                }
            
                if (gameData.currentLap == gameData.maxLap)
                {
                    Debug.Log("Finish");
                    gameData.totalTime = gameData.currentTotalTime;

                    gameData.isGameOver = true;
                    EndGame();

                    if (HighScoreManager.Instance.IsEligibleForLeaderboard(gameData.totalTime))
                    {
                        Debug.Log("Eligible for leaderboard");
                        HighScoreManager.Instance.AddNewScore("", gameData.totalTime);
                    }

                    _triggeredCheckpoints.Clear();
                    return;
                }
            
                gameData.currentLap++;
                Debug.Log($"Lap {gameData.currentLap}/{gameData.maxLap} | {gameData.currentLapTime}");
                gameData.currentLapTime = 0;
                _triggeredCheckpoints.Clear();
            }
            else
            {
                Debug.Log("Wrong direction!"); 
            }
        }
    }

    private bool HasTriggeredAllRequiredCheckpoints()
    {
        if (_requiredCheckpointSet == null || _requiredCheckpointSet.Count == 0)
            return true;

        foreach (var checkpoint in _requiredCheckpointSet)
        {
            if (!_triggeredCheckpoints.Contains(checkpoint))
                return false;
        }

        return true;
    }

    private void EndGame()
    {
        if (_playerController != null)
            _playerController.enabled = false;

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        if (uiUpdater != null)
            uiUpdater.ActivateEndPanel();
        else
            Debug.LogWarning("[Tracker] End game reached, but no UI_Updater reference was found.");
    }
}
