using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 퍼즐 화면(H1~H5)에 호루스 그림 넣기 (HorusArtSetup이 부름)
// 판 그림 크기는 그림 속 별 자리 간격을 재서 맞춤:
//   H1_01 별 간격 184px (5x4)  -> 화면 간격 90  -> 판 501 x 401
//   H2_01 별 간격 129px (5x5)  -> 화면 간격 80  -> 판 630 x 630 (그림이 1015px로 잘려 있음)
//   H4_01 별 간격 가로 157 / 세로 134px (3x3) -> 화면 가로 110 / 세로 93.3 -> 판 535 x 535
public static class HorusUiArt
{
    private static readonly string[] StarArts = { "H2/H2_star_basic", "H2/H2_star_off", "H2/H2_star_Dark clouds", "H2/H2_star_on" };

    public static int Apply()
    {
        int count = 0;
        count += LinkBoard("H1_WingOverlay", "H1/H1_01", new Vector2(501f, 401f), Vector2.zero, 90f, 0f, 40f);
        count += LinkBoard("H2_ChartOverlay", "H2/H2_01", new Vector2(630f, 630f), new Vector2(3f, 0f), 80f, 0f, 46f);
        count += LinkBoard("H4_PathOverlay", "H4/H4_01", new Vector2(535f, 535f), new Vector2(3f, -5f), 110f, 93.3f, 54f);
        count += LinkChartCards();
        count += LinkWingCovers();
        count += LinkPathLock();
        count += HorusWedjatArt.Apply();
        count += LinkSky();
        return count;
    }

    // ================= 별 판 (H1, H2, H4 공용 StarBoardView) =================
    private static int LinkBoard(string overlay, string boardArt, Vector2 boardSize, Vector2 offset,
        float spacing, float spacingY, float starSize)
    {
        StarBoardView board = Board(overlay);
        if (board == null) return 0;
        var so = new SerializedObject(board);
        HorusArtSetup.SetSprite(so, "normalSprite", StarArts[0]);
        HorusArtSetup.SetSprite(so, "darkSprite", StarArts[1]);
        HorusArtSetup.SetSprite(so, "cloudSprite", StarArts[2]);
        HorusArtSetup.SetSprite(so, "litSprite", StarArts[3]);
        HorusArtSetup.SetSprite(so, "boardSprite", boardArt);
        HorusArtSetup.SetVector(so, "boardSize", boardSize);
        HorusArtSetup.SetVector(so, "boardOffset", offset);
        HorusArtSetup.SetFloat(so, "spacing", spacing);
        HorusArtSetup.SetFloat(so, "spacingY", spacingY);
        HorusArtSetup.SetFloat(so, "starSize", starSize);
        so.ApplyModifiedPropertiesWithoutUndo();
        return 1;
    }

    private static StarBoardView Board(string overlay)
    {
        foreach (StarBoardView b in HorusArtSetup.All<StarBoardView>())
        {
            if (b.gameObject.name == overlay) return b;
        }
        Debug.LogWarning($"[HorusUiArt] {overlay} 의 StarBoardView를 못 찾음");
        return null;
    }

    // ================= H2 성좌 도판 (메스케티우, 사흐, 소프데트, 우자트) =================
    private static int LinkChartCards()
    {
        ConstellationChartPuzzle puzzle = Object.FindFirstObjectByType<ConstellationChartPuzzle>(FindObjectsInactive.Include);
        GameObject overlay = HorusArtSetup.Find("H2_ChartOverlay");
        if (puzzle == null || overlay == null) return 0;
        Image card = UiImage(overlay.transform, "ChartCard", new Vector2(480f, 40f), new Vector2(240f, 240f), null);
        card.gameObject.SetActive(false); // 퍼즐이 흐린 선과 같이 켬
        var so = new SerializedObject(puzzle);
        HorusArtSetup.Prop(so, "chartCard").objectReferenceValue = card;
        HorusArtSetup.SetSpriteList(so, "stageCards", new[] { "H2/H2_05", "H2/H2_04", "H2/H2_03", "H2/H2_02" });
        so.ApplyModifiedPropertiesWithoutUndo();
        return 1;
    }

