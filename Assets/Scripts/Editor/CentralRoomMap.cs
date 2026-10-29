using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// [임시] 중앙 홀 바닥, 벽 타일 (CentralRoomBuilder가 부름)
// 위 2줄 = 벽 (윗면 + 앞면, 가운데 2칸은 북쪽 문 자리라 비움), 왼쪽/오른쪽 1줄 = 옆 벽, 아래 1줄 = 턱 (가운데 2칸은 계단 입구)
public static class CentralRoomMap
{
    private const string Prefix = "Hall_Tile_";

    public static Tilemap Build(string tileFolder, int w, int h, string layer, int order)
    {
        var grid = new GameObject("Grid", typeof(Grid));
        Tilemap floor = Create(grid.transform, "Floor", layer, order - 10, false);
        Tilemap wall = Create(grid.transform, "Wall", layer, order - 9, true);

        var rng = new System.Random(77);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                floor.SetTile(new Vector3Int(x, y, 0), Tile(tileFolder, rng.NextDouble() < 0.8 ? "FloorA" : "FloorB"));

        FillWalls(wall, tileFolder, w, h);
        return floor;
    }

    private static void FillWalls(Tilemap wall, string folder, int w, int h)
    {
        int doorL = w / 2 - 1, doorR = w / 2;
        for (int x = 0; x < w; x++)
        {
            wall.SetTile(new Vector3Int(x, h - 1, 0), Tile(folder, "WallTop"));
            if (x != doorL && x != doorR) wall.SetTile(new Vector3Int(x, h - 2, 0), Tile(folder, "WallFront"));
            if (x != doorL && x != doorR) wall.SetTile(new Vector3Int(x, 0, 0), Tile(folder, "Edge"));
        }
        for (int y = 1; y < h - 2; y++)
        {
            wall.SetTile(new Vector3Int(0, y, 0), Tile(folder, "WallSide"));
            wall.SetTile(new Vector3Int(w - 1, y, 0), Tile(folder, "WallSide"));
        }
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

    private static TileBase Tile(string folder, string name)
    {
        string path = $"{folder}/{Prefix}{name}.asset";
        TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
        if (tile != null) return tile;

        Sprite sprite = PrologueAssetFinder.TileSprite(Prefix + name);
        if (sprite == null) return null;

        PrologueAssetFinder.EnsureFolder(folder);
        Tile created = ScriptableObject.CreateInstance<Tile>();
        created.sprite = sprite;
        AssetDatabase.CreateAsset(created, path);
        return created;
    }
}
