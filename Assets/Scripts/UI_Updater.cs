using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_Updater : MonoBehaviour
{
    [Header("UI Object references")]
    [SerializeField] private GameObject _totalScore;
    [SerializeField] private GameObject  _currentTrickScore;
    [SerializeField] private GameObject  _trickDescription;
    [SerializeField] private GameObject  _currentTime;
    [SerializeField] private GameObject  _bestTime;
    [SerializeField] private GameObject  _lapInfo;
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;
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
        _bestTimeText = _bestTime.GetComponent<TMP_Text>();
        _lapInfoText = _lapInfo.GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        endPanel.SetActive(false);
        _trickDescriptionText.text = "";
        _currentTrickScoreText.text = "";
        gameData.ResetData();
        if (HighScoreManager.Instance != null && HighScoreManager.Instance.TryGetBestScore(out float bestScore))
            _bestTimeText.text = $"BEST TIME:{FormatTime(bestScore)}";
        else if (gameData.bestTotalTime > 0)
            _bestTimeText.text = $"BEST TIME:{FormatTime(gameData.bestTotalTime)}";
        else
            _bestTimeText.text = "BEST TIME:--:---";
        
        restartButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
        
        menuButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(0);
        });
    }

    void Update()
    {
        _totalScoreText.text = $"Score: {Mathf.Floor(gameData.score)}";
        _lapInfoText.text = $"{gameData.currentLap}/{gameData.maxLap}";
        _currentTimeText.text = FormatTime(gameData.currentTotalTime);
        
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

    public void ActivateEndPanel()
    {
        endPanel.SetActive(true);
        highScoreText.text = BuildEndPanelHighScoreText();
    }

    private string BuildEndPanelHighScoreText()
    {
        // 8 hardcoded baseline times (in seconds) for the default lap count.
        // These are display-only and are scaled based on the current max lap amount.
        float[] baselineTimesSeconds =
        {
            55f,
            65f,
            75f,
            85f,
            95f,
            105f,
            112f,
            120f
        };

        int maxLap = Mathf.Max(1, gameData != null ? gameData.maxLap : 1);

        var entries = new System.Collections.Generic.List<(string name, float timeSeconds, bool isPlayer)>();
        for (int i = 0; i < baselineTimesSeconds.Length; i++)
        {
            float scaled = baselineTimesSeconds[i] * maxLap;
            entries.Add(($"TIME {i + 1}", scaled, false));
        }

        float currentRunTime = 0f;
        if (gameData != null)
        {
            currentRunTime = gameData.totalTime > 0f ? gameData.totalTime : gameData.currentTotalTime;
        }

        float savedBest = 0f;
        bool hasSavedBest = HighScoreManager.Instance != null && HighScoreManager.Instance.TryGetBestScore(out savedBest);

        float playerBest = 0f;
        if (hasSavedBest && savedBest > 0f)
            playerBest = savedBest;
        if (currentRunTime > 0f)
            playerBest = playerBest > 0f ? Mathf.Min(playerBest, currentRunTime) : currentRunTime;

        if (playerBest > 0f)
            entries.Add(("YOU", playerBest, true));

        entries.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            string name = e.isPlayer ? $"> {e.name}" : e.name;
            sb.Append($"{i + 1}. {name} : {FormatTime(e.timeSeconds)}");
            if (i < entries.Count - 1)
                sb.Append('\n');
        }

        return sb.ToString();
    }

    private static string FormatTime(float timeSeconds)
    {
        int totalSeconds = Mathf.FloorToInt(timeSeconds);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        int milliseconds = Mathf.RoundToInt((timeSeconds - totalSeconds) * 1000f);
        if (milliseconds >= 1000)
        {
            milliseconds = 0;
            seconds++;
            if (seconds >= 60)
            {
                seconds = 0;
                minutes++;
            }
        }

        return minutes > 0
            ? $"{minutes:00}:{seconds:00}:{milliseconds:000}"
            : $"{seconds:00}:{milliseconds:000}";
    }
}
