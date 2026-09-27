using UnityEngine;

public class RaPuzzleUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RaPuzzleManager puzzleManager;

    [Header("클리어 연출")]
    [SerializeField] private GameObject clearText;

    private void Start()
    {
        if (clearText != null) clearText.SetActive(false);
    }

    private void OnEnable()
    {
        if (puzzleManager != null)
            puzzleManager.OnPuzzleCleared += HandleCleared;
    }

    private void OnDisable()
    {
        if (puzzleManager != null)
            puzzleManager.OnPuzzleCleared -= HandleCleared;
    }

    private void HandleCleared()
    {
        Debug.Log("[RaPuzzleUI] 라의 방 클리어 UI 활성화!");
        if (clearText != null) clearText.SetActive(true);
    }
}