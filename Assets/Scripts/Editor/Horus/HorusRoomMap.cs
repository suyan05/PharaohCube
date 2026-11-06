using UnityEngine;
using UnityEngine.Tilemaps;

// 호루스의 방 바닥/벽 타일 (HorusRoomBuilder가 부름). 호루스 전용 밤하늘 신전 타일(HorusTileArt)을 씀
// 아래 글자 그림이 곧 맵 (작업 도면 Horus_Map_Plan.png와 같은 데이터). 맨 윗줄 = y 17, 맨 왼칸 = x -9
//   '#' 벽 윗면 (현무암 + 금 띠)       'B' 벽 앞면 (청금석 벽돌. 6칸마다 우자트 / 매 문양)
//   '.' 본당 바닥 (청금석, 가끔 금 별가루 / 금 간 돌)   'S' 행렬길 + 제단 고리 (금 별 상감)
//   'a' 입구 전실 바닥 (모래빛 돌)   'D' 북문 자리 바닥   ' ' 빈칸
//   매의 회랑은 여기 없음 -> 열릴 때 HorusHiddenGallery가 칠함 (처음엔 안 보이게)
// [추가] 퍼즐 방 2개 (본당이 물건으로 막혀서 큰 퍼즐은 각자 방으로 뺌 - 라의 방처럼)
//   해의 방 (H1 매의 거울): 본당 북서쪽 위. 바닥 x 0~13, y 18~31. 본당 북쪽 벽 (1~2, 16~17)이 문
//   나일의 방 (H4 돌배 경주): 본당 동쪽. 바닥 x 25~40, y 0~12. 본당 동쪽 벽 (24, 2~3)이 문
//   [수정] 두 방 모두 퍼즐 칸을 1.5칸 간격으로 넓혀서 방도 키움 (물건 사이로 지나가기 쉽게)
public static class HorusRoomMap
{
    public static readonly RectInt SunRoom = new RectInt(0, 18, 14, 14);  // x 0~13, y 18~31
    public static readonly RectInt NileRoom = new RectInt(25, 0, 16, 13); // x 25~40, y 0~12
    private static readonly Vector2Int[] Doors =
    {
        new Vector2Int(1, 16), new Vector2Int(2, 16), new Vector2Int(1, 17), new Vector2Int(2, 17),
        new Vector2Int(24, 2), new Vector2Int(24, 3)
    };

    public const int Left = -9;
    public const int Top = 17;

    private static readonly string[] Rows =
    {
        "        ##########################",
        "        #BBBBBBBBBBBDDBBBBBBBBBBB#",
        "        #........................#",
        "        #..........SSSS..........#",
        "        #.........S....S.........#",
        "        #.........S....S.........#",
        "        #.........S....S.........#",
        "        #..........SSSS..........#",
        "        #...........SS...........#",
        "        #...........SS...........#",
        "        #...........SS...........#",
        "        #...........SS...........#",
        "        #...........SS...........#",
        "        #...........SS...........#",
        "        #...........SS...........#",
        "        #...........SS...........#",
        "        #...........SS...........#",
        "        #...........SS...........#",
        "        ############SS############",
        "                #BBBaaBBB#        ",
        "                #aaaSSaaa#        ",
        "                #aaaSSaaa#        ",
        "                #aaaSSaaa#        ",
        "                #aaaSSaaa#        ",
        "                #aaaSSaaa#        ",
        "                #aaaSSaaa#        ",
        "                ##########        "
    };

