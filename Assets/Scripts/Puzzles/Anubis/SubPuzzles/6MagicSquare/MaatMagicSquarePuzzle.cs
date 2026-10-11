using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 42인의 제약 마방진 (서브퍼즐 4) - 숫자판 버전 (튜토리얼에서 사용)
// [수정] 숨겨진 '좌상단 8 / 우하단 2' 규칙 삭제 → 가로·세로·대각선 합이 모두 15면 정답
// [추가] 미리 채울 칸(presetValues), 튜토리얼 가이드용 이벤트/필터

public class MaatMagicSquarePuzzle : MonoBehaviour
{
    [Header("난이도")]
    [Tooltip("미리 채울 숫자 (0 = 빈칸, 1~9 = 고정칸)\n배치: 0 1 2 / 3 4 5 / 6 7 8")]
    [SerializeField] private int[] presetValues = { 0, 0, 0, 0, 5, 0, 0, 0, 0 };
    [TextArea(2, 4)]
    [SerializeField] private string hintText = "<b>[42인의 제약 마방진]</b>\n1~9를 한 번씩 사용해 가로·세로·대각선의 합을 모두 15로 맞추십시오.";

    [Header("클리어 처리")]
    [Tooltip("사자의 서 단서 슬롯 (-1이면 없음)")]
    [SerializeField] private int clueIndex = 3;
    [TextArea(2, 4)]
    [SerializeField] private string clueText = "청동의 그릇(B)은 흙의 단지(C) 다섯 개의 무게와 같도다. (B = 5C)";
    [Tooltip("클리어 보상 아이템 ID (비우면 지급 안 함)")]
    [SerializeField] private string rewardItemId = AnubisItemIds.BronzeBowl;

    [Header("3x3 보드 슬롯 9개")]
    [SerializeField] private Button[] boardSlotButtons = new Button[9];
    [SerializeField] private TMP_Text[] boardSlotTexts = new TMP_Text[9];
    [SerializeField] private Image[] boardSlotHighlights = new Image[9];

    [Header("숫자 패드 1~9")]
    [SerializeField] private Button[] numPadButtons = new Button[9];

    [Header("라인 합계 (가로3, 세로3, 대각선2)")]
    [SerializeField] private TMP_Text[] lineSumTexts = new TMP_Text[8];

    [Header("시스템 버튼 및 알림 패널")]
    [SerializeField] private Button btnCheckSolution;
    [SerializeField] private Button btnClearBoard;
    [SerializeField] private Button btnClose;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private TMP_Text textFeedbackNotice;
    [SerializeField] private TMP_Text textHintNotice;

    public event Action<int> OnSlotSelected;       // 칸 선택됨 (칸 번호)
    public event Action<int> OnSlotRejected;       // 칸 선택이 필터에 막힘
    public event Action<int, int> OnNumberPlaced;  // 숫자 입력됨 (칸, 숫자)
    public event Action<int, int> OnInputRejected; // 숫자 입력이 필터에 막힘
    public event Action OnBoardReset;              // 보드 초기화됨
    public event Action OnSolved;                  // 정답

    public Func<int, bool> SlotFilter { get; set; }       // false면 칸 선택 막음
    public Func<int, int, bool> InputFilter { get; set; } // false면 숫자 입력 막음
    public bool IsCleared => isCleared;

    private readonly int[] boardValues = new int[9];
    private readonly bool[] isFixed = new bool[9];
    private int selectedSlotIndex = -1;
    private bool isCleared;
    private bool rewarded;
    private bool started;
    private Coroutine feedbackCoroutine;

    private readonly Color colDefaultSlot = new Color(0.2f, 0.18f, 0.15f, 1f);
    private readonly Color colSelectedSlot = new Color(0.9f, 0.75f, 0.2f, 1f);
    private readonly Color colFixedSlot = new Color(0.4f, 0.32f, 0.15f, 1f);

