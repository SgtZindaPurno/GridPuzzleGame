using UnityEngine;
using UnityEngine.UI;

public class UndoButtonController : MonoBehaviour
{
    [SerializeField] private Button undoButton;

    public void SetInteractable(bool canUndo)
    {
        if (undoButton != null) undoButton.interactable = canUndo;
    }
}
