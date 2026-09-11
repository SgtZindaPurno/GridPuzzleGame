using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class PowerUpManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Button powerUpButton;
    [SerializeField] private TextMeshProUGUI powerUpButtonText;

    [Header("Settings")]
    [SerializeField] private int scoreThresholdStep = 200;
    [SerializeField] private int bonusMovesPerActivation = 3;

    [Header("Events")]
    public UnityEvent OnPowerUpAvailable;
    public UnityEvent OnPowerUpUnavailable;
    public UnityEvent<int> OnPowerUpActivated;    // passes total bonus moves granted
    public UnityEvent<int> OnPowerUpMovesChanged; // passes moves left
    public UnityEvent OnPowerUpExpired;

    // State
    private int nextThreshold;
    private int activeMovesLeft = 0;
    private bool lastAvailableState = false;

    // --- Public accessors (used by HistoryManager) ---
    public int NextThreshold => nextThreshold;
    public int ActiveMovesLeft => activeMovesLeft;

    public bool IsActive => activeMovesLeft > 0;
    public bool IsAvailable =>
        !IsActive
        && scoreManager != null
        && scoreManager.CurrentScore >= nextThreshold;

    private void Awake()
    {
        nextThreshold = scoreThresholdStep;
    }

    private void Start()
    {
        RefreshState();
    }

    // ---------------------------------------------------------------
    // Hooks
    // ---------------------------------------------------------------

    // Hook to ScoreManager.OnScoreChanged
    public void OnScoreUpdated(int newScore)
    {
        RefreshState();
    }

    // Hook to the button's OnClick
    public void ActivatePowerUp()
    {
        if (!IsAvailable)
        {
            Debug.Log("[PowerUp] Not available.");
            return;
        }

        activeMovesLeft = bonusMovesPerActivation;
        nextThreshold = scoreManager.CurrentScore + scoreThresholdStep;
        gridManager.SuppressSpawn = true;

        Debug.Log($"[PowerUp] Activated! {activeMovesLeft} bonus moves. Next threshold: {nextThreshold}");

        OnPowerUpActivated?.Invoke(activeMovesLeft);
        OnPowerUpMovesChanged?.Invoke(activeMovesLeft);
        RefreshState();
    }

    // Hook to GridManager.OnMoveCompleted
    public void OnMoveCompleted()
    {
        if (activeMovesLeft <= 0) return;

        activeMovesLeft--;
        Debug.Log($"[PowerUp] Bonus moves left: {activeMovesLeft}");

        OnPowerUpMovesChanged?.Invoke(activeMovesLeft);

        if (activeMovesLeft <= 0)
        {
            gridManager.SuppressSpawn = false;
            OnPowerUpExpired?.Invoke();
            Debug.Log("[PowerUp] Expired.");
        }

        RefreshState();
    }

    // Called by HistoryManager when restoring a snapshot
    public void RestoreState(int restoredThreshold, int restoredMovesLeft)
    {
        nextThreshold = restoredThreshold;
        activeMovesLeft = restoredMovesLeft;
        gridManager.SuppressSpawn = restoredMovesLeft > 0;

        OnPowerUpMovesChanged?.Invoke(activeMovesLeft);
        RefreshState();
    }

    // ---------------------------------------------------------------
    // UI / State
    // ---------------------------------------------------------------
    private void RefreshState()
    {
        bool nowAvailable = IsAvailable;

        if (powerUpButton != null)
            powerUpButton.interactable = nowAvailable;

        if (powerUpButtonText != null)
            powerUpButtonText.text = activeMovesLeft > 0
                ? $"Moves Left: {activeMovesLeft}"
                : "";

        if (nowAvailable != lastAvailableState)
        {
            if (nowAvailable) OnPowerUpAvailable?.Invoke();
            else OnPowerUpUnavailable?.Invoke();
            lastAvailableState = nowAvailable;
        }
    }
}