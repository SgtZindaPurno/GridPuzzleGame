using UnityEngine;
using UnityEngine.Events;

public class InputHandler : MonoBehaviour
{
    [Header("Swipe Settings")]
    [SerializeField] private float minSwipeDistance = 30f;

    [Header("Events")]
    [Tooltip("Passes: 0=Up, 1=Down, 2=Left, 3=Right")]
    public UnityEvent<int> OnSwipeDetected;

    // Internal
    private Vector2 touchStartPos;
    private Vector2 touchEndPos;
    private bool SwipeInputEnabled = true;

    // Call this from anywhere (e.g., win/lose screen) to freeze input
    public void SetSwipeInputEnabled(bool enabled)
    {
        SwipeInputEnabled = enabled;
        Debug.Log($"[InputHandler] Input {(enabled ? "enabled" : "disabled")}");
    }

    private void Update()
    {
        if (!SwipeInputEnabled) return;
        DetectSwipe();
    }

    private void DetectSwipe()
    {
        // --- Touch input (takes priority when present) ---
        

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    break;
                case TouchPhase.Ended:
                    touchEndPos = touch.position;
                    ProcessSwipe();
                    break;
            }
            return; // Don't also process mouse in the same frame
        }

        // --- Mouse input (Editor / standalone desktop) ---
        if (Input.GetMouseButtonDown(0))
            touchStartPos = Input.mousePosition;
        else if (Input.GetMouseButtonUp(0))
        {
            touchEndPos = Input.mousePosition;
            ProcessSwipe();
        }
    }

    private void ProcessSwipe()
    {
        Vector2 delta = touchEndPos - touchStartPos;
        if (delta.magnitude < minSwipeDistance) return;

        int direction;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            direction = delta.x > 0 ? 3 : 2;   // Right : Left
        else
            direction = delta.y > 0 ? 0 : 1;   // Up : Down

        Debug.Log($"[InputHandler] Swipe detected: {DirectionName(direction)}");
        OnSwipeDetected?.Invoke(direction);
    }

    private string DirectionName(int d) =>
        d switch { 0 => "Up", 1 => "Down", 2 => "Left", 3 => "Right", _ => "?" };
}