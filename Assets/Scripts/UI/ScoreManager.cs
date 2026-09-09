using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    private int currentScore;
    private int highScore;

    private void Start()
    {
        // Load high score from device storage (defaults to 0 if not found)
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        UpdateUI();
    }

    // Call this when the game starts or restarts
    public void ResetScore()
    {
        currentScore = 0;
        UpdateUI();
    }

    // This method will be triggered by GridManager's OnScoreGained event
    public void AddScore(int pointsAdded)
    {
        currentScore += pointsAdded;

        // Check for new high score
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Current Score: "+ currentScore.ToString();

        if (highScoreText != null)
            highScoreText.text = "High Score: "+ highScore.ToString();
    }

    // Optional: Useful for developer testing
    [ContextMenu("Reset High Score")]
    private void ClearHighScore()
    {
        PlayerPrefs.DeleteKey("HighScore");
        highScore = 0;
        UpdateUI();
    }
}