using System;
using System.Collections.Generic;
using UnityEngine;

// 유물 카드 일러스트 연결 (아트: Assets/image/ui/ACH 의 Common_Card_Ill_ACH_* 24장)
// - 게임 중에 이름으로 불러올 수 있게 Resources/Notebook/ACH_*.jpg 로 복사해 둠 (원본은 그대로)
// - NotebookCardArt.Of("ACH_RA_P5") -> 그 업적 카드 그림 (없으면 null -> 예전처럼 글자 한 자로 표시)
// - 임포트 설정이 Sprite가 아니어도 되게 Texture2D로 읽어서 스프라이트를 만듦
public static class NotebookCardArt
{
    private const string Folder = "Notebook/ACH_";
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // 업적 ID -> 그림 파일 이름 (ACH_ 뒤)
    private static readonly Dictionary<string, string> Files = new Dictionary<string, string>
    {
        { "ACH_PRO_BRUSH", "PAINT_POTTERY" }, { "ACH_PRO_SCARAB", "SCARAB_AMULET" }, { "ACH_PRO_RIDDLE", "DREAM_STELE" },
        { "ACH_RA_P1", "SHADOW_CLOCK" }, { "ACH_RA_P2", "BRONZE_MIRROR" }, { "ACH_RA_P3", "WINGED_SUN" },
        { "ACH_RA_P4", "PAINTED_FRESCO" }, { "ACH_RA_P5", "SOLAR_BARGE" }, { "ACH_RA_NIGHT", "RAM_SUN_AMULET" },
        { "ACH_RA_CLEAR", "RA_PLATE" }, { "ACH_BAS_CLEAR", "BRONZE_CAT" }, { "ACH_THO_1", "SCRIBE_PALETTE" },
        { "ACH_THO_2", "IBIS_STATUE" }, { "ACH_THO_CLEAR", "ROSETTA_STONE" }, { "ACH_HOR_STAR", "DENDERA_ZODIAC" },
        { "ACH_HOR_CLEAR", "WEDJAT_EYE" }, { "ACH_SOB_CLEAR", "SOBEK_STATUE" }, { "ACH_ANU_CLEAR", "WEIGHING_HEART" },
        { "ACH_SP_GIFTS", "GOLD_PECTORAL" }, { "ACH_SP_TWICE", "AMMIT_RELIEF" }, { "ACH_SP_MUMMY", "ANTHROPOID_COFFIN" },
        { "ACH_SP_TRUE", "MAAT_FEATHER" }, { "ACH_SP_NOHINT", "OBELISK" }, { "ACH_SP_ALL", "GOLDEN_MASK" }
    };

    public static Sprite Of(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId) || !Files.TryGetValue(achievementId, out string file)) return null;
        if (cache.TryGetValue(achievementId, out Sprite s)) return s;
        try
        {
            Texture2D tex = Resources.Load<Texture2D>(Folder + file);
            s = tex != null ? Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : null;
            if (tex == null) Debug.LogWarning($"[수첩 카드] 그림 없음: Resources/{Folder}{file}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[수첩 카드] {achievementId} 그림 읽기 오류: {e.Message}");
            s = null;
        }
        cache[achievementId] = s;
        return s;
    }
}
