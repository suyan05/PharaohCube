using System.Collections.Generic;
using UnityEngine;

// [추가] 소베크의 방 전실 벽화 4장 대본 (바스테트 벽화와 같은 방식: BastetMuralViewer + MuralStage 인형극)
// 벽화 하나를 보면
//   0) 그림 먼저: 깨끗한 벽화 + 제목 '벽화 n/4 · 이름' + 벽에 새겨진 글   (MU_SobekP{n}_bg)
//   1) 올려다보기: 플레이어 등 뒤에서 낡은 벽화를 올려다보며 한마디        (MU_SobekW{n}_*)
//   2) 이야기 연출: 벽화 속 인물이 움직이며 이야기를 보여 줌               (MU_SobekS{n}_*)
// 그림: Tools/SobekArt/sobek_murals.py / 위치표: SobekMuralArt (같은 도구가 자동으로 만듦)
// 스토리: 바스테트·소베크·아누비스 스토리 기획서 B안 "저승의 강, 멈춘 배"
// 움직임 값: 벽화 픽셀 기준 (오른쪽 +x, 아래 +y), 회전은 도(+ = 반시계), 시간은 장면 진행도 0~1
public static class SobekMuralScenes
{
    public const int Count = 4;
    public static readonly string[] Titles = { "물의 주인", "태초의 알", "강을 삼킨 뱀", "소베크의 입" };

    // 벽에 새겨진 글 (수첩 메모에도 씀)
    public static readonly string[] WallLines =
    {
        "나일은 소베크의 땀에서 흘러나왔다. 물이 닿는 곳마다 곡식이 자랐다.",
        "태초의 물가에 소베크가 알을 낳았다. 알이 깨지자 세상이 열렸다.",
        "한밤을 넘긴 배 앞에서 뱀이 강을 통째로 삼켰다. 배는 마른 모래 위에 멈췄다.",
        "물의 주인은 마지막 범람을 신전에 가두고 문을 잠갔다. 네 부적이 모이면 입이 열린다."
    };

    // 올려다볼 때 '나'의 한마디
    private static readonly string[] LookLines =
    {
        "악어 머리의 신... 몸에서 강이 흘러나오고 있어.",
        "물가에 알 하나. 금이 간 틈으로 빛이 새어 나와.",
        "태양의 배 앞에서 뱀이 강물을 마시고 있어.",
        "악어 입 모양의 문. 홈이 네 개... 신전 안쪽에 있던 그 문이야."
    };

    private const string Wall = "벽화";
    private const string Me = "나";

    public static string Title(int index) => index >= 1 && index <= Count ? Titles[index - 1] : "벽화";
    public static string PictureTitle(int index, int number) => $"벽화 {number}/{Count}  -  {Title(index)}";
    public static string WallLine(int index) => index >= 1 && index <= Count ? WallLines[index - 1] : "";
    public static string PictureArt(int index) => $"SobekP{index}";

    // 그림 다음에 이어질 연출. withLook = 올려다보기, withStory = 이야기 (둘 다 끄면 그림만)
    public static MuralShot[] For(int index, bool withLook, bool withStory)
    {
        if (index < 1 || index > Count)
        {
            Debug.LogWarning($"[소베크 벽화] 모르는 벽화 번호: {index}");
            return new MuralShot[0];
        }
        var list = new List<MuralShot>();
        if (withLook) list.Add(Look(index).Say(Me, LookLines[index - 1]));
        if (withStory)
        {
            switch (index)
            {
                case 1: list.Add(MasterOfWater()); break;
                case 2: list.Add(FirstEgg()); break;
                case 3: list.Add(SwallowedRiver()); break;
                default: list.Add(MouthOfSobek()); break;
            }
        }
        return list.ToArray();
    }

    // ================= 1) 올려다보기 =================
    // 카메라: 플레이어 뒤통수 가까이에서 시작 -> 천천히 뒤로 빠지며 벽화 전체가 드러남
    private static MuralShot Look(int index)
    {
        string art = $"SobekW{index}";
        float px = SobekMuralArt.Pos(art, "player").x;
        return new MuralShot(art, art, 4.5f)
            .Camera(1.22f, 1f, new Vector2((px - 640f) * 0.45f, 50f), Vector2.zero)
            .Lay("rays").Lay("player", 1.2f, 1.1f)
            .Pulse("rays", 0.45f, 0.9f)
            .Move("player", new MuralPose(0f, 30f), MuralPose.Rest, 0f, 0.5f)
            .Dust(46);
    }

    // ================= 2) 이야기 연출 =================
    // 벽화 1: 물의 주인 -> 소베크가 나타나고, 그 몸에서 강이 흘러나와 들판까지 닿으면 곡식이 자람
    private static MuralShot MasterOfWater()
    {
        return new MuralShot("SobekS1", "SobekS1", 9f)
            .Camera(1.12f, 1f, new Vector2(-200f, -20f), new Vector2(30f, 0f))
            .Lay("sun", 3f, 0.9f).Lay("grain").Lay("river").Lay("god")
            .Move("god", MuralPose.Hidden, MuralPose.Rest, 0f, 0.2f)
            .Move("river", new MuralPose(-420f, 0f, 0f, 0f), MuralPose.Rest, 0.2f, 0.55f)
            .Move("grain", new MuralPose(0f, 90f, 0f, 0f), MuralPose.Rest, 0.5f, 0.8f)
            .Move("sun", new MuralPose(0f, 60f, 0f, 0f), MuralPose.Rest, 0.6f, 0.9f)
            .Say(Wall, "나일은 물의 주인 소베크의 땀에서 흘러나왔다.")
            .Say(Wall, "물이 닿는 곳마다 곡식이 자랐고, 사람들은 강가에 모여 살았다.")
            .Say(Me, "그래서 이 신전은 온통 물길이구나.");
    }

    // 벽화 2: 태초의 알 -> 태초의 물에서 악어가 기어 나오고, 땅이 솟아 알이 깨지며 해가 떠오름
    private static MuralShot FirstEgg()
    {
        return new MuralShot("SobekS2", "SobekS2", 10f)
            .Camera(1.1f, 1f, new Vector2(-180f, 40f), new Vector2(40f, 0f))
            .Lay("water", 2f, 0.6f).Lay("light").Lay("mound").Lay("egg").Lay("croc", 1f, 0.8f).Lay("sun", 3f, 0.8f)
            .Move("croc", new MuralPose(-300f, 30f, 0f, 0f), MuralPose.Rest, 0f, 0.3f)
            .Move("mound", new MuralPose(0f, 110f, 0f, 0f), MuralPose.Rest, 0.25f, 0.45f)
            .Move("egg", new MuralPose(0f, 120f, 0f, 0f), MuralPose.Rest, 0.3f, 0.5f)
            .Move("light", MuralPose.Hidden, MuralPose.Rest, 0.55f, 0.7f)
            .Pulse("light", 0.4f, 1.4f)
            .Move("sun", new MuralPose(0f, 220f, 0f, 0f), MuralPose.Rest, 0.65f, 0.95f)
            .Say(Wall, "처음에는 끝없는 물뿐이었다.")
            .Say(Wall, "소베크가 물가에 알을 낳자, 알이 깨지며 땅이 솟고 해가 떠올랐다.")
            .Say(Me, "악어알이 세상을 열었다... 그래서 악어를 신처럼 모셨던 거야.");
    }

    // 벽화 3: 강을 삼킨 뱀 -> 태양의 배가 밤의 강을 건넘 -> 아펩이 솟아 강을 마심 -> 강이 사라지고 배가 모래에 멈춤
    private static MuralShot SwallowedRiver()
    {
        return new MuralShot("SobekS3", "SobekS3", 11f)
            .Camera(1.08f, 1.02f, new Vector2(-140f, 0f), new Vector2(80f, 10f))
            .Lay("river", 2f, 0.7f).Lay("boat").Lay("sun", 2f, 0.9f).Lay("apep").Lay("head")
            .Move("boat", new MuralPose(-300f, 0f, 0f, 0f), MuralPose.Rest, 0f, 0.3f)
            .Move("sun", new MuralPose(-300f, 0f, 0f, 0f), MuralPose.Rest, 0f, 0.3f)
            .Move("apep", new MuralPose(0f, 200f, 0f, 0f), MuralPose.Rest, 0.25f, 0.45f)
            .Move("head", new MuralPose(60f, 200f, 0f, 0f), MuralPose.Rest, 0.25f, 0.45f)
            .Move("head", MuralPose.Rest, new MuralPose(-10f, 12f, -6f, 1f), 0.5f, 0.6f)
            .Move("river", MuralPose.Rest, new MuralPose(0f, 40f, 0f, 0f), 0.55f, 0.85f)
            .Move("boat", MuralPose.Rest, new MuralPose(0f, 30f, -4f, 1f), 0.7f, 0.85f)
            .Move("sun", MuralPose.Rest, new MuralPose(0f, 30f, 0f, 0.35f), 0.7f, 0.9f)
            .Dust(20)
            .Say(Wall, "한밤을 넘긴 태양의 배 앞에, 뱀 아펩이 솟아올랐다.")
            .Say(Wall, "뱀은 저승의 강을 통째로 삼켰고, 배는 마른 모래 위에 멈췄다.")
            .Say(Me, "배가 멈추면 해가 다시 뜨지 못해... 이게 지금 일어나는 일이야.");
    }

    // 벽화 4: 소베크의 입 -> 소베크가 나타나 마지막 물결을 신전 문 안으로 몰아넣음 -> 부적 홈 네 개가 빛남
    private static MuralShot MouthOfSobek()
    {
        return new MuralShot("SobekS4", "SobekS4", 10f)
            .Camera(1.1f, 1f, new Vector2(-220f, 20f), new Vector2(30f, 0f))
            .Lay("gate").Lay("flood", 2f, 1.2f).Lay("god").Lay("sockets")
            .Move("god", MuralPose.Hidden, MuralPose.Rest, 0f, 0.2f)
            .Move("flood", new MuralPose(-160f, 40f, 0f, 0f), MuralPose.Rest, 0.15f, 0.4f)
            .Move("flood", MuralPose.Rest, new MuralPose(260f, 10f, 0f, 0f), 0.45f, 0.7f)
            .Move("sockets", MuralPose.Hidden, MuralPose.Rest, 0.72f, 0.85f)
            .Pulse("sockets", 0.5f, 2.2f)
            .Say(Wall, "뱀이 물을 다 마시기 전, 물의 주인은 마지막 범람을 신전에 가두고 문을 잠갔다.")
            .Say(Wall, "네 섬의 시험을 지나 부적 넷을 모은 자에게 소베크의 입이 열린다.")
            .Say(Me, "가둬 둔 물을 강으로 돌려보내면... 배가 다시 뜰 수 있어.");
    }

    // 조각 추가 (위치는 SobekMuralArt 표에서. 호루스 MuralArt 표는 건드리지 않음)
    private static MuralShot Lay(this MuralShot shot, string name, float bobAmp = 0f, float bobSpeed = 0f)
    {
        shot.layers.Add(new MuralLayer { name = name, pos = SobekMuralArt.Pos(shot.art, name), bobAmp = bobAmp, bobSpeed = bobSpeed });
        return shot;
    }
}
