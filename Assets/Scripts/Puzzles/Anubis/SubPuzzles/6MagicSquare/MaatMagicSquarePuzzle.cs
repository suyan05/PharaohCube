using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 42인의 제약 마방진 (서브퍼즐 4). 3x3 합 15 맞추기
// 패널 루트에 부착

public class MaatMagicSquarePuzzle : MonoBehaviour
{
    [Header("3x3 보드 슬롯 버튼 9개")]
    [SerializeField] private Button[] boardSlotButtons = new Button[9];
    [SerializeField] private TMP_Text[] boardSlotTexts = new TMP_Text[9];
    [SerializeField] private Image[] boardSlotHighlights = new Image[9];

    [Header("하단 1~9 숫자 선택 버튼 9개")]
    [SerializeField] private Button[] numPadButtons = new Button[9];

    [Header("실시간 라인 합계 표시 (가로3, 세로3, 대각선2 = 총 8개)")]
    [SerializeField] private TMP_Text[] lineSumTexts = new TMP_Text[8];

    [Header("시스템 버튼 및 알림 패널")]
    [SerializeField] private Button btnCheckSolution;                           // 봉인 해제 판정
    [SerializeField] private Button btnClearBoard;                              // 보드 비우기
    [SerializeField] private Button btnClose;                                   // 나가기
    [SerializeField] private GameObject successNotice;
    [SerializeField] private TMP_Text textFeedbackNotice;
    [SerializeField] private TMP_Text textHintNotice;

    private int[] boardValues = new int[9];
    private int selectedSlotIndex = -1;
    private bool isCleared = false;
    private Coroutine feedbackCoroutine;

    private const int CENTER_INDEX = 4;
    private const int CENTER_VALUE = 5;

    private const int SETH_INDEX = 0;
    private const int SETH_REQUIRED = 8;
    private const int HORUS_INDEX = 8;
    private const int HORUS_REQUIRED = 2;

    private readonly Color colDefaultSlot = new Color(0.2f, 0.18f, 0.15f, 1f);
    private readonly Color colSelectedSlot = new Color(0.9f, 0.75f, 0.2f, 1f);
    private readonly Color colFixedSlot = new Color(0.4f, 0.32f, 0.15f, 1f);

    private void Start()
    {
        for (int i = 0; i < boardSlotButtons.Length; i++)
        {
            int idx = i;
            if (boardSlotButtons[idx] != null)
                boardSlotButtons[idx].onClick.AddListener(() => OnSelectSlot(idx));
        }

        for (int i = 0; i < numPadButtons.Length; i++)
        {
            int num = i + 1;
            if (numPadButtons[i] != null)
                numPadButtons[i].onClick.AddListener(() => OnInputNumber(num));
        }

        if (btnCheckSolution != null) btnCheckSolution.onClick.AddListener(CheckSolution);
        if (btnClearBoard != null) btnClearBoard.onClick.AddListener(ResetBoard);
        if (btnClose != null) btnClose.onClick.AddListener(() => gameObject.SetActive(false));

        if (successNotice != null) successNotice.SetActive(false);
        if (textFeedbackNotice != null) textFeedbackNotice.text = "";

        if (textHintNotice != null)
        {
            textHintNotice.text = "<b>[42인의 제약 마방진]</b>\n모든 가로·세로·대각선의 합을 15로 맞추되,\n대각선의 상극 신수석(좌상단 세트: 8 / 우하단 호루스: 2)을 반드시 만족시키십시오.";
        }

        InitBoard();
    }

    private void OnEnable()
    {
        ClearFeedback();
        UpdateVisuals();
    }

    private void InitBoard()
    {
        for (int i = 0; i < 9; i++) boardValues[i] = 0;
        boardValues[CENTER_INDEX] = CENTER_VALUE;
        selectedSlotIndex = -1;
        UpdateVisuals();
    }

    private void OnSelectSlot(int index)
    {
        if (isCleared) return;
        if (index == CENTER_INDEX)
        {
            ShowFeedback("<color=#E5A93C>중앙 태양석(5)은 마아트의 절대 핵이므로 변경할 수 없습니다.</color>");
            return;
        }

        selectedSlotIndex = index;
        UpdateVisuals();
    }

    private void OnInputNumber(int number)
    {
        if (isCleared || selectedSlotIndex == -1 || selectedSlotIndex == CENTER_INDEX) return;
        ClearFeedback();

        for (int i = 0; i < 9; i++)
        {
            if (i != selectedSlotIndex && boardValues[i] == number)
            {
                boardValues[i] = 0;
            }
        }

        boardValues[selectedSlotIndex] = number;
        UpdateVisuals();
    }

    private void CheckSolution()
    {
        if (isCleared) return;

        for (int i = 0; i < 9; i++)
        {
            if (boardValues[i] == 0)
            {
                ShowFeedback("<color=#FF5555>아직 채워지지 않은 신석 슬롯이 존재합니다.</color>");
                return;
            }
        }

        if (boardValues[SETH_INDEX] != SETH_REQUIRED || boardValues[HORUS_INDEX] != HORUS_REQUIRED)
        {
            ShowFeedback("<color=#FF4444>상극 신수 불일치: 좌상단 세트석(8)과 우하단 호루스석(2)의 위치가 틀렸습니다!</color>");
            return;
        }

        int r0 = boardValues[0] + boardValues[1] + boardValues[2];
        int r1 = boardValues[3] + boardValues[4] + boardValues[5];
        int r2 = boardValues[6] + boardValues[7] + boardValues[8];

        int c0 = boardValues[0] + boardValues[3] + boardValues[6];
        int c1 = boardValues[1] + boardValues[4] + boardValues[7];
        int c2 = boardValues[2] + boardValues[5] + boardValues[8];

        int d0 = boardValues[0] + boardValues[4] + boardValues[8];
        int d1 = boardValues[2] + boardValues[4] + boardValues[6];

        if (r0 == 15 && r1 == 15 && r2 == 15 &&
            c0 == 15 && c1 == 15 && c2 == 15 &&
            d0 == 15 && d1 == 15)
        {
            isCleared = true;
            ClearFeedback();
            if (successNotice != null) successNotice.SetActive(true);

            if (AnubisNotebookManager.Instance != null)
            {
                AnubisNotebookManager.Instance.UnlockClue(3,
                    "청동의 그릇(B)은 흙의 단지(C) 다섯 개의 무게와 같도다. (B = 5C)");
            }

            if (ItemInventory.Instance != null)
                ItemInventory.Instance.Grant(AnubisItemIds.BronzeBowl);
        }
        else
        {
            ShowFeedback("<color=#FF4444>마방진 불일치: 합이 15가 되지 않는 가로·세로·대각선 라인이 존재합니다.</color>");
        }
    }

    private void ResetBoard()
    {
        if (isCleared) return;
        InitBoard();
        ClearFeedback();
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < 9; i++)
        {
            if (boardSlotTexts[i] != null)
            {
                boardSlotTexts[i].text = (boardValues[i] == 0) ? "" : boardValues[i].ToString();
            }

            if (boardSlotHighlights[i] != null)
            {
                if (i == CENTER_INDEX)
                {
                    boardSlotHighlights[i].color = colFixedSlot;
                }
                else
                {
                    boardSlotHighlights[i].color = (i == selectedSlotIndex) ? colSelectedSlot : colDefaultSlot;
                }
            }
        }

        if (lineSumTexts != null && lineSumTexts.Length >= 8)
        {
            int r0 = boardValues[0] + boardValues[1] + boardValues[2];
            int r1 = boardValues[3] + boardValues[4] + boardValues[5];
            int r2 = boardValues[6] + boardValues[7] + boardValues[8];
            int c0 = boardValues[0] + boardValues[3] + boardValues[6];
            int c1 = boardValues[1] + boardValues[4] + boardValues[7];
            int c2 = boardValues[2] + boardValues[5] + boardValues[8];
            int d0 = boardValues[0] + boardValues[4] + boardValues[8];
            int d1 = boardValues[2] + boardValues[4] + boardValues[6];

            UpdateLineSumText(lineSumTexts[0], r0);
            UpdateLineSumText(lineSumTexts[1], r1);
            UpdateLineSumText(lineSumTexts[2], r2);
            UpdateLineSumText(lineSumTexts[3], c0);
            UpdateLineSumText(lineSumTexts[4], c1);
            UpdateLineSumText(lineSumTexts[5], c2);
            UpdateLineSumText(lineSumTexts[6], d0);
            UpdateLineSumText(lineSumTexts[7], d1);
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