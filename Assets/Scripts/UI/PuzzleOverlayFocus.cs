using System;
using System.Collections.Generic;
using UnityEngine;

// "지금 퍼즐 화면이 열려 있나?"를 한 곳에서 관리
// 오버레이가 열리면 Enter, 닫히면 Exit -> 목표 문구, 알림 UI, 일시정지 메뉴가 이걸 보고 판단
public static class PuzzleOverlayFocus
{
    private static readonly HashSet<int> openOverlays = new HashSet<int>();
    private static int lastClosedFrame = -1;

    // true = 퍼즐 화면 열림, false = 전부 닫힘 (상태가 바뀔 때만 호출)
    public static event Action<bool> OnFocusChanged;

    public static bool IsActive => openOverlays.Count > 0;

    // 이번 프레임에 퍼즐이 닫혔는지 (같은 Esc로 일시정지까지 열리는 것 방지용)
    public static bool ClosedThisFrame => lastClosedFrame == Time.frameCount;

    // 퍼즐이 열려 있거나 방금 닫혔으면 true -> Esc를 다른 곳에서 쓰면 안 됨
    public static bool IsEscapeConsumed => IsActive || ClosedThisFrame;

    // 플레이 시작할 때마다 초기화 (에디터에서 이전 플레이 값이 남지 않게)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        openOverlays.Clear();
        lastClosedFrame = -1;
        OnFocusChanged = null;
    }

    public static void Enter(UnityEngine.Object owner)
    {
        if (owner == null) return;

        bool wasActive = IsActive;
        openOverlays.Add(owner.GetInstanceID());
        if (!wasActive && IsActive) Notify(true);
    }

    public static void Exit(UnityEngine.Object owner)
    {
        if (owner == null) return;

        bool wasActive = IsActive;
        openOverlays.Remove(owner.GetInstanceID());
        if (wasActive && !IsActive)
        {
            lastClosedFrame = Time.frameCount;
            Notify(false);
        }
    }

    private static void Notify(bool active)
    {
        try
        {
            OnFocusChanged?.Invoke(active);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PuzzleOverlayFocus] 알림 중 오류: {e}");
        }
    }
}