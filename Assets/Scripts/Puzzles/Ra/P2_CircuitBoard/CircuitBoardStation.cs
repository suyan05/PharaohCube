using System;
using UnityEngine;
using UnityEngine.UI;

// OBJ_003 회로판: E키로 오버레이 열기/닫기 + 거울 클릭(회전)
// 조각은 회로판을 열 때 CircuitBoardPuzzle이 정답 칸에 자동으로 끼움 -> 조각 선택 버튼 없음
// 판을 크게 쓰려고 안내 문구는 판 오른쪽에 세로로 배치
// 밤: 회로판은 이미 파란 빛으로 완성된 상태 (조작 없음, 보기만)
public class CircuitBoardStation : MonoBehaviour, IInteractable
{
    // CircuitBoardPuzzle.AdvanceTutorial의 튜토리얼 클릭 수와 같은 값 (거울 한 바퀴 = 4번)
    private const int TutorialClickCount = 4;

    private const string LockedMessage = "회로판은 이미 밤빛으로 물들어 있다. 더 이상 손댈 곳이 없다.";

    // 판 오른쪽 안내 영역 (1920x1080 기준, 판 가운데가 0,0)
    private static readonly Vector2 StatusPos = new Vector2(640f, 80f);
    private static readonly Vector2 StatusSize = new Vector2(520f, 240f);
    private static readonly Vector2 HelpPos = new Vector2(640f, -150f);
    private static readonly Vector2 HelpSize = new Vector2(520f, 120f);

    [Header("연결")]
    [SerializeField] private CircuitBoardPuzzle puzzle;
    [SerializeField] private CircuitBoardView view;
    [SerializeField] private GameObject overlayPanel;

    private Text statusText;
    private Text helpText;
    private Font font;
    private Color paper, sandWall;

    private bool isOpen;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;

    private void Start()
    {
        try
        {
            paper = Hex("#F6F1E7");
            sandWall = Hex("#C9A66B");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            WarnIfDuplicate();
            BuildTexts();
            overlayPanel.SetActive(false);

            view.OnCellClicked += HandleCellClick;
            puzzle.OnTutorialProgress += HandleTutorialProgress;
            puzzle.OnBoardChanged += RefreshStatus;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 회로판 초기화 오류: {e}");
        }
    }

    // 같은 회로판 화면을 쓰는 Station이 2개면 안내 문구가 겹치고 클릭이 꼬임
    private void WarnIfDuplicate()
    {
        var all = FindObjectsByType<CircuitBoardStation>(FindObjectsSortMode.None);
        if (all.Length > 1)
        {
            Debug.LogWarning($"[{name}] CircuitBoardStation이 씬에 {all.Length}개 있음 -> 하나만 남겨야 함");
        }
    }

    private void OnDestroy()
    {
        if (view != null) view.OnCellClicked -= HandleCellClick;
        if (puzzle != null)
        {
            puzzle.OnTutorialProgress -= HandleTutorialProgress;
            puzzle.OnBoardChanged -= RefreshStatus;
        }
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseBoard();
        }
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        return isOpen ? "E: 회로판 닫기" : "E: 태양의 빛 회로판 조사";
    }

    public void Interact(GameObject player)
    {
        try
        {
            if (isOpen) CloseBoard();
            else OpenBoard(player);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 회로판 상호작용 오류: {e}");
        }
    }

    // ================= 열기 / 닫기 =================
    private void OpenBoard(GameObject player)
    {
        playerController = player.GetComponent<PlayerController2D>();
        playerRb = player.GetComponent<Rigidbody2D>();

        // 이동 멈추기 (컴포넌트만 끄면 속도가 남아서 미끄러지니까 속도도 0으로)
        if (playerController != null) playerController.enabled = false;
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;

        overlayPanel.SetActive(true);
        isOpen = true;
        statusText.text = "";

        // 튜토리얼 미완료면 안내만, 완료면 1단계 자동 클리어 + 가져온 조각 자동 장착
        puzzle.Open();
        RefreshHelp();
        RefreshStatus();
        Debug.Log("[P2] 회로판 열림");
    }

    private void CloseBoard()
    {
        overlayPanel.SetActive(false);
        isOpen = false;

        if (playerController != null) playerController.enabled = true;
        Debug.Log("[P2] 회로판 닫힘");
    }

    // ================= 화면 글자 (판 오른쪽) =================
    private void BuildTexts()
    {
        RectTransform root = overlayPanel.GetComponent<RectTransform>();

        statusText = CreateSideText(root, "", 28, StatusPos, StatusSize, paper);
        helpText = CreateSideText(root, "", 22, HelpPos, HelpSize, sandWall);
        RefreshHelp();
    }

    // 밤(조작 잠금)에는 거울 조작 안내를 숨김
    private void RefreshHelp()
    {
        if (helpText == null) return;
        helpText.text = puzzle.IsLocked
            ? "E / Esc: 닫기"
            : "거울 클릭: 회전\n깜빡이는 문양에 빛을 보내자\nE / Esc: 닫기";
    }

    // 줄바꿈되는 왼쪽 정렬 글자
    private Text CreateSideText(RectTransform root, string text, int fontSize, Vector2 pos, Vector2 size, Color color)
    {
        Text t = CreateText(root, text, fontSize, pos, size, color);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.alignment = TextAnchor.UpperLeft;
        t.lineSpacing = 1.2f;
        return t;
    }

    // 튜토리얼이 끝난 뒤의 안내 (튜토리얼 중에는 HandleTutorialProgress가 담당)
    private void RefreshStatus()
    {
        if (!isOpen || statusText == null) return;
        if (!puzzle.TutorialDone) return;

        statusText.text = BuildStatusMessage();
    }

    private string BuildStatusMessage()
    {
        if (puzzle.IsLocked) return LockedMessage;
        if (puzzle.IsComplete) return "네 문양이 모두 빛난다. 태양 제단으로 가자.";

        string missing = puzzle.MissingHint();
        if (missing != null) return missing;

        return puzzle.PieceJustPlaced
            ? "조각이 제자리에 끼워졌다! 거울을 돌려 깜빡이는 문양에 빛을 보내자."
            : "거울을 클릭해서 돌려, 깜빡이는 문양에 빛을 보내자.";
    }

    // 4번째 클릭 알림은 튜토리얼 완료 플래그가 켜지기 "직전"에 와서 TutorialDone이 아직 false임
    // -> 클릭 수로 완료를 따로 확인해야 "4/4 회전 - 계속 클릭..." 문구가 남지 않음
    private void HandleTutorialProgress(int clicks)
    {
        if (clicks >= TutorialClickCount)
        {
            statusText.text = "빛이 다시 문양에 닿았다! 거울을 돌려 길을 바꾸는 방법을 익혔다.";
            return;
        }

        if (puzzle.TutorialDone)
        {
            statusText.text = "";
            return;
        }

        statusText.text = clicks == 0
            ? "거울을 클릭해서 돌려 보자. (한 바퀴 돌리면 원래대로 돌아온다)"
            : $"{clicks}/{TutorialClickCount} 회전 - 계속 클릭해서 원래 방향으로 돌려 보자";
    }

    // ================= 보드 클릭 처리 =================
    // 거울/조각 좌클릭 = 회전. 우클릭, 빈 칸, 프리즘 소켓은 반응 없음 (조각은 자동 장착)
    // 밤(잠금): 어디를 눌러도 완성 안내만
    private void HandleCellClick(Vector2Int tile, bool rightClick)
    {
        if (!isOpen || rightClick) return;

        if (puzzle.IsLocked)
        {
            statusText.text = LockedMessage;
            return;
        }
        if (tile == CircuitBoardPuzzle.PrismTile) return;

        try
        {
            bool hasPiece = puzzle.GetPieces().Exists(p => p.tile == tile);
            if (hasPiece) puzzle.RotatePiece(tile);
            else statusText.text = "빈 자리다. 조각은 가져오면 알아서 끼워진다.";
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 회로판 클릭 오류: {e}");
        }
    }

    // ================= 도우미 함수 =================
    private Text CreateText(RectTransform parent, string text, int fontSize, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}