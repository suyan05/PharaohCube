#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// 메뉴: Tools/Bastet/★ 바스테트 전체 세팅 (한 번에)
// 아래 메뉴들을 올바른 순서로 한 번에 실행 (여러 번 눌러도 중복으로 만들지 않음)
//  1) 횃불 제단 프리팹 (없을 때만)
//  2) 아이템 카탈로그에 바스테트 아이템 추가
//  3) 5개 방 배치: 씬에 맵(Ground 타일맵)이 있으면 그 맵에 맞춰(BastetMapFitBuilder), 없으면 요청서 배치도 크기로
//  4) 실전 방 세팅 (보상, 비석 "!", 목표, 힌트, 입장 연출, 출구)
//  5) 5개 방 배치 한 번 더 (4번에서 생긴 B3·B4 제단과 비석을 각 방으로 옮김)
//  6) 플레이어에 임시 횃불
//  8) [추가] 벽 횃불로 방 밝히기 (라의 방 방식, 방 불이 자동으로 켜지지 않음)
//  7) [추가] 석상·발판 그림 붙이기 (Assets/image/Bastet/Puzzle/, 맵 배치 안에서도 한 번 실행됨)
public static class BastetOneClickSetup
{
    [MenuItem("Tools/Bastet/★ 바스테트 전체 세팅 (한 번에)", priority = 0)]
    public static void Run()
    {
        if (!EditorUtility.DisplayDialog("바스테트 전체 세팅",
                "지금 열린 씬에 바스테트 5개 방 구조를 세팅합니다.\n(예전 UI B1·B2·B5 제단은 꺼지고 걷는 퍼즐로 바뀝니다)\n\n먼저 씬을 저장(Ctrl+S)하거나 복사본을 만들어 두세요.",
                "진행", "취소"))
            return;

        Debug.Log("[BastetOneClickSetup] ===== 시작 =====");
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Common/TorchAltar.prefab") == null) TorchAltarPrefabBuilder.Build();
        BastetCatalogSetup.Run();
        // [수정] 씬에 칠해 둔 Ground 타일맵이 있으면 그 맵에 맞춰 배치, 없으면 요청서 배치도 크기로
        bool hasMap = Object.FindFirstObjectByType<UnityEngine.Tilemaps.Tilemap>() != null;
        if (hasMap) { if (!BastetMapFitBuilder.Build(true)) return; }
        else BastetAreaBuilder.Build();
        BastetRoomSetup.Apply();
        if (hasMap) BastetMapFitBuilder.Build(false); else BastetAreaBuilder.Build();
        BastetWalkBuilder.AddTorchToPlayer();
        if (hasMap) // [추가] 라의 방처럼 벽 횃불로 방 밝히기
        {
            var tl = new System.Text.StringBuilder("[BastetTorchLighting] 횃불로 방 밝히기\n");
            BastetTorchLighting.Build(tl);
            Debug.Log(tl.ToString());
        }
        if (!hasMap) // [추가] 맵 배치(MapFit)는 안에서 이미 붙임
        {
            var log = new System.Text.StringBuilder("[BastetArtApplier] 그림 붙이기\n");
            BastetArtApplier.Apply(BastetArtApplier.BigPlate, log);
            Debug.Log(log.ToString());
        }
        Debug.Log("[BastetOneClickSetup] ===== 끝 ===== 위 로그를 확인하고 Ctrl+S. 다음: Scene 뷰 분홍 사각형대로 타일 칠하기");
    }
}
#endif
