using System;
using UnityEngine;

// 다이얼 위 방향 (화면 기준: 오른쪽 = 동, 왼쪽 = 서, Center = 가운데/머리 위)
public enum DialSide { None, East, West, Center }

// 그림자 길이
public enum ShadowLength { None, Short, Long }

// 시간 하나의 그림자 판정 (기획서 4.1 그림자 판정표 한 줄)
[Serializable]
public class ShadowRule
{
    public TimeSymbol time;

    [Tooltip("그림자가 눕는 쪽 (Center = 발밑)")]
    public DialSide shadowSide;

    public ShadowLength shadowLength;

    [Tooltip("해 위치 (같은 문제 3회 오답 힌트에 표시, Center = 머리 위)")]
    public DialSide sunSide;

    public ShadowRule() { }

    public ShadowRule(TimeSymbol time, DialSide shadowSide, ShadowLength shadowLength, DialSide sunSide)
    {
        this.time = time;
        this.shadowSide = shadowSide;
        this.shadowLength = shadowLength;
        this.sunSide = sunSide;
    }
}

// P1 태양 그림자 시계 데이터 (낮용 1개, 밤용 1개)
// 파일 우클릭(점 3개 메뉴) -> "기획서 값 채우기: 낮/밤"으로 기획서 값을 한 번에 넣을 수 있음
[CreateAssetMenu(fileName = "P1_ShadowClockData", menuName = "PharaohCube/Ra/P1 ShadowClock Data")]
public class ShadowClockPuzzleData : RaPuzzleData
{
    public const int RuleCount = 4;

    private static readonly TimeSymbol[] AllTimes =
    {
        TimeSymbol.Dawn, TimeSymbol.Noon, TimeSymbol.Dusk, TimeSymbol.Night
    };

    // 판정표가 잘못됐을 때 쓰는 기본값 (낮)
    private static readonly ShadowRule[] DayDefaults = CreateDayRules();

    [Header("P1 - 그림자 판정표 (새벽/정오/황혼/밤 한 번씩)")]
    [SerializeField] private ShadowRule[] rules = CreateDayRules();

    // 낮: 그림자는 해의 반대쪽
    public static ShadowRule[] CreateDayRules()
    {
        return new[]
        {
            new ShadowRule(TimeSymbol.Dawn, DialSide.West, ShadowLength.Long, DialSide.East),
            new ShadowRule(TimeSymbol.Noon, DialSide.Center, ShadowLength.Short, DialSide.Center),
            new ShadowRule(TimeSymbol.Dusk, DialSide.East, ShadowLength.Long, DialSide.West),
            new ShadowRule(TimeSymbol.Night, DialSide.None, ShadowLength.None, DialSide.None)
        };
    }

    // 밤(저승): 그림자가 해를 향함. 정오, 밤은 그대로 (기획서 6장)
    public static ShadowRule[] CreateNightRules()
    {
        return new[]
        {
            new ShadowRule(TimeSymbol.Dawn, DialSide.East, ShadowLength.Long, DialSide.East),
            new ShadowRule(TimeSymbol.Noon, DialSide.Center, ShadowLength.Short, DialSide.Center),
            new ShadowRule(TimeSymbol.Dusk, DialSide.West, ShadowLength.Long, DialSide.West),
            new ShadowRule(TimeSymbol.Night, DialSide.None, ShadowLength.None, DialSide.None)
        };
    }

    // 시간에 맞는 판정 (판정표에 없으면 낮 기본값)
    public ShadowRule GetRule(TimeSymbol time)
    {
        ShadowRule found = FindRule(rules, time);
        return found ?? FindRule(DayDefaults, time);
    }

    private static ShadowRule FindRule(ShadowRule[] list, TimeSymbol time)
    {
        if (list == null) return null;

        foreach (ShadowRule r in list)
        {
            if (r != null && r.time == time) return r;
        }
        return null;
    }

    // 4칸이고 4개 시간이 한 번씩만 들어 있는지
    public static bool IsValidRules(ShadowRule[] list)
    {
        if (list == null || list.Length != RuleCount) return false;

        foreach (TimeSymbol t in AllTimes)
        {
            int count = 0;
            foreach (ShadowRule r in list)
            {
                if (r != null && r.time == t) count++;
            }
            if (count != 1) return false;
        }
        return true;
    }

    private void OnValidate()
    {
        if (!IsValidRules(rules))
        {
            Debug.LogWarning($"[{name}] 판정표는 새벽/정오/황혼/밤이 한 번씩 들어간 4칸이어야 함 (빠진 시간은 낮 기본값 사용)");
        }
    }

    [ContextMenu("기획서 값 채우기: 낮")]
    private void FillDay()
    {
        SetCommon("", "F_RA_P1_DONE", true, true);
        rules = CreateDayRules();
        MarkDirty();
    }

    [ContextMenu("기획서 값 채우기: 밤")]
    private void FillNight()
    {
        SetCommon("F_RA_NIGHT_OPEN", "F_RA_NIGHT_P1_DONE", false, false);
        rules = CreateNightRules();
        MarkDirty();
    }
}