using UnityEngine;

// 토트의 방 벽화 컷 (MuralStage로 재생). 벽화를 다 맞추면 그 벽화가 살아 움직이며 이야기를 들려줌
// 그림: Resources/Horus/Mural/MU_ThFA_bg, MU_ThFA_thoth ... (th_fresco.py) / 숨은 방: MU_VaultTh_* (th_vault.py)
// 카메라 pan 값 = 벽화 픽셀 기준 가운데(640, 360)에서의 차이 (x 오른쪽 +, y 아래 +)
public static class ThothMuralScenes
{
    private const string M = ThothLines.Mural;
    private const string Me = ThothLines.Me;

    public static MuralShot[] For(string fresco)
    {
        if (fresco == "A") return new[] { WritingA() };
        if (fresco == "B") return new[] { WagerB1(), WagerB2() };
        if (fresco == "C") return new[] { EyeC() };
        return new MuralShot[0];
    }

    // A: 글자를 만든 토트 - 글자의 뜻 + 읽는 방향 규칙
    private static MuralShot WritingA()
    {
        return new MuralShot("ThFA", "ThFA", 9f)
            .Camera(1.35f, 1f, new Vector2(-300f, -40f), new Vector2(0f, 0f))
            .Layer("thoth", 2f, 0.6f).Move("thoth", new MuralPose(-40f, 0f, 0f, 0f), MuralPose.Rest, 0.05f, 0.35f)
            .Layer("table").Layer("baboon", 1.5f, 0.9f)
            .Dust(20)
            .Say(M, "태초에 토트는 말을 그림으로 묶었다. 그 그림이 글자가 되었다.")
            .Say(M, "따오기는 토트, 저울은 진실, 갈대 펜은 기록, 둥근 달은 달, 눈은 눈.")
            .Say(M, "글은 새와 사람이 바라보는 쪽에서 시작된다.")
            .Say(Me, "새가 오른쪽을 보면 오른쪽부터 읽는다는 거구나.");
    }

    // B: 달과의 내기 (1) 세네트 판 앞의 두 신
    private static MuralShot WagerB1()
    {
        return new MuralShot("ThFB1", "ThFB", 8f)
            .Camera(1.25f, 1.05f, new Vector2(0f, 120f), new Vector2(0f, 40f))
            .Layer("board")
            .Layer("thoth", 1.5f, 0.7f).Move("thoth", new MuralPose(-60f, 0f, 0f, 0f), MuralPose.Rest, 0.0f, 0.3f)
            .Layer("khonsu", 1.5f, 0.8f).Move("khonsu", new MuralPose(60f, 0f, 0f, 0f), MuralPose.Rest, 0.1f, 0.4f)
            .Layer("days").Move("days", MuralPose.Hidden, MuralPose.Hidden, 0f, 1f)
            .Layer("moon").Move("moon", MuralPose.Hidden, MuralPose.Hidden, 0f, 1f)
            .Dust(16)
            .Say(M, "하늘의 여신 누트는 일 년 어느 날에도 아이를 낳을 수 없다는 저주를 받았다.")
            .Say(M, "토트는 달의 신 콘수에게 세네트 내기를 걸었다. 이길 때마다 달의 빛을 조금씩 땄다.");
    }

    // B: 달과의 내기 (2) 다섯 날과 그 밤의 달 (단서: 왼쪽 반달)
    private static MuralShot WagerB2()
    {
        return new MuralShot("ThFB2", "ThFB", 9f)
            .Camera(1.0f, 1.5f, new Vector2(0f, 0f), new Vector2(0f, -170f))
            .Layer("board").Layer("thoth").Layer("khonsu")
            .Layer("days").Move("days", new MuralPose(0f, 0f, 0f, 0f), MuralPose.Rest, 0.05f, 0.35f)
            .Layer("moon", 2f, 0.4f).Move("moon", new MuralPose(0f, 0f, 0f, 0f), MuralPose.Rest, 0.35f, 0.7f)
            .Pulse("moon", 0.25f, 1.2f)
            .Dust(16)
            .Say(M, "토트는 일흔둘 중 하나의 빛을 모아 다섯 날을 만들었다. 해에도 달에도 속하지 않은 날.")
            .Say(M, "그 다섯 날에 누트의 아이들이 태어났다. 오시리스, 호루스, 세트, 이시스, 네프티스.")
            .Say(M, "내기를 한 그 밤, 달은 왼쪽 절반만 빛나고 있었다.")
            .Say(Me, "왼쪽 반달... 이게 중요한 것 같아.");
    }

