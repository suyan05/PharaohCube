using UnityEngine;

// 프롤로그 '전체 이야기' 벽화 인형극 (최종 스토리 B안 - 라의 열두 시간)
// 스핑크스 수수께끼를 맞힌 뒤 한 번만 (F_PROLOGUE_STORY). 화면은 MuralStage가 그림
// 1 라의 배: 매일 밤 저승의 열두 시간을 건넘 + 하린도 그 배에 실려 들어옴 (아누비스 '라의 배에 탄 자' 근거)
// 2 아펩: 혼돈의 뱀이 태양을 다섯 조각으로 깨뜨림
// 3 다섯 신전: 조각이 신전마다 흩어지고 짐승이 갇힘
// 4 심판의 저울: 심장이 깃털보다 가벼워야 해가 뜬다
public static class PrologueMuralScenes
{
    private const string Sphinx = "스핑크스";

    public static MuralShot[] Story() => new[] { RaBoat(), Apep(), Temples(), Scale() };

    private static MuralShot RaBoat()
    {
        return new MuralShot("Story1", "Story1", 8f)
            .Camera(1.12f, 1f, new Vector2(-160f, 40f), new Vector2(60f, 30f))
            .Layer("boat", 3f, 1.6f)
            .Move("boat", new MuralPose(-420f, 0f), new MuralPose(120f, 0f), 0f, 1f)
            .Dust(30)
            .Say(Sphinx, "매일 밤, 태양의 신 라는 배를 타고 저승의 열두 시간을 건너 새벽에 다시 떠오른다.")
            .Say(Sphinx, "해가 저문 순간, 너도 태양의 배에 실려 저승에 들어왔다. 해가 뜨기 전에는 돌아갈 수 없다.");
    }

    // 다섯 조각이 한 덩어리(원래 자리)였다가 뱀이 솟구치면 사방으로 흩어짐
    private static MuralShot Apep()
    {
        MuralShot s = new MuralShot("Story2", "Story2", 8f)
            .Camera(1f, 1.1f, Vector2.zero, new Vector2(80f, -20f))
            .Layer("apep", 4f, 0.9f)
            .Move("apep", new MuralPose(-260f, 260f, -10f, 0f), MuralPose.Rest, 0.05f, 0.5f);
        Vector2[] fly = { new Vector2(40f, -260f), new Vector2(330f, -120f), new Vector2(300f, 220f), new Vector2(-220f, 230f), new Vector2(-300f, -140f) };
        for (int i = 0; i < 5; i++)
        {
            string piece = $"piece{i + 1}";
            s.Layer(piece);
            s.Move(piece, MuralPose.Rest, new MuralPose(fly[i].x, fly[i].y, 50f * (i % 2 == 0 ? 1f : -1f), 0.8f), 0.5f, 0.85f);
        }
        return s.Say(Sphinx, "그런데 오늘 밤, 혼돈의 뱀 아펩이 태양을 깨뜨렸다.")
                .Say(Sphinx, "조각은 신전마다 흩어졌고, 신전을 지키던 짐승들은 갇혀 버렸다.");
    }

    // 신전 위에 조각이 하나씩 내려앉아 숨 쉬듯 빛남
    private static MuralShot Temples()
    {
        MuralShot s = new MuralShot("Story3", "Story3", 7f)
            .Camera(1.08f, 1f, new Vector2(0f, 40f), Vector2.zero);
        for (int i = 0; i < 5; i++)
        {
            string frag = $"frag{i + 1}";
            float t0 = 0.08f + i * 0.14f;
            s.Layer(frag).Pulse(frag, 0.35f, 2f + i * 0.3f);
            s.Move(frag, new MuralPose(0f, -60f, 0f, 0f), MuralPose.Rest, t0, t0 + 0.14f);
        }
        return s.Say(Sphinx, "다섯 신의 신전에서 태양의 조각 다섯 개를 되찾고, 짐승들을 데리고 나와라.");
    }

    // 저울이 흔들리다 수평으로 (심장과 깃털)
    private static MuralShot Scale()
    {
        return new MuralShot("Story4", "Story4", 8f)
            .Camera(1.15f, 1f, new Vector2(60f, -40f), Vector2.zero)
            .Layer("anubis").Layer("scale")
            .Move("anubis", MuralPose.Hidden, MuralPose.Rest, 0f, 0.3f)
            .Move("scale", MuralPose.Rest, new MuralPose(0f, 0f, 5f), 0.15f, 0.35f)
            .Move("scale", new MuralPose(0f, 0f, 5f), new MuralPose(0f, 0f, -3f), 0.35f, 0.6f)
            .Move("scale", new MuralPose(0f, 0f, -3f), MuralPose.Rest, 0.6f, 0.85f)
            .Say(Sphinx, "그리고 마지막 심판에서, 네 심장이 진실의 깃털보다 가벼워야 한다.")
            .Say(Sphinx, "그래야 해가 뜨고, 너도 깨어난다.");
    }
}
