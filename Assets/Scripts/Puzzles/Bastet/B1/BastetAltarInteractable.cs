using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BastetAltarInteractable : MonoBehaviour, IInteractable
{
    [Header("안내 문구 및 대상 퍼즐")]
    [SerializeField] private string promptText = "고양이 발자국 제단 조사";
    [SerializeField] private PuzzleBase targetPuzzle;
    [SerializeField] private GameObject puzzleUIPanel;

    public string GetPrompt()
    {
        return $"E: {promptText}";
    }

    public void Interact(GameObject player)
    {
        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(true);
        }

        if (targetPuzzle != null)
        {
            targetPuzzle.Open();
        }
    }
}