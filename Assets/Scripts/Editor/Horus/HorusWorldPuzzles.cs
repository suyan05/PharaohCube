using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// H4, H5를 '화면 퍼즐'에서 '몸으로 푸는 퍼즐'로 바꾸는 빌더 단계 (HorusRoomBuilder가 그림 연결 뒤에 부름)
// - H4 세트의 봉인: E로 화면을 여는 StarPathLockStation을 빼고 HorusSealWalk를 붙임. 별판을 걸어 다닐 수 있게 크게 + 트리거로
// - H5 호루스의 하늘: E로 화면을 여는 SkyAlignStation을 빼고 HorusSkyRing을 붙임 (판정은 그대로 SkyAlignPuzzle)
// - 예전 퍼즐 스크립트(StarPathLockPuzzle, SkyAlignPuzzle)와 화면(오버레이)은 지우지 않음. 화면만 꺼 둠
// - 별판이 커지면서 겹치던 물건(달의 눈 장착대, 청금석 별 2)을 옆으로 옮김
public static class HorusWorldPuzzles
{
    public static readonly Vector2 SealBoardPos = new Vector2(19.4f, 4.2f);
    public static readonly Vector2 SealBoardSize = new Vector2(5.6f, 5.6f); // 별 간격 약 1.1칸 (걸어서 밟을 수 있게)

    private static readonly string[] SkyStarArt = { "H5/H5_02", "H5/H5_03", "H5/H5_04", "H5/H5_05", "H5/H5_07" };

    public static void Apply()
    {
        try
        {
            SealWalk();
            SkyRing();
            MoveNeighbors();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HorusWorldPuzzles] 오류: {e}");
        }
    }

    // ================= H4 =================
    private static void SealWalk()
    {
        GameObject h4 = HorusArtSetup.Find("H4_StarPathLock");
        if (h4 == null)
        {
            Debug.LogWarning("[HorusWorldPuzzles] H4_StarPathLock 없음 - 건너뜀");
            return;
        }
        RemoveStation<StarPathLockStation>(h4);
        h4.transform.position = SealBoardPos;
        HorusWorldArt.FitObject(h4, HorusArtSetup.Art("H4/H4_01"), SealBoardSize, Color.white);
        foreach (Collider2D c in h4.GetComponents<Collider2D>()) c.isTrigger = true; // 위로 걸어 다님
        HorusSealWalk walk = h4.GetComponent<HorusSealWalk>();
        if (walk == null) walk = h4.AddComponent<HorusSealWalk>();
        var so = new SerializedObject(walk);
        HorusArtSetup.Prop(so, "board").objectReferenceValue = h4.GetComponent<SpriteRenderer>();
        HorusArtSetup.Prop(so, "sealSprite").objectReferenceValue = HorusArtSetup.Art("H4/H4_02");
        GameObject mural = HorusArtSetup.Find("OBJ_H08_Mural");
        HorusArtSetup.Prop(so, "mural").objectReferenceValue = mural != null ? mural.GetComponent<SpriteRenderer>() : null;
        HorusArtSetup.Prop(so, "clearFlag").stringValue = "F_HO_H4_DONE";
        so.ApplyModifiedPropertiesWithoutUndo();
        HideOverlay("H4_PathOverlay");
    }

    // ================= H5 =================
    private static void SkyRing()
    {
        GameObject h5 = HorusArtSetup.Find("H5_SkyAlign");
        if (h5 == null || h5.GetComponent<SkyAlignPuzzle>() == null)
        {
            Debug.LogWarning("[HorusWorldPuzzles] H5_SkyAlign / SkyAlignPuzzle 없음 - 건너뜀");
            return;
        }
        RemoveStation<SkyAlignStation>(h5);
        HorusSkyRing ring = h5.GetComponent<HorusSkyRing>();
        if (ring == null) ring = h5.AddComponent<HorusSkyRing>();
        var so = new SerializedObject(ring);
        HorusArtSetup.Prop(so, "puzzle").objectReferenceValue = h5.GetComponent<SkyAlignPuzzle>();
        HorusArtSetup.SetSpriteList(so, "starSprites", SkyStarArt);
        so.ApplyModifiedPropertiesWithoutUndo();
        HideOverlay("H5_SkyOverlay");
    }

    // ================= 겹치는 물건 옮기기 =================
    private static void MoveNeighbors()
    {
        Move("OBJ_H04_MoonEyeMount", new Vector2(23.2f, 1.8f)); // 별판 동쪽 끝(해 표시) 옆 -> 남동쪽 구석
        GameObject lapis = HorusArtSetup.Find("OBJ_H16_LapisStar_2");
        if (lapis != null && Vector2.Distance(lapis.transform.position, new Vector2(21f, 2.6f)) < 0.5f) Move("OBJ_H16_LapisStar_2", new Vector2(21.2f, 7.6f));
    }

    // ================= 도우미 =================
    private static void RemoveStation<T>(GameObject go) where T : Component
    {
        T station = go.GetComponent<T>();
        if (station != null) Object.DestroyImmediate(station);
    }

    private static void HideOverlay(string objName)
    {
        GameObject overlay = HorusArtSetup.Find(objName);
        if (overlay != null) overlay.SetActive(false);
    }

    private static void Move(string objName, Vector2 pos)
    {
        GameObject go = HorusArtSetup.Find(objName);
        if (go == null) return;
        go.transform.position = new Vector3(pos.x, pos.y, go.transform.position.z);
        EditorUtility.SetDirty(go);
    }
}
