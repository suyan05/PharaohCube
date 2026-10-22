using System.Collections.Generic;
using UnityEngine;

// 플레이어 횃불과 퍼즐을 이어 주는 연결점
// - 퍼즐은 횃불 코드를 직접 몰라도 TorchLink.Extinguish("이유")만 부르면 됨
// - 지금은 PlayerTorchTemp(임시 횃불)가 구현. 팀장 공통 횃불이 나오면 그 클래스가 ITorchLight만 구현하고 Register 하면 끝
public interface ITorchLight
{
    bool HasTorch { get; }   // 횃불을 가지고 있는지 (재가 되면 false)
    bool IsLit { get; }      // 지금 불이 붙어 있는지
    bool HasEmber { get; }   // 라의 불씨(히든) - 절대 꺼지지 않음
    void Extinguish(string reason);
    void Relight(string message);   // [추가] 횃불 제단·화로에서 다시 붙이기 (횃불을 받는 것도 포함)
}

public static class TorchLink
{
    private static ITorchLight current;

    public static ITorchLight Current => current;

    public static void Register(ITorchLight torch) => current = torch;

    public static void Unregister(ITorchLight torch)
    {
        if (current == torch) current = null;
    }

    // 퍼즐 처벌: 횃불 끄기 (횃불이 없으면 아무 일도 안 일어남)
    public static bool Extinguish(string reason)
    {
        if (current == null)
        {
            Debug.LogWarning($"[TorchLink] 등록된 횃불이 없어 끄지 못함 ({reason}) -> 플레이어에 PlayerTorchTemp 붙이기");
            return false;
        }
        if (!current.IsLit) return false;
        current.Extinguish(reason);
        return true;
    }

    public static bool IsLit => current != null && current.IsLit;

    // [추가] 퍼즐을 푸는 동안 횃불 타는 시간 멈춤 (여러 퍼즐이 동시에 요청해도 안전하게 집합으로 관리)
    private static readonly HashSet<Object> burnPausers = new HashSet<Object>();

    public static bool BurnPaused => burnPausers.Count > 0;

    public static void SetBurnPaused(Object owner, bool paused)
    {
        if (owner == null) return;
        bool changed = paused ? burnPausers.Add(owner) : burnPausers.Remove(owner);
        if (changed) Debug.Log($"[TorchLink] 타는 시간 {(BurnPaused ? "멈춤" : "다시 흐름")} ({owner.name}, 요청 {burnPausers.Count}개)");
    }
}
