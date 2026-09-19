using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SobekPuzzleUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private SobekPuzzleManager puzzleManager;

    [Header("9개 타일 버튼 (왼쪽 위부터 순서대로, index 0~8)")]
    [SerializeField] private Button[] cellButtons = new Button[9];
    [SerializeField] private TMP_Text[] cellLabels = new TMP_Text[9];

    [Header("클리어 표시용 (선택)")]
    [SerializeField] private GameObject clearText;

    private void Start()
    {
        for (int i = 0; i < cellButtons.Length; i++)
        {
            int index = i; // 람다에서 쓸 변수 따로 캡처 (클로저 문제 방지)
            cellButtons[index].onClick.AddListener(() =>
            {
                Debug.Log($"[SobekPuzzleUI] 클릭된 버튼의 index = {index}, 오브젝트 이름 = {cellButtons[index].gameObject.name}");
                puzzleManager.RotateCell(index);
            });

            RefreshAllLabels();
            if (clearText != null) clearText.SetActive(false);
        }
    }

    private void OnEnable()
    {
        puzzleManager.OnCellRotated += HandleCellRotated;
        puzzleManager.OnPuzzleCleared += HandleCleared;
    }

    private void OnDisable()
    {
        puzzleManager.OnCellRotated -= HandleCellRotated;
        puzzleManager.OnPuzzleCleared -= HandleCleared;
    }

    private void HandleCellRotated(int index)
    {
        RefreshLabel(index);
    }

    private void RefreshAllLabels()
    {
        for (int i = 0; i < cellLabels.Length; i++)
        {
            RefreshLabel(i);
        }
    }

    private void RefreshLabel(int index)
    {
        CanalCellData cell = puzzleManager.GetCell(index);
        bool[] openings = puzzleManager.GetCurrentOpenings(index);

        // 뚫린 방향을 화살표로 표시 (상,우,하,좌 순서)
        string dirText = "";
        if (openings[0]) dirText += "↑";
        if (openings[1]) dirText += "→";
        if (openings[2]) dirText += "↓";
        if (openings[3]) dirText += "←";

        cellLabels[index].text = $"{cell.type}\n{dirText}";
    }

    private void HandleCleared()
    {
        Debug.Log("[SobekPuzzleUI] 클리어! (연출은 나중에 추가 예정)");
        if (clearText != null) clearText.SetActive(true);
    }
}