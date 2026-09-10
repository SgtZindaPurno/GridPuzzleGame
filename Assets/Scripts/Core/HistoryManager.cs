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

    private struct Snapshot
    {
        public int[,] grid;
        public int score;
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

        int[,] gridCopy = (int[,])gridManager.GetGridData().Clone();
        int snapshotScore = scoreManager != null ? scoreManager.CurrentScore : 0;

        history.Push(new Snapshot { grid = gridCopy, score = snapshotScore });
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