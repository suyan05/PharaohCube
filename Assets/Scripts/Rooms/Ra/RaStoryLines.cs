using System.Collections.Generic;

// [추가] 라의 방 서브 스토리: 라는 누구이고, 왜 낮과 밤의 모습이 다른가 (이집트 신화 바탕)
// - 낮: 라의 하루는 세 얼굴. 새벽 케프리(쇠똥구리) -> 한낮 라(매의 머리) -> 해 질 녘 아툼(늙은 왕)
// - 밤: 해가 지면 라는 죽는다. 양의 머리를 한 '아우프'(태양의 몸)가 되어 밤의 배에 누워 저승을 건넘
//       한밤에 죽은 자의 왕 오시리스와 하나가 되어야 다시 젊어짐 -> 그래서 밤에는 모든 것이 거꾸로 (밤 퍼즐 단서)
// - 새벽: 다시 태어난 첫 얼굴이 케프리 -> 앞으로 길을 안내하는 쇠똥구리가 바로 라의 새벽 얼굴
// Rashrine이 단계별 대사 앞에 붙여서 들려줌 (씬에 저장된 대사는 그대로 두고 앞에만 더함)
public static class RaStoryLines
{
    public static readonly RaDialogueLine[] Day =
    {
        new RaDialogueLine(RaPortrait.Default, "나는 라. 하늘을 건너는 태양이다."),
        new RaDialogueLine(RaPortrait.Default, "새벽의 나는 쇠똥구리 케프리. 어린 해를 굴려 지평선 위로 밀어 올린다."),
        new RaDialogueLine(RaPortrait.Glow, "한낮의 나는 매의 머리를 한 라. 가장 높이, 가장 뜨겁게 빛난다."),
        new RaDialogueLine(RaPortrait.Default, "해 질 녘의 나는 늙은 왕 아툼. 지친 걸음으로 서쪽 끝에 닿는다."),
        new RaDialogueLine(RaPortrait.Default, "하루 동안 나는 세 번 태어나고 세 번 늙는다. 이 신전의 방들은 그 하루를 새긴 것이다.")
    };

    public static readonly RaDialogueLine[] BeforeNight =
    {
        new RaDialogueLine(RaPortrait.Default, "해가 지면 나는 죽는다. 아툼의 몸으로 서쪽 지평선 아래, 저승으로 내려간다."),
        new RaDialogueLine(RaPortrait.Night, "그곳의 나는 양의 머리를 한 아우프. 빛이 아니라, 빛을 잃은 태양의 몸이다.")
    };

    public static readonly RaDialogueLine[] Night =
    {
        new RaDialogueLine(RaPortrait.Night, "밤의 배에 누운 나는 스스로 걷지 못한다. 신들이 나를 끌고, 밤의 열두 시간을 건넌다."),
        new RaDialogueLine(RaPortrait.Night, "한밤에 나는 죽은 자의 왕 오시리스와 하나가 된다. 끝이 시작이 되고, 늙음이 젊음이 되는 곳."),
        new RaDialogueLine(RaPortrait.Night, "그래서 밤에는 모든 것이 거꾸로 흐른다. 낮의 순서를 뒤집어 보아라.")
    };

    public static readonly RaDialogueLine[] Dawn =
    {
        new RaDialogueLine(RaPortrait.Glow, "보아라, 새벽이다. 다시 태어난 나의 첫 얼굴, 케프리가 깨어났다."),
        new RaDialogueLine(RaPortrait.Glow, "앞으로 네 곁을 날 작은 쇠똥구리를 기억해라. 그는 나의 새벽이다.")
    };

    // 이야기 대사 + 씬에 저장된 원래 대사
    public static RaDialogueLine[] Merge(RaDialogueLine[] story, RaDialogueLine[] original)
    {
        var list = new List<RaDialogueLine>();
        if (story != null) list.AddRange(story);
        if (original != null) list.AddRange(original);
        return list.ToArray();
    }
}
