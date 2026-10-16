using System;
using UnityEngine;

// 낮/밤 데이터를 바꿔 끼우는 라의 방 퍼즐 공통 부모
// - 자식은 낮 데이터, 밤 데이터 칸을 갖고 CurrentData로 지금 쓸 데이터를 알려줌
// - F_RA_NIGHT_OPEN 플래그가 켜지는 순간 클리어 상태를 되돌리고 ApplyPhase()로 밤 데이터 적용
public abstract class RaPhasedPuzzle : PuzzleBase
{
    // 낮 -> 밤으로 바뀐 직후 (화면 스크립트가 다시 그릴 때 사용)
    public event Action OnPhaseChanged;

    private bool subscribed;
    private bool warnedNightMissing;
    private bool warnedDayMissing;

    // 지금 쓰는 데이터 (낮이면 낮 데이터, 밤이면 밤 데이터)
    protected abstract RaPuzzleData CurrentData { get; }

    // 지금 데이터 기준으로 퍼즐 상태를 처음부터 다시 세팅
    protected abstract void ApplyPhase();

    public bool IsNight => RaPhaseState.IsNight;

    protected bool UseItemGate
    {
        get
        {
            RaPuzzleData d = CurrentData;
            return d == null || d.UseItemGate;
        }
    }

    protected bool GrantRewards
    {
        get
        {
            RaPuzzleData d = CurrentData;
            return d == null || d.GrantRewards;
        }
    }

    // 자동 힌트 사용 여부 (낮 = true, 밤 = false). 화면 스크립트도 읽어야 해서 public
    public bool UseAutoHint
    {
        get
        {
            RaPuzzleData d = CurrentData;
            return d == null || d.UseAutoHint;
        }
    }

    // 데이터에 플래그가 적혀 있으면 데이터 값, 비어 있으면 Inspector 값
    protected override string RequiredFlag
    {
        get
        {
            RaPuzzleData d = CurrentData;
            return d != null && !string.IsNullOrEmpty(d.RequiredFlag) ? d.RequiredFlag : base.RequiredFlag;
        }
    }

    protected override string ClearFlag
    {
        get
        {
            RaPuzzleData d = CurrentData;
            return d != null && !string.IsNullOrEmpty(d.ClearFlag) ? d.ClearFlag : base.ClearFlag;
        }
    }

    protected virtual void Start()
    {
        try
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning($"[{name}] GameManager가 없어서 낮/밤 전환을 못 받음");
                return;
            }
            GameManager.Instance.OnFlagChanged += HandleFlagChanged;
            subscribed = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 낮/밤 구독 오류: {e}");
        }
    }

    protected virtual void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnFlagChanged -= HandleFlagChanged;
        }
    }

    private void HandleFlagChanged(string flag)
    {
        if (flag != RaPhaseState.NightFlag) return;

        try
        {
            ResetClearedState();
            ApplyPhase();
            Debug.Log($"[{name}] 밤 데이터로 전환");
            OnPhaseChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 밤 전환 오류: {e}");
        }
    }

    // 낮/밤 데이터 고르기
    // 밤인데 밤 데이터가 비어 있으면 낮 데이터, 낮 데이터도 비어 있으면 코드 기본값(fallback)
    protected T PickData<T>(T day, T night, ref T fallback) where T : RaPuzzleData
    {
        if (RaPhaseState.IsNight)
        {
            if (night != null) return night;
            WarnOnce(ref warnedNightMissing, "Night Data가 비어 있음 -> 낮 데이터 사용");
        }

        if (day != null) return day;

        WarnOnce(ref warnedDayMissing, "Day Data가 비어 있음 -> 코드 기본값 사용");
        if (fallback == null) fallback = ScriptableObject.CreateInstance<T>();
        return fallback;
    }

    private void WarnOnce(ref bool warned, string message)
    {
        if (warned) return;
        warned = true;
        Debug.LogWarning($"[{name}] {message}");
    }
}