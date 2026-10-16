using System;
using UnityEngine;

// P4 동쪽에서 서쪽으로 데이터 (낮용 1개, 밤용 1개)
// 파일 우클릭(점 3개 메뉴) -> "기획서 값 채우기: 낮/밤"으로 기획서 값을 한 번에 넣을 수 있음
// 벽화 그림 순서(왼쪽부터 새벽-정오-황혼-밤)와 같아야 함. 낮 = 그대로, 밤 = 역순
[CreateAssetMenu(fileName = "P4_DayCycleData", menuName = "PharaohCube/Ra/P4 DayCycle Data")]
public class DayCyclePuzzleData : RaPuzzleData
{
    public const int SlotCount = 4;

    // 벽화 그림 순서 (왼쪽부터). 데이터가 잘못됐을 때도 이 순서 사용
    private static readonly TimeSymbol[] DefaultPanels =
    {
        TimeSymbol.Dawn, TimeSymbol.Noon, TimeSymbol.Dusk, TimeSymbol.Night
    };

    [Header("P4 - 벽화 패널")]
    [Tooltip("벽화 패널 순서 (왼쪽부터). 새벽/정오/황혼/밤이 한 번씩만 들어가야 함. 벽화 그림과 같은 순서로")]
    [SerializeField]
    private TimeSymbol[] panelOrder =
    {
        TimeSymbol.Dawn, TimeSymbol.Noon, TimeSymbol.Dusk, TimeSymbol.Night
    };

    [Tooltip("켜면 플레이할 때마다 패널 순서를 무작위로 섞음 (벽화가 고정 그림이면 끄기)")]
    [SerializeField] private bool randomizePanels = false;

    [Tooltip("켜면 정답이 패널의 역순 (밤: 서 -> 동, 저승 항해는 거꾸로)")]
    [SerializeField] private bool reverseAnswer = false;

    public bool RandomizePanels => randomizePanels;
    public bool ReverseAnswer => reverseAnswer;

    // 패널 순서 복사본 (잘못된 값이면 기본 순서)
    public TimeSymbol[] CopyPanelOrder()
    {
        TimeSymbol[] source = IsValidOrder(panelOrder) ? panelOrder : DefaultPanels;
        var copy = new TimeSymbol[SlotCount];
        Array.Copy(source, copy, SlotCount);
        return copy;
    }

    // 4칸이고 4개 시간이 한 번씩만 들어 있는지
    public static bool IsValidOrder(TimeSymbol[] order)
    {
        if (order == null || order.Length != SlotCount) return false;

        foreach (TimeSymbol s in DefaultPanels)
        {
            if (Array.IndexOf(order, s) < 0) return false;
        }
        return true;
    }

    private void OnValidate()
    {
        if (!IsValidOrder(panelOrder))
        {
            Debug.LogWarning($"[{name}] 벽화 패널은 새벽/정오/황혼/밤이 한 번씩 들어간 4칸이어야 함 (지금 값이면 기본 순서 사용)");
        }
    }

    [ContextMenu("기획서 값 채우기: 낮")]
    private void FillDay()
    {
        SetCommon("", "F_RA_P4_DONE", true, true);
        panelOrder = (TimeSymbol[])DefaultPanels.Clone();
        randomizePanels = false;
        reverseAnswer = false;
        MarkDirty();
    }

    [ContextMenu("기획서 값 채우기: 밤")]
    private void FillNight()
    {
        SetCommon("F_RA_NIGHT_P3_DONE", "F_RA_NIGHT_P4_DONE", false, false);
        panelOrder = (TimeSymbol[])DefaultPanels.Clone();
        randomizePanels = false;
        reverseAnswer = true; // 밤 정답: 밤 -> 황혼 -> 정오 -> 새벽
        MarkDirty();
    }
}