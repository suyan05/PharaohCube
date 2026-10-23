using System;
using UnityEngine;

// 스핑크스의 꿈 대사 묶음 (기획서 3.2 대사 초안 + 연출용으로 몇 줄 추가)
// SphinxDreamDirector Inspector의 Lines 칸에서 전부 바꿀 수 있음
[Serializable]
public class SphinxDreamLines
{
    private const string Sphinx = "스핑크스";
    private const string Me = "나";

    [Header("앞발 모래")]
    public DialogueLine[] firstPaw = { new DialogueLine(Me, "모래 아래로 거대한 돌 발톱이 드러났다. 반대쪽도...") };
    public DialogueLine[] steleRevealed = { new DialogueLine(Me, "앞발 사이에서... 비석이 솟아올랐어!") };
    public DialogueLine[] askToRead =
    {
        new DialogueLine(Sphinx, "그것은 신들의 글. 인간의 눈으로는 읽을 수 없다."),
        new DialogueLine(Sphinx, "읽고 싶다면, 내 물음에 답하라.")
    };

    [Header("수수께끼 (3.2의 3~5)")]
    public DialogueLine riddle = new DialogueLine(Sphinx, "아침에는 쇠똥구리로 태어나고, 한낮에는 매가 되며, 저녁에는 노인이 되어 눕는다. 나는 누구인가?");
    public string[] options = { "태양", "나일강", "파라오" };
    [Tooltip("options에서 정답 번호 (0부터)")]
    public int correctIndex = 0;
    public DialogueLine[] wrong = { new DialogueLine(Sphinx, "다시 생각하라. 매일 동쪽에서 나서 서쪽으로 지는 자다.") };
    public DialogueLine[] right =
    {
        new DialogueLine(Sphinx, "그렇다. 케프리, 라, 아툼. 태양은 세 이름으로 하루를 걷는다."),
        new DialogueLine(Sphinx, "이 렌즈를 가져가라. 신들의 글을 읽을 수 있을 것이다.")
    };
    public DialogueLine[] afterLens = { new DialogueLine(Me, "렌즈... 이걸로 비석을 비추면 읽을 수 있을지도.") };

    [Header("비석 해독 뒤 (비석 글 자체는 SteleReader에 있음)")]
    public DialogueLine[] afterDecode = { new DialogueLine(Me, "여섯 신의 시험... 첫 문은 태양의 신. 라의 신전인가?") };
    public DialogueLine[] farewell =
    {
        new DialogueLine(Sphinx, "모래를 헤친 자여, 아래로 내려가라."),
        new DialogueLine(Sphinx, "태양이 너를 기다린다.")
    };
}
