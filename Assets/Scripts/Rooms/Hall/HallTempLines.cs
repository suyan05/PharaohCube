using System;

// [임시] 중앙 홀 대사 묶음 (HallTempDirector Inspector의 Lines 칸에서 전부 바꿀 수 있음)
// 목소리 대사는 총정리 기획서 5.2의 3번
[Serializable]
public class HallTempLines
{
    private const string Me = "나";
    private const string Voice = "???";

    public string title = "중앙 홀";
    public string subtitle = "여섯 신의 신전";

    public DialogueLine[] arrive =
    {
        new DialogueLine(Me, "계단 아래에... 이런 곳이 있었다니."),
        new DialogueLine(Me, "가운데 있는 건... 큐브?")
    };

    public DialogueLine[] voice =
    {
        new DialogueLine(Voice, "다음 신전에는 빛이 들지 않는다."),
        new DialogueLine(Voice, "라가 내어준 이 불을 가져가라.")
    };

    public DialogueLine[] needTorch = { new DialogueLine(Me, "불을 옮겨 담을 게 필요해. 저 걸이에 횃불이 있다.") };

    public DialogueLine[] gotTorch = { new DialogueLine(Me, "됐다. 이 불이면 어두운 곳도 갈 수 있겠어.") };

    public DialogueLine[] toDoor = { new DialogueLine(Me, "북쪽 문 너머가 첫 번째 신전... 태양의 신, 라.") };
}
