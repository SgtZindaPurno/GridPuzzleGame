using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GridVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager; // Drag the GameManager here
    [SerializeField] private RectTransform gridPanel; // The parent panel
    [SerializeField] private GameObject tilePrefab;   // Your UI Tile prefab

    [Header("Appearance")]
    [SerializeField] private float tileSpacing = 10f;
    [SerializeField] private Color[] tileColors; // Index 0 = empty, 1 = 2, 2 = 4, 3 = 8, etc.

    private GameObject[,] tileObjects;

    
    public void InitializeVisuals()
    {
        if (tileColors == null || tileColors.Length == 0)
        {
            tileColors = new Color[] {
                new Color(0.8f, 0.8f, 0.8f, 1f), // 0 (unused)
                new Color(0.93f, 0.89f, 0.85f, 1f), // 2
                new Color(0.93f, 0.88f, 0.78f, 1f), // 4
                new Color(0.95f, 0.69f, 0.47f, 1f), // 8
                new Color(0.96f, 0.58f, 0.38f, 1f), // 16
                new Color(0.96f, 0.48f, 0.28f, 1f), // 32
                new Color(0.96f, 0.37f, 0.18f, 1f), // 64
                new Color(0.93f, 0.82f, 0.13f, 1f), // 128
                new Color(0.93f, 0.78f, 0.08f, 1f), // 256
                new Color(0.60f, 0.78f, 0.13f, 1f), // 512
                new Color(0.30f, 0.78f, 0.30f, 1f), // 1024
                new Color(0.10f, 0.60f, 0.30f, 1f)  // 2048
            };
        }



        CreateTileGrid();
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        // Called after every move to refresh the board
        int rows = gridManager.rows;
        int cols = gridManager.cols;
        int[,] grid = gridManager.GetGridData();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int value = grid[r, c];
                GameObject tile = tileObjects[r, c];
                if (tile == null) continue;

                // Update text
                TextMeshProUGUI text = tile.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = value > 0 ? value.ToString() : "";
                }

                // Update background color based on value
                Image img = tile.GetComponent<Image>();
                if (img != null)
                {
                    int colorIndex = Mathf.Clamp(Mathf.RoundToInt(Mathf.Log(value, 2)), 0, tileColors.Length - 1);
                    img.color = tileColors[colorIndex];
                }

               
            }
        }
    }

    private void CreateTileGrid()
    {
       

        int rows = gridManager.rows;
        int cols = gridManager.cols;
        tileObjects = new GameObject[rows, cols];

        // Wait one frame to let Unity recalculate the rect size, OR force it.
        // Force a refresh of the layout to get accurate width/height
        Canvas.ForceUpdateCanvases();

        float panelWidth = gridPanel.rect.width;
        float panelHeight = gridPanel.rect.height;

        // Safety check to avoid division by zero if the panel has no size
        if (panelWidth <= 0 || panelHeight <= 0)
        {
            Debug.LogError("GridPanel has no size! Set a fixed Width/Height on its RectTransform.");
            return;
        }

        float cellSize = Mathf.Min(
            (panelWidth - tileSpacing * (cols + 1)) / cols,
            (panelHeight - tileSpacing * (rows + 1)) / rows
        );

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                GameObject tile = Instantiate(tilePrefab, gridPanel);
                RectTransform rect = tile.GetComponent<RectTransform>();

                // Calculate position. 
                // Since pivot is Top-Left (0,1), X goes right, Y goes DOWN.
                // So row 0 (top) has the smallest Y value.
                float x = tileSpacing + c * (cellSize + tileSpacing);
                float y = -tileSpacing - r * (cellSize + tileSpacing);

                rect.anchoredPosition = new Vector2(x, y);
                rect.sizeDelta = new Vector2(cellSize, cellSize);

                tileObjects[r, c] = tile;
                tile.SetActive(true); 
            }
        }
    }

}