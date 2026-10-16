using System;
using UnityEngine;

// P3 일식 타일 복원 데이터 (낮용 1개, 밤용 1개)
// 파일 우클릭(점 3개 메뉴) -> "기획서 값 채우기: 낮/밤"으로 기획서 값을 한 번에 넣을 수 있음
[CreateAssetMenu(fileName = "P3_EclipseData", menuName = "PharaohCube/Ra/P3 Eclipse Data")]
public class EclipsePuzzleData : RaPuzzleData
{
    public const int PieceCount = 3; // 회전 조각 A, B, C

    [Header("P3 - 회전")]
    [Tooltip("켜면 시계 방향, 끄면 반시계 방향으로 90도씩 회전")]
    [SerializeField] private bool rotateClockwise = true;

    [Tooltip("정답 회전 횟수 (A, B, C 순서, 0~3). 위 회전 방향 기준")]
    [SerializeField] private int[] targets = { 1, 2, 3 };

    public bool RotateClockwise => rotateClockwise;

    public int TargetAt(int index)
    {
        if (targets == null || index < 0 || index >= targets.Length) return 0;
        return ((targets[index] % 4) + 4) % 4;
    }

    // 칸 수가 3개가 아니면 자동으로 맞춤
    private void OnValidate()
    {
        if (targets == null || targets.Length != PieceCount)
        {
            Array.Resize(ref targets, PieceCount);
        }
    }

    [ContextMenu("기획서 값 채우기: 낮")]
    private void FillDay()
    {
        SetCommon("", "F_RA_P3_DONE", true, true);
        SetAutoHint(true);
        rotateClockwise = true;
        targets = new[] { 1, 2, 3 };
        MarkDirty();
    }

    [ContextMenu("기획서 값 채우기: 밤")]
    private void FillNight()
    {
        SetCommon("F_RA_NIGHT_P1_DONE", "F_RA_NIGHT_P3_DONE", false, false);
        SetAutoHint(false); // 밤: 30초 붉은 윤곽 없음 -> 힌트 버튼
        rotateClockwise = false;
        targets = new[] { 3, 1, 2 };
        MarkDirty();
    }
}