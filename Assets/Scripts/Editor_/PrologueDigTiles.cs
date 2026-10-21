using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

// 메뉴 한 번으로 발굴 현장 바닥 + 언덕 타일을 28 x 14칸에 정확히 깔아주는 도구
// 메뉴: PharaohCube -> Prologue -> 발굴 현장 바닥 깔기   (Prologue_Dig 씬을 열고 실행)
//
// 만드는 것: DigGrid (월드 0, 0) 아래
//   Floor: 바닥 A 70% / B 20% / C 10% 섞어서 전체 (항상 같은 무늬로 나오게 고정 시드)
//   Dune : 맵 가장자리 언덕 + 위쪽 두 군데 1칸 튀어나옴 (기획서 1.2) + TilemapCollider2D
// 타일 에셋: Assets/Tile/Prologue/Pro_Tile_Desert_*.asset (팔레트에서 이미 만들었으면 그거 씀, 없으면 새로 만듦)
// Ctrl+Z로 한 번에 되돌릴 수 있음
public static class PrologueDigTiles
{
    private const string SpritePath = "Assets/image/Prologue/World/Pro_Tile_Desert_";
    private const string TileFolder = "Assets/Tile/Prologue";
    private const string GridName = "DigGrid";
    private const int MapW = 28;
    private const int MapH = 14;

    private static readonly Dictionary<string, TileBase> cache = new Dictionary<string, TileBase>();
    private static string sortingLayer = "Default";
    private static int baseOrder;

    [MenuItem("PharaohCube/Prologue/발굴 현장 바닥 깔기")]
    private static void Build()
    {
        try
        {
            if (GameObject.Find(GridName) != null)
            {
                EditorUtility.DisplayDialog("발굴 현장", $"이미 '{GridName}'가 있어요. 다시 깔려면 먼저 지워주세요.", "확인");
                return;
            }

            cache.Clear();
            ReadPlayerSorting();
            var grid = new GameObject(GridName, typeof(Grid));
            Undo.RegisterCreatedObjectUndo(grid, "발굴 현장 바닥");

            FillFloor(CreateTilemap(grid.transform, "Floor", baseOrder - 10, false));
            FillDune(CreateTilemap(grid.transform, "Dune", baseOrder - 9, true));

            EditorSceneManager.MarkSceneDirty(grid.scene);
            Debug.Log("[PrologueDigTiles] 바닥 28 x 14 완료 -> Ctrl+S로 씬 저장");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PrologueDigTiles] 바닥 깔기 오류: {e}");
        }
    }

    // ================= 타일맵 =================
    private static Tilemap CreateTilemap(Transform parent, string name, int order, bool solid)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false);

        TilemapRenderer renderer = go.GetComponent<TilemapRenderer>();
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = order;

        if (solid) go.AddComponent<TilemapCollider2D>();
        return go.GetComponent<Tilemap>();
    }

    private static void FillFloor(Tilemap map)
    {
        TileBase a = GetTile("FloorA");
        TileBase b = GetTile("FloorB");
        TileBase c = GetTile("FloorC");
        var rng = new System.Random(2814);

        for (int x = 0; x < MapW; x++)
        {
            for (int y = 0; y < MapH; y++)
            {
                double r = rng.NextDouble();
                map.SetTile(new Vector3Int(x, y, 0), r < 0.7 ? a : (r < 0.9 ? b : c));
            }
        }
    }

    // 위: 윗면(맨 위 줄) + 앞면(그 아래 줄), 왼쪽·오른쪽: 옆 가장자리, 아래: 낮은 턱
    private static void FillDune(Tilemap map)
    {
        for (int x = 0; x < MapW; x++)
        {
            Set(map, x, MapH - 1, "DuneTop");
            Set(map, x, MapH - 2, x == 0 ? "DuneCornerL" : (x == MapW - 1 ? "DuneCornerR" : "DuneFront"));
            Set(map, x, 0, x == 0 || x == MapW - 1 ? "DuneTop" : "DuneEdgeB");
        }

        for (int y = 1; y < MapH - 2; y++)
        {
            Set(map, 0, y, "DuneEdgeL");
            Set(map, MapW - 1, y, "DuneEdgeR");
        }

        Bump(map, 9, 11);  // 책상 위쪽
        Bump(map, 19, 20); // 발굴 구역 위쪽
    }

    // 위쪽 언덕이 한 칸 더 튀어나온 곳 (단조롭지 않게)
    private static void Bump(Tilemap map, int fromX, int toX)
    {
        for (int x = fromX; x <= toX; x++)
        {
            Set(map, x, MapH - 2, "DuneTop");
            Set(map, x, MapH - 3, "DuneFront");
        }
        Set(map, fromX - 1, MapH - 2, "DuneInnerL");
        Set(map, toX + 1, MapH - 2, "DuneInnerR");
    }

    private static void Set(Tilemap map, int x, int y, string tileName)
    {
        map.SetTile(new Vector3Int(x, y, 0), GetTile(tileName));
    }

    // ================= 타일 에셋 =================
    private static TileBase GetTile(string tileName)
    {
        if (cache.TryGetValue(tileName, out TileBase cached)) return cached;

        string assetPath = $"{TileFolder}/Pro_Tile_Desert_{tileName}.asset";
        TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(assetPath);
        if (tile == null) tile = CreateTileAsset(tileName, assetPath);

        cache[tileName] = tile;
        return tile;
    }

    private static TileBase CreateTileAsset(string tileName, string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + tileName + ".png");
        if (sprite == null) throw new InvalidOperationException($"그림 없음: {SpritePath}{tileName}.png");

        EnsureFolder();
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        AssetDatabase.CreateAsset(tile, assetPath);
        return tile;
    }

    private static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(TileFolder)) return;
        if (!AssetDatabase.IsValidFolder("Assets/Tile")) AssetDatabase.CreateFolder("Assets", "Tile");
        AssetDatabase.CreateFolder("Assets/Tile", "Prologue");
    }

    // ================= 정렬 =================
    // 바닥이 플레이어보다 항상 뒤에 그려지도록 플레이어 기준으로 맞춤
    private static void ReadPlayerSorting()
    {
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        SpriteRenderer sr = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        if (sr == null)
        {
            Debug.LogWarning("[PrologueDigTiles] 플레이어 그림을 못 찾음 -> 정렬 Default / 0 기준");
            return;
        }
        sortingLayer = sr.sortingLayerName;
        baseOrder = sr.sortingOrder;
    }
}