using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 호루스 퍼즐 개편 (빌더 단계, HorusRoomBuilder가 그림 연결 뒤에 부름)
// - H1 매의 거울: 관측대 = 작은 '매의 눈 받침' 도트 + HorusSunMirrors (거울 석상 5개, 날개 달린 해, 세트의 붉은 돌은 실행 중에 생김)
// - H3 우자트: 화면 퍼즐은 그대로(순서 규칙 추가), 방 안 그림만 작은 돌 탁자 도트로
// - H4 돌배 경주: 경기장 옆 작은 '경주의 비석' + HorusBoatRace (바위, 물, 배, 별 자리는 실행 중에 생김)
// - H5 하늘의 고리: 제단 + HorusSkyRings (고리 4겹, 레버 4개는 실행 중에 생김)
// - 예전 퍼즐 스크립트/화면(오버레이)은 지우지 않음. E로 화면을 여는 Station만 빼고 화면은 꺼 둠
// - 큰 퍼즐 그림이 방 안에 그대로 보이던 문제(이질감) -> 방 안엔 작은 도트 소품만
public static class HorusWorldPuzzles
{
    private const string Obj = "Assets/image/Horus/Object/";

    public static void Apply()
    {
        try
        {
            SunMirrors();
            WedjatTable();
            BoatRace();
            SkyRings();
            MoveNeighbors();
            Objectives();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HorusWorldPuzzles] 오류: {e}");
        }
    }

    // ================= H1 =================
    private static void SunMirrors()
    {
        GameObject h1 = Prepare("H1_MirrorWing", new Vector2(3.5f, 8.5f), Px("HO_EyeStand_Dust"), new Vector2(1.1f, 1.2f));
        if (h1 == null) return;
        Remove<MirrorWingStation>(h1);
        HorusSunMirrors p = Add<HorusSunMirrors>(h1);
        var so = new SerializedObject(p);
        Ref(so, "eyeStand", h1.GetComponent<SpriteRenderer>());
        Ref(so, "dustySprite", Px("HO_EyeStand_Dust"));
        Ref(so, "cleanSprite", Px("HO_EyeStand"));
        Ref(so, "litSprite", Px("HO_EyeStand_Lit"));
        Ref(so, "mirrorA", Px("HO_Mirror_A"));
        Ref(so, "mirrorB", Px("HO_Mirror_B"));
        Ref(so, "mirrorFlat", Px("HO_Mirror_Flat"));
        Ref(so, "sunSprite", Px("HO_SunDisc"));
        Ref(so, "setStoneSprite", Px("HO_SetStone"));
        Str(so, "clearFlag", "F_HO_H1_DONE");
        so.ApplyModifiedPropertiesWithoutUndo();
        Hide("H1_WingOverlay");
    }

    // ================= H3 (화면 퍼즐 유지, 방 안 그림만) =================
    private static void WedjatTable()
    {
        Prepare("H3_WedjatRestore", new Vector2(6.2f, 3.4f), Px("HO_WedjatTable"), new Vector2(1.6f, 1.2f));
    }

    // ================= H4 =================
    private static void BoatRace()
    {
        // 세트의 붉은 봉인 그림(H4_02)을 작게 = 경주의 비석
        GameObject h4 = Prepare("H4_StarPathLock", new Vector2(15.0f, 2.1f), HorusArtSetup.Art("H4/H4_02"), new Vector2(0.9f, 0.9f));
        if (h4 == null) return;
        Remove<StarPathLockStation>(h4);
        Remove<HorusSealWalk>(h4);
        HorusBoatRace p = Add<HorusBoatRace>(h4);
        var so = new SerializedObject(p);
        Ref(so, "rockSprite", Px("HO_Rock"));
        Ref(so, "waterSprite", Px("HO_Water"));
        Ref(so, "sunkSprite", Px("HO_Water_Sunk"));
        Ref(so, "socketSprite", Px("HO_StarSocket"));
        Ref(so, "woodSprite", Px("HO_Boat_Wood"));
        Ref(so, "setSprite", Px("HO_Boat_Set"));
        Ref(so, "sealSprite", Px("HO_SetStone"));
        Str(so, "clearFlag", "F_HO_H4_DONE");
        so.ApplyModifiedPropertiesWithoutUndo();
        Hide("H4_PathOverlay");
    }

    // ================= H5 =================
    private static void SkyRings()
    {
        GameObject h5 = HorusArtSetup.Find("H5_SkyAlign");
        if (h5 == null) return;
        Remove<SkyAlignStation>(h5);
        Remove<HorusSkyRing>(h5);
        HorusSkyRings p = Add<HorusSkyRings>(h5);
        var so = new SerializedObject(p);
        HorusArtSetup.SetSpriteList(so, "starSprites", new[] { "H5/H5_02", "H5/H5_03", "H5/H5_04", "H5/H5_05", "H5/H5_07" });
        Ref(so, "blankSprite", Px("HO_RingDot"));
        SerializedProperty levers = HorusArtSetup.Prop(so, "leverSprites");
        levers.arraySize = 4;
        for (int i = 0; i < 4; i++) levers.GetArrayElementAtIndex(i).objectReferenceValue = Px($"HO_Lever_{i + 1}");
        Ref(so, "cluesFrom", h5.GetComponent<SkyAlignPuzzle>());
        Str(so, "clearFlag", "F_HO_H5_DONE");
        so.ApplyModifiedPropertiesWithoutUndo();
        Hide("H5_SkyOverlay");
    }

    // ================= 길 막힘 정리 =================
    // 돌배 경기장(x 16~24, y 0~7) 안에 있던 것들을 밖으로. 경기장과 거울 구역 사이에 2칸 이상 길이 남게
    private static void MoveNeighbors()
    {
        Move("OBJ_H08_Mural", new Vector2(9.0f, 0.75f)); // 남쪽 벽, 입구 서쪽 (돌배 경기장 입구 앞 길을 비움)
        Move("OBJ_H04_MoonEyeMount", new Vector2(19.5f, 7.6f));
        Move("OBJ_H16_LapisStar_2", new Vector2(21.6f, 7.8f));
        GameObject mount = HorusArtSetup.Find("OBJ_H04_MoonEyeMount");
        if (mount != null) HorusWorldArt.FitObject(mount, HorusArtSetup.Art("Object/Object_02"), new Vector2(0.7f, 0.7f), Color.white);
    }

    // 목표 문구를 새 퍼즐에 맞게
    private static void Objectives()
    {
        HorusObjectiveGuide guide = Object.FindFirstObjectByType<HorusObjectiveGuide>(FindObjectsInactive.Include);
        if (guide == null) return;
        var so = new SerializedObject(guide);
        SerializedProperty steps = HorusArtSetup.Prop(so, "steps");
        if (steps == null) return;
        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(i);
            string flag = step.FindPropertyRelative("doneFlag").stringValue;
            SerializedProperty text = step.FindPropertyRelative("objective");
            if (flag == "F_HO_H1_DONE") text.stringValue = "깃털로 서쪽 관측대를 닦고, 거울 석상을 돌려 해의 빛을 매의 눈에 모으자";
            else if (flag == "F_HO_H4_DONE") text.stringValue = "매의 인장으로 남동쪽 경기장을 열고, 나무배 두 척을 별 자리까지 밀자";
            else if (flag == "F_HO_H5_DONE") text.stringValue = "제단 둘레 레버로 하늘 고리를 돌려, 별이 떠오른 순서대로 북쪽 선에 세우자";
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================= 도우미 =================
    // 오브젝트를 옮기고 작은 도트 그림으로 바꿈 + 충돌: 발밑 막힘 상자 + E용 조사 상자
    private static GameObject Prepare(string objName, Vector2 pos, Sprite art, Vector2 size)
    {
        GameObject go = HorusArtSetup.Find(objName);
        if (go == null)
        {
            Debug.LogWarning($"[HorusWorldPuzzles] {objName} 없음 - 건너뜀");
            return null;
        }
        go.transform.position = new Vector3(pos.x, pos.y, go.transform.position.z);
        foreach (Collider2D c in go.GetComponents<Collider2D>()) Object.DestroyImmediate(c);
        if (art != null) HorusWorldArt.FitObject(go, art, size, Color.white);
        float k = Mathf.Max(go.transform.localScale.x, 0.001f);
        HorusWorld.AddBox(go, new Vector2(size.x * 0.85f, size.y * 0.45f), k, false);
        BoxCollider2D reach = go.AddComponent<BoxCollider2D>();
        reach.isTrigger = true;
        reach.size = (size + new Vector2(0.4f, 0.4f)) / k;
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 0; // 발밑 높이로 앞뒤 정렬 (예전 '항상 아래' 설정 대신)
        YSortHint hint = go.GetComponent<YSortHint>();
        if (hint != null) hint.mode = YSortHint.Mode.Sort;
        EditorUtility.SetDirty(go);
        return go;
    }

    private static Sprite Px(string name) => HorusArtSetup.Pixel(Obj + name + ".png");

    private static T Add<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static void Remove<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c != null) Object.DestroyImmediate(c);
    }

    private static void Ref(SerializedObject so, string field, Object value)
    {
        SerializedProperty p = HorusArtSetup.Prop(so, field);
        if (p != null) p.objectReferenceValue = value;
    }

    private static void Str(SerializedObject so, string field, string value)
    {
        SerializedProperty p = HorusArtSetup.Prop(so, field);
        if (p != null) p.stringValue = value;
    }

    private static void Hide(string objName)
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
