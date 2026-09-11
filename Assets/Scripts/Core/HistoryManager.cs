using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class HistoryManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ScoreManager scoreManager;

    [Header("Settings")]
    [SerializeField] private int maxHistorySize = 20;

    [Header("Events")]
    public UnityEvent OnUndoPerformed;              // Visualizer refresh
    public UnityEvent OnUndoUnavailable;            // Nothing left to undo
    public UnityEvent<bool> OnUndoAvailabilityChanged; // For greying out button

    private Stack<Snapshot> history = new Stack<Snapshot>();

    [SerializeField] private PowerUpManager powerUpManager;

    private struct Snapshot
    {
        public TileData[,] grid;
        public int score;
        public int nextThreshold;
        public int activeMovesLeft;
    }

    public bool CanUndo => history.Count > 0;

    // Hook to GridManager.OnBeforeMove
    public void CaptureState()
    {
        // Enforce max size (Stack has no Trim; rebuild without the oldest)
        if (history.Count >= maxHistorySize)
        {
            Snapshot[] temp = history.ToArray();  // [newest ... oldest]
            history.Clear();
            for (int i = temp.Length - 2; i >= 0; i--)
                history.Push(temp[i]);
        }

        TileData[,] gridCopy = (TileData[,])gridManager.GetGridData().Clone();
        int snapshotScore = scoreManager != null ? scoreManager.CurrentScore : 0;
        int threshold = powerUpManager != null ? powerUpManager.NextThreshold : 0;
        int movesLeft = powerUpManager != null ? powerUpManager.ActiveMovesLeft : 0;


        history.Push(new Snapshot 
        {   grid = gridCopy, 
            score = snapshotScore,
            nextThreshold = threshold,
            activeMovesLeft = movesLeft
        });
        OnUndoAvailabilityChanged?.Invoke(true);
    }

    // Hook to GridManager.OnMoveBlocked
    public void DiscardLastSnapshot()
    {
        if (history.Count > 0) history.Pop();
        OnUndoAvailabilityChanged?.Invoke(CanUndo);
    }

    // Hook to the Undo Button's OnClick
    public void Undo()
    {
        if (history.Count == 0)
        {
            Debug.Log("Nothing to undo.");
            OnUndoUnavailable?.Invoke();
            return;
        }

        Snapshot state = history.Pop();
        gridManager.SetGridData(state.grid);
        if (scoreManager != null) scoreManager.SetScore(state.score);
        if (powerUpManager != null) powerUpManager.RestoreState(state.nextThreshold, state.activeMovesLeft);

        OnUndoPerformed?.Invoke();
        OnUndoAvailabilityChanged?.Invoke(CanUndo);
        Debug.Log($"Undo performed. Restored score: {state.score}");
    }

    public void ClearHistory()
    {
        history.Clear();
        OnUndoAvailabilityChanged?.Invoke(false);
    }
}