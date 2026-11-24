using System;
using UnityEngine;

// [추가] 아누비스 심판(메인 저울)에서 쓰는 다른 신전의 히든 아이템
// - 토트의 석판 (ITEM_THOTH_TABLET): 기회 3번을 다 쓰면 원래는 서브 퍼즐이 초기화됨
//   -> 석판이 있으면 석판이 부서지며 초기화를 막고 기회를 다시 채움 (1회용)
// - 호루스의 축복 (ITEM_HORUS_BLESSING): 저울 화면에 왼쪽 접시 무게 합계가 보임 (계산은 직접)
// AnubisPuzzleManager2D가 부름
public static class AnubisSecondChance
{
    public const string ThothTablet = "ITEM_THOTH_TABLET";
    public const string HorusBlessing = "ITEM_HORUS_BLESSING";

    public const string TabletMessage = "토트의 석판이 빛나며 부서졌다.\n서기관 토트가 심판의 기록을 고쳐 적는다. 심판의 기회가 다시 주어진다.";

    public static bool Has(string itemId)
    {
        return ItemInventory.Instance != null && ItemInventory.Instance.Has(itemId);
    }

    // 석판이 있으면 쓰고 true
    public static bool TryUseThothTablet()
    {
        try
        {
            if (!Has(ThothTablet)) return false;
            ItemInventory.Instance.Consume(ThothTablet);
            GameAudio.Play("sfx_th_chime", 0.8f, 0.8f);
            Debug.Log("[AnubisSecondChance] 토트의 석판 사용 -> 초기화 막음");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AnubisSecondChance] 석판 사용 오류: {e}");
            return false;
        }
    }

    // 저울 위 안내 글 (깃털 무게 + 남은 기회 + 호루스의 눈)
    public static string WeightLine(float feather, float left, int chancesLeft, int maxChances)
    {
        string text = $"진실의 깃털(마아트): <b>{feather:F1} kg</b>   심판의 기회 {chancesLeft}/{maxChances}";
        if (Has(HorusBlessing)) text += $"\n<color=#7FD4FF>호루스의 눈: 왼쪽 접시 {left:F1} kg</color>";
        if (Has(ThothTablet)) text += "\n<color=#E8C66A>토트의 석판이 품속에서 따뜻하다</color>";
        return text;
    }

    public static string FailLine(int chancesLeft)
    {
        return $"저울이 기울었다... 암무트가 입맛을 다신다.\n남은 심판의 기회: {chancesLeft}번";
    }
}
