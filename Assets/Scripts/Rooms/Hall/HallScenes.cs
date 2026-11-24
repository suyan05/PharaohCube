using System;
using UnityEngine;

// [추가] 중앙 홀 씬 이름을 한 곳에서 관리
// - 진짜 중앙 홀: CentralRoom_Tilemap (메뉴 PharaohCube -> Hall -> 중앙 홀(타일맵) 연결하기 로 세팅)
// - 예전 임시 홀: CentralRoom_Temp -> 씬 파일에 이 이름이 저장돼 있어도 Resolve가 진짜 홀로 바꿔 줌
// - 씬을 부르는 공용 함수(SceneFlow, MenuFader, AnubisSceneFader)가 Resolve를 거침
public static class HallScenes
{
    public const string Main = "CentralRoom_Tilemap";
    public const string Legacy = "CentralRoom_Temp";

    // 중앙 홀 씬인지 (진짜, 임시 둘 다)
    public static bool IsHall(string sceneName)
    {
        return sceneName == Main || sceneName == Legacy;
    }

    // 예전 임시 홀 이름이면 진짜 홀로 바꿈 (진짜 홀이 빌드 목록에 있을 때만)
    public static string Resolve(string sceneName)
    {
        try
        {
            if (sceneName != Legacy) return sceneName;
            if (!Application.CanStreamedLevelBeLoaded(Main)) return sceneName;
            Debug.Log($"[HallScenes] '{Legacy}' -> '{Main}' (진짜 중앙 홀로 이동)");
            return Main;
        }
        catch (Exception e)
        {
            Debug.LogError($"[HallScenes] 씬 이름 확인 오류: {e}");
            return sceneName;
        }
    }
}