    // ================= H1 먼지 덮개 (깃털 쓰기 전) + 완성 매 그림 (클리어 후) =================
    private static int LinkWingCovers()
    {
        MirrorWingPuzzle puzzle = Object.FindFirstObjectByType<MirrorWingPuzzle>(FindObjectsInactive.Include);
        GameObject overlay = HorusArtSetup.Find("H1_WingOverlay");
        if (puzzle == null || overlay == null) return 0;
        var pso = new SerializedObject(puzzle);
        string unlocked = FlagOr(pso, "unlockedFlag", "F_HO_H1_UNLOCKED");
        string done = FlagOr(pso, "clearFlag", "F_HO_H1_DONE");

        Image dust = UiImage(overlay.transform, "DustCover", Vector2.zero, new Vector2(530f, 424f), HorusArtSetup.Art("H1/H1_02"));
        dust.raycastTarget = true; // 먼지가 있는 동안 별을 못 누름
        SetFlagImage(dust, unlocked, false);
        Image falcon = UiImage(overlay.transform, "ClearFalcon", Vector2.zero, new Vector2(380f, 277f), HorusArtSetup.Art("H1/H1_03"));
        SetFlagImage(falcon, done, true);
        PlaceAbove(overlay.transform, "StarContainer", dust.transform, falcon.transform);
        return 2;
    }

    private static string FlagOr(SerializedObject so, string field, string fallback)
    {
        SerializedProperty p = so.FindProperty(field);
        return p != null && !string.IsNullOrEmpty(p.stringValue) ? p.stringValue : fallback;
    }

    private static void SetFlagImage(Image img, string flag, bool showWhenOn)
    {
        HorusFlagImage fi = img.GetComponent<HorusFlagImage>();
        if (fi == null) fi = img.gameObject.AddComponent<HorusFlagImage>();
        var so = new SerializedObject(fi);
        HorusArtSetup.Prop(so, "flag").stringValue = flag;
        HorusArtSetup.Prop(so, "showWhenFlagOn").boolValue = showWhenOn;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 별 묶음 바로 위(= 글자, 버튼보다는 아래)에 놓음
    private static void PlaceAbove(Transform parent, string anchorName, params Transform[] items)
    {
        Transform anchor = parent.Find(anchorName);
        int index = anchor != null ? anchor.GetSiblingIndex() + 1 : parent.childCount - 1;
        foreach (Transform t in items) t.SetSiblingIndex(Mathf.Min(index++, parent.childCount - 1));
    }

    // ================= H4 붉은 봉인 + 매의 비행 벽화 =================
    private static int LinkPathLock()
    {
        StarPathLockView view = Object.FindFirstObjectByType<StarPathLockView>(FindObjectsInactive.Include);
        if (view == null) return 0;
        var so = new SerializedObject(view);
        HorusArtSetup.SetSprite(so, "sealSprite", "H4/H4_02");
        HorusArtSetup.SetSprite(so, "muralSprite", "H4/H4_03");
        HorusArtSetup.SetVector(so, "muralSize", new Vector2(800f, 240f));
        HorusArtSetup.SetFloat(so, "muralY", -400f);
        HorusArtSetup.SetFloat(so, "compassOffset", 300f);
        so.ApplyModifiedPropertiesWithoutUndo();
        return 1;
    }

    // ================= H5 제단 + 별 6개 (Stars 순서: 메스케티우, 사흐, 소프데트, 우자트, 붉은 별, 매) =================
    private static int LinkSky()
    {
        SkyAlignView view = Object.FindFirstObjectByType<SkyAlignView>(FindObjectsInactive.Include);
        if (view == null) return 0;
        var so = new SerializedObject(view);
        HorusArtSetup.SetSprite(so, "altarSprite", "H5/H5_01");
        HorusArtSetup.SetVector(so, "altarSize", new Vector2(853f, 640f));
        HorusArtSetup.SetSpriteList(so, "starSprites",
            new[] { "H5/H5_02", "H5/H5_03", "H5/H5_04", "H5/H5_05", "H5/H5_07", "H5/H5_06" });
        HorusArtSetup.SetFloat(so, "artStarSize", 104f);
        so.ApplyModifiedPropertiesWithoutUndo();
        return 1;
    }

    // ================= 도우미 =================
    // 이름이 같은 자식이 있으면 다시 씀 (여러 번 눌러도 하나만)
    public static Image UiImage(Transform parent, string objName, Vector2 pos, Vector2 size, Sprite sprite)
    {
        Transform existing = parent.Find(objName);
        GameObject go = existing != null ? existing.gameObject : new GameObject(objName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = Color.white;
        return img;
    }
}