    private void Start()
    {
        for (int i = 0; i < boardSlotButtons.Length; i++)
        {
            int idx = i;
            if (boardSlotButtons[idx] != null) boardSlotButtons[idx].onClick.AddListener(() => OnSelectSlot(idx));
        }
        for (int i = 0; i < numPadButtons.Length; i++)
        {
            int num = i + 1;
            if (numPadButtons[i] != null) numPadButtons[i].onClick.AddListener(() => OnInputNumber(num));
        }

        if (btnCheckSolution != null) btnCheckSolution.onClick.AddListener(CheckSolution);
        if (btnClearBoard != null) btnClearBoard.onClick.AddListener(ResetBoard);
        if (btnClose != null) btnClose.onClick.AddListener(() => gameObject.SetActive(false));

        if (textHintNotice != null) textHintNotice.text = hintText;
        if (successNotice != null) successNotice.SetActive(false);
        if (textFeedbackNotice != null) textFeedbackNotice.text = "";

        started = true;
        InitBoard();
    }

    private void OnEnable()
    {
        ClearFeedback();
        UpdateVisuals();
    }

    public void SetPreset(int[] values)
    {
        presetValues = values;
        if (started && !isCleared) InitBoard();
    }

    public void SetHint(string text)
    {
        if (textHintNotice != null) textHintNotice.text = text;
    }

    public RectTransform GetSlotRect(int index) =>
        (index >= 0 && index < boardSlotButtons.Length && boardSlotButtons[index] != null) ? (RectTransform)boardSlotButtons[index].transform : null;

    public RectTransform GetNumberRect(int number) =>
        (number >= 1 && number <= numPadButtons.Length && numPadButtons[number - 1] != null) ? (RectTransform)numPadButtons[number - 1].transform : null;

    public RectTransform GetCheckRect() => btnCheckSolution != null ? (RectTransform)btnCheckSolution.transform : null;

    private void InitBoard()
    {
        for (int i = 0; i < 9; i++)
        {
            int v = (presetValues != null && i < presetValues.Length) ? Mathf.Clamp(presetValues[i], 0, 9) : 0;
            boardValues[i] = v;
            isFixed[i] = v != 0;
        }
        selectedSlotIndex = -1;
        UpdateVisuals();
        OnBoardReset?.Invoke();
    }

    private void OnSelectSlot(int index)
    {
        if (isCleared) return;

        if (isFixed[index])
        {
            ShowFeedback("<color=#E5A93C>고정된 신석은 바꿀 수 없습니다.</color>");
            return;
        }
        if (SlotFilter != null && !SlotFilter(index))
        {
            OnSlotRejected?.Invoke(index);
            return;
        }

        selectedSlotIndex = index;
        UpdateVisuals();
        OnSlotSelected?.Invoke(index);
    }

    private void OnInputNumber(int number)
    {
        if (isCleared) return;
        if (selectedSlotIndex == -1)
        {
            ShowFeedback("<color=#E5A93C>먼저 왼쪽의 빈칸을 선택하세요.</color>");
            return;
        }
        if (isFixed[selectedSlotIndex]) return;
        if (InputFilter != null && !InputFilter(selectedSlotIndex, number))
        {
            OnInputRejected?.Invoke(selectedSlotIndex, number);
            return;
        }

        ClearFeedback();

        // 같은 숫자가 다른 칸에 있으면 비움 (고정칸의 숫자는 쓸 수 없음)
        for (int i = 0; i < 9; i++)
        {
            if (i == selectedSlotIndex || boardValues[i] != number) continue;
            if (isFixed[i])
            {
                ShowFeedback($"<color=#E5A93C>{number}은(는) 이미 고정된 신석입니다.</color>");
                return;
            }
            boardValues[i] = 0;
        }

        int placedSlot = selectedSlotIndex;
        boardValues[placedSlot] = number;
        selectedSlotIndex = -1; // 입력 후 선택 해제
        UpdateVisuals();
        OnNumberPlaced?.Invoke(placedSlot, number);
    }

