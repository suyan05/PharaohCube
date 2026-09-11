using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FinalPuzzleUI : MonoBehaviour
{
    public static FinalPuzzleUI Instance { get; private set; }

    [Header("UI Roots")]
    [SerializeField] private GameObject finalPuzzlePanel;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private TextMeshProUGUI txtDescription;
    [SerializeField] private Button btnClose;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (btnClose != null)
        {
            btnClose.onClick.AddListener(Close);
        }

        // 게임 시작 시에는 화면 숨김
        if (finalPuzzlePanel != null)
        {
            finalPuzzlePanel.SetActive(false);
        }
    }

    // CubeSealManager에서 호출할 함수
    public void Open()
    {
        Debug.Log("[FinalPuzzleUI] 최종 봉인 퍼즐 UI가 열렸습니다.");
        if (finalPuzzlePanel != null)
        {
            finalPuzzlePanel.SetActive(true);
        }
    }

    public void Close()
    {
        if (finalPuzzlePanel != null)
        {
            finalPuzzlePanel.SetActive(false);
        }
    }
}