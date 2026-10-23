using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 프롤로그 그림 이름 검사
// 메뉴: PharaohCube -> Prologue -> 그림 이름 검사
// 배치 도구가 찾는 파일 이름을 하나씩 확인해서 Console에 "있음 N개 / 없음 목록"을 찍음
// 없는 그림은 비슷한 이름 후보도 같이 알려줌 -> 파일 이름을 표 이름으로 바꾸면 해결
public static class PrologueArtCheck
{
    private static readonly string[] SphinxNames =
    {
        "Pro_Obj_SphinxHead", "Pro_Obj_SphinxHead_EyeGlow", "Pro_Obj_SphinxPaw",
        "Pro_Obj_PawSand_Big", "Pro_Obj_PawSand_Small", "Pro_Obj_PawSand_Gone",
        "Pro_Obj_Stele_Buried", "Pro_Obj_Stele_Shown", "Pro_Obj_Stairs_Closed", "Pro_Obj_Stairs_Open",
        "Pro_FX_DreamMote_01", "Pro_FX_DreamMote_02",
        "Pro_Tile_Dream_FloorA", "Pro_Tile_Dream_FloorB", "Pro_Tile_Dream_FloorC",
        "Pro_Tile_Dream_EdgeTop", "Pro_Tile_Dream_EdgeFront", "Pro_Tile_Dream_EdgeL",
        "Pro_Tile_Dream_EdgeR", "Pro_Tile_Dream_EdgeB", "Pro_Tile_Dream_CornerL", "Pro_Tile_Dream_CornerR"
    };

    [MenuItem("PharaohCube/Prologue/그림 이름 검사 (스핑크스)")]
    private static void CheckSphinx()
    {
        var missing = new List<string>();
        foreach (string name in SphinxNames)
        {
            if (!PrologueAssetFinder.Exists(name)) missing.Add(name);
        }

        int found = SphinxNames.Length - missing.Count;
        if (missing.Count == 0)
        {
            Debug.Log($"[그림 이름 검사] 스핑크스 그림 {found}개 전부 있음");
            return;
        }

        Debug.LogWarning($"[그림 이름 검사] 있음 {found}개 / 없음 {missing.Count}개 -> 아래 경고에 비슷한 이름 후보가 나옴");
        foreach (string name in missing) PrologueAssetFinder.Sprite(name); // 없는 것마다 후보와 함께 경고
    }
}
