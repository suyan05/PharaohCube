using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 42인의 제약 마방진 - 실전 (서브퍼즐 4)
// 숫자 대신 이집트 신들의 상징을 배치. 상징 = 숫자 대응표는 4번 방 양피지에만 있음
// 내부적으로는 숫자(1~9)로 판정: 가로·세로·대각선 8줄의 합이 모두 15면 정답
public class SymbolSquarePuzzleReal : MonoBehaviour
{
    [Header("상징 (인덱스 0 = 숫자 1 ... 인덱스 8 = 숫자 9)")]
    [SerializeField]
    private string[] symbolNames =
    {
        "스카라브", "뱀", "따오기", "고양이", "자칼", "악어", "사자", "암무트", "매"
    };
    [Tooltip("상징 그림 (선택). 넣으면 글자 대신 그림으로 표시")]
    [SerializeField] private Sprite[] symbolSprites = new Sprite[9];

    [Header("난이도")]
    [Tooltip("미리 채울 숫자 (0 = 빈칸, 1~9 = 고정칸)\n배치: 0 1 2 / 3 4 5 / 6 7 8")]
    [SerializeField] private int[] presetValues = { 0, 0, 0, 0, 5, 0, 0, 0, 0 };
    [Tooltip("오른쪽 선택판에 놓일 순서 (숫자). 1~9 순서가 아니게 섞어야 함")]
    [SerializeField] private int[] paletteOrder = { 6, 1, 8, 3, 5, 9, 2, 7, 4 };
    [TextArea(2, 4)]
    [SerializeField] private string hintText = "<b>[42인의 심판관]</b>\n왼쪽 빈칸을 고른 뒤, 오른쪽에서 신의 상징을 고르십시오.";

    [Header("클리어 처리")]
    [Tooltip("사자의 서 단서 슬롯 (-1이면 없음)")]
    [SerializeField] private int clueIndex = 3;
    [TextArea(2, 4)]
    [SerializeField] private string clueText = "청동의 그릇(B)은 흙의 단지(C) 다섯 개의 무게와 같도다. (B = 5C)";
    [Tooltip("클리어 보상 아이템 ID (비우면 지급 안 함)")]
    [SerializeField] private string rewardItemId = AnubisItemIds.BronzeBowl;
    [TextArea(1, 3)]
    [SerializeField] private string clearMessage = "42인의 심판관이 고개를 끄덕입니다!\n사자의 서에 네 번째 비문이 기록되었습니다.";

    [Header("보드 9칸")]
    [SerializeField] private Button[] boardButtons = new Button[9];
    [SerializeField] private Image[] boardImages = new Image[9];
    [SerializeField] private TMP_Text[] boardTexts = new TMP_Text[9];
    [SerializeField] private Image[] boardIcons = new Image[9];

    [Header("상징 선택판 9개")]
    [SerializeField] private Button[] paletteButtons = new Button[9];
    [SerializeField] private TMP_Text[] paletteTexts = new TMP_Text[9];
    [SerializeField] private Image[] paletteIcons = new Image[9];

    [Header("시스템")]
    [SerializeField] private Button btnCheck;
    [SerializeField] private Button btnClear;
    [SerializeField] private Button btnClose;
    [SerializeField] private TMP_Text textHint;
    [SerializeField] private TMP_Text textFeedback;
    [SerializeField] private TMP_Text textAttempts;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private TMP_Text textSuccessNotice;

    private static readonly Color ColEmpty = new Color(0.20f, 0.18f, 0.15f, 1f);
    private static readonly Color ColSelected = new Color(0.90f, 0.75f, 0.20f, 1f);
    private static readonly Color ColFixed = new Color(0.40f, 0.32f, 0.15f, 1f);
    private static readonly Color ColFilled = new Color(0.30f, 0.24f, 0.16f, 1f);

    private readonly int[] board = new int[9];     // 각 칸의 숫자 (0 = 빈칸)
    private readonly bool[] isFixed = new bool[9];
    private int selected = -1;
    private int attempts;
    private bool isCleared;
    private bool rewarded;
    private Coroutine feedbackRoutine;

    private void Start()
    {
        for (int i = 0; i < 9; i++)
        {
            int idx = i;
            if (boardButtons[i] != null) boardButtons[i].onClick.AddListener(() => SelectSlot(idx));
            if (paletteButtons[i] != null) paletteButtons[i].onClick.AddListener(() => PickSymbol(idx));
        }
        if (btnCheck != null) btnCheck.onClick.AddListener(CheckSolution);
        if (btnClear != null) btnClear.onClick.AddListener(ClearBoard);
        if (btnClose != null) btnClose.onClick.AddListener(() => gameObject.SetActive(false));

        if (textHint != null) textHint.text = hintText;
        if (textSuccessNotice != null) textSuccessNotice.text = clearMessage;
        if (successNotice != null) successNotice.SetActive(false);
        if (textFeedback != null) textFeedback.text = "";

        SetupPalette();
        InitBoard();
    }

    // 숫자 → 상징 이름 / 그림
    private string NameOf(int number) => (number >= 1 && number <= symbolNames.Length) ? symbolNames[number - 1] : "";
    private Sprite SpriteOf(int number) => (symbolSprites != null && number >= 1 && number <= symbolSprites.Length) ? symbolSprites[number - 1] : null;

    private int PaletteNumber(int paletteIndex) =>
        (paletteOrder != null && paletteIndex < paletteOrder.Length) ? Mathf.Clamp(paletteOrder[paletteIndex], 1, 9) : paletteIndex + 1;

    private void SetupPalette()
    {
        for (int i = 0; i < 9; i++)
        {
            int number = PaletteNumber(i);
            ShowSymbol(paletteTexts[i], paletteIcons[i], number);
        }
    }

    private void InitBoard()
    {
        for (int i = 0; i < 9; i++)
        {
            int v = (presetValues != null && i < presetValues.Length) ? Mathf.Clamp(presetValues[i], 0, 9) : 0;
            board[i] = v;
            isFixed[i] = v != 0;
        }
        selected = -1;
        UpdateBoard();
    }

    private void SelectSlot(int index)
    {
        if (isCleared) return;
        if (isFixed[index])
        {
            ShowFeedback("<color=#E5A93C>그 자리는 신이 이미 지키고 있습니다.</color>");
            return;
        }
        selected = index;
        UpdateBoard();
    }

    private void PickSymbol(int paletteIndex)
    {
        if (isCleared) return;
        if (selected == -1)
        {
            ShowFeedback("<color=#E5A93C>먼저 왼쪽의 빈칸을 고르십시오.</color>");
            return;
        }

        int number = PaletteNumber(paletteIndex);

        for (int i = 0; i < 9; i++)
        {
            if (i == selected || board[i] != number) continue;
            if (isFixed[i])
            {
                ShowFeedback($"<color=#E5A93C>{NameOf(number)}은(는) 이미 자리를 지키고 있습니다.</color>");
                return;
            }
            board[i] = 0;
        }

        board[selected] = number;
        selected = -1;
        UpdateBoard();
    }

    private void CheckSolution()
    {
        if (isCleared) return;

        for (int i = 0; i < 9; i++)
        {
            if (board[i] == 0)
            {
                ShowFeedback("<color=#FF6655>아직 비어 있는 자리가 있습니다.</color>");
                return;
            }
        }

        attempts++;
        int ok = CountBalancedLines();

        if (ok == 8)
        {
            isCleared = true;
            if (successNotice != null) successNotice.SetActive(true);
            ClearFeedbackNow();
            GiveReward();
        }
        else
        {
            ShowFeedback($"<color=#FF6655>균형을 이룬 줄: {ok} / 8</color>");
        }
        UpdateBoard();
    }

    private int Line(int a, int b, int c) => board[a] + board[b] + board[c];

    private int CountBalancedLines()
    {
        int ok = 0;
        if (Line(0, 1, 2) == 15) ok++;
        if (Line(3, 4, 5) == 15) ok++;
        if (Line(6, 7, 8) == 15) ok++;
        if (Line(0, 3, 6) == 15) ok++;
        if (Line(1, 4, 7) == 15) ok++;
        if (Line(2, 5, 8) == 15) ok++;
        if (Line(0, 4, 8) == 15) ok++;
        if (Line(2, 4, 6) == 15) ok++;
        return ok;
    }

    private void ClearBoard()
    {
        if (isCleared) return;
        ClearFeedbackNow();
        InitBoard();
    }

    private void GiveReward()
    {
        if (rewarded) return;
        rewarded = true;
        if (clueIndex >= 0 && !string.IsNullOrEmpty(clueText) && AnubisNotebookManager.Instance != null)
            AnubisNotebookManager.Instance.UnlockClue(clueIndex, clueText);
        if (!string.IsNullOrEmpty(rewardItemId) && ItemInventory.Instance != null)
            ItemInventory.Instance.Grant(rewardItemId);
        Debug.Log($"[{name}] 클리어! 시도 {attempts}번", this);
    }

    private void UpdateBoard()
    {
        for (int i = 0; i < 9; i++)
        {
            ShowSymbol(boardTexts[i], boardIcons[i], board[i]);
            if (boardImages[i] != null)
            {
                boardImages[i].color = isFixed[i] ? ColFixed
                    : (i == selected) ? ColSelected
                    : (board[i] != 0) ? ColFilled : ColEmpty;
            }
        }
        if (textAttempts != null) textAttempts.text = $"판정 횟수: {attempts}";
    }

    // 그림이 있으면 그림, 없으면 이름 글자로 표시
    private void ShowSymbol(TMP_Text text, Image icon, int number)
    {
        Sprite sprite = number > 0 ? SpriteOf(number) : null;
        if (icon != null)
        {
            icon.gameObject.SetActive(sprite != null);
            icon.sprite = sprite;
        }
        if (text != null) text.text = (number > 0 && sprite == null) ? NameOf(number) : "";
    }

    private void ShowFeedback(string msg)
    {
        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(FeedbackRoutine(msg));
    }

    private IEnumerator FeedbackRoutine(string msg)
    {
        if (textFeedback != null) textFeedback.text = msg;
        yield return new WaitForSeconds(2.5f);
        if (textFeedback != null) textFeedback.text = "";
        feedbackRoutine = null;
    }

    private void ClearFeedbackNow()
    {
        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = null;
        if (textFeedback != null) textFeedback.text = "";
    }

    [ContextMenu("테스트: 정답 배치 (8 1 6 / 3 5 7 / 4 9 2)")]
    private void TestFillAnswer()
    {
        int[] answer = { 8, 1, 6, 3, 5, 7, 4, 9, 2 };
        for (int i = 0; i < 9; i++) if (!isFixed[i]) board[i] = answer[i];
        UpdateBoard();
        Debug.Log($"[{name}] 정답 배치 완료. '심판' 버튼을 눌러 확인하세요.", this);
    }
}

//윗줄	    암무트 / 스카라브 / 악어
//가운뎃줄	따오기 /  (자칼)  / 사자
//아랫줄	고양이 /    매    / 뱀