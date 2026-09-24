using UnityEngine;

// OBJ_003 서쪽 벽 회로판: E키로 오버레이 열기/닫기 + 보드 클릭 조작
public class CircuitBoardStation : MonoBehaviour, IInteractable
{
    [Header("연결")]
    [SerializeField] private CircuitBoardPuzzle puzzle;
    [SerializeField] private CircuitBoardView view;
    [SerializeField] private GameObject overlayPanel;

    // A안: 빈 슬롯 클릭 시 가장 최근에 얻은 조각부터 자동 삽입
    private static readonly string[] AutoInsertOrder =
    {
        ItemIds.Frag3, ItemIds.Frag2, ItemIds.Frag1,
        CircuitBoardPuzzle.MirrorB, CircuitBoardPuzzle.MirrorA
    };

    private bool isOpen;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;

    private void Start()
    {
        overlayPanel.SetActive(false);
        view.OnCellClicked += HandleCellClick;
    }

    private void OnDestroy()
    {
        if (view != null) view.OnCellClicked -= HandleCellClick;
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

        // 이동 멈추기 (컴포넌트만 끄면 속도가 남아서 미끄러지니까 속도 0으로)
        if (playerController != null) playerController.enabled = false;
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;

        overlayPanel.SetActive(true);
        isOpen = true;

        puzzle.Open(); // 처음 열 때 1단계(관찰) 자동 클리어
        Debug.Log("[P2] 회로판 열림 (좌클릭: 회전/삽입, 우클릭: 회수, E/Esc: 닫기)");
    }

    private void CloseBoard()
    {
        overlayPanel.SetActive(false);
        isOpen = false;

        if (playerController != null) playerController.enabled = true;
        Debug.Log("[P2] 회로판 닫힘");
    }

    // ================= 클릭 처리 =================
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
            TryAutoInsert(tile);                       // 빈 슬롯 좌클릭 = 삽입
        }
    }

    // A안(자동선택) 은 'TryAutoInsert' 안에만 있어서, 추후 B안으로 변경 시 이 부분 만 수정하면 ㅇㅋ
    private void TryAutoInsert(Vector2Int tile)
    {
        foreach (string id in AutoInsertOrder)
        {
            if (ItemInventory.Instance.Has(id))
            {
                puzzle.InsertPiece(tile, id, MirrorType.Slash); // '/'로 넣고, 클릭으로 회전
                return;
            }
        }

        Debug.Log("[P2] 넣을 거울 조각이 없음");
    }
}