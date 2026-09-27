using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// OBJ_003 서쪽 벽 회로판: E키로 오버레이 열기/닫기 + 보드 클릭 조작
public class CircuitBoardStation : MonoBehaviour, IInteractable
{
    [Header("연결")]
    [SerializeField] private CircuitBoardPuzzle puzzle;
    [SerializeField] private CircuitBoardView view;
    [SerializeField] private GameObject overlayPanel;

    // B안: 조각 선택 버튼 (표시 순서, 버튼 글자)
    private static readonly string[] PieceIds =
    {
        ItemIds.Frag1, ItemIds.Frag2, ItemIds.Frag3,
        CircuitBoardPuzzle.MirrorA, CircuitBoardPuzzle.MirrorB
    };
    private static readonly string[] PieceLabels =
    {
        "조각 1", "조각 2", "조각 3", "거울 A", "거울 B"
    };

    private readonly Dictionary<string, Image> pieceButtons = new Dictionary<string, Image>();
    private string selectedPieceId; // null이면 선택 안 됨
    private Text statusText;
    private Font font;

    private Color gold, paper, darkBrown, sandWall;

    private bool isOpen;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;

    private void Start()
    {
        gold = Hex("#E8B23A");
        paper = Hex("#F6F1E7");
        darkBrown = Hex("#4A3423");
        sandWall = Hex("#C9A66B");
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildPieceBar();
        overlayPanel.SetActive(false);

        view.OnCellClicked += HandleCellClick;
        ItemInventory.Instance.OnItemGranted += OnInventoryChanged;
        ItemInventory.Instance.OnItemConsumed += OnInventoryChanged;
    }

    private void OnDestroy()
    {
        if (view != null) view.OnCellClicked -= HandleCellClick;
        if (ItemInventory.Instance != null)
        {
            ItemInventory.Instance.OnItemGranted -= OnInventoryChanged;
            ItemInventory.Instance.OnItemConsumed -= OnInventoryChanged;
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
        return isOpen ? "E: 회로판 닫기" : "E: 회로판 조사";
    }

    public void Interact(GameObject player)
    {
        if (isOpen) CloseBoard();
        else OpenBoard(player);
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

        selectedPieceId = null;
        RefreshPieceBar();
        statusText.text = "";

        puzzle.Open(); // 처음 열 때 1단계(관찰) 자동 클리어
        Debug.Log("[P2] 회로판 열림");
    }

    private void CloseBoard()
    {
        overlayPanel.SetActive(false);
        isOpen = false;
        selectedPieceId = null;

        if (playerController != null) playerController.enabled = true;
        Debug.Log("[P2] 회로판 닫힘");
    }

    // ================= 조각 선택 버튼 줄 (B안) =================
    private void BuildPieceBar()
    {
        RectTransform root = overlayPanel.GetComponent<RectTransform>();

        CreateText(root, "보유 조각", 24, new Vector2(0, -385), new Vector2(400, 36), sandWall);

        for (int i = 0; i < PieceIds.Length; i++)
        {
            string id = PieceIds[i];
            float x = -360f + i * 180f;

            Image img = CreateRect(root, $"PieceBtn_{id}", new Vector2(x, -440), new Vector2(160, 60), paper);
            img.raycastTarget = true;

            Button btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => OnPieceButtonClicked(id));

            CreateText(img.rectTransform, PieceLabels[i], 26, Vector2.zero, new Vector2(160, 60), darkBrown);
            pieceButtons[id] = img;
        }

        statusText = CreateText(root, "", 24, new Vector2(0, -495), new Vector2(1400, 36), paper);

        CreateText(root, "조각 선택 → 빈 슬롯 클릭  |  거울 좌클릭: 회전  |  우클릭: 회수  |  E / Esc: 닫기",
            20, new Vector2(0, 470), new Vector2(1600, 32), sandWall);
    }

    private void OnPieceButtonClicked(string id)
    {
        // 같은 버튼 다시 누르면 선택 취소
        selectedPieceId = selectedPieceId == id ? null : id;
        RefreshPieceBar();
    }

    private void OnInventoryChanged(string itemId)
    {
        RefreshPieceBar();
    }

    private void RefreshPieceBar()
    {
        // 선택한 조각을 더 이상 안 갖고 있으면 선택 해제
        if (selectedPieceId != null && !ItemInventory.Instance.Has(selectedPieceId))
        {
            selectedPieceId = null;
        }

        foreach (var pair in pieceButtons)
        {
            bool owned = ItemInventory.Instance.Has(pair.Key);
            pair.Value.gameObject.SetActive(owned); // 가진 조각만 보이게
            pair.Value.color = pair.Key == selectedPieceId ? gold : paper;
        }
    }

    // ================= 보드 클릭 처리 =================
    private void HandleCellClick(Vector2Int tile, bool rightClick)
    {
        if (!isOpen) return;

        // 프리즘 소켓
        if (tile == CircuitBoardPuzzle.PrismTile)
        {
            if (rightClick) puzzle.RemovePrism();
            else puzzle.InsertPrism();
            return;
        }

        bool hasPiece = puzzle.GetPieces().Exists(p => p.tile == tile);

        if (hasPiece)
        {
            if (rightClick) puzzle.RemovePiece(tile);  // 우클릭 = 회수
            else puzzle.RotatePiece(tile);             // 좌클릭 = 회전
        }
        else if (!rightClick)
        {
            TryInsertSelected(tile);                   // 빈 슬롯 좌클릭 = 선택한 조각 삽입
        }
    }

    private void TryInsertSelected(Vector2Int tile)
    {
        if (selectedPieceId == null)
        {
            statusText.text = "먼저 아래에서 넣을 조각을 선택하세요.";
            return;
        }

        puzzle.InsertPiece(tile, selectedPieceId, MirrorType.Slash); // '/'로 넣고, 클릭으로 회전
        statusText.text = "";
        // 삽입되면 인벤토리에서 빠지면서 RefreshPieceBar가 선택을 자동 해제함
    }

    // ================= 도우미 함수 =================
    private Image CreateRect(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

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