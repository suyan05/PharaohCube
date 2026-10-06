using System;
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

    [Header("아트 - 보드 (비우면 기존 색 네모로 표시)")]
    [SerializeField] private Sprite boardBackgroundSprite;  // Circuit board
    [SerializeField] private Sprite floorSprite;            // 바닥 타일 (없으면 색)
    [SerializeField] private Sprite obstacleSprite;         // obstacle
    [SerializeField] private Sprite slotSprite;             // 빈 슬롯 (없으면 색)

    [Header("아트 - 거울 (그림이 이미 기울어져 있으면 회전 안 함)")]
    [SerializeField] private Sprite mirrorSlashSprite;      // Mirror tiles A  '/'
    [SerializeField] private Sprite mirrorBackslashSprite;  // Mirror tiles B  '\'
    [SerializeField] private Sprite fragmentSlashSprite;    // 거울 조각 '/' (없으면 거울 그림 사용)
    [SerializeField] private Sprite fragmentBackslashSprite;// 거울 조각 '\'

    [Header("아트 - 프리즘")]
    [SerializeField] private Sprite prismSocketSprite;      // Prism Socket (빈 소켓)
    [SerializeField] private Sprite prismOnSprite;          // 프리즘 장착됨

    [Header("아트 - 목표 문양 (G1 새벽 / G2 정오 / G3 황혼 / G4 밤 순서)")]
    [SerializeField] private Sprite[] goalSprites = new Sprite[4];

    [Header("아트 - 광원")]
    [SerializeField] private Sprite sourceSprite;

    [Header("타일 그림 크기 (칸 대비 배율)")]
    [SerializeField] private float pieceScale = 1f;
    [SerializeField] private float goalScale = 0.9f;

    // 칸이 클릭되면 (칸 좌표, 우클릭 여부) 전달
    public event Action<Vector2Int, bool> OnCellClicked;

    // 기획서 11번 아트 가이드 색상 (스프라이트가 없을 때 쓰는 대체 색)
    private Color floorColor, obstacleColor, slotColor, prismSocketColor, prismOnColor;
    private Color beamOrange, beamBlue, mirrorColor, fragmentColor, gridColor;

    private RectTransform tileLayer, beamLayer, pieceLayer, goalLayer, clickLayer;

    private int Cols => CircuitBoardPuzzle.MaxX - CircuitBoardPuzzle.MinX + 1; // 7
    private int Rows => CircuitBoardPuzzle.MaxY - CircuitBoardPuzzle.MinY + 1; // 9

    private void Start()
    {
        SetupColors();

        boardRoot.sizeDelta = new Vector2(Cols * cellSize, Rows * cellSize);

        // 그리는 순서 = 레이어 순서 (아래 -> 위). 클릭 레이어가 맨 위
        tileLayer = CreateLayer("TileLayer");
        beamLayer = CreateLayer("BeamLayer");
        pieceLayer = CreateLayer("PieceLayer");
        goalLayer = CreateLayer("GoalLayer");
        clickLayer = CreateLayer("ClickLayer");

        BuildTiles();
        BuildClickAreas();

        puzzle.OnBoardChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (puzzle != null) puzzle.OnBoardChanged -= Refresh;
    }

    private void SetupColors()
    {
        floorColor = Hex("#E8D9B8"); // 사암 바닥
        gridColor = Hex("#4A3423"); // 짙은 갈색 (칸 사이 선)
        obstacleColor = Hex("#6B5438"); // 장애물 (갈색)
        slotColor = Hex("#C9A66B"); // 빈 슬롯 (사암 벽색)
        prismSocketColor = Hex("#7846A0"); prismSocketColor.a = 0.35f;
        prismOnColor = Hex("#7846A0"); // 프리즘 보라
        beamOrange = Hex("#E07A3F"); // 주황 빛
        beamBlue = Hex("#3E7BD6"); // 밤빛 파랑
        mirrorColor = Hex("#1E3A5F"); // 초기 거울 A, B (라피스 블루)
        fragmentColor = Hex("#E8B23A"); // 거울 조각 (1)(2)(3) (금색)
    }

    // ================= 고정 요소 (한 번만 그림) =================
    private void BuildTiles()
    {
        // 보드 배경. 그림이 있으면 격자선 없이 한 장으로 깔림
        Vector2 bgSize = new Vector2(Cols * cellSize + 6, Rows * cellSize + 6);
        if (boardBackgroundSprite != null)
        {
            CreateSprite(tileLayer, "Background", Vector2.zero, bgSize, boardBackgroundSprite);
        }
        else
        {
            CreateRect(tileLayer, "Background", Vector2.zero, bgSize, gridColor);
        }

        for (int x = CircuitBoardPuzzle.MinX; x <= CircuitBoardPuzzle.MaxX; x++)
        {
            for (int y = CircuitBoardPuzzle.MinY; y <= CircuitBoardPuzzle.MaxY; y++)
            {
                BuildOneTile(new Vector2Int(x, y));
            }
        }

        // 프리즘 소켓 (빈 상태)
        DrawTileArt(tileLayer, "PrismSocket", CircuitBoardPuzzle.PrismTile,
            prismSocketSprite, prismSocketColor, 0.7f);

        // 광원 (1,4)
        DrawTileArt(tileLayer, "Source", CircuitBoardPuzzle.Source,
            sourceSprite, beamOrange, 0.5f);
    }

    private void BuildOneTile(Vector2Int tile)
    {
        Sprite sprite = floorSprite;
        Color color = floorColor;

        if (CircuitBoardPuzzle.IsObstacle(tile))
        {
            sprite = obstacleSprite;
            color = obstacleColor;
        }
        else if (CircuitBoardPuzzle.IsSlot(tile))
        {
            sprite = slotSprite;
            color = slotColor;
        }

        // 배경 그림이 있으면 바닥 칸은 생략 (배경이 이미 격자를 그리고 있음)
        bool skipFloor = boardBackgroundSprite != null && sprite == null
                      && !CircuitBoardPuzzle.IsObstacle(tile) && !CircuitBoardPuzzle.IsSlot(tile);
        if (skipFloor) return;

        string objName = $"Tile_{tile.x}_{tile.y}";
        Vector2 size = new Vector2(cellSize - 3, cellSize - 3);

        if (sprite != null) CreateSprite(tileLayer, objName, TileToPos(tile), size, sprite);
        else CreateRect(tileLayer, objName, TileToPos(tile), size, color);
    }

    // 클릭 가능한 칸: 슬롯 5개 + 프리즘 소켓 1개
    private void BuildClickAreas()
    {
        for (int x = CircuitBoardPuzzle.MinX; x <= CircuitBoardPuzzle.MaxX; x++)
        {
            for (int y = CircuitBoardPuzzle.MinY; y <= CircuitBoardPuzzle.MaxY; y++)
            {
                Vector2Int tile = new Vector2Int(x, y);
                bool clickable = CircuitBoardPuzzle.IsSlot(tile) || tile == CircuitBoardPuzzle.PrismTile;
                if (!clickable) continue;

                // 투명한 이미지 (투명해도 클릭은 받음)
                Image img = CreateRect(clickLayer, $"Click_{x}_{y}", TileToPos(tile),
                    new Vector2(cellSize, cellSize), new Color(1, 1, 1, 0));
                img.raycastTarget = true;

                var click = img.gameObject.AddComponent<BoardCellClick>();
                click.tile = tile;
                click.onClick = (t, right) => OnCellClicked?.Invoke(t, right);
            }
        }
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

        // 빔은 항상 가로 또는 세로라서 회전 없이 직사각형으로 충분
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
            Sprite sprite = PieceSprite(isFragment, type);

            if (sprite != null)
            {
                // 그림에 이미 기울기가 그려져 있으므로 회전하지 않음
                CreateSprite(pieceLayer, $"Mirror_{id}", TileToPos(tile),
                    new Vector2(cellSize * pieceScale, cellSize * pieceScale), sprite);
                continue;
            }

            // 그림이 없으면 기존 방식: 가로 막대를 45도 돌려서 표시
            Color c = isFragment ? fragmentColor : mirrorColor;
            float angle = type == MirrorType.Slash ? 45f : -45f;
            CreateRect(pieceLayer, $"Mirror_{id}", TileToPos(tile),
                new Vector2(cellSize * 1.1f, 9f), c, angle);
        }

        if (puzzle.PrismOn)
        {
            if (prismOnSprite != null)
            {
                CreateSprite(pieceLayer, "Prism", TileToPos(CircuitBoardPuzzle.PrismTile),
                    new Vector2(cellSize * pieceScale, cellSize * pieceScale), prismOnSprite);
            }
            else
            {
                CreateRect(pieceLayer, "Prism", TileToPos(CircuitBoardPuzzle.PrismTile),
                    new Vector2(cellSize * 0.55f, cellSize * 0.55f), prismOnColor, 45f);
            }
        }
    }

    // 조각 전용 그림이 없으면 거울 그림을 대신 씀
    private Sprite PieceSprite(bool isFragment, MirrorType type)
    {
        if (isFragment)
        {
            Sprite s = type == MirrorType.Slash ? fragmentSlashSprite : fragmentBackslashSprite;
            if (s != null) return s;
        }
        return type == MirrorType.Slash ? mirrorSlashSprite : mirrorBackslashSprite;
    }

    private void DrawGoals()
    {
        for (int i = 0; i < puzzle.GoalCount; i++)
        {
            puzzle.GetGoal(i, out char edge, out int x, out bool needBlue, out string flag, out string label);

            int y = edge == 'N' ? CircuitBoardPuzzle.MaxY + 1 : CircuitBoardPuzzle.MinY - 1;
            Vector2 pos = TileToPos(new Vector2Int(x, y));
            Vector2 size = new Vector2(cellSize * goalScale, cellSize * goalScale);

            bool lit = GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
            Sprite sprite = i < goalSprites.Length ? goalSprites[i] : null;

            if (sprite != null)
            {
                Image img = CreateSprite(goalLayer, $"Goal_{label}", pos, size, sprite);
                // 꺼져 있으면 어둡게, 켜지면 원래 색
                img.color = lit ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.75f);
                continue;
            }

            Color c = needBlue ? beamBlue : beamOrange;
            if (!lit) c.a = 0.3f; // 꺼져 있으면 흐리게

            Image fallback = CreateRect(goalLayer, $"Goal_{label}", pos, size, c);
            CreateLabel(fallback.rectTransform, label);
        }
    }

    // ================= 도우미 함수 =================
    // 보드 좌표 (x, y) -> 화면 위치 (보드 중앙이 0,0)
    private Vector2 TileToPos(Vector2Int tile)
    {
        float px = (tile.x - CircuitBoardPuzzle.MinX - (Cols - 1) / 2f) * cellSize;
        float py = (tile.y - CircuitBoardPuzzle.MinY - (Rows - 1) / 2f) * cellSize;
        return new Vector2(px, py);
    }

    // 그림이 있으면 그림으로, 없으면 색 네모로
    private void DrawTileArt(RectTransform layer, string objName, Vector2Int tile,
        Sprite sprite, Color color, float scale)
    {
        Vector2 size = new Vector2(cellSize * scale, cellSize * scale);
        if (sprite != null) CreateSprite(layer, objName, TileToPos(tile), size, sprite);
        else CreateRect(layer, objName, TileToPos(tile), size, color);
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

    private Image CreateSprite(RectTransform parent, string name, Vector2 pos, Vector2 size,
        Sprite sprite, float rotZ = 0f)
    {
        Image img = CreateRect(parent, name, pos, size, Color.white, rotZ);
        img.sprite = sprite;
        img.preserveAspect = true;
        return img;
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
        img.raycastTarget = false; // 기본은 클릭 안 받음 (클릭 영역만 따로 켬)
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