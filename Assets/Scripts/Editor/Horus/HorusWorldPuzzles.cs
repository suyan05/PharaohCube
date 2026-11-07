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
// - [수정] 본당이 물건으로 막혀서 H1은 '해의 방'(북서쪽), H4는 '나일의 방'(동쪽)으로 옮김 (HorusRoomMap.SunRoom / NileRoom)
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
            SolidPillars();
            SteleOrder();
            SteleNearPuzzles();
            RoomSconces();
            IsisStatue();
            CorridorMurals();
            CorridorSconces();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HorusWorldPuzzles] 오류: {e}");
        }
    }

    // ================= H1 =================
    private static void SunMirrors()
    {
        // 해의 방: 거울 격자 왼쪽 아래 = (0, 19), 칸 1.5. 매의 눈 = 격자 (3, 3) 칸 = (5.25, 24.25)
        GameObject h1 = Prepare("H1_MirrorWing", new Vector2(5.25f, 24.25f), Px("HO_EyeStand_Dust"), new Vector2(1.1f, 1.2f));
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
        Ref(so, "blockSprite", Px("HO_Column"));
        HorusArtSetup.SetVector(so, "origin", new Vector2(0f, 19f));
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
        // 나일의 방: 경기장 왼쪽 아래 = (27, 1), 입구 = (27, 2). 비석은 경기장 왼쪽 빈 줄 (문에서 들어오는 길은 비움)
        GameObject h4 = Prepare("H4_StarPathLock", new Vector2(25.9f, 7.2f), HorusArtSetup.Art("H4/H4_02"), new Vector2(0.9f, 0.9f));
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
        HorusArtSetup.SetVector(so, "origin", new Vector2(27f, 1f));
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
        // [수정] 별자리 그림 -> 해/달/매/왕관/세트의 붉은 별 (순서: 0 왕관, 1 해, 2 달, 3 매, 4 붉은 별)
        SerializedProperty stars = HorusArtSetup.Prop(so, "starSprites");
        string[] icons = { "HO_Sym_Crown", "HO_Sym_Sun", "HO_Sym_Moon", "HO_Sym_Falcon", "HO_Sym_SetStar" };
        stars.arraySize = icons.Length;
        for (int i = 0; i < icons.Length; i++) stars.GetArrayElementAtIndex(i).objectReferenceValue = Px(icons[i]);
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
        Move("OBJ_H16_LapisStar_2", new Vector2(20.6f, 6.4f)); // 성도판을 안쪽으로 옮기면서 겹치지 않게
        Move("H2_ConstellationChart", new Vector2(22.5f, 8f)); // [수정] 동쪽 벽 너머가 나일의 방이 되면서 벽에 걸쳐 보이던 것 -> 본당 안쪽으로
        GameObject mount = HorusArtSetup.Find("OBJ_H04_MoonEyeMount");
        if (mount != null) HorusWorldArt.FitObject(mount, HorusArtSetup.Art("Object/Object_02"), new Vector2(0.7f, 0.7f), Color.white);
    }

    // [추가] 기둥: 몸통 전체를 막음 (예전엔 밑동만 막혀서 기둥 위로 지나가는 것처럼 보였음)
    private static void SolidPillars()
    {
        foreach (string n in new[] { "Pillar_NW", "Pillar_NE", "Pillar_SW", "Pillar_SE" })
        {
            GameObject go = HorusArtSetup.Find(n);
            if (go == null) continue;
            foreach (Collider2D c in go.GetComponents<Collider2D>()) Object.DestroyImmediate(c);
            float k = Mathf.Max(go.transform.localScale.x, 0.001f);
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 2.5f) / k;
            // [수정] 기둥 그림의 기준점이 아래쪽이라 (0, 0) 근처에 두면 상자가 그림 아래로 내려감 -> 그림 한가운데에 맞춤
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            box.offset = sr != null && sr.sprite != null ? (Vector2)sr.sprite.bounds.center : Vector2.zero;
            EditorUtility.SetDirty(go);
        }
    }

    // [추가] 단서 비석은 매가 안내하는 차례가 와야 '!'가 뜨고 읽힘 (ClueStele.requiredFlag)
    // 순서: 성좌1 -> H1 -> 성좌2 -> H3 -> 성좌3 -> H4 -> 성좌4 -> H5
    private static void SteleOrder()
    {
        SetRequired("OBJ_HS2_ClueStele", "");             // 별의 길 (성도판 처음부터)
        SetRequired("OBJ_HS1_ClueStele", "F_HO_CHART_1");  // 해의 눈 (H1 차례)
        SetRequired("OBJ_HS3_ClueStele", "F_HO_CHART_2");  // 찢긴 눈 (H3 차례)
        SetRequired("OBJ_H11_WarningSlab", "F_HO_CHART_4"); // 경고 석판 (H5 차례)
    }

    private static void SetRequired(string objName, string flag)
    {
        GameObject go = HorusArtSetup.Find(objName);
        ClueStele stele = go != null ? go.GetComponent<ClueStele>() : null;
        if (stele == null) return;
        var so = new SerializedObject(stele);
        SerializedProperty p = so.FindProperty("requiredFlag");
        if (p == null) return;
        p.stringValue = flag;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // [수정] 비석이 어느 퍼즐 것인지 헷갈린다는 의견 -> 각 비석을 자기 퍼즐 바로 옆(방 입구)으로 옮기고 머리 위에 퍼즐 문양
    // 기둥 횃불과 떨어지므로 '횃불을 켜야 읽힘'은 끄고, 순서는 매 안내(requiredFlag)로만 잠금
    private static void SteleNearPuzzles()
    {
        SteleAt("OBJ_HS1_ClueStele", new Vector2(3.6f, 14.7f), "HO_Sym_Sun");      // H1: 해의 방 문(북서) 앞
        SteleAt("OBJ_HS2_ClueStele", new Vector2(22.6f, 10.4f), "HO_Sym_Star");    // H2: 동쪽 벽 성도판 위
        SteleAt("OBJ_HS3_ClueStele", new Vector2(4.3f, 4.4f), "HO_Sym_Eye");       // H3: 우자트 석탁 왼쪽
        SteleAt("OBJ_H11_WarningSlab", new Vector2(10.0f, 8.4f), "HO_Sym_Crown");  // H5: 제단 남서쪽 (가운데 길은 비움)
    }

    private static void SteleAt(string objName, Vector2 pos, string emblem)
    {
        GameObject go = HorusArtSetup.Find(objName);
        if (go == null)
        {
            Debug.LogWarning($"[HorusWorldPuzzles] {objName} 없음 - 비석 이동 건너뜀");
            return;
        }
        go.transform.position = new Vector3(pos.x, pos.y, go.transform.position.z);
        ClueStele stele = go.GetComponent<ClueStele>();
        if (stele != null)
        {
            var so = new SerializedObject(stele);
            SerializedProperty light = so.FindProperty("needLight");
            if (light != null) light.boolValue = false;
            SerializedProperty torches = so.FindProperty("roomTorches");
            if (torches != null) torches.arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        Emblem(go, emblem);
        EditorUtility.SetDirty(go);
    }

    // 비석 머리 위에 떠 있는 작은 퍼즐 문양 (항상 위 정렬)
    private static void Emblem(GameObject stele, string icon)
    {
        Sprite s = Px(icon);
        SpriteRenderer host = stele.GetComponent<SpriteRenderer>();
        if (s == null || host == null) return;
        var go = new GameObject($"{stele.name}_Emblem");
        go.transform.position = new Vector3(host.bounds.center.x, host.bounds.max.y + 0.3f, 0f);
        go.transform.localScale = Vector3.one * (0.42f / s.bounds.size.x);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingLayerName = host.sortingLayerName;
        sr.sortingOrder = 21;
    }

    // [추가] 입구 전실 이시스 여신상: E -> 이시스가 짧게 나타나 벽화로 기억을 보여 줌 (들어오자마자 나오던 연출 대신)
    // 자리: 전실 북서쪽 모서리 (북쪽 문 x 11~13을 막지 않게)
    private static void IsisStatue()
    {
        Sprite art = Px("HO_IsisStatue");
        var go = new GameObject("OBJ_HA0_IsisStatue");
        go.transform.position = new Vector3(8.9f, -2.9f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = art;
        sr.sortingLayerName = string.IsNullOrEmpty(HorusRoomObjects.Layer) ? "Default" : HorusRoomObjects.Layer;
        Vector2 size = new Vector2(0.95f, 1.9f);
        if (art != null) HorusWorldArt.FitObject(go, art, size, Color.white);
        float k = Mathf.Max(go.transform.localScale.x, 0.001f);
        HorusWorld.AddBox(go, new Vector2(0.9f, 0.4f), k, false);
        BoxCollider2D reach = go.AddComponent<BoxCollider2D>();
        reach.isTrigger = true;
        reach.size = (size + new Vector2(0.5f, 0.3f)) / k;
        reach.offset = art != null ? (Vector2)art.bounds.center : Vector2.zero;
        HorusIsisStatue statue = go.AddComponent<HorusIsisStatue>();
        var so = new SerializedObject(statue);
        Ref(so, "director", Object.FindFirstObjectByType<HorusDirector>(FindObjectsInactive.Include));
        so.ApplyModifiedPropertiesWithoutUndo();
        IsisObjective(sr);
        EditorUtility.SetDirty(go);
    }

    // 목표 HUD 맨 앞에 '이시스 여신상 살펴보기' 단계 (이미 있으면 그대로)
    private static void IsisObjective(SpriteRenderer statue)
    {
        HorusObjectiveGuide guide = Object.FindFirstObjectByType<HorusObjectiveGuide>(FindObjectsInactive.Include);
        if (guide == null) return;
        var so = new SerializedObject(guide);
        SerializedProperty steps = HorusArtSetup.Prop(so, "steps");
        if (steps == null) return;
        for (int i = 0; i < steps.arraySize; i++)
            if (steps.GetArrayElementAtIndex(i).FindPropertyRelative("doneFlag").stringValue == "F_HO_INTRO_PLAYED") return;
        steps.InsertArrayElementAtIndex(0);
        SerializedProperty step = steps.GetArrayElementAtIndex(0);
        step.FindPropertyRelative("doneFlag").stringValue = "F_HO_INTRO_PLAYED";
        step.FindPropertyRelative("objective").stringValue = "입구 전실의 이시스 여신상을 살펴보자";
        step.FindPropertyRelative("pathToLight").objectReferenceValue = null;
        step.FindPropertyRelative("target").objectReferenceValue = statue;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // [추가] 입구 복도 찢긴 벽화 4점: 벽화 타일(HorusRoomMap.PaintMurals) 앞에 조사 상자 + 안 본 벽화 반짝임
    private static readonly string[] MuralKeys = { HorusMuralScenes.Wall1Key, HorusMuralScenes.Wall2Key, HorusMuralScenes.Wall3Key, HorusMuralScenes.Wall4Key };
    private static readonly string[] MuralTitles = { "찢긴 벽화 - 왕좌와 그림자", "찢긴 벽화 - 커다란 눈", "찢긴 벽화 - 강 위의 배", "찢긴 벽화 - 우는 여신" };

    private static void CorridorMurals()
    {
        for (int i = 0; i < MuralKeys.Length; i++)
        {
            Vector2 c = HorusRoomMap.MuralCenter(i);
            var go = new GameObject($"OBJ_HC{i + 1}_MuralWall");
            go.transform.position = new Vector3(c.x, c.y - 0.8f, 0f); // 벽 바로 앞 바닥 쪽 (플레이어가 서는 곳)
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(3.6f, 1.4f);
            HorusMuralWall wall = go.AddComponent<HorusMuralWall>();
            var so = new SerializedObject(wall);
            Str(so, "muralKey", MuralKeys[i]);
            Str(so, "title", MuralTitles[i]);
            Str(so, "seenFlag", $"F_HO_WALL_{i + 1}_SEEN");
            Ref(so, "glint", Glint(go.transform, new Vector2(c.x, c.y + 0.4f)));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(go);
        }
    }

    // 아직 안 본 벽화 위에 은은하게 깜빡이는 빛 (HorusMuralWall이 깜빡이게 함)
    private static SpriteRenderer Glint(Transform parent, Vector2 pos)
    {
        var go = new GameObject("Glint");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(2.6f, 1.2f, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = HorusRoomObjects.Circle;
        sr.color = new Color(1f, 0.85f, 0.5f, 0.3f);
        sr.sortingLayerName = string.IsNullOrEmpty(HorusRoomObjects.Layer) ? "Default" : HorusRoomObjects.Layer;
        sr.sortingOrder = 22;
        return sr;
    }

    // 복도 벽 횃대 (벽화 사이사이). 장식이라 끌 수 없음
    private static void CorridorSconces()
    {
        foreach (float x in new[] { -7.5f, -1.5f, 3.5f, 9.5f, 13.5f, 19.5f })
            Sconce(new Vector2(x, HorusRoomMap.MuralTopY + 0.2f), 5f, "HO_CorridorSconce");
    }

    private static void Sconce(Vector2 p, float radius, string objName)
    {
        Sprite on = Px("HO_Sconce_On");
        var go = new GameObject(objName);
        go.transform.position = p;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = on;
        sr.sortingLayerName = string.IsNullOrEmpty(HorusRoomObjects.Layer) ? "Default" : HorusRoomObjects.Layer;
        sr.sortingOrder = 21;
        if (on != null) go.transform.localScale = Vector3.one * (0.56f / on.bounds.size.x);
        var light = new GameObject("Light", typeof(UnityEngine.Rendering.Universal.Light2D)).GetComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.transform.position = p + Vector2.up * 0.3f;
        light.transform.SetParent(go.transform, true);
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = new Color(1f, 0.78f, 0.5f);
        light.intensity = 0.9f;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = 0.4f;
    }

    // [추가] 퍼즐 방 북쪽 벽의 켜진 횃대 (어두워서 안 보이던 문제). 장식이라 끌 수 없음
    private static void RoomSconces()
    {
        Sprite on = Px("HO_Sconce_On");
        foreach (Vector2 p in new[] { new Vector2(2f, 31.7f), new Vector2(7f, 31.7f), new Vector2(12f, 31.7f),
                                      new Vector2(28f, 12.7f), new Vector2(33f, 12.7f), new Vector2(38f, 12.7f) })
        {
            var go = new GameObject("HO_RoomSconce");
            go.transform.position = p;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = on;
            sr.sortingLayerName = string.IsNullOrEmpty(HorusRoomObjects.Layer) ? "Default" : HorusRoomObjects.Layer; // 플레이어와 같은 레이어
            sr.sortingOrder = 21; // 벽 장식 (항상 위)
            if (on != null) go.transform.localScale = Vector3.one * (0.56f / on.bounds.size.x);
            var light = new GameObject("Light", typeof(UnityEngine.Rendering.Universal.Light2D)).GetComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.transform.position = p + Vector2.up * 0.3f;
            light.transform.SetParent(go.transform, true);
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            light.color = new Color(1f, 0.78f, 0.5f);
            light.intensity = 1f;
            light.pointLightOuterRadius = 7f;
            light.pointLightInnerRadius = 0.5f;
        }
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
            if (flag == "F_HO_H1_DONE") text.stringValue = "북서쪽 '해의 방'에서 깃털로 관측대를 닦고, 거울을 돌려 해의 빛을 매의 눈에 모으자";
            else if (flag == "F_HO_H4_DONE") text.stringValue = "동쪽 '나일의 방'에서 매의 인장으로 경기장을 열고, 나무배 두 척을 강 건너 별 자리로 밀자";
            else if (flag == "F_HO_H5_DONE") text.stringValue = "제단 둘레 레버로 하늘 고리를 돌려, 북쪽 칸에 비치는 그림(해, 달, 매, 왕관)을 맞추자";
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
