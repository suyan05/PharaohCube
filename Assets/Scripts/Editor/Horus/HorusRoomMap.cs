using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// 호루스의 방 바닥/벽 타일 (HorusRoomBuilder가 부름). 라의 방과 같은 타일(Assets/Tile/Title_*)을 씀
// 아래 글자 그림이 곧 맵 (작업 도면 Horus_Map_Plan.png와 같은 데이터). 맨 윗줄 = y 17, 맨 왼칸 = x -9
//   '#' 벽 윗면 (Title_12)        'B' 벽 앞면 벽돌 (Title_07 (2))
//   '.' 본당 바닥  'a' 입구 전실 바닥  'g' 매의 회랑 바닥  'D' 북문 자리 바닥
//   ' ' 빈칸. 매의 회랑은 여기 없음 -> 열릴 때 HorusHiddenGallery가 칠함 (처음엔 안 보이게)
// 바닥은 라의 방처럼 3종을 섞음 (Title_09 많이, Title_10 가끔, Title_11 조금)
public static class HorusRoomMap
{
    public const int Left = -9;
    public const int Top = 17;

    private const string TileDir = "Assets/Tile/";

    private static readonly string[] Rows =
    {
        "        ##########################",
        "        #BBBBBBBBBBBDDBBBBBBBBBBB#",
        "        #........................#",
        "        #........................#",
        "        #........................#",
        "        #........................#",
        "        #........................#",
        "        #........................#",
        "        #........................#",
        "        #........................#",
        "        #........................#",
        "        #........................#",
        "        #........................#",
        "        #........................#",
        "        #........................#",
        "        #........................#",
        "        #........................#",
        "        #........................#",
        "        ############aa############",
        "                #BBBaaBBB#        ",
        "                #aaaaaaaa#        ",
        "                #aaaaaaaa#        ",
        "                #aaaaaaaa#        ",
        "                #aaaaaaaa#        ",
        "                #aaaaaaaa#        ",
        "                #aaaaaaaa#        ",
        "                ##########        "
    };

    public static Tilemap Build(string layer, int order)
    {
        var grid = new GameObject("Grid", typeof(Grid));
        Tilemap ground = Create(grid.transform, "Ground", layer, order - 10, false);
        Tilemap wall = Create(grid.transform, "Wall", layer, order - 9, true);
        TileBase top = Load("Title_12_0");
        TileBase face = Load("Title_07 (2)_0");
        TileBase[] floors = FloorTiles();
        var rng = new System.Random(21);
        for (int r = 0; r < Rows.Length; r++)
        {
            for (int i = 0; i < Rows[r].Length; i++)
            {
                var cell = new Vector3Int(Left + i, Top - r, 0);
                char c = Rows[r][i];
                if (c == '#') wall.SetTile(cell, top);
                else if (c == 'B') wall.SetTile(cell, face);
                else if (c != ' ') ground.SetTile(cell, Floor(floors, rng));
            }
        }
        return ground;
    }

    private static TileBase Floor(TileBase[] floors, System.Random rng)
    {
        double v = rng.NextDouble();
        return v < 0.55 ? floors[0] : v < 0.85 ? floors[1] : floors[2];
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

    public static TileBase[] FloorTiles() => new[] { Load("Title_09_0"), Load("Title_10_0"), Load("Title_11_0") };

    public static TileBase WallTop() => Load("Title_12_0");

    public static TileBase WallFace() => Load("Title_07 (2)_0");

    public static TileBase Load(string name)
    {
        TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(TileDir + name + ".asset");
        if (tile == null) Debug.LogWarning($"[HorusRoomMap] 타일 없음: {TileDir}{name}.asset");
        return tile;
    }

    // 벽 윗면 타일 그림 (숨은 벽 오브젝트에 씀)
    public static Sprite WallTopSprite()
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TileDir + "Title_12_0.asset");
        return tile != null ? tile.sprite : null;
    }
}