    private void CheckSolution()
    {
        if (isCleared) return;

        for (int i = 0; i < 9; i++)
        {
            if (boardValues[i] == 0)
            {
                ShowFeedback("<color=#FF5555>아직 채워지지 않은 칸이 있습니다.</color>");
                return;
            }
        }

        int wrongLines = 8 - CountLinesOf15();
        if (wrongLines == 0)
        {
            isCleared = true;
            ClearFeedback();
            if (successNotice != null) successNotice.SetActive(true);

            if (!rewarded)
            {
                rewarded = true;
                if (clueIndex >= 0 && !string.IsNullOrEmpty(clueText) && AnubisNotebookManager.Instance != null)
                    AnubisNotebookManager.Instance.UnlockClue(clueIndex, clueText);
                if (!string.IsNullOrEmpty(rewardItemId) && ItemInventory.Instance != null)
                    ItemInventory.Instance.Grant(rewardItemId);
            }
            OnSolved?.Invoke();
        }
        else
        {
            ShowFeedback($"<color=#FF4444>합이 15가 아닌 줄이 {wrongLines}개 있습니다. (초록색 숫자가 맞는 줄)</color>");
        }
    }

    private int LineSum(int a, int b, int c) => boardValues[a] + boardValues[b] + boardValues[c];

    private int CountLinesOf15()
    {
        int ok = 0;
        if (LineSum(0, 1, 2) == 15) ok++;
        if (LineSum(3, 4, 5) == 15) ok++;
        if (LineSum(6, 7, 8) == 15) ok++;
        if (LineSum(0, 3, 6) == 15) ok++;
        if (LineSum(1, 4, 7) == 15) ok++;
        if (LineSum(2, 5, 8) == 15) ok++;
        if (LineSum(0, 4, 8) == 15) ok++;
        if (LineSum(2, 4, 6) == 15) ok++;
        return ok;
    }

    private void ResetBoard()
    {
        if (isCleared) return;
        ClearFeedback();
        InitBoard();
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < 9; i++)
        {
            if (boardSlotTexts[i] != null)
                boardSlotTexts[i].text = (boardValues[i] == 0) ? "" : boardValues[i].ToString();

            if (boardSlotHighlights[i] != null)
                boardSlotHighlights[i].color = isFixed[i] ? colFixedSlot : (i == selectedSlotIndex) ? colSelectedSlot : colDefaultSlot;
        }

        if (lineSumTexts != null && lineSumTexts.Length >= 8)
        {
            UpdateLineSumText(lineSumTexts[0], LineSum(0, 1, 2));
            UpdateLineSumText(lineSumTexts[1], LineSum(3, 4, 5));
            UpdateLineSumText(lineSumTexts[2], LineSum(6, 7, 8));
            UpdateLineSumText(lineSumTexts[3], LineSum(0, 3, 6));
            UpdateLineSumText(lineSumTexts[4], LineSum(1, 4, 7));
            UpdateLineSumText(lineSumTexts[5], LineSum(2, 5, 8));
            UpdateLineSumText(lineSumTexts[6], LineSum(0, 4, 8));
            UpdateLineSumText(lineSumTexts[7], LineSum(2, 4, 6));
        }
    }

    private void UpdateLineSumText(TMP_Text txt, int sum)
    {
        if (txt == null) return;
        txt.text = sum.ToString();
        txt.color = (sum == 15) ? new Color(0.2f, 1f, 0.4f, 1f) : new Color(0.85f, 0.85f, 0.85f, 0.7f);
    }

    private void ShowFeedback(string msg)
    {
        ClearFeedback();
        feedbackCoroutine = StartCoroutine(FeedbackRoutine(msg));
    }

    private IEnumerator FeedbackRoutine(string msg)
    {
        if (textFeedbackNotice != null) textFeedbackNotice.text = msg;
        yield return new WaitForSeconds(2.5f);
        if (textFeedbackNotice != null) textFeedbackNotice.text = "";
    }

    private void ClearFeedback()
    {
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }
        if (textFeedbackNotice != null) textFeedbackNotice.text = "";
    }
}