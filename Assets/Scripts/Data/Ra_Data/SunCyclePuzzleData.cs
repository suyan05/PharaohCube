using UnityEngine;

// P5 태양의 순환 데이터 (낮용 1개, 밤용 1개)
// 파일 우클릭(점 3개 메뉴) -> "기획서 값 채우기: 낮/밤"으로 기획서 값을 한 번에 넣을 수 있음
[CreateAssetMenu(fileName = "P5_SunCycleData", menuName = "PharaohCube/Ra/P5 SunCycle Data")]
public class SunCyclePuzzleData : RaPuzzleData
{
    // 데이터가 비었을 때 쓰는 기본 정답 (낮 A안)
    private static readonly SunButton[] DefaultAnswer =
    {
        SunButton.Rising, SunButton.Noon, SunButton.Setting, SunButton.Boat, SunButton.Return
    };

    [Header("P5 - 제단")]
    [Tooltip("이 플래그가 켜져야 제단이 깨어남 (비우면 처음부터 깨어 있음)")]
    [SerializeField] private string wakeFlag = "F_RA_CIRCUIT_4";

    [Tooltip("버튼 정답 순서")]
    [SerializeField]
    private SunButton[] answer =
    {
        SunButton.Rising, SunButton.Noon, SunButton.Setting, SunButton.Boat, SunButton.Return
    };

    public string WakeFlag => wakeFlag;

    public int AnswerLength => HasAnswer ? answer.Length : DefaultAnswer.Length;

    public SunButton AnswerAt(int index)
    {
        SunButton[] source = HasAnswer ? answer : DefaultAnswer;
        if (index < 0 || index >= source.Length) return source[0];
        return source[index];
    }

    private bool HasAnswer => answer != null && answer.Length > 0;

    [ContextMenu("기획서 값 채우기: 낮")]
    private void FillDay()
    {
        SetCommon("", "F_RA_P5_DONE", true, true);
        wakeFlag = "F_RA_CIRCUIT_4";
        answer = (SunButton[])DefaultAnswer.Clone(); // (1) -> (2) -> (3) -> (5) -> (6), A안
        MarkDirty();
    }

    [ContextMenu("기획서 값 채우기: 밤")]
    private void FillNight()
    {
        SetCommon("", "F_RA_NIGHT_P5_DONE", false, false);
        wakeFlag = "F_RA_NIGHT_P4_DONE";
        answer = new[]
        {
            SunButton.Setting, SunButton.Boat, SunButton.Night, SunButton.Return, SunButton.Rising
        }; // (3) -> (5) -> (4) -> (6) -> (1)
        MarkDirty();
    }
}
