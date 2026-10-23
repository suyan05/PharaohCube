using System;

// 스핑크스의 꿈 시작 연출 대사 (SphinxWakeCutscene Inspector의 Lines 칸에서 전부 바꿀 수 있음)
// 1인칭 대사는 괄호로 써서 "속으로 하는 말" 느낌
[Serializable]
public class SphinxWakeLines
{
    private const string Sphinx = "스핑크스";
    private const string Me = "나";

    public string voice = "...일어나거라.";

    // 1인칭 (눈 뜬 직후, 아직 몽롱함)
    public DialogueLine[] blurry = { new DialogueLine(Me, "(...뭐지... 눈앞이 뿌옇다...)") };
    public DialogueLine[] povSphinx = { new DialogueLine(Sphinx, "눈을 떴구나. 모래를 헤치고 내게 온 자여.") };
    public DialogueLine[] povReaction = { new DialogueLine(Me, "(거대한... 얼굴...?)") };

    // 3인칭 (정신 차린 뒤)
    public DialogueLine[] wake =
    {
        new DialogueLine(Me, "여기는...? 분명 구멍으로 떨어졌는데."),
        new DialogueLine(Me, "밤하늘이... 발밑에도 있어. 꿈인가?")
    };

    public DialogueLine[] greet =
    {
        new DialogueLine(Sphinx, "나는 지평선의 호루스, 이 신전의 문지기다."),
        new DialogueLine(Me, "스, 스핑크스가... 말을...?!"),
        new DialogueLine(Sphinx, "모래가 나를 삼키고 있다. 내 발을 덮은 모래를 걷어내라. 그러면 나도 너에게 보답하리라.")
    };
}
