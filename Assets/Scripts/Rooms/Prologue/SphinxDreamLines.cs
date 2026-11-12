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
        new DialogueLine(Sphinx, "너의 일은 하나다. 다섯 신의 신전에서 태양의 조각을 되찾아라."),
        new DialogueLine(Sphinx, "길을 열어 주마. 단, 태양을 알아보는 자만 들어갈 수 있다. 내 물음에 답하라.")
    };

    [Header("수수께끼 (3.2의 3~5)")]
    public DialogueLine riddle = new DialogueLine(Sphinx, "아침에는 풍뎅이, 한낮에는 매, 저녁에는 노인. 나는 무엇이냐?");
    public string[] options = { "태양", "나일강", "파라오" };
    [Tooltip("options에서 정답 번호 (0부터)")]
    public int correctIndex = 0;
    public DialogueLine[] wrong = { new DialogueLine(Sphinx, "다시 생각하라. 매일 동쪽에서 나서 서쪽으로 지는 자다.") };
    public DialogueLine[] right =
    {
        new DialogueLine(Me, "해... 태양이에요. 케프리, 라, 아툼."),
        new DialogueLine(Sphinx, "그렇다. 태양은 세 이름으로 하루를 걷는다.")
    };
    public DialogueLine[] afterLens = { new DialogueLine(Me, "렌즈... 이걸로 비석을 비추면 읽을 수 있을지도.") };

    [Header("[추가] 최종 스토리(B안): 정답 뒤 전체 이야기 벽화 (한 번만) -> 렌즈")]
    public string storyFlag = "F_PROLOGUE_STORY";
    public DialogueLine[] lensGive = { new DialogueLine(Sphinx, "이 렌즈를 가져가라. 숨은 글을 읽게 해 줄 것이다.") };

    [Header("비석 해독 뒤 (비석 글 자체는 SteleReader에 있음)")]
    public DialogueLine[] afterDecode = { new DialogueLine(Me, "여섯 신의 시험... 첫 문은 태양의 신. 라의 신전인가?") };
    public DialogueLine[] farewell =
    {
        new DialogueLine(Sphinx, "모래를 헤친 자여, 계단 아래가 중앙 홀이다."),
        new DialogueLine(Sphinx, "밤은 열두 시간. 신전은 여섯, 조각은 다섯. 서두르거라.")
    };
}