    public static Tilemap Build(string layer, int order)
    {
        var grid = new GameObject("Grid", typeof(Grid));
        Tilemap ground = Create(grid.transform, "Ground", layer, order - 10, false);
        Tilemap wall = Create(grid.transform, "Wall", layer, order - 9, true);
        var rng = new System.Random(21);
        for (int r = 0; r < Rows.Length; r++)
        {
            for (int i = 0; i < Rows[r].Length; i++)
            {
                var cell = new Vector3Int(Left + i, Top - r, 0);
                char c = Rows[r][i];
                if (c == '#') wall.SetTile(cell, WallTop());
                else if (c == 'B') wall.SetTile(cell, WallFaceAt(cell.x));
                else if (c != ' ') ground.SetTile(cell, FloorFor(c, rng));
            }
        }
        PaintRoom(ground, wall, SunRoom, rng);
        PaintRoom(ground, wall, NileRoom, rng);
        foreach (Vector2Int d in Doors) OpenDoor(ground, wall, d, rng);
        return ground;
    }

    // 방 하나: 바닥 + 둘레 벽 (북쪽은 벽 앞면 + 윗면 두 줄, 나머지는 윗면 한 줄)
    private static void PaintRoom(Tilemap ground, Tilemap wall, RectInt room, System.Random rng)
    {
        for (int x = room.xMin - 1; x <= room.xMax; x++)
        {
            for (int y = room.yMin - 1; y <= room.yMax + 1; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                bool inside = x >= room.xMin && x < room.xMax && y >= room.yMin && y < room.yMax;
                if (inside) ground.SetTile(cell, HallFloor(rng));
                else if (y == room.yMax && x >= room.xMin && x < room.xMax) wall.SetTile(cell, WallFaceAt(x));
                else if (wall.GetTile(cell) == null && ground.GetTile(cell) == null) wall.SetTile(cell, WallTop());
            }
        }
    }

    // 벽에 문 구멍 (벽 타일을 지우고 바닥을 깖)
    private static void OpenDoor(Tilemap ground, Tilemap wall, Vector2Int d, System.Random rng)
    {
        var cell = new Vector3Int(d.x, d.y, 0);
        wall.SetTile(cell, null);
        ground.SetTile(cell, HorusTileArt.Get(HorusTileArt.FloorStar));
    }

    private static TileBase FloorFor(char c, System.Random rng)
    {
        if (c == 'S') return HorusTileArt.Get(HorusTileArt.FloorStar);
        if (c == 'a') return HorusTileArt.Get(HorusTileArt.FloorAnte);
        return HallFloor(rng);
    }

    // 본당 바닥: 청금석 70%, 금 별가루 18%, 금 간 돌 12%
    public static TileBase HallFloor(System.Random rng)
    {
        double v = rng.NextDouble();
        string name = v < 0.7 ? HorusTileArt.FloorA : v < 0.88 ? HorusTileArt.FloorB : HorusTileArt.FloorC;
        return HorusTileArt.Get(name);
    }

    // 벽 앞면: 6칸마다 우자트(눈) 문양, 그 사이에 매 문양
    public static TileBase WallFaceAt(int x)
    {
        int m = ((x % 6) + 6) % 6;
        if (m == 2) return HorusTileArt.Get(HorusTileArt.WallFaceEye);
        if (m == 5) return HorusTileArt.Get(HorusTileArt.WallFaceFalcon);
        return HorusTileArt.Get(HorusTileArt.WallFace);
    }

    public static TileBase[] FloorTiles() => new[]
    {
        HorusTileArt.Get(HorusTileArt.FloorA), HorusTileArt.Get(HorusTileArt.FloorA),
        HorusTileArt.Get(HorusTileArt.FloorB), HorusTileArt.Get(HorusTileArt.FloorC)
    };

    public static TileBase WallTop() => HorusTileArt.Get(HorusTileArt.WallTop);

    public static TileBase WallFace() => HorusTileArt.Get(HorusTileArt.WallFace);

    private static Tilemap Create(Transform parent, string name, string layer, int order, bool solid)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false);
        TilemapRenderer r = go.GetComponent<TilemapRenderer>();
        r.sortingLayerName = layer;
        r.sortingOrder = order;
        if (solid) go.AddComponent<TilemapCollider2D>();
        return go.GetComponent<Tilemap>();
    }
}
