using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CircuitBoardView : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private CircuitBoardPuzzle puzzle;
    [SerializeField] private RectTransform boardRoot;

    [Header("크기")]
    [SerializeField] private float cellSize = 60f;
    [SerializeField] private float beamWidth = 10f;

    // 기획서 11번 아트 가이드 색상
    private Color floorColor, obstacleColor, slotColor, prismSocketColor, prismOnColor;
    private Color beamOrange, beamBlue, mirrorColor, fragmentColor, gridColor;

    private RectTransform tileLayer, beamLayer, pieceLayer, goalLayer;

    private int Cols => CircuitBoardPuzzle.MaxX - CircuitBoardPuzzle.MinX + 1; // 7
    private int Rows => CircuitBoardPuzzle.MaxY - CircuitBoardPuzzle.MinY + 1; // 9

    private void Start()
    {
        SetupColors();

        boardRoot.sizeDelta = new Vector2(Cols * cellSize, Rows * cellSize);

        // 그리는 순서 = 레이어 순서 (아래 → 위)
        tileLayer = CreateLayer("TileLayer");
        beamLayer = CreateLayer("BeamLayer");
        pieceLayer = CreateLayer("PieceLayer");
        goalLayer = CreateLayer("GoalLayer");

        BuildTiles();

        puzzle.OnBoardChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (puzzle != null) puzzle.OnBoardChanged -= Refresh;
    }

    private void SetupColors()
    {
        floorColor = Hex("#E8D9B8");                                        // 사암 바닥
        gridColor = Hex("#4A3423");                                         // 짙은 갈색 (칸 사이 선)
        obstacleColor = Hex("#6B5438");                                     // 장애물 (갈색)
        slotColor = Hex("#C9A66B");                                         // 빈 슬롯 (사암 벽색)
        prismSocketColor = Hex("#7846A0"); prismSocketColor.a = 0.35f;
        prismOnColor = Hex("#7846A0");                                      // 프리즘 보라
        beamOrange = Hex("#E07A3F");                                        // 주황 빛
        beamBlue = Hex("#3E7BD6");                                          // 밤빛 파랑
        mirrorColor = Hex("#1E3A5F");                                       // 초기 거울 A, B (라피스 블루)
        fragmentColor = Hex("#E8B23A");                                     // 거울 조각 ①②③ (금색)
    }

    // ================= 고정 요소 (한 번만 그림) =================
    private void BuildTiles()
    {
        // 보드 배경 (칸 사이가 비어서 격자선처럼 보임)
        CreateRect(tileLayer, "Background", Vector2.zero,
            new Vector2(Cols * cellSize + 6, Rows * cellSize + 6), gridColor);

        for (int x = CircuitBoardPuzzle.MinX; x <= CircuitBoardPuzzle.MaxX; x++)
        {
            for (int y = CircuitBoardPuzzle.MinY; y <= CircuitBoardPuzzle.MaxY; y++)
            {
                Vector2Int tile = new Vector2Int(x, y);

                Color c = floorColor;
                if (CircuitBoardPuzzle.IsObstacle(tile)) c = obstacleColor;
                else if (CircuitBoardPuzzle.IsSlot(tile)) c = slotColor;

                CreateRect(tileLayer, $"Tile_{x}_{y}", TileToPos(tile),
                    new Vector2(cellSize - 3, cellSize - 3), c);
            }
        }

        // 프리즘 소켓 (옅은 보라)
        CreateRect(tileLayer, "PrismSocket", TileToPos(CircuitBoardPuzzle.PrismTile),
            new Vector2(cellSize * 0.7f, cellSize * 0.7f), prismSocketColor);

        // 광원 (1,4)
        CreateRect(tileLayer, "Source", TileToPos(CircuitBoardPuzzle.Source),
            new Vector2(cellSize * 0.5f, cellSize * 0.5f), beamOrange);
    }

    // ================= 바뀌는 요소 (보드 바뀔 때마다 다시 그림) =================
    private void Refresh()
    {
        ClearLayer(beamLayer);
        ClearLayer(pieceLayer);
        ClearLayer(goalLayer);

        DrawBeam();
        DrawPieces();
        DrawGoals();
    }

    private void DrawBeam()
    {
        var path = puzzle.BeamPath;
        if (path.Count == 0) return;

        Color current = beamOrange;

        for (int i = 0; i < path.Count; i++)
        {
            // 프리즘 칸을 지난 뒤부터 파란색
            if (puzzle.PrismOn && path[i] == CircuitBoardPuzzle.PrismTile)
                current = beamBlue;

            Vector2 from = TileToPos(path[i]);
            Vector2 to;

            if (i + 1 < path.Count)
                to = TileToPos(path[i + 1]);
            else if (puzzle.BeamExited)
                to = ExitPos(path[i]);   // 보드 밖으로 한 칸 더
            else
                break;                   // 차단/소멸이면 여기서 끝

            DrawSegment(from, to, current);
        }
    }

    private Vector2 ExitPos(Vector2Int last)
    {
        switch (puzzle.ExitEdge)
        {
            case 'N': return TileToPos(new Vector2Int(puzzle.ExitX, CircuitBoardPuzzle.MaxY + 1));
            case 'S': return TileToPos(new Vector2Int(puzzle.ExitX, CircuitBoardPuzzle.MinY - 1));
            case 'E': return TileToPos(new Vector2Int(CircuitBoardPuzzle.MaxX + 1, last.y));
            default: return TileToPos(new Vector2Int(CircuitBoardPuzzle.MinX - 1, last.y));
        }
    }

    private void DrawSegment(Vector2 a, Vector2 b, Color color)
    {
        Vector2 mid = (a + b) / 2f;
        Vector2 diff = b - a;

        // 빔은 항상 가로, 세로라서 회전 없이 직사각형으로 충분
        Vector2 size = Mathf.Abs(diff.x) > Mathf.Abs(diff.y)
            ? new Vector2(Mathf.Abs(diff.x) + beamWidth, beamWidth)
            : new Vector2(beamWidth, Mathf.Abs(diff.y) + beamWidth);

        CreateRect(beamLayer, "Beam", mid, size, color);
    }

    private void DrawPieces()
    {
        foreach (var (tile, id, type) in puzzle.GetPieces())
        {
            bool isFragment = id != CircuitBoardPuzzle.MirrorA && id != CircuitBoardPuzzle.MirrorB;
            Color c = isFragment ? fragmentColor : mirrorColor;

            // 가로 막대를 +45도 돌리면 '/', -45도 돌리면 '\'
            float angle = type == MirrorType.Slash ? 45f : -45f;

            CreateRect(pieceLayer, $"Mirror_{id}", TileToPos(tile),
                new Vector2(cellSize * 1.1f, 9f), c, angle);
        }

        if (puzzle.PrismOn)
        {
            CreateRect(pieceLayer, "Prism", TileToPos(CircuitBoardPuzzle.PrismTile),
                new Vector2(cellSize * 0.55f, cellSize * 0.55f), prismOnColor, 45f);
        }
    }

    private void DrawGoals()
    {
        for (int i = 0; i < puzzle.GoalCount; i++)
        {
            puzzle.GetGoal(i, out char edge, out int x, out bool needBlue, out string flag, out string label);

            int y = edge == 'N' ? CircuitBoardPuzzle.MaxY + 1 : CircuitBoardPuzzle.MinY - 1;
            Vector2 pos = TileToPos(new Vector2Int(x, y));

            bool lit = GameManager.Instance != null && GameManager.Instance.HasFlag(flag);

            Color c = needBlue ? beamBlue : beamOrange;
            if (!lit) c.a = 0.3f; // 꺼져 있으면 흐리게

            Image img = CreateRect(goalLayer, $"Goal_{label}", pos,
                new Vector2(cellSize * 0.8f, cellSize * 0.8f), c);

            CreateLabel(img.rectTransform, label);
        }
    }

    // ================= 도우미 함수 =================
    // 보드 좌표 (x, y) → 화면 위치 (보드 중앙이 0,0) 변환임
    private Vector2 TileToPos(Vector2Int tile)
    {
        float px = (tile.x - CircuitBoardPuzzle.MinX - (Cols - 1) / 2f) * cellSize;
        float py = (tile.y - CircuitBoardPuzzle.MinY - (Rows - 1) / 2f) * cellSize;
        return new Vector2(px, py);
    }

    private RectTransform CreateLayer(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(boardRoot, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private Image CreateRect(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color, float rotZ = 0f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localRotation = Quaternion.Euler(0, 0, rotZ);

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false; // 클릭은 다음 단계에서 따로 처리
        return img;
    }

    private void CreateLabel(RectTransform parent, string text)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
    }

    private void ClearLayer(RectTransform layer)
    {
        foreach (Transform child in layer)
        {
            Destroy(child.gameObject);
        }
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
/*라벨은 일부러 G1~G4 영어로 사용했습니다. TextMeshPro 한글 없어서*/