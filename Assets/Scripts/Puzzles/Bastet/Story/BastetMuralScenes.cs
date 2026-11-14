using UnityEngine;

// [추가] 바스테트의 방 벽화 4장 컷신 대본 (호루스 벽화와 같은 MuralStage 인형극)
// 벽화 하나를 보면 (BastetMuralViewer가 재생)
//   0) 그림 먼저: 깨끗한 벽화 그림 + 제목 '벽화 이름 (n/4)' + 벽에 새겨진 글   (MU_BastetP{n}_bg)
//   1) 올려다보기: 플레이어 등 뒤에서 낡은 벽화를 올려다보며 한마디 (빛줄기 + 먼지)   (MU_BastetW{n}_*)
//   2) 이야기 연출: 벽화 속 인물이 움직이며 그 그림의 이야기를 보여 줌 (스토리 기획서 B안 바스테트)   (MU_BastetS{n}_*)
// 그림은 Tools/BastetMural/bastet_murals.py가 만듦
// 위치표: BastetMuralArt (같은 도구가 자동으로 만듦)
// 움직임 값: 벽화 픽셀 기준 (오른쪽 +x, 아래 +y), 회전은 도(+ = 반시계), 시간은 장면 진행도 0~1
public static class BastetMuralScenes
{
    public const int Count = 4;
    public static readonly string[] Titles = { "라의 눈", "나무 아래의 칼", "집을 지키는 고양이", "잠든 고양이들" };

    // 벽에 새겨진 글 (스토리 기획서 벽화 문장 그대로) - 수첩 메모에도 씀
    public static readonly string[] WallLines =
    {
        "라의 눈에서 딸이 태어났다. 그 눈은 밤에도 감기지 않는다.",
        "밤마다 뱀이 배를 덮친다. 밤마다 고양이가 칼을 든다.",
        "바스테트는 집을 지킨다. 곡식과, 아이와, 잠든 자의 숨을.",
        "사람들은 죽은 고양이도 미라로 만들어 여신 곁에 뉘었다."
    };

    // 올려다볼 때 '나'의 한마디 (그림을 보다가 이야기 속으로 빠져드는 연결)
    private static readonly string[] LookLines =
    {
        "눈 하나에서... 누군가 걸어 나오고 있어.",
        "나무 아래 고양이... 앞발에 칼을 쥐고 있어.",
        "곡식 창고, 잠든 사람, 아이... 그리고 고양이.",
        "고양이 미라들이 여신 곁에 줄지어 서 있어."
    };

    // 올려다보는 장면에서 플레이어가 선 자리 (그림 도구의 PLAYER_X와 같아야 함)
    private static readonly float[] PlayerX = { 250f, 200f, 250f, 1185f };

    private const string Wall = "벽화";
    private const string Me = "나";

    public static string Title(int index) => index >= 1 && index <= Count ? Titles[index - 1] : "벽화";

    // 그림 먼저 보기 화면의 제목 / 글 / 그림 이름
    public static string PictureTitle(int index, int number) => $"벽화 {number}/{Count}  ·  {Title(index)}";
    public static string WallLine(int index) => index >= 1 && index <= Count ? WallLines[index - 1] : "";
    public static string PictureArt(int index) => $"BastetP{index}";

    // 그림 다음에 이어질 연출. withLook = 올려다보기 장면, withStory = 이야기 장면 (둘 다 끄면 그림만)
    public static MuralShot[] For(int index, bool withLook, bool withStory)
    {
        if (index < 1 || index > Count)
        {
            Debug.LogWarning($"[바스테트 벽화] 모르는 벽화 번호: {index}");
            return new MuralShot[0];
        }
        var list = new System.Collections.Generic.List<MuralShot>();
        if (withLook) list.Add(Look(index).Say(Me, LookLines[index - 1]));
        if (withStory)
        {
            switch (index)
            {
                case 1: list.Add(EyeOfRa()); break;
                case 2: list.Add(KnifeUnderTree()); break;
                case 3: list.Add(HouseGuard()); break;
                default: list.Add(SleepingCats()); break;
            }
        }
        return list.ToArray();
    }

    // ================= 1) 올려다보기 =================
    // 카메라: 플레이어 뒤통수 가까이에서 시작 -> 천천히 뒤로 빠지며 벽화 전체가 드러남
    private static MuralShot Look(int index)
    {
        string art = $"BastetW{index}";
        float px = PlayerX[index - 1];
        return new MuralShot(art, art, 4.5f)
            .Camera(1.22f, 1f, new Vector2((px - 640f) * 0.45f, 50f), Vector2.zero)
            .Lay("rays").Lay("player", 1.2f, 1.1f)
            .Pulse("rays", 0.45f, 0.9f)
            .Move("player", new MuralPose(0f, 30f), MuralPose.Rest, 0f, 0.5f)
            .Dust(46);
    }

    // ================= 2) 이야기 연출 =================
    // 벽화 1: 라의 눈에서 딸이 태어남 -> 빛을 따라 바스테트가 걸어 나오고, 발치에 고양이
    private static MuralShot EyeOfRa()
    {
        return new MuralShot("BastetS1", "BastetS1", 9f)
            .Camera(1.1f, 1f, new Vector2(-40f, -60f), new Vector2(40f, 0f))
            .Lay("sun", 3f, 0.9f).Lay("eye").Lay("beam").Lay("goddess").Lay("cat")
            .Move("sun", new MuralPose(0f, 40f, 0f, 0f), MuralPose.Rest, 0f, 0.25f)
            .Move("eye", new MuralPose(0f, 0f, 0f, 0.15f), MuralPose.Rest, 0.1f, 0.35f)
            .Move("beam", MuralPose.Hidden, MuralPose.Rest, 0.3f, 0.45f)
            .Pulse("beam", 0.4f, 1.4f)
            .Move("goddess", new MuralPose(-300f, -70f, 0f, 0f), MuralPose.Rest, 0.4f, 0.75f)
            .Move("cat", new MuralPose(0f, 20f, 0f, 0f), MuralPose.Rest, 0.75f, 0.95f)
            .Say(Wall, "태양신 라의 눈에서 딸이 태어났다. 그 이름은 바스테트.")
            .Say(Wall, "낮에는 집을 지키는 고양이, 밤에는 아버지의 배를 지키는 사자.")
            .Say(Me, "라의 눈... 밤에도 감기지 않는 눈이라고 했지.");
    }

    // 벽화 2: 나무 아래의 칼 -> 아펩이 기어 오고, 고양이가 칼을 들어 머리를 벰
    private static MuralShot KnifeUnderTree()
    {
        return new MuralShot("BastetS2", "BastetS2", 10f)
            .Camera(1f, 1.14f, Vector2.zero, new Vector2(110f, 30f))
            .Lay("apep").Lay("head").Lay("cat", 1.2f, 1.3f).Lay("knife")
            .Move("apep", new MuralPose(380f, 0f, 0f, 0f), MuralPose.Rest, 0f, 0.35f)
            .Move("apep", MuralPose.Rest, new MuralPose(0f, 12f, 0f, 0.25f), 0.72f, 1f)
            .Move("head", new MuralPose(380f, 0f, 0f, 0f), MuralPose.Rest, 0f, 0.35f)
            .Move("head", MuralPose.Rest, new MuralPose(70f, 150f, -60f, 0f), 0.7f, 0.95f)
            .Move("knife", new MuralPose(0f, 30f, 0f, 0f), new MuralPose(0f, -24f, 12f, 1f), 0.35f, 0.5f)
            .Move("knife", new MuralPose(0f, -24f, 12f, 1f), new MuralPose(46f, 36f, -72f, 1f), 0.62f, 0.7f)
            .Say(Wall, "밤마다 뱀 아펩이 라의 배를 덮쳤다.")
            .Say(Wall, "그때마다 신성한 나무 아래에서 고양이가 칼을 들어 뱀의 머리를 베었다.")
            .Say(Me, "그런데 오늘 밤엔... 그 고양이들이 이 어둠에 갇혀 있다고 했어.");
    }

    // 벽화 3: 집을 지키는 고양이 -> 숨어드는 뱀, 고양이가 걸어와 쫓아냄. 등잔불이 흔들림
    private static MuralShot HouseGuard()
    {
        return new MuralShot("BastetS3", "BastetS3", 9f)
            .Camera(1.12f, 1f, new Vector2(-220f, 30f), Vector2.zero)
            .Lay("flame").Lay("snake").Lay("cat", 1f, 1.6f)
            .Pulse("flame", 0.35f, 3.2f)
            .Move("snake", new MuralPose(120f, 0f, 0f, 0f), MuralPose.Rest, 0.1f, 0.35f)
            .Move("snake", MuralPose.Rest, new MuralPose(170f, 0f, 0f, 0f), 0.68f, 0.9f)
            .Move("cat", new MuralPose(-280f, 0f, 0f, 0f), MuralPose.Rest, 0.3f, 0.62f)
            .Say(Wall, "바스테트는 집을 지킨다. 곡식과, 아이와, 잠든 자의 숨을.")
            .Say(Wall, "곡식을 노리는 쥐도, 잠든 이를 노리는 뱀도 고양이 앞에서는 물러났다.")
            .Say(Me, "그래서 사람들이 고양이를 그렇게 아꼈구나.");
    }

    // 벽화 4: 잠든 고양이들 -> 여신이 나타나고, 미라들이 하나씩 그 곁에 섬
    private static MuralShot SleepingCats()
    {
        MuralShot s = new MuralShot("BastetS4", "BastetS4", 10f)
            .Camera(1.12f, 1f, new Vector2(-200f, 20f), Vector2.zero)
            .Lay("goddess")
            .Move("goddess", MuralPose.Hidden, MuralPose.Rest, 0f, 0.3f);
        for (int i = 1; i <= 5; i++)
        {
            float t0 = 0.28f + (i - 1) * 0.1f;
            s.Lay($"mummy{i}").Move($"mummy{i}", new MuralPose(0f, 26f, 0f, 0f), MuralPose.Rest, t0, t0 + 0.14f);
        }
        return s.Say(Wall, "사람들은 죽은 고양이도 미라로 만들어 여신 곁에 뉘었다.")
                .Say(Wall, "잊히지 않도록. 다시 깨어나 여신의 밤을 지키도록.")
                .Say(Me, "...잊히지 않는 것. 이 방의 시험도 '기억'이겠지.");
    }

    // 조각 추가 (위치는 BastetMuralArt 표에서. 호루스 MuralArt 표는 건드리지 않음)
    private static MuralShot Lay(this MuralShot shot, string name, float bobAmp = 0f, float bobSpeed = 0f)
    {
        shot.layers.Add(new MuralLayer { name = name, pos = BastetMuralArt.Pos(shot.art, name), bobAmp = bobAmp, bobSpeed = bobSpeed });
        return shot;
    }
}

