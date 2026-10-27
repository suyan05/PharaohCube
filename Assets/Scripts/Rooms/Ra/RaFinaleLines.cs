using System;

// 라 엔딩 대사 묶음 (RaFinale Inspector의 Lines 칸에서 전부 바꿀 수 있음)
// 라의 세 얼굴: 새벽 = 케프리(쇠똥구리), 한낮 = 라, 해 질 녘 = 아툼 (실제 이집트 신화)
[Serializable]
public class RaFinaleLines
{
    private const string Ra = "라";
    private const string Me = "나";

    public string title = "태양의 신, 라";
    public string subtitle = "신들의 왕";
    public string cubeCaption = "큐브의 첫 번째 면 - 태양";

    public DialogueLine[] appear =
    {
        new DialogueLine(Ra, "고개를 들어라, 고고학자여."),
        new DialogueLine(Ra, "나는 라. 새벽에는 케프리, 한낮에는 라, 해 질 녘에는 아툼이라 불리는 자."),
        new DialogueLine(Ra, "밤마다 저승의 강을 건너, 아침마다 다시 떠오르는 태양이다."),
        new DialogueLine(Me, "...이게, 진짜 라..."),
        new DialogueLine(Ra, "너는 나의 낮을 걸었고, 나의 밤까지 들여다보았다."),
        new DialogueLine(Ra, "빛만 좇는 자는 결코 닿지 못하는 곳이다.")
    };

    public DialogueLine[] gift =
    {
        new DialogueLine(Ra, "받아라. 여섯 신의 큐브, 그 첫 번째 면이다.")
    };

    public DialogueLine[] farewell =
    {
        new DialogueLine(Ra, "큐브는 아직 잠들어 있다. 남은 다섯 신이 너를 시험할 것이다."),
        new DialogueLine(Ra, "중앙 홀로 돌아가라. 태양은 반드시 다시 떠오른다.")
    };
}
