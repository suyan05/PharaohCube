using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// 토트의 방 바닥/벽 타일 (ThothRoomBuilder가 부름). 아트팀 토트 타일(thoth_tile, thoth_wall) 복사본 + 벽 윗면(코드로 그림)
// 아래 글자 그림이 곧 맵. 맨 윗줄 = y 27, 맨 왼칸 = x 0 (칸 하나 = 1 유닛)
//   '.' 바닥   'L' 벽 앞면 아랫줄   'U' 벽 앞면 윗줄   '#' 벽 윗면   ' ' 빈칸
// 방: 남쪽 복도(x 16~19, y 1~7) -> 중앙 홀(x 10~25, y 8~21, 가운데 달 시계 + 천창, 북쪽에 지혜의 문)
//     서쪽 서고(x 1~8, y 10~19, 벽화 A) / 동쪽 서고(x 27~34, y 10~19, 벽화 B + 책상 + 숨은 홈)
//     북쪽 벽감(x 11~14, y 22~24, 벽화 C - 달 시계를 맞추기 전엔 돌판으로 막힘)
// 그림: Assets/Resources/Thoth/World/TH_*.png -> 타일 에셋: Assets/Tile/Thoth/TH_*.asset (없으면 자동 생성)
public static class ThothRoomMap
{
    private const string ImageDir = "Assets/Resources/Thoth/World/";
    private const string TileDir = "Assets/Tile/Thoth";
    public const int Top = 27;

    private static readonly string[] Rows =
    {
        "           ####                     ",
        "           UUUU                     ",
        "          #LLLL#                    ",
        "          #....###########          ",
        "          U....UUUUUUUUUUU          ",
        " #########L....LLLLLLLLLLL######### ",
        " UUUUUUUU#................#UUUUUUUU ",
        "#LLLLLLLL#................#LLLLLLLL#",
        "#........#................#........#",
        "#........#................#........#",
        "#........#................#........#",
        "#........U................U........#",
        "#........L................L........#",
        "#..................................#",
        "#..................................#",
        "#........#................#........#",
        "#........#................#........#",
        "#........#................#........#",
        "##########................##########",
        "         #................#         ",
        "         #######....#######         ",
        "               #....#               ",
        "               #....#               ",
        "               #....#               ",
        "               #....#               ",
        "               #....#               ",
        "               #....#               ",
        "               ######               ",
    };

    public static Tilemap Build(string layer, int order, out Tilemap wall)
    {
        var grid = new GameObject("Grid", typeof(Grid));
        Tilemap ground = Create(grid.transform, "Ground", layer, order - 10, false);
        wall = Create(grid.transform, "Wall", layer, order - 9, true);
        var rng = new System.Random(36);
        for (int r = 0; r < Rows.Length; r++)
        {
            for (int x = 0; x < Rows[r].Length; x++)
            {
                var cell = new Vector3Int(x, Top - r, 0);
                char c = Rows[r][x];
                if (c == '.') ground.SetTile(cell, Tile(rng.NextDouble() < 0.78 ? "TH_Floor_A" : "TH_Floor_B"));
                else if (c == 'L') wall.SetTile(cell, Tile(x % 3 == 1 ? "TH_Wall_Face_B" : "TH_Wall_Face_A"));
                else if (c == 'U') wall.SetTile(cell, Tile(x % 3 == 2 ? "TH_Wall_Face_A" : "TH_Wall_Face_B"));
                else if (c == '#') wall.SetTile(cell, Tile("TH_Wall_Top"));
            }
        }
        return ground;
    }

    public static TileBase Tile(string name)
    {
        string tilePath = $"{TileDir}/{name}.asset";
        TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(tilePath);
        if (tile != null) return tile;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ImageDir + name + ".png");
        if (sprite == null)
        {
            Debug.LogWarning($"[ThothRoomMap] 타일 그림 없음: {ImageDir}{name}.png");
            return null;
        }
        PrologueAssetFinder.EnsureFolder(TileDir);
        Tile created = ScriptableObject.CreateInstance<Tile>();
        created.sprite = sprite;
        AssetDatabase.CreateAsset(created, tilePath);
        return created;
    }

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
