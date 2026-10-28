using System;

// 신전 하나를 끝내고 중앙 홀로 돌아왔을 때의 정보 (HallTempDirector Inspector의 Routes 칸)
// 진행 순서: 프롤로그 -> 중앙 홀 -> 라 -> 중앙 홀 -> 바스테트 -> 중앙 홀 -> 토트 -> 호루스 -> 소베크 -> 아누비스
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
    public string objective = "북쪽 문으로 바스테트의 신전에 들어가자";

    public static HallRoute[] Defaults()
    {
        return new[] { Ra(), Bastet() };
    }

    // [추가] 바스테트를 끝내고 돌아오면 -> 아누비스의 방 (튜토리얼 없이 실전 방)
    private static HallRoute Bastet()
    {
        return new HallRoute
        {
            god = "바스테트",
            clearFlag = "F_BASTET_ENDING_DONE",
            returnedFlag = "F_HALL_BASTET_RETURNED",
            faceTitle = "큐브의 두 번째 면 - 고양이",
            nextScene = "Puzzle_Test_Anubis_Tilemap",
            nextPrompt = "아누비스의 신전으로",
            objective = "북쪽 문으로 아누비스의 신전에 들어가자",
            cubeLines = new[]
            {
                new DialogueLine("???", "고양이의 면이 깨어났다."),
                new DialogueLine("???", "다음 문이 열린다. 죽은 자를 심판하는 자, 아누비스의 신전이다."),
                new DialogueLine("나", "두 번째 빛... 이제 아누비스야.")
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
