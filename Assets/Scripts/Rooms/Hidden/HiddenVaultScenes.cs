using UnityEngine;

// 숨은 방 두 곳의 내용 (대사, 통로 구성, 마지막 벽화 컷). 문구는 여기서만 고치면 됨
// - 라: "밤의 뱃길" - 태양의 배가 밤마다 지나는 열두 문. 일곱째 문 아래에서 아펩이 기다린다는 복선
//   (최종 스토리 B안: 밤은 열두 시간 / 호루스의 신전 = 밤의 일곱째 시간)
// - 호루스: "매의 둥지" - 갈대숲에 숨겨져 별을 보며 자란 아이. 왜 호루스가 별을 따라 배를 이끄는지
public static class HiddenVaultScenes
{
    private const string Me = "나";
    private const string Wall = "벽의 글귀";
    private const string Mural = "벽화";

    // ================= 라 =================
    public static SideWalkScene RaWalk()
    {
        return new SideWalkScene("밤의 뱃길", "라의 신전 아래", 930f)
            .Tint(new Color(1f, 0.84f, 0.66f, 1f), new Color(1f, 0.68f, 0.32f, 1f))
            .Start(new DialogueLine(Me, "신전 밑에 이런 길이 숨어 있었다니..."),
                   new DialogueLine(Me, "벽을 따라 문이 새겨져 있어. 끝까지 가 보자."))
            .Gate(300f, "다섯째 시간", false)
            .Gate(540f, "여섯째 시간", false)
            .Gate(780f, "일곱째 시간", true)
            .Spot(200f, 60f, null, "E: 벽의 글귀 읽기",
                  new DialogueLine(Wall, "해가 지면 라는 배에 올라 밤의 강을 건넌다. 문은 열둘, 한 시간에 하나."),
                  new DialogueLine(Me, "밤은 열두 시간... 스핑크스가 한 말과 같아."))
            .Spot(430f, 96f, "SW_Mark", "E: 긁힌 자국 살펴보기",
                  new DialogueLine(Me, "누가 벽을 긁어 놨어. 제 몸을 넘어가며 똬리를 튼... 뱀?"),
                  new DialogueLine(Me, "발굴 현장 모래 위에 있던 자국이랑 똑같이 생겼어."))
            .Spot(780f, 70f, null, "E: 검게 칠해진 문 살펴보기",
                  new DialogueLine(Wall, "일곱째 시간. 강이 가장 얕아지는 곳. 그것이 물을 마셔 배를 세운다."),
                  new DialogueLine(Me, "이 문만 새까맣게 칠해져 있어. 누가 일부러 지운 것처럼."))
            .Exit("E: 끝 방으로 들어가기");
    }

    public static MuralShot[] RaReveal()
    {
        MuralShot s = new MuralShot("VaultRa", "VaultRa", 10f)
            .Camera(1.5f, 1f, new Vector2(60f, 150f), Vector2.zero) // 뱀을 가까이 보다가 -> 물러나며 배 전체가 드러남
            .Layer("rays").Pulse("rays", 0.5f, 1.3f)
            .Layer("player").Move("player", new MuralPose(0f, 280f), MuralPose.Rest, 0.5f, 0.85f)
            .Dust(26)
            .Say(Mural, "라의 배가 열두 문을 지난다. 문마다 신 하나가 배를 지킨다.")
            .Say(Mural, "그러나 일곱째 문 아래, 똬리를 튼 것이 배를 올려다본다.")
            .Say(Me, "아펩... 저게 태양을 깨뜨린 뱀이야.")
            .Say(Me, "일곱째 시간... 어느 신이 지키는 문일까. 기억해 두자."); // [수정] 라의 방 시점엔 아직 모름. 호루스 신전 입구에서 케프리가 "밤의 일곱째 시간"이라고 알려줄 때 이어짐
        return new[] { s };
    }

    public static DialogueLine[] RaAfter => new[]
    {
        new DialogueLine(Me, "본 것을 전부 수첩에 옮겨 적었다."),
        new DialogueLine(Me, "일곱째 문. 기억해 두자.")
    };

    // ================= 호루스 =================
    public static SideWalkScene HorusWalk()
    {
        return new SideWalkScene("매의 둥지", "매의 회랑 깊은 곳", 640f)
            .Tint(new Color(0.66f, 0.76f, 1f, 1f), new Color(0.75f, 0.8f, 1f, 1f))
            .Start(new DialogueLine(Me, "둥지 뒤로 좁은 길이 이어져 있어. 바람이 저 안에서 불어와."))
            .Spot(210f, 60f, null, "E: 벽의 글귀 읽기",
                  new DialogueLine(Wall, "어미는 아이를 갈대숲 깊이 숨겼다. 붉은 사막의 눈이 닿지 않는 곳에."),
                  new DialogueLine(Me, "복도 벽화에 있던 '갈대숲의 아이' 이야기야."))
            .Spot(400f, 96f, "SW_Mark", "E: 긁힌 자국 살펴보기",
                  new DialogueLine(Me, "여기에도 뱀이 새겨져 있어. 그런데 이건... 위에 발톱 자국이 덮여 있네."),
                  new DialogueLine(Me, "매가 뱀을 지우려고 한 걸까."))
            .Exit("E: 둥지 안쪽으로 들어가기");
    }

    public static MuralShot[] HorusReveal()
    {
        MuralShot s = new MuralShot("VaultHo", "VaultHo", 10f)
            .Camera(1.45f, 1f, new Vector2(0f, -190f), Vector2.zero) // 별 천장에서 -> 아래로 내려오며 둥지가 드러남
            .Layer("rays").Pulse("rays", 0.5f, 1.1f)
            .Layer("player").Move("player", new MuralPose(0f, 280f), MuralPose.Rest, 0.5f, 0.85f)
            .Dust(30)
            .Say(Mural, "매의 아이는 갈대 둥지에 누워 밤마다 별을 헤아렸다.")
            .Say(Mural, "하늘의 길을 모두 외운 아이만이, 훗날 태양의 배를 이끌 수 있었다.")
            .Say(Me, "그래서 호루스가 별을 따라 배를 이끄는 거구나.");
        return new[] { s };
    }

    public static DialogueLine[] HorusAfter => new[]
    {
        new DialogueLine(Me, "별을 외운 아이... 수첩에 적어 두자.")
    };
}
