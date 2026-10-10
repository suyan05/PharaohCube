using UnityEngine;

// P2 태양의 빛 회로 데이터 (낮용 1개, 밤용 1개)
// 파일 우클릭(점 3개 메뉴) -> "기획서 값 채우기: 낮/밤"으로 기획서 값을 한 번에 넣을 수 있음
[CreateAssetMenu(fileName = "P2_CircuitData", menuName = "PharaohCube/Ra/P2 Circuit Data")]
public class CircuitBoardPuzzleData : RaPuzzleData
{
    [Header("P2 - 회로판")]
    [Tooltip("켜면 4단계를 모두 마친 상태(파란 빛 점등)로 시작하고 조작을 막음 (밤 버전: 회로판 사용 안 함)")]
    [SerializeField] private bool startCompleted = false;

    public bool StartCompleted => startCompleted;

    [ContextMenu("기획서 값 채우기: 낮")]
    private void FillDay()
    {
        SetCommon("", "", true, true); // 플래그는 비워서 기존 Inspector 값 사용
        startCompleted = false;
        MarkDirty();
    }

    [ContextMenu("기획서 값 채우기: 밤")]
    private void FillNight()
    {
        SetCommon("", "", false, false);
        startCompleted = true;
        MarkDirty();
    }
}