    // C: 되찾은 눈 - 토트가 호루스의 눈을 고침. 아래 다섯 글자 = 문 주문
    private static MuralShot EyeC()
    {
        return new MuralShot("ThFC", "ThFC", 10f)
            .Camera(1.3f, 1.25f, new Vector2(30f, -100f), new Vector2(0f, 200f))
            .Layer("horus", 1.5f, 0.6f)
            .Layer("thoth", 1.5f, 0.7f).Move("thoth", new MuralPose(-50f, 0f, 0f, 0f), MuralPose.Rest, 0f, 0.3f)
            .Layer("eye", 3f, 0.5f).Move("eye", new MuralPose(0f, -30f, 0f, 0f), MuralPose.Rest, 0.25f, 0.55f)
            .Pulse("eye", 0.2f, 1.4f)
            .Dust(22)
            .Say(M, "세트와 싸우다 호루스는 눈을 잃었다. 눈은 여섯 조각으로 부서졌다.")
            .Say(M, "토트는 조각을 하나하나 찾아 맞추어 눈을 되살렸다. 부서진 것을 맞추는 일, 그것이 지혜다.")
            .Say(M, "그 눈 아래에 지혜의 문을 여는 다섯 글자가 적혀 있다.")
            .Say(Me, "맨 아래 줄... 따오기가 바라보는 쪽부터 읽어야 해.");
    }

    // ================= 숨은 방: 토트의 책 =================
    public static SideWalkScene VaultWalk()
    {
        return new SideWalkScene("토트의 책", "서고 아래 물길", 700f)
            .Tint(new Color(0.62f, 0.86f, 0.92f, 1f), new Color(0.6f, 0.9f, 1f, 1f))
            .Start(new DialogueLine(Me, "벽 너머로 물소리가 들려. 서고 밑으로 물길이 흐르고 있어."))
            .Spot(220f, 60f, null, "E: 벽의 글귀 읽기",
                  new DialogueLine("벽의 글귀", "왕자 세트네는 토트가 직접 쓴 책을 찾아 강 밑으로 내려갔다."),
                  new DialogueLine(Me, "토트의 책... 이야기로만 듣던 거야."))
            .Spot(450f, 90f, "SW_Mark", "E: 긁힌 자국 살펴보기",
                  new DialogueLine("벽의 글귀", "책은 쇠, 구리, 나무, 상아와 흑단, 은, 금 상자 안에. 그 둘레를 끝없는 뱀이 감고 있다."),
                  new DialogueLine(Me, "상자가 여섯 겹... 그리고 또 뱀이야."))
            .Exit("E: 물길 끝으로 들어가기");
    }

    public static MuralShot[] VaultReveal()
    {
        MuralShot s = new MuralShot("VaultTh", "VaultTh", 10f)
            .Camera(1.5f, 1f, new Vector2(0f, 200f), Vector2.zero) // 물 밑 상자를 가까이 보다가 -> 물러나며 강가의 토트가 드러남
            .Layer("rays").Pulse("rays", 0.5f, 1.2f)
            .Layer("player").Move("player", new MuralPose(0f, 280f), MuralPose.Rest, 0.5f, 0.85f)
            .Dust(26)
            .Say(M, "강 밑 여섯 겹 상자 속에, 토트가 직접 쓴 책이 잠들어 있다.")
            .Say(M, "책을 읽은 자는 새와 짐승의 말을 듣지만, 그 대가로 가장 소중한 것을 잃는다.")
            .Say(Me, "세트네도 결국 책을 돌려놓았다고 했지... 읽지 말고 기억만 하자.");
        return new[] { s };
    }
}
