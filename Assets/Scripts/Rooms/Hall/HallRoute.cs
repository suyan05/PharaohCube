using System;
using System.Collections.Generic;
using UnityEngine;

// 신전 하나를 끝내고 중앙 홀로 돌아왔을 때의 정보 (HallTempDirector Inspector의 Routes 칸)
// 진행 순서: 프롤로그 -> 중앙 홀 -> 라 -> 중앙 홀 -> 바스테트 -> 중앙 홀 -> 토트 -> 호루스 -> 소베크 -> 아누비스
// [추가] 토트의 방(Puzzle_Thoth_Tilemap)이 빌드 목록에 있으면 바스테트 -> 토트 -> 호루스 순서로 이어짐
// - clearFlag: 그 신전을 끝냈다는 플래그 (라 = 태양 형판을 받음)
// - returnedFlag: 돌아온 연출을 이미 봤다는 플래그 (한 번만 보여줌)
// - nextScene / nextPrompt / objective: 이제 북쪽 문이 어디로 가는지
// [확인 필요] 바스테트 이후 신전의 끝 플래그는 각 담당자에게 받아서 칸을 추가해야 함
[Serializable]
public class HallRoute
{
    public string god = "라";
    public string clearFlag = "F_RA_PLATE_GIVEN";
    public string returnedFlag = "F_HALL_RA_RETURNED";
    public string faceTitle = "큐브의 첫 번째 면 - 태양";
    public DialogueLine[] cubeLines;
    public string nextScene = "Puzzle_Test_Bastet_Tliemap"; // [수정] 시연: 튜토리얼 방 없이 바로 실전 방
    public string nextPrompt = "바스테트의 신전으로";
    public string objective = "바스테트의 신전에 들어가자";
    // [추가] 돌아왔을 때 큐브에 형판을 끼울 칸 (0 라, 1 바스테트, 2 토트, 3 호루스, 4 소베크, 5 아누비스, -1 = 끼우는 연출 없음)
    public int cubeSlot = 0;
    // [추가] 다음 신전으로 가는 북쪽 문에 횃불이 필요한지 (신전에서 돌아온 뒤에는 필요 없음)
    public bool needTorch = false;

    public static HallRoute[] Defaults()
    {
        return new[] { Ra(), Bastet(), Thoth(), Horus(), Sobek(), Anubis() }; // [수정] 소베크 추가 / [추가] 토트 (바스테트 -> 토트 -> 호루스) / [추가] 아누비스 (마지막 면)
    }

    // [추가] 씬에 저장된 Routes 칸(예전 기본값)을 지금 순서에 맞춤 (HallTempDirector.Awake에서 부름, 씬 파일은 안 바뀜)
    // - 없는 신전(소베크)은 뒤에 붙임 -> 소베크를 끝내고 돌아와도 홀 첫 튜토리얼이 아니라 "돌아옴" 연출
    // - 호루스 다음 문이 예전 기본값(아누비스)이고 소베크 씬이 빌드 목록에 있으면 -> 소베크로
    public static HallRoute[] Sync(HallRoute[] current)
    {
        var list = new List<HallRoute>();
        if (current != null) foreach (HallRoute r in current) if (r != null) list.Add(r);
        bool sobekReady = Application.CanStreamedLevelBeLoaded(SobekScene);
        bool thothReady = Application.CanStreamedLevelBeLoaded(ThothIds.Scene);
        if (thothReady) RouteBastetToThoth(list);
        foreach (HallRoute r in list)
        {
            if (r.god == "호루스" && !sobekReady && r.nextScene == SobekScene) { r.nextScene = AnubisScene; r.nextPrompt = "아누비스의 신전으로"; r.objective = "아누비스의 신전에 들어가자"; continue; }
            if (r.god != "호루스" || !sobekReady || r.nextScene != AnubisScene) continue;
            HallRoute h = Horus();
            r.nextScene = h.nextScene; r.nextPrompt = h.nextPrompt; r.objective = h.objective; r.cubeLines = h.cubeLines;
            Debug.Log("[HallRoute] 호루스 다음 문 -> 소베크의 신전");
        }
        foreach (HallRoute d in Defaults())
        {
            if (d.god == "소베크" && !sobekReady) continue;
            if (d.god == "토트" && !thothReady) continue;
            if (d.god == "아누비스" && !Application.CanStreamedLevelBeLoaded(AnubisScene)) continue;
            if (list.Exists(r => r.god == d.god || r.clearFlag == d.clearFlag)) continue;
            InsertInOrder(list, d); // [수정] 토트는 바스테트 바로 뒤에 (LatestCleared가 순서를 봄)
            Debug.Log($"[HallRoute] 경로 추가: {d.god} (끝 플래그 {d.clearFlag})");
        }
        return list.ToArray();
    }

    private const string SobekScene = "Sobek_Hub";
    private const string HorusScene = "Puzzle_Horus_Tilemap";

    private static void InsertInOrder(List<HallRoute> list, HallRoute d)
    {
        int bastet = list.FindIndex(r => r.god == "바스테트");
        if (d.god == "토트" && bastet >= 0) list.Insert(bastet + 1, d);
        else list.Add(d);
    }

    // [추가] 씬에 저장된 바스테트 경로가 아직 호루스로 가면 -> 토트의 방으로 (토트 씬이 빌드 목록에 있을 때만)
    private static void RouteBastetToThoth(List<HallRoute> list)
    {
        foreach (HallRoute r in list)
        {
            if (r.god != "바스테트" || r.nextScene != HorusScene) continue;
            HallRoute b = Bastet();
            r.nextScene = b.nextScene; r.nextPrompt = b.nextPrompt; r.objective = b.objective; r.cubeLines = b.cubeLines;
            Debug.Log("[HallRoute] 바스테트 다음 문 -> 토트의 방");
        }
    }

    // [추가] 토트를 끝내고 돌아오면 (지혜의 형판, F_TH_DONE) -> 호루스의 방
    private static HallRoute Thoth()
    {
        return new HallRoute
        {
            god = "토트",
            cubeSlot = 2,
            clearFlag = ThothIds.Done,
            returnedFlag = "F_HALL_THOTH_RETURNED",
            faceTitle = "큐브의 세 번째 면 - 달",
            nextScene = HorusScene,
            nextPrompt = "호루스의 신전으로",
            objective = "호루스의 신전에 들어가자",
            cubeLines = new[]
            {
                new DialogueLine("???", "달의 면이 깨어났다."),
                new DialogueLine("???", "다음 문이 열린다. 하늘을 나는 매, 호루스의 신전이다."),
                new DialogueLine("나", "아펩이 하늘의 눈을 다시 부쉈다고 했지... 토트의 벽화에서 본 순서를 잊지 말자.")
            }
        };
    }
    private const string AnubisScene = "Puzzle_Test_Anubis_Tilemap";

    // [수정] 바스테트를 끝내고 돌아오면 -> 토트의 방 (토트 씬이 빌드 목록에 없으면 예전처럼 호루스의 방)
    private static HallRoute Bastet()
    {
        if (Application.CanStreamedLevelBeLoaded(ThothIds.Scene)) return BastetToThoth();
        return new HallRoute
        {
            god = "바스테트",
            cubeSlot = 1,
            clearFlag = "F_BASTET_MAIN_DONE", // [수정] 메인 퍼즐을 풀었으면 엔딩을 건너뛰었어도 '바스테트에서 돌아옴'으로 봄
            returnedFlag = "F_HALL_BASTET_RETURNED",
            faceTitle = "큐브의 두 번째 면 - 고양이",
            nextScene = "Puzzle_Horus_Tilemap", // 메뉴 PharaohCube -> Horus -> 호루스 타일맵 씬 만들기 로 생기는 씬
            nextPrompt = "호루스의 신전으로",
            objective = "호루스의 신전에 들어가자",
            cubeLines = new[]
            {
                new DialogueLine("???", "고양이의 면이 깨어났다."),
                new DialogueLine("???", "다음 문이 열린다. 하늘을 나는 매, 호루스의 신전이다."),
                new DialogueLine("나", "두 번째 빛... 다음은 호루스.")
            }
        };
    }

    private static HallRoute BastetToThoth()
    {
        return new HallRoute
        {
            god = "바스테트",
            cubeSlot = 1,
            clearFlag = "F_BASTET_MAIN_DONE",
            returnedFlag = "F_HALL_BASTET_RETURNED",
            faceTitle = "큐브의 두 번째 면 - 고양이",
            nextScene = ThothIds.Scene,
            nextPrompt = "토트의 신전으로",
            objective = "토트의 신전에 들어가자",
            cubeLines = new[]
            {
                new DialogueLine("???", "고양이의 면이 깨어났다."),
                new DialogueLine("???", "다음 문이 열린다. 글자와 달을 다스리는 서기관, 토트의 서고다."),
                new DialogueLine("나", "두 번째 빛... 다음은 토트.")
            }
        };
    }

