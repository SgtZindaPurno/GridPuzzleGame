using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GridVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private RectTransform gridPanel;
    [SerializeField] private GameObject tilePrefab;

    [Header("Appearance")]
    [SerializeField] private float tileSpacing = 10f;
    [SerializeField] private Color[] tileColors;

    [Header("Animation Durations")]
    [SerializeField] private float spawnAnimDuration = 0.15f;

    private float cellSize;
    private readonly Dictionary<int, GameObject> tileViews = new Dictionary<int, GameObject>();

    // ------------------------------------------------------------------
    // Initialization
    // ------------------------------------------------------------------
    public void InitializeVisuals()
    {
        if (tileColors == null || tileColors.Length == 0)
        {
            tileColors = new Color[] {
                new Color(0.80f, 0.80f, 0.80f, 1f),
                new Color(0.93f, 0.89f, 0.85f, 1f),
                new Color(0.93f, 0.88f, 0.78f, 1f),
                new Color(0.95f, 0.69f, 0.47f, 1f),
                new Color(0.96f, 0.58f, 0.38f, 1f),
                new Color(0.96f, 0.48f, 0.28f, 1f),
                new Color(0.96f, 0.37f, 0.18f, 1f),
                new Color(0.93f, 0.82f, 0.13f, 1f),
                new Color(0.93f, 0.78f, 0.08f, 1f),
                new Color(0.60f, 0.78f, 0.13f, 1f),
                new Color(0.30f, 0.78f, 0.30f, 1f),
                new Color(0.10f, 0.60f, 0.30f, 1f)
            };
        }

        Canvas.ForceUpdateCanvases();

        float panelWidth = gridPanel.rect.width;
        float panelHeight = gridPanel.rect.height;

        if (panelWidth <= 0 || panelHeight <= 0)
        {
            Debug.LogError("GridPanel has no size!");
            return;
        }

        cellSize = Mathf.Min(
            (panelWidth - tileSpacing * (gridManager.cols + 1)) / gridManager.cols,
            (panelHeight - tileSpacing * (gridManager.rows + 1)) / gridManager.rows
        );

        // Create views for tiles already in the grid (initial spawn)
        var grid = gridManager.GetGridData();
        for (int r = 0; r < gridManager.rows; r++)
            for (int c = 0; c < gridManager.cols; c++)
            {
                var t = grid[r, c];
                if (!t.IsEmpty) CreateTileView(t.id, r, c, t.value, animateSpawn: true);
            }
    }

    // ------------------------------------------------------------------
    // Called by GridManager.OnMovePlanned — start tweens
    // ------------------------------------------------------------------
    public void AnimateMove(MoveResult plan)
    {
        foreach (var m in plan.movements)
        {
            if (tileViews.TryGetValue(m.tileId, out var go))
            {
                var rect = go.GetComponent<RectTransform>();
                Vector2 target = GetCellPosition(m.toRow, m.toCol);
                StartCoroutine(TweenPosition(rect, target, 0.12f));
            }
        }
    }

    // ------------------------------------------------------------------
    // Called by GridManager.OnMoveCompleted — reconcile visuals with data
    // ------------------------------------------------------------------
    public void SyncWithGrid()
    {
        var grid = gridManager.GetGridData();

        // 1) Which IDs are still alive?
        var activeIds = new HashSet<int>();
        for (int r = 0; r < gridManager.rows; r++)
            for (int c = 0; c < gridManager.cols; c++)
                if (!grid[r, c].IsEmpty) activeIds.Add(grid[r, c].id);

        // 2) Destroy views whose tile was consumed (merged / removed)
        var toRemove = new List<int>();
        foreach (var kv in tileViews)
            if (!activeIds.Contains(kv.Key)) toRemove.Add(kv.Key);

        foreach (var id in toRemove)
        {
            Destroy(tileViews[id]);
            tileViews.Remove(id);
        }

        // 3) Create views for new IDs; update survivors
        for (int r = 0; r < gridManager.rows; r++)
        {
            for (int c = 0; c < gridManager.cols; c++)
            {
                var t = grid[r, c];
                if (t.IsEmpty) continue;

                if (tileViews.TryGetValue(t.id, out var go))
                {
                    // Snap to final position (tween may have minor float drift)
                    var rect = go.GetComponent<RectTransform>();
                    rect.anchoredPosition = GetCellPosition(r, c);
                    UpdateTileAppearance(go, t.value);
                }
                else
                {
                    // Brand new tile — either spawn or merge result
                    CreateTileView(t.id, r, c, t.value, animateSpawn: true);
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // Called by HistoryManager.OnUndoPerformed — full visual reset
    // ------------------------------------------------------------------
    public void HardRefresh()
    {
        foreach (var go in tileViews.Values) Destroy(go);
        tileViews.Clear();

        var grid = gridManager.GetGridData();
        for (int r = 0; r < gridManager.rows; r++)
            for (int c = 0; c < gridManager.cols; c++)
            {
                var t = grid[r, c];
                if (!t.IsEmpty) CreateTileView(t.id, r, c, t.value, animateSpawn: false);
            }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    private GameObject CreateTileView(int id, int row, int col, int value, bool animateSpawn)
    {
        var go = Instantiate(tilePrefab, gridPanel);
        var rect = go.GetComponent<RectTransform>();

        rect.pivot = new Vector2(0, 1);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.sizeDelta = new Vector2(cellSize, cellSize);
        rect.anchoredPosition = GetCellPosition(row, col);

        UpdateTileAppearance(go, value);
        tileViews[id] = go;

        if (animateSpawn)
        {
            go.transform.localScale = Vector3.zero;
            StartCoroutine(TweenScale(go.transform, Vector3.one, spawnAnimDuration));
        }

        return go;
    }

    private Vector2 GetCellPosition(int row, int col)
    {
        float x = tileSpacing + col * (cellSize + tileSpacing);
        float y = -tileSpacing - row * (cellSize + tileSpacing);
        return new Vector2(x, y);
    }

    private void UpdateTileAppearance(GameObject go, int value)
    {
        var text = go.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = value > 0 ? value.ToString() : "";

        var img = go.GetComponent<Image>();
        if (img != null)
        {
            int idx = Mathf.Clamp(Mathf.RoundToInt(Mathf.Log(value, 2)), 0, tileColors.Length - 1);
            var col = tileColors[idx];
            col.a = 1f;
            img.color = col;
        }
    }

    private IEnumerator TweenPosition(RectTransform rect, Vector2 target, float duration)
    {
        if (rect == null) yield break;

        Vector2 start = rect.anchoredPosition;
        float t = 0f;

        while (t < duration)
        {
            if (rect == null) yield break;   // <-- tile was destroyed mid-tween

            t += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(start, target, Mathf.Clamp01(t / duration));
            yield return null;
        }

        if (rect != null)
            rect.anchoredPosition = target;
    }

    private IEnumerator TweenScale(Transform tr, Vector3 target, float duration)
    {
        if (tr == null) yield break;

        Vector3 start = tr.localScale;
        float t = 0f;

        while (t < duration)
        {
            if (tr == null) yield break;   // <-- tile was destroyed mid-tween

            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            p = 1f - Mathf.Pow(1f - p, 3f);   // ease-out cubic
            tr.localScale = Vector3.Lerp(start, target, p);
            yield return null;
        }

        if (tr != null)
            tr.localScale = target;
    }
}