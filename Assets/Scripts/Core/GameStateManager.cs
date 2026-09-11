using UnityEngine;
using UnityEngine.Events;

public class GameStateManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private HistoryManager historyManager;
    [SerializeField] private PowerUpManager powerUpManager;
    [SerializeField] private InputHandler inputHandler;

    [Header("UI Panels")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    [Header("Settings")]
    [SerializeField] private int winTileValue = 2048;

    [Header("Events")]
    public UnityEvent OnWin;
    public UnityEvent OnLose;
    public UnityEvent OnContinueAfterWin;
    public UnityEvent OnGameRestarted;

    private bool winAlreadyShown = false;
    private bool gameEnded = false;

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    // ---------------------------------------------------------------
    // Hook this to GridManager.OnMoveCompleted
    // ---------------------------------------------------------------
    public void OnMoveCompleted()
    {
        if (gameEnded || gridManager == null) return;
        CheckGameState();
    }

    // ---------------------------------------------------------------
    // Win / Lose detection
    // ---------------------------------------------------------------
    private void CheckGameState()
    {
        var grid = gridManager.GetGridData();
        int rows = gridManager.rows;
        int cols = gridManager.cols;

        // --- Win check (only if not already shown this session) ---
        if (!winAlreadyShown)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (grid[r, c].value >= winTileValue)
                    {
                        ShowWin();
                        return;
                    }
                }
            }
        }

        // --- Lose check ---
        if (IsLose(grid, rows, cols))
            ShowLose();
    }

    private bool IsLose(TileData[,] grid, int rows, int cols)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                // Any empty cell ? not a loss
                if (grid[r, c].IsEmpty) return false;

                // Any adjacent equal pair ? not a loss
                if (c < cols - 1 && grid[r, c].value == grid[r, c + 1].value) return false;
                if (r < rows - 1 && grid[r, c].value == grid[r + 1, c].value) return false;
            }
        }
        return true;
    }

    // ---------------------------------------------------------------
    // Show screens
    // ---------------------------------------------------------------
    private void ShowWin()
    {
        winAlreadyShown = true;
        gameEnded = true;
        if (inputHandler != null) inputHandler.SetSwipeInputEnabled(false);
        if (winPanel != null) winPanel.SetActive(true);
        OnWin?.Invoke();
        Debug.Log("[GameState] ?? WIN! 2048 reached.");
    }

    private void ShowLose()
    {
        gameEnded = true;
        if (inputHandler != null) inputHandler.SetSwipeInputEnabled(false);
        if (losePanel != null) losePanel.SetActive(true);
        OnLose?.Invoke();
        Debug.Log("[GameState] ?? LOSE! No moves left.");
    }

    // ---------------------------------------------------------------
    // Buttons
    // ---------------------------------------------------------------

    // Hook to Win Panel's "Continue" button
    public void ContinueAfterWin()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (inputHandler != null) inputHandler.SetSwipeInputEnabled(true);
        gameEnded = false;
        // winAlreadyShown stays true — we don't want to nag again
        OnContinueAfterWin?.Invoke();
        Debug.Log("[GameState] Continuing after win.");
    }

    // Hook to Lose Panel's "Restart" button
    public void RestartGame()
    {
        if (losePanel != null) losePanel.SetActive(false);

        // Reset order matters! Grid reset last so its event drives final visual refresh.
        if (scoreManager != null) scoreManager.ResetScore();
        if (historyManager != null) historyManager.ClearHistory();
        if (powerUpManager != null) powerUpManager.ResetPowerUp();
        if (gridManager != null) gridManager.ResetGrid();

        if (inputHandler != null) inputHandler.SetSwipeInputEnabled(true);

        gameEnded = false;
        winAlreadyShown = false;
        OnGameRestarted?.Invoke();
        Debug.Log("[GameState] ?? Game restarted.");
    }
}