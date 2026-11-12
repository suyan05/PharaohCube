using System;
using System.Collections;
using UnityEngine;

// 숨은 방 공통 흐름: 옆모습 통로를 직접 걸어감 -> 끝 방에서 등 뒤 시점 벽화 컷 -> 원래 방으로
//   yield return HiddenVault.Play(통로, 벽화 컷, 플래그, 끝나고 할 말);   (Cine.Begin ~ Cine.End 사이)
// - 다 보면 플래그가 켜짐: 라 F_RA_HIDDEN_VAULT / 호루스 F_HO_NEST_VAULT
// [확인 필요] 히든 아이템 보상은 다른 담당자 작업 -> 이 플래그를 보고 아이템을 주도록 연결하면 됨
public static class HiddenVault
{
    public const string RaFlag = "F_RA_HIDDEN_VAULT";
    public const string HorusFlag = "F_HO_NEST_VAULT";

    public static IEnumerator Play(SideWalkScene walk, MuralShot[] reveal, string flag, DialogueLine[] afterLines)
    {
        yield return SideWalkStage.Play(walk);
        yield return MuralStage.Play(reveal);
        yield return SideWalkStage.Release();
        Mark(flag);
        yield return Cine.Say(afterLines);
    }

    public static bool Seen(string flag)
    {
        GameManager gm = GameManager.Instance;
        return gm != null && gm.HasFlag(flag);
    }

    private static void Mark(string flag)
    {
        try
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && !string.IsNullOrEmpty(flag) && !gm.HasFlag(flag)) gm.SetFlag(flag);
        }
        catch (Exception e)
        {
            Debug.LogError($"[숨은 방] 플래그 저장 오류 ({flag}): {e.Message}");
        }
    }
}
