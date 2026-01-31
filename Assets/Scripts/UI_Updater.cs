using System;
using TMPro;
using UnityEngine;

public class UI_Updater : MonoBehaviour
{
    [Header("UI Object references")]
    [SerializeField] private GameObject _totalScore;
    [SerializeField] private GameObject  _currentTrickScore;
    [SerializeField] private GameObject  _trickDescription;
    [SerializeField] private GameObject  _currentTime;
    [SerializeField] private GameObject  _bestTime;
    [SerializeField] private GameObject  _lapInfo;
    [Header("Data object")]
    [SerializeField] private GameDataSO gameData;

   private TMP_Text _totalScoreText;
   private TMP_Text _currentTrickScoreText;
   private TMP_Text _trickDescriptionText;
   private TMP_Text _currentTimeText;
   private TMP_Text _bestTimeText;
   private TMP_Text _lapInfoText;

    private void Awake()
    {
        _totalScoreText = _totalScore.GetComponent<TextMeshProUGUI>();
        _currentTrickScoreText = _currentTrickScore.GetComponent<TextMeshProUGUI>();
        _trickDescriptionText = _trickDescription.GetComponent<TextMeshProUGUI>();
        _currentTimeText = _currentTime.GetComponent<TextMeshProUGUI>();
        _bestTimeText = _bestTime.GetComponent<TextMeshProUGUI>();
        _lapInfoText = _lapInfo.GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        _trickDescriptionText.text = "";
        _currentTrickScoreText.text = "";
        gameData.ResetData();
    }

    void Update()
    {
        _totalScoreText.text = $"Score: {Mathf.Floor(gameData.score)}";
        _lapInfoText.text = $"{gameData.currentLap}/{gameData.maxLap}";
        _currentTimeText.text = $"{Mathf.FloorToInt(gameData.currentTotalTime % 60):00}:{Mathf.RoundToInt((gameData.currentTotalTime % 1f) * 1000f):000}";
        _bestTimeText.text = $"BEST TIME:{gameData.bestTotalTime}";
        
        if (gameData.trickNames.Count > 0)
        {
            _currentTrickScoreText.text = $"{Mathf.Floor(gameData.trickPointPool)} x {gameData.trickMultiplier}";
            string trickDescription = "";
            for (int i = 0; i < gameData.trickNames.Count; ++i)
            {
                if (i > 3)
                {
                    trickDescription += " + ...";
                    break;
                }
                if(i > 0) trickDescription += " + ";
                trickDescription += gameData.trickNames[i].ToString();
            }
            _trickDescriptionText.text = trickDescription;
        }
        else
        {
            _trickDescriptionText.text = "";
            _currentTrickScoreText.text = "";
        }
    }
}