    // [추가] 호루스를 끝내고 돌아오면 -> [수정] 소베크의 방 (소베크 씬이 빌드 목록에 없으면 Sync가 아누비스로 둠)
    // [확인 필요] faceTitle 몇 번째 면인지: 기획 순서(라-바스테트-토트-호루스)대로 네 번째로 적음
    private static HallRoute Horus()
    {
        return new HallRoute
        {
            god = "호루스",
            cubeSlot = 3,
            clearFlag = "F_HO_H5_DONE",
            returnedFlag = "F_HALL_HORUS_RETURNED",
            faceTitle = "큐브의 네 번째 면 - 하늘",
            nextScene = SobekScene,
            nextPrompt = "소베크의 신전으로",
            objective = "소베크의 신전에 들어가자",
            cubeLines = new[]
            {
                new DialogueLine("???", "하늘의 면이 깨어났다."),
                new DialogueLine("???", "다음 문이 열린다. 나일의 주인, 악어의 신 소베크의 신전이다."),
                new DialogueLine("나", "매의 눈까지 찾았어... 다음은 소베크야.")
            }
        };
    }

    // [추가] 소베크를 끝내고 돌아오면 (메인 퍼즐 클리어) -> 아누비스의 방
    // [수정] 중앙 홀 타일맵에는 아누비스 문이 없음 -> 소베크의 문이 물이 빠진 길(아누비스)로 이어짐 (HallDoorBoard)
    // [확인 필요] 소베크 허브에서 물이 빠지며 바로 아누비스로 가는 길이 생기면, 그쪽 담당자와 이 경로를 맞추기
    private static HallRoute Sobek()
    {
        return new HallRoute
        {
            god = "소베크",
            cubeSlot = 4,
            clearFlag = "F_SOBEK_MAIN_DONE",
            returnedFlag = "F_HALL_SOBEK_RETURNED",
            faceTitle = "큐브의 다섯 번째 면 - 악어",
            nextScene = AnubisScene,
            nextPrompt = "물이 빠진 길로 아누비스에게",
            objective = "소베크의 문 아래, 물이 빠진 길로 아누비스의 신전에 가자",
            cubeLines = new[]
            {
                new DialogueLine("???", "물의 면이 깨어났다."),
                new DialogueLine("???", "마지막 문이 열린다. 죽은 자를 심판하는 자, 아누비스의 신전이다."),
                new DialogueLine("나", "강이 다시 흐르고 배가 떴어... 이제 심장을 다는 저울로 가자.")
            }
        };
    }

    // [추가] 아누비스의 심판을 통과하고 돌아오면 -> 마지막 면. 더 갈 문은 없음 (모든 문이 잠김)
    // [확인 필요] 최종 엔딩 연출은 아직 없음 (기획 확정 후 추가)
    private static HallRoute Anubis()
    {
        return new HallRoute
        {
            god = "아누비스",
            cubeSlot = 5,
            clearFlag = "F_ANUBIS_MAIN_DONE",
            returnedFlag = "F_HALL_ANUBIS_RETURNED",
            faceTitle = "큐브의 여섯 번째 면 - 심판",
            nextScene = "",
            nextPrompt = "",
            objective = "큐브의 여섯 면이 모두 깨어났다",
            cubeLines = new[]
            {
                new DialogueLine("???", "심판의 면이 깨어났다. 여섯 면이 모두 빛난다."),
                new DialogueLine("나", "깃털보다 가벼운 심장... 이제 새벽이 올 거야.")
            }
        };
    }

    private static HallRoute Ra()
    {
        return new HallRoute
        {
            cubeLines = new[]
            {
                new DialogueLine("???", "태양의 면이 깨어났다."),
                new DialogueLine("???", "다음 문이 열린다. 고양이 여신, 바스테트의 신전이다."),
                new DialogueLine("나", "큐브에 빛이 하나 들어왔어... 다음은 바스테트.")
            }
        };
    }
}
