using UnityEngine;

// [추가] 바스테트 숨은 방 "향실" 내용 (대사, 옆모습 통로 구성, 마지막 벽화 컷). 문구는 여기서만 고치면 됨
// - 비석을 모두 해독하면 목걸이의 방 벽에 틈이 드러남 -> 좁은 통로를 걸어감 -> 향과 기름 그릇을 지키는 바스테트 벽화
// - 끝에서 히든 유물 "바스테트의 그릇"(ITEM_BASTET_VESSEL)을 얻음 -> 횃불이 두 배 밝고 두 배 오래 탐
// - 벽화 그림: Resources/Horus/Mural/MU_BastetV1_* (Tools/BastetVault/bastet_vault_art.py 가 만듦)
// 흐름 공통 코드는 사랑님의 HiddenVault / SideWalkStage / MuralStage를 그대로 씀 (그 파일들은 안 고침)
public static class BastetVaultScenes
{
    private const string Me = "나";
    private const string Wall = "벽의 글귀";
    private const string Mural = "벽화";
    private const string Art = "BastetV1";

    public static SideWalkScene Walk()
    {
        return new SideWalkScene("향실", "바스테트의 신전 깊은 곳", 720f)
            .Tint(new Color(1f, 0.88f, 0.74f, 1f), new Color(1f, 0.74f, 0.42f, 1f))
            .Start(new DialogueLine(Me, "벽 틈 너머로 향 냄새가 짙어진다."),
                   new DialogueLine(Me, "비석에 적혀 있던 '향로의 재'... 이 길을 말한 거였어."))
            .Spot(220f, 60f, null, "E: 벽의 글귀 읽기",
                  new DialogueLine(Wall, "여신은 향과 기름을 지킨다. 그 그릇에 담긴 불은 밤을 넘어서도 꺼지지 않는다."),
                  new DialogueLine(Me, "불을 오래 타게 하는 그릇... 횃불에 쓸 수 있을지도 몰라."))
            .Spot(470f, 96f, "SW_Mark", "E: 바닥의 재 살펴보기",
                  new DialogueLine(Me, "바닥에 재가 쌓여 있어. 아직 따뜻해."),
                  new DialogueLine(Me, "누군가 오랫동안 여기서 불을 지켜 왔던 거야."))
            .Exit("E: 향실 안으로 들어가기");
    }

    public static MuralShot[] Reveal()
    {
        MuralShot s = new MuralShot("BastetV1", Art, 11f)
            .Camera(1.4f, 1f, new Vector2(130f, -30f), Vector2.zero) // 그릇을 가까이 보다가 -> 물러나며 여신과 고양이가 드러남
            .Dust(28)
            .Say(Mural, "여신은 고양이 귀를 단 그릇에 불을 담아 신전을 밝혔다.")
            .Say(Mural, "그릇의 불은 두 배로 밝고, 두 배로 오래 탔다.")
            .Say("바스테트", "비석을 모두 읽어 낸 자여. 이 그릇을 가져가라. 어둠 속에서 길이 될 것이다.")
            .Say(Me, "바스테트의 그릇... 이걸로 횃불을 더 오래 지킬 수 있겠어.");
        Lay(s, "glow", 770f, 330f);
        Lay(s, "rays", 807f, 360f);
        Lay(s, "goddess", 600f, 408f);
        Lay(s, "vessel", 770f, 330f, 4f, 1f);
        Lay(s, "cat", 900f, 507f, 1f, 1.3f);
        Lay(s, "player", 250f, 474.5f);
        s.Move("goddess", MuralPose.Hidden, MuralPose.Rest, 0.05f, 0.25f)
         .Move("glow", MuralPose.Hidden, MuralPose.Rest, 0.2f, 0.4f)
         .Pulse("glow", 0.35f, 1.6f)
         .Pulse("rays", 0.5f, 1.2f)
         .Move("vessel", new MuralPose(0f, 60f, 0f, 0f), MuralPose.Rest, 0.25f, 0.5f)
         .Move("cat", MuralPose.Hidden, MuralPose.Rest, 0.45f, 0.6f)
         .Move("player", new MuralPose(0f, 280f), MuralPose.Rest, 0.5f, 0.85f);
        return new[] { s };
    }

    public static DialogueLine[] After => new[]
    {
        new DialogueLine(Me, "향실에서 본 것을 수첩에 옮겨 적었다.")
    };

    // 조각 추가 (위치는 벽화 픽셀 직접 지정: 자동 생성 표 BastetMuralArt, 호루스 MuralArt는 건드리지 않음)
    private static void Lay(MuralShot shot, string name, float x, float y, float bobAmp = 0f, float bobSpeed = 0f)
    {
        shot.layers.Add(new MuralLayer { name = name, pos = new Vector2(x, y), bobAmp = bobAmp, bobSpeed = bobSpeed });
    }
}
