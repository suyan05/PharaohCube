using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 하나의 Tilemap에 같이 그려진 타일 중, 지정한 타일(또는 그 타일의 그림)만 다른 Tilemap으로 옮긴다.
/// 사용법: 아무 오브젝트에 붙이고 필드 채운 뒤,
/// 컴포넌트 우측 점 3개 메뉴 -> "Split Tiles" 클릭.
/// 에디터 전용 도구라서 게임 실행 중에는 아무 일도 하지 않는다.
/// </summary>
public class TilemapSplitter : MonoBehaviour
{
    [Header("원본 (지금 다 그려져 있는 Tilemap)")]
    [SerializeField] private Tilemap source;

    [Header("옮겨 받을 Tilemap (예: Walls)")]
    [SerializeField] private Tilemap target;

    [Header("방법 1: 옮길 그림(스프라이트) - Project 창 PNG를 그대로 드래그")]
    [SerializeField] private List<Sprite> spritesToMove = new List<Sprite>();

    [Header("방법 2: 옮길 Tile 에셋 - 팔레트 만들 때 생긴 .asset 파일")]
    [SerializeField] private List<TileBase> tilesToMove = new List<TileBase>();

#if UNITY_EDITOR
    [ContextMenu("Split Tiles")]
    private void SplitTiles()
    {
        try
        {
            if (!IsValidSetup()) return;

            Undo.RegisterCompleteObjectUndo(new Object[] { source, target }, "Split Tiles");
            int movedCount = MoveMatchingTiles();

            source.CompressBounds();
            target.CompressBounds();
            EditorUtility.SetDirty(source);
            EditorUtility.SetDirty(target);

            Debug.Log($"[TilemapSplitter] 타일 {movedCount}개를 '{target.name}'(으)로 옮겼습니다.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TilemapSplitter] 실패: {e.Message}");
        }
    }

    private bool IsValidSetup()
    {
        if (source == null || target == null)
        {
            Debug.LogWarning("[TilemapSplitter] source 또는 target이 비어 있습니다.");
            return false;
        }
        if (source == target)
        {
            Debug.LogWarning("[TilemapSplitter] source와 target이 같은 Tilemap입니다.");
            return false;
        }
        if (spritesToMove.Count == 0 && tilesToMove.Count == 0)
        {
            Debug.LogWarning("[TilemapSplitter] 옮길 그림이나 타일이 하나도 등록되지 않았습니다.");
            return false;
        }
        return true;
    }

    private int MoveMatchingTiles()
    {
        var spriteSet = new HashSet<Sprite>(spritesToMove);
        var tileSet = new HashSet<TileBase>(tilesToMove);
        spriteSet.Remove(null);
        tileSet.Remove(null);
        int count = 0;

        foreach (Vector3Int pos in source.cellBounds.allPositionsWithin)
        {
            TileBase tile = source.GetTile(pos);
            if (tile == null) continue;
            if (!ShouldMove(tile, source.GetSprite(pos), tileSet, spriteSet)) continue;

            target.SetTile(pos, tile);
            target.SetTransformMatrix(pos, source.GetTransformMatrix(pos));
            source.SetTile(pos, null);
            count++;
        }
        return count;
    }

    private static bool ShouldMove(TileBase tile, Sprite sprite,
        HashSet<TileBase> tileSet, HashSet<Sprite> spriteSet)
    {
        if (tileSet.Contains(tile)) return true;
        return sprite != null && spriteSet.Contains(sprite);
    }
#endif
}