using UnityEngine;

// 호루스의 방 벽화 컷신 대본 (이집트 벽화 인형극)
// [수정] 최종 스토리(B안)에는 이시스가 나오지 않음 -> 회상은 호루스가 자기 기억으로 들려줌 (이시스 장면 데이터는 남겨 둠, 안 부름)
// - (안 씀) 입구 이시스 석상: IsisKey   오시리스의 왕좌 -> 갈대숲의 이시스와 아기 호루스
// - 성좌 2 (H3 우자트 차례): Eye()   호루스와 세트의 싸움, 눈이 여섯 조각 -> 토트가 큰 조각부터 이어 붙임
// - 성좌 3 (H4 돌배 차례): Race()   돌배 경주, 세트의 돌배가 가라앉음 -> 퍼즐 힌트 (세트 배를 강에 가라앉혀 다리로)
// - H5 클리어 (엔딩): Ending()   신들의 재판 -> 왕이 된 호루스 + 서쪽 벽 복선
// 움직임 값: 벽화 픽셀 기준 (오른쪽 +x, 아래 +y), 회전은 도(+ = 반시계), 시간은 장면 진행도 0~1
public static class HorusMuralScenes
{
    public const string IsisKey = "MURAL_ISIS";
    public const string EyeKey = "MURAL_EYE";
    public const string RaceKey = "MURAL_RACE";
    public const string EndingKey = "MURAL_ENDING";
    public const string Wall1Key = "WALL_1";   // [추가] 입구 복도의 찢긴 벽화 4점 (등 뒤에서 올려다보는 장면)
    public const string Wall2Key = "WALL_2";
    public const string Wall3Key = "WALL_3";
    public const string Wall4Key = "WALL_4";
    private const string Me = "나";
    private const string Wall = "벽화";
    public static readonly string[] WallTitles = { "빼앗긴 왕좌", "갈대숲의 아이", "여든 해의 다툼", "왕관을 쓴 매" };

    private const string Isis = "이시스";
    private const string Horus = "호루스";
    private static readonly string[] EyeParts = { "eye_white", "eye_pupil", "eye_brow", "eye_line", "eye_tail", "eye_tear" };

    public static MuralShot[] For(string key)
    {
        switch (key)
        {
            case IsisKey: return new[] { IntroThrone(), IntroMarsh() };
            case EyeKey: return new[] { EyeFight(), EyeThoth() };
            case RaceKey: return new[] { RaceStart(), RaceSink() };
            case EndingKey: return new[] { EndCourt(), EndThrone() };
            case Wall1Key: return new[] { WallThrone() };
            case Wall2Key: return new[] { WallMarsh() };
            case Wall3Key: return new[] { WallCourt() };
            case Wall4Key: return new[] { WallCrowned() };
            default:
                Debug.LogWarning($"[벽화] 모르는 컷신: {key}");
                return new MuralShot[0];
        }
    }

    // ================= 입구: 이시스의 기억 =================
    private static MuralShot IntroThrone()
    {
        return new MuralShot("IntroA", "IntroA", 8f)
            .Camera(1.12f, 1f, new Vector2(-120f, -20f), Vector2.zero)
            .Layer("osiris").Layer("set")
            .Move("set", new MuralPose(320f, 0f, 0f, 0f), new MuralPose(0f, 0f), 0.15f, 0.55f)
            .Move("osiris", MuralPose.Rest, new MuralPose(0f, 0f, 0f, 0.25f), 0.65f, 0.95f)
            .Say(Isis, "아주 먼 옛날, 내 남편 오시리스는 이 땅을 다스린 선한 왕이었다.")
            .Say(Isis, "그러나 그의 아우 세트가 질투에 눈이 멀어 왕을 쓰러뜨리고 왕좌를 빼앗았지.");
    }

    private static MuralShot IntroMarsh()
    {
        return new MuralShot("IntroB", "IntroB", 7f)
            .Camera(1f, 1.1f, Vector2.zero, new Vector2(80f, 10f))
            .Layer("isis", 3f, 1.4f).Layer("child")
            .Move("isis", MuralPose.Hidden, MuralPose.Rest, 0f, 0.35f)
            .Move("child", MuralPose.Hidden, MuralPose.Rest, 0.35f, 0.7f)
            .Say(Isis, "나는 나일의 갈대숲에 숨어 아들 호루스를 낳고, 세트의 눈을 피해 몰래 길렀다.")
            .Say(Isis, "이제 그 아이가 잃어버린 하늘을 되찾을 때가 왔다.");
    }

    // ================= 성좌 2: 찢긴 눈 =================
    private static MuralShot EyeFight()
    {
        MuralShot s = new MuralShot("EyeA", "EyeA", 7f)
            .Camera(1.05f, 1.15f, Vector2.zero, new Vector2(0f, -30f))
            .Layer("horus").Layer("set")
            .Move("set", new MuralPose(140f, 0f), new MuralPose(-20f, 0f), 0.1f, 0.4f);
        Vector2[] fly = { new Vector2(-260f, -60f), new Vector2(40f, -150f), new Vector2(230f, -110f), new Vector2(330f, 60f), new Vector2(-330f, 90f), new Vector2(-60f, 200f) };
        for (int i = 0; i < EyeParts.Length; i++)
        {
            s.Layer(EyeParts[i]);
            s.Move(EyeParts[i], MuralPose.Rest, new MuralPose(fly[i].x, fly[i].y, 40f * (i % 2 == 0 ? 1f : -1f), 0.55f), 0.45f, 0.8f);
        }
        return s.Say(Horus, "왕좌를 두고 나는 세트와 오래도록 싸웠다.")
                .Say(Horus, "싸움 끝에 세트는 나의 왼눈을 뽑아 여섯 조각으로 찢어 버렸다.");
    }

    // 토트가 큰 몫부터 차례로 이어 붙임 (H3 규칙 힌트). 마지막 눈물 조각은 마법으로 천천히 채워짐
    private static MuralShot EyeThoth()
    {
        MuralShot s = new MuralShot("EyeB", "EyeB", 9f)
            .Camera(1f, 1.08f, Vector2.zero, new Vector2(60f, -10f))
            .Layer("thoth");
        Vector2[] from = { new Vector2(-300f, -120f), new Vector2(260f, -160f), new Vector2(300f, 100f), new Vector2(-260f, 130f), new Vector2(120f, 200f), new Vector2(0f, -220f) };
        for (int i = 0; i < EyeParts.Length; i++)
        {
            float t0 = 0.08f + i * 0.13f;
            float t1 = i == EyeParts.Length - 1 ? 0.98f : t0 + 0.12f;
            s.Layer(EyeParts[i]);
            s.Move(EyeParts[i], new MuralPose(from[i].x, from[i].y, 30f, 0f), MuralPose.Rest, t0, t1);
        }
        return s.Say(Horus, "지혜의 신 토트가 흩어진 조각을 주워 모았다. 가장 큰 몫부터, 차례로.")
                .Say(Horus, "마지막 한 조각은 끝내 찾지 못해, 토트가 마법으로 채웠지.")
                .Say(Horus, "...그 눈을 다시 이어다오. 큰 조각부터다.");
    }

    // ================= 성좌 3: 돌배 경주 =================
    private static MuralShot RaceStart()
    {
        return new MuralShot("RaceA", "Race", 8f)
            .Camera(1.1f, 1.05f, new Vector2(-160f, 40f), new Vector2(60f, 40f))
            .Layer("horus", 3f, 2.2f).Layer("boatH", 3f, 2.2f).Layer("set", 2f, 1.6f).Layer("boatS", 2f, 1.6f).Layer("water")
            .Move("boatH", new MuralPose(-240f, 0f), MuralPose.Rest, 0f, 1f)
            .Move("horus", new MuralPose(-240f, 0f), MuralPose.Rest, 0f, 1f)
            .Move("boatS", new MuralPose(-160f, 0f), MuralPose.Rest, 0f, 1f)
            .Move("set", new MuralPose(-160f, 0f), MuralPose.Rest, 0f, 1f)
            .Say(Horus, "세트가 또 꾀를 냈다. 돌로 만든 배로 경주해서 이기는 자가 왕이 되자고.")
            .Say(Horus, "나는 나무배에 흰 석회를 발라 돌배처럼 꾸몄다.");
    }

    private static MuralShot RaceSink()
    {
        return new MuralShot("RaceB", "Race", 8f)
            .Camera(1.05f, 1.15f, new Vector2(60f, 40f), new Vector2(180f, 60f))
            .Layer("horus", 3f, 2.2f).Layer("boatH", 3f, 2.2f).Layer("set").Layer("boatS").Layer("water")
            .Move("boatH", MuralPose.Rest, new MuralPose(150f, 0f), 0f, 1f)
            .Move("horus", MuralPose.Rest, new MuralPose(150f, 0f), 0f, 1f)
            .Move("boatS", MuralPose.Rest, new MuralPose(20f, 130f, -9f, 1f), 0.15f, 0.85f)
            .Move("set", MuralPose.Rest, new MuralPose(30f, 150f, -9f, 0.2f), 0.15f, 0.9f)
            .Say(Horus, "무거운 세트의 돌배는... 강물에 가라앉고 말았다.")
            .Say(Horus, "그 가라앉은 돌배를 밟고 나는 강을 건넜다. 세트의 붉은 배를 강에 밀어 넣어라.");
    }

    // ================= 엔딩: 신들의 재판 =================
    private static MuralShot EndCourt()
    {
        return new MuralShot("EndA", "EndA", 7f)
            .Camera(1.1f, 1f, new Vector2(160f, 0f), Vector2.zero)
            .Layer("ennead").Layer("horus")
            .Move("ennead", MuralPose.Hidden, MuralPose.Rest, 0f, 0.4f)
            .Move("horus", new MuralPose(-120f, 0f, 0f, 0f), MuralPose.Rest, 0.3f, 0.7f)
            .Say(Horus, "신들은 여든 해 동안 나와 세트의 다툼을 지켜보았다.")
            .Say(Horus, "그리고 마침내, 나를 두 땅의 왕으로 인정했다.");
    }

    private static MuralShot EndThrone()
    {
        return new MuralShot("EndB", "EndB", 8f)
            .Camera(1.15f, 1f, new Vector2(120f, 40f), Vector2.zero)
            .Layer("isis", 2f, 1.2f).Layer("horus").Layer("sun", 4f, 0.9f)
            .Move("horus", new MuralPose(0f, 40f, 0f, 0f), MuralPose.Rest, 0f, 0.4f)
            .Move("isis", MuralPose.Hidden, MuralPose.Rest, 0.25f, 0.6f)
            .Move("sun", new MuralPose(0f, -80f, 0f, 0f), MuralPose.Rest, 0.45f, 0.9f)
            .Say(Horus, "내 눈이 다시 떴다. 별길이 보인다.")
            .Say(Horus, "잊지 마라. 부서진 것은 맞추는 자만이 되찾는다.");
    }

    // ================= 입구 복도: 찢긴 벽화 4장 (최종 스토리 호루스 방 벽화 1~4) =================
    // 자막 이름 칸은 HorusMuralWall이 '벽화 이름 (n/4)'로 바꿔 넣음
    // 카메라: 플레이어 뒤통수 가까이에서 시작 -> 천천히 뒤로 빠지며 벽화 전체가 드러남. 빛줄기는 숨 쉬고 먼지가 떠오름
    private static MuralShot WallView(string id, float playerX, float duration)
    {
        Vector2 start = new Vector2((playerX - 640f) * 0.45f, 50f);
        return new MuralShot(id, id, duration)
            .Camera(1.22f, 1f, start, Vector2.zero)
            .Layer("rays").Layer("player", 1.2f, 1.1f)
            .Pulse("rays", 0.45f, 0.9f)
            .Move("player", new MuralPose(0f, 30f), MuralPose.Rest, 0f, 0.5f)
            .Dust(46);
    }

    private static MuralShot WallThrone()
    {
        return WallView("Wall1", 790f, 7f)
            .Say(Wall, "세트는 형을 속여 관에 가두고 왕좌를 빼앗았다.");
    }

    private static MuralShot WallMarsh()
    {
        return WallView("Wall2", 1060f, 7f)
            .Say(Wall, "이시스는 갈대숲에 아이를 숨기고, 매는 그곳에서 자랐다.");
    }

    private static MuralShot WallCourt()
    {
        return WallView("Wall3", 700f, 7f)
            .Say(Wall, "호루스와 세트는 여든 해 동안 신들 앞에서 다퉜다.");
    }

    private static MuralShot WallCrowned()
    {
        return WallView("Wall4", 260f, 7f)
            .Say(Wall, "마침내 매는 두 땅의 왕이 되었다.")
            .Say(Wall, "그때부터 모든 파라오는 '살아 있는 호루스'라 불렸다.");
    }
}
