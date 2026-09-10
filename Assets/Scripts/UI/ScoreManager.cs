using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    [Header("Events")]
    public UnityEvent<int> OnScoreChanged;   // For other systems (e.g., floating "+8" popups)

    private int currentScore;
    private int highScore;

    public int CurrentScore => currentScore;

    private void Start()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        UpdateUI();
    }

    // Hook this to GridManager.OnMergeOccurred
    public void AddScore(int points)
    {
        currentScore += points;

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }

        UpdateUI();
        OnScoreChanged?.Invoke(currentScore);
    }

    // Hook this to HistoryManager (undo restore)
    public void SetScore(int absoluteValue)
    {
        currentScore = absoluteValue;
        UpdateUI();
        OnScoreChanged?.Invoke(currentScore);
    }

    public void ResetScore()
    {
        currentScore = 0;
        UpdateUI();
        OnScoreChanged?.Invoke(currentScore);
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = "Current Score: " + currentScore;
        if (highScoreText != null) highScoreText.text = "High Score: " + highScore;
    }

    [ContextMenu("Reset High Score")]
    private void ClearHighScore()
    {
        PlayerPrefs.DeleteKey("HighScore");
        highScore = 0;
        UpdateUI();
    }
}