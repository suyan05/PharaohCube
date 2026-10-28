#if UNITY_EDITOR
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// [추가] 메뉴: Tools/Bastet/★ 오브젝트·퍼즐 그림 입히기 (비석·상자·봉인석·신상·B3·B4)
// 임시 그림(Test/Object의 Slate, situation_on, P5_on, 사막 타일)과 보라색 팝업을 바스테트 그림으로 바꿈
//  - 비석(BastetClueStele)        <- Bastet_Obj_Stele (1x2칸, 바닥 기준)
//  - B3/B4 퍼즐 상자(제단)         <- Bastet_Obj_Altar_Necklace / _Claw (1.5칸)
//  - 방 입구 봉인석(BastetRoomGate) <- Bastet_Obj_SealStone (원래 크기만큼 타일처럼 반복)
//  - 메인 신상(B5 시작 석상)        <- Bastet_Obj_Shrine + 눈 빛 + 머리 위 해/달 원반
//  - B3 목걸이 / B4 발톱 팝업       <- Assets/image/Bastet/UI/Puzzle/ 그림 (판, 조각, 눈 보석, 발톱 칸, 버튼)
// 위치는 "그림 바닥"이 원래 그림 바닥과 같게 맞춤 (자식 오브젝트는 제자리 유지). 여러 번 눌러도 안전
public static class BastetObjectArtApplier
{
    private const string W = BastetArtImport.Folder;   // 월드 그림
    private const string U = BastetArtImport.UiFolder; // 팝업 그림

    private static readonly Color Ink = new Color(0.23f, 0.14f, 0.06f);       // 제목 글자 (파피루스 위)
    private static readonly Color InkSoft = new Color(0.42f, 0.25f, 0.07f);   // 안내 글자
    private static readonly Color Paper = new Color(0.96f, 0.92f, 0.82f);     // 버튼 글자

    [MenuItem("Tools/Bastet/★ 오브젝트·퍼즐 그림 입히기 (비석·상자·봉인석·신상·B3·B4)", priority = 3)]
    private static void Apply()
    {
        if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("그림 입히기", "Play를 멈춘 뒤 눌러 주세요.", "확인"); return; }
        var log = new StringBuilder("[BastetObjectArtApplier] 오브젝트·퍼즐 그림 입히기\n");
        FixImports();

        // [수정] 한 부분에서 오류가 나도 나머지는 계속 (오류는 빨간 로그로 이유를 보여 줌)
        int n = 0;
        n += Safe("비석", Steles, log);
        n += Safe("퍼즐 상자", Altars, log);
        n += Safe("봉인석", Gates, log);
        n += Safe("메인 신상", Shrine, log);
        n += Safe("B3 목걸이 팝업", NecklacePopup, log);
        n += Safe("B4 발톱 팝업", ClawPopup, log);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("그림 입히기", $"{n}곳에 그림을 입혔습니다.\n자세한 내용은 Console. 확인 후 Ctrl+S\n(위치가 어색하면 Scene에서 살짝 옮겨 주세요)", "확인");
    }

    // [추가] 팝업만 따로 입히기
    [MenuItem("Tools/Bastet/그림 입히기 - B4 발톱 팝업만", priority = 4)]
    private static void ApplyClawOnly() => ApplyOne("B4 발톱 팝업", ClawPopup);

    [MenuItem("Tools/Bastet/그림 입히기 - B3 목걸이 팝업만", priority = 4)]
    private static void ApplyNecklaceOnly() => ApplyOne("B3 목걸이 팝업", NecklacePopup);

    private static void ApplyOne(string label, System.Func<StringBuilder, int> step)
    {
        if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("그림 입히기", "Play를 멈춘 뒤 눌러 주세요.", "확인"); return; }
        var log = new StringBuilder($"[BastetObjectArtApplier] {label}\n");
        FixImports();
        int n = Safe(label, step, log);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("그림 입히기", n > 0 ? $"{label}에 그림을 입혔습니다. Ctrl+S로 저장해 주세요." : $"{label}을(를) 못 찾았거나 오류가 났습니다. Console을 확인해 주세요.", "확인");
    }

    private static int Safe(string label, System.Func<StringBuilder, int> step, StringBuilder log)
    {
        try { return step(log); }
        catch (System.Exception e)
        {
            log.AppendLine($"- {label}: 오류 -> 건너뜀 ({e.Message})");
            Debug.LogError($"[BastetObjectArtApplier] {label} 오류: {e}");
            return 0;
        }
    }

    // ================= 월드 오브젝트 =================
    private static int Steles(StringBuilder log)
    {
        Sprite s = LoadW("Bastet_Obj_Stele");
        if (s == null) return 0;
        int n = 0;
        foreach (var st in Object.FindObjectsByType<BastetClueStele>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!st.gameObject.scene.IsValid()) continue;
            if (Swap(st.gameObject, s, new Vector2(0.9f, 0.8f), true)) n++;
        }
        log.AppendLine($"- 비석 {n}개: {s.name}");
        return n;
    }

    private static int Altars(StringBuilder log)
    {
        Sprite neck = LoadW("Bastet_Obj_Altar_Necklace"), claw = LoadW("Bastet_Obj_Altar_Claw");
        int n = 0;
        foreach (var a in Object.FindObjectsByType<BastetAltarInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!a.gameObject.scene.IsValid()) continue;
            Sprite s = a.TargetPuzzle is BastetNecklacePuzzle ? neck : a.TargetPuzzle is BastetClawPatternPuzzle ? claw : null;
            if (s == null) continue; // B1·B2·B5 옛 제단은 꺼져 있으므로 그대로
            if (Swap(a.gameObject, s, new Vector2(1.3f, 0.75f), true)) { n++; log.AppendLine($"- 퍼즐 상자 {a.name}: {s.name}"); }
        }
        return n;
    }

    private static int Gates(StringBuilder log)
    {
        Sprite s = LoadW("Bastet_Obj_SealStone");
        if (s == null) return 0;
        int n = 0;
        foreach (var g in Object.FindObjectsByType<BastetRoomGate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!g.gameObject.scene.IsValid()) continue;
            var sr = g.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            // 원래 보이던 크기(배율 포함)를 기억 -> 배율 1 + 타일 반복으로 같은 크기
            Vector2 size = sr.sprite != null && sr.sprite != s ? (Vector2)sr.bounds.size : sr.drawMode == SpriteDrawMode.Tiled ? sr.size : (Vector2)s.bounds.size;
            size = new Vector2(Mathf.Max(1f, Mathf.Round(size.x)), Mathf.Max(2f, Mathf.Round(size.y / 2f) * 2f)); // 돌판 1x2칸 단위로
            Transform t = g.transform;
            KeepChildren(t, () =>
            {
                Undo.RecordObject(t, "봉인석");
                Undo.RecordObject(sr, "봉인석");
                t.localScale = Vector3.one;
                sr.sprite = s;
                sr.color = Color.white;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = size;
            });
            var box = g.GetComponent<BoxCollider2D>();
            if (box != null) { Undo.RecordObject(box, "봉인석"); box.size = size; box.offset = Vector2.zero; }
            EditorUtility.SetDirty(g);
            n++;
            log.AppendLine($"- 봉인석 {g.name}: {s.name} x {size.x}x{size.y}칸");
        }
        return n;
    }

    private static int Shrine(StringBuilder log)
    {
        Sprite body = LoadW("Bastet_Obj_Shrine"), eyes = LoadW("Bastet_Obj_Shrine_Eyes_Light");
        Sprite sun = LoadW("Bastet_Obj_ShrineDisc_Sun"), moon = LoadW("Bastet_Obj_ShrineDisc_Moon");
        if (body == null) return 0;
        foreach (var st in Object.FindObjectsByType<BastetPuzzleStarter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!(st.Target is BastetSunMoonWalk walk) || !st.gameObject.scene.IsValid()) continue;
            var sr = st.GetComponent<SpriteRenderer>();
            Swap(st.gameObject, body, new Vector2(1.7f, 0.6f), true);
            // 눈 빛
            Transform e = st.transform.Find("Eyes");
            if (e != null && eyes != null)
            {
                Undo.RecordObject(e, "신상 눈");
                e.localScale = Vector3.one;
                e.localPosition = (Vector3)((eyes.pivot - body.pivot) / body.pixelsPerUnit);
                var er = e.GetComponent<SpriteRenderer>();
                if (er != null) { Undo.RecordObject(er, "신상 눈"); er.sprite = eyes; er.sortingOrder = (sr != null ? sr.sortingOrder : 0) + 1; }
            }
            Transform paw = st.transform.Find("Paw");
            if (paw != null) { Undo.RecordObject(paw.gameObject, "신상"); paw.gameObject.SetActive(false); }
            Undo.RecordObject(st, "신상");
            st.EditorSetArt(sr, null, null); // E를 누르면 살짝 움찔
            // 머리 위 해/달 원반
            if (sun != null && moon != null)
            {
                Transform disc = st.transform.Find("Disc");
                if (disc == null)
                {
                    var go = new GameObject("Disc");
                    Undo.RegisterCreatedObjectUndo(go, "원반");
                    disc = go.transform;
                    disc.SetParent(st.transform, false);
                    go.AddComponent<SpriteRenderer>();
                }
                Undo.RecordObject(disc, "원반");
                disc.localPosition = new Vector3(0f, body.bounds.size.y + 0.15f, 0f); // 감실 들보 바로 위
                disc.localScale = Vector3.one * 0.8f;
                var dr = disc.GetComponent<SpriteRenderer>();
                Undo.RecordObject(dr, "원반");
                dr.sprite = sun;
                if (sr != null) { dr.sortingLayerID = sr.sortingLayerID; dr.sortingOrder = sr.sortingOrder + 2; }
                var so = new SerializedObject(walk);
                so.FindProperty("discRenderer").objectReferenceValue = dr;
                so.FindProperty("sunDiscSprite").objectReferenceValue = sun;
                so.FindProperty("moonDiscSprite").objectReferenceValue = moon;
                so.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(st);
            log.AppendLine($"- 메인 신상 {st.name}: {body.name} + 눈 빛 + 해/달 원반 (밤이 되면 달로 바뀜)");
            return 1;
        }
        log.AppendLine("- 메인 신상: B5 시작 석상(BastetPuzzleStarter -> BastetSunMoonWalk)을 못 찾음");
        return 0;
    }

    // 그림 바꾸기: 배율 1, 그림 바닥 = 원래 그림 바닥 (가로 가운데 유지), 단단한 부분은 아래쪽만
    private static bool Swap(GameObject go, Sprite s, Vector2 solid, bool alignBottom)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) return false;
        Transform t = go.transform;
        bool already = sr.sprite == s;
        Bounds old = sr.bounds;
        KeepChildren(t, () =>
        {
            Undo.RecordObject(t, "그림");
            Undo.RecordObject(sr, "그림");
            t.localScale = Vector3.one;
            sr.sprite = s;
            sr.color = Color.white;
            sr.drawMode = SpriteDrawMode.Simple;
            if (alignBottom && !already)
            {
                Bounds now = sr.bounds;
                t.position += new Vector3(old.center.x - now.center.x, old.min.y - now.min.y, 0f);
            }
        });
        var box = go.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Undo.RecordObject(box, "그림");
            Vector2 piv = s.pivot / s.pixelsPerUnit; // 그림 왼쪽 아래 기준 Pivot 위치
            float bottom = -piv.y;
            box.size = solid;
            box.offset = new Vector2(s.bounds.size.x * 0.5f - piv.x, bottom + solid.y * 0.5f);
        }
        EditorUtility.SetDirty(go);
        return true;
    }

    // 부모를 옮기거나 배율을 바꿔도 자식("!" 말풍선, 빛 등)은 화면 위 제자리에 남게
    private static void KeepChildren(Transform t, System.Action change)
    {
        int c = t.childCount;
        var pos = new Vector3[c];
        var scl = new Vector3[c];
        for (int i = 0; i < c; i++) { pos[i] = t.GetChild(i).position; scl[i] = t.GetChild(i).lossyScale; }
        change();
        for (int i = 0; i < c; i++)
        {
            Transform ch = t.GetChild(i);
            if (ch.name == "Eyes" || ch.name == "Disc") continue; // 신상 눈·원반은 따로 맞춤
            Undo.RecordObject(ch, "자식 위치");
            ch.position = pos[i];
            Vector3 ps = t.lossyScale;
            ch.localScale = new Vector3(scl[i].x / Mathf.Max(0.0001f, ps.x), scl[i].y / Mathf.Max(0.0001f, ps.y), 1f);
        }
    }

    // ================= B3 목걸이 팝업 =================
    // [수정] 씬에 팝업이 여러 개(복사본) 있어도 전부 입힘
    private static int NecklacePopup(StringBuilder log)
    {
        int n = 0;
        foreach (var p in Object.FindObjectsByType<BastetNecklacePuzzle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (p.gameObject.scene.IsValid()) n += NecklaceOne(p, log);
        if (n == 0) log.AppendLine("- B3 팝업 없음 (Tools/Bastet/B3·B4 팝업 퍼즐 되살리기 먼저)");
        return n;
    }

    private static int NecklaceOne(BastetNecklacePuzzle p, StringBuilder log)
    {
        Sprite panel = LoadU("Bastet_UI_Panel"), board = LoadU("Bastet_UI_NecklaceBoard"), piece = LoadU("Bastet_UI_NecklacePiece");
        Sprite gem = LoadU("Bastet_UI_EyeGem"), cord = LoadU("Bastet_UI_CenterCord");
        if (panel == null || piece == null) { log.AppendLine("- B3: 그림 없음 " + U); return 0; }

        Panel(p.gameObject, panel);
        var so = new SerializedObject(p);
        SerializedProperty pieces = so.FindProperty("pieces");
        for (int i = 0; i < pieces.arraySize; i++)
        {
            var img = pieces.GetArrayElementAtIndex(i).FindPropertyRelative("pieceImage").objectReferenceValue as Image;
            if (img == null) continue;
            SetImage(img, piece, Image.Type.Simple, Color.white);
            Size(img.rectTransform, 80, 80);
            HideTexts(img.transform);
        }
        if (so.FindProperty("centerEyeGem").objectReferenceValue is Image g && gem != null) { SetImage(g, gem, Image.Type.Simple, new Color(0.75f, 0.75f, 0.8f)); Size(g.rectTransform, 66, 66); }
        if (so.FindProperty("centerLine").objectReferenceValue is Image l && cord != null) { SetImage(l, cord, Image.Type.Simple, new Color(0.75f, 0.75f, 0.8f)); Size(l.rectTransform, 18, 380); }
        // 목걸이 판 (Necklace_Board에 배경 그림)
        Transform b = p.transform.Find("Necklace_Board");
        if (b != null && board != null)
        {
            var bi = b.GetComponent<Image>();
            if (bi == null) { bi = Undo.AddComponent<Image>(b.gameObject); bi.raycastTarget = false; }
            SetImage(bi, board, Image.Type.Sliced, Color.white);
            Size((RectTransform)b, 500, 440);
        }
        Buttons(log, (Button)so.FindProperty("btnCheckSymmetry").objectReferenceValue, (Button)so.FindProperty("btnClose").objectReferenceValue);
        Texts(p.transform, so.FindProperty("textStatusNotice").objectReferenceValue as TMP_Text);
        log.AppendLine($"- B3 목걸이 팝업 '{p.name}': 파피루스 판 + 목걸이 판 + 연꽃 조각 8개 + 눈 보석 + 금 끈 + 버튼");
        return 1;
    }

    // ================= B4 발톱 팝업 =================
    private static int ClawPopup(StringBuilder log)
    {
        int n = 0;
        foreach (var p in Object.FindObjectsByType<BastetClawPatternPuzzle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (p.gameObject.scene.IsValid()) n += ClawOne(p, log);
        if (n == 0) log.AppendLine("- B4 팝업 없음 (Tools/Bastet/B3·B4 팝업 퍼즐 되살리기 먼저)");
        return n;
    }

    private static int ClawOne(BastetClawPatternPuzzle p, StringBuilder log)
    {
        Sprite panel = LoadU("Bastet_UI_Panel"), wall = LoadU("Bastet_UI_ClawWall");
        var claws = new Sprite[4];
        for (int i = 0; i < 4; i++) claws[i] = LoadU($"Bastet_UI_Claw_{i}");
        if (panel == null || claws[3] == null) { log.AppendLine("- B4: 그림 없음 " + U); return 0; }

        Panel(p.gameObject, panel);
        var so = new SerializedObject(p);
        SerializedProperty cs = so.FindProperty("clawSprites");
        if (cs == null) { log.AppendLine("- B4: 스크립트가 옛 버전 (BastetClawPatternPuzzle.cs를 새 파일로 덮어쓰기)"); return 0; }
        cs.arraySize = 4;
        for (int i = 0; i < 4; i++) cs.GetArrayElementAtIndex(i).objectReferenceValue = claws[i];
        SerializedProperty slots = so.FindProperty("patternSlots");
        for (int i = 0; i < slots.arraySize; i++)
        {
            var e = slots.GetArrayElementAtIndex(i);
            if (e.FindPropertyRelative("slotBackground").objectReferenceValue is Image bg) SetImage(bg, claws[0], Image.Type.Simple, Color.white);
            if (e.FindPropertyRelative("slotText").objectReferenceValue is TMP_Text tx) { Undo.RecordObject(tx, "글자"); tx.color = Ink; tx.fontStyle = FontStyles.Bold; }
        }
        SerializedProperty cands = so.FindProperty("candidateButtons");
        for (int i = 0; i < cands.arraySize && i < 3; i++)
        {
            if (!(cands.GetArrayElementAtIndex(i).objectReferenceValue is Button btn)) continue;
            var img = btn.GetComponent<Image>();
            if (img != null) SetImage(img, claws[i + 1], Image.Type.Simple, Color.white);
            Size((RectTransform)btn.transform, 120, 82);
            HideTexts(btn.transform);
        }
        so.ApplyModifiedProperties();

        // 발톱 칸 뒤 벽 판 (격자 정렬에서 빠지게 LayoutElement.ignoreLayout)
        Transform area = p.transform.Find("Pattern_Slots_Area");
        if (area != null && wall != null)
        {
            Transform bgT = area.Find("ClawWall_BG");
            if (bgT == null)
            {
                var go = new GameObject("ClawWall_BG", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                Undo.RegisterCreatedObjectUndo(go, "벽 판");
                bgT = go.transform;
                bgT.SetParent(area, false);
                bgT.SetAsFirstSibling();
                go.GetComponent<LayoutElement>().ignoreLayout = true;
            }
            var rt = (RectTransform)bgT;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-26, -22); rt.offsetMax = new Vector2(26, 22);
            var wi = bgT.GetComponent<Image>();
            SetImage(wi, wall, Image.Type.Sliced, Color.white);
            wi.raycastTarget = false;
        }
        Transform label = p.transform.Find("Candidate_Panel/Text_Candidate_Label");
        if (label != null && label.GetComponent<TMP_Text>() is TMP_Text lt) { Undo.RecordObject(lt, "글자"); lt.color = InkSoft; }
        Buttons(log, (Button)so.FindProperty("btnSubmit").objectReferenceValue, (Button)so.FindProperty("btnReset").objectReferenceValue, (Button)so.FindProperty("btnClose").objectReferenceValue);
        Texts(p.transform, so.FindProperty("textStatusNotice").objectReferenceValue as TMP_Text);
        EditorUtility.SetDirty(p);
        log.AppendLine($"- B4 발톱 팝업 '{p.name}': 파피루스 판 + 벽 판 + 발톱 자국 칸(빈칸/1/2/3줄) + 후보 3개 + 버튼");
        return 1;
    }

    // ================= 팝업 공통 =================
    private static void Panel(GameObject root, Sprite panel)
    {
        var img = root.GetComponent<Image>();
        if (img == null) return;
        SetImage(img, panel, Image.Type.Simple, Color.white);
        Size((RectTransform)root.transform, 1200, 800);
    }

    private static void Texts(Transform root, TMP_Text status)
    {
        Transform title = root.Find("Text_Title");
        if (title != null && title.GetComponent<TMP_Text>() is TMP_Text t) { Undo.RecordObject(t, "글자"); t.color = Ink; }
        if (status != null) { Undo.RecordObject(status, "글자"); status.color = InkSoft; }
    }

    private static void Buttons(StringBuilder log, params Button[] buttons)
    {
        Sprite s = LoadU("Bastet_UI_Button");
        if (s == null) return;
        foreach (Button b in buttons)
        {
            if (b == null) continue;
            var img = b.GetComponent<Image>();
            bool close = b.name.Contains("Close");
            if (img != null) SetImage(img, s, Image.Type.Sliced, close ? new Color(0.85f, 0.62f, 0.55f) : Color.white); // 나가기는 붉은 기운
            foreach (var t in b.GetComponentsInChildren<TMP_Text>(true)) { Undo.RecordObject(t, "글자"); t.color = Paper; }
        }
    }

    private static void SetImage(Image img, Sprite s, Image.Type type, Color c)
    {
        Undo.RecordObject(img, "그림");
        img.sprite = s;
        img.type = type;
        img.color = c;
        img.preserveAspect = false;
        EditorUtility.SetDirty(img);
    }

    private static void Size(RectTransform rt, float w, float h)
    {
        Undo.RecordObject(rt, "크기");
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void HideTexts(Transform t)
    {
        foreach (var tx in t.GetComponentsInChildren<TMP_Text>(true))
        {
            Undo.RecordObject(tx.gameObject, "글자 숨김");
            tx.gameObject.SetActive(false); // 화살표 글자(▲) 대신 그림이 방향을 보여 줌
        }
    }

    // ================= 그림 불러오기 / 가져오기 설정 =================
    private static Sprite LoadW(string f) => Load(W, f);
    private static Sprite LoadU(string f) => Load(U, f);

    // 팝업 그림 이름 (폴더가 달라도 이름으로 찾음)
    private static readonly string[] UiNames =
    {
        "Bastet_UI_Panel", "Bastet_UI_NecklaceBoard", "Bastet_UI_NecklacePiece", "Bastet_UI_EyeGem", "Bastet_UI_CenterCord",
        "Bastet_UI_Claw_0", "Bastet_UI_Claw_1", "Bastet_UI_Claw_2", "Bastet_UI_Claw_3", "Bastet_UI_ClawWall", "Bastet_UI_Button"
    };

    // [수정] 정해진 폴더에 없으면 프로젝트 전체에서 같은 이름의 그림을 찾음 (UI/Puzzle 대신 UI 폴더에 넣어도 됨)
    private static Sprite Load(string folder, string file)
    {
        string path = FindPath(folder, file);
        Sprite s = path != null ? AssetDatabase.LoadAssetAtPath<Sprite>(path) : null;
        if (s == null) Debug.LogWarning($"[BastetObjectArtApplier] 그림 없음(또는 Sprite가 아님): {file}.png ({(path ?? "프로젝트에 없음")})");
        return s;
    }

    public static string FindPath(string folder, string file)
    {
        string p = folder + file + ".png";
        if (AssetImporter.GetAtPath(p) != null) return p;
        foreach (string guid in AssetDatabase.FindAssets(file + " t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == file) return path;
        }
        return null;
    }

    // 그림이 이 스크립트보다 먼저 들어와 기본값으로 가져온 경우 바로잡음
    private static void FixImports()
    {
        string[] world = { "Bastet_Obj_Stele", "Bastet_Obj_Altar_Necklace", "Bastet_Obj_Altar_Claw", "Bastet_Obj_SealStone",
                           "Bastet_Obj_Shrine", "Bastet_Obj_Shrine_Eyes_Light", "Bastet_Obj_ShrineDisc_Sun", "Bastet_Obj_ShrineDisc_Moon" };
        foreach (string f in world)
        {
            string path = FindPath(W, f);
            if (path == null || !(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            Vector2 piv = BastetArtImport.PivotFor(f);
            bool ok = ti.textureType == TextureImporterType.Sprite && Mathf.Approximately(ti.spritePixelsPerUnit, BastetArtImport.PixelsPerUnit)
                      && ti.filterMode == FilterMode.Point && st.spriteMeshType == SpriteMeshType.FullRect && (st.spritePivot - piv).sqrMagnitude < 0.0001f;
            if (ok) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = BastetArtImport.PixelsPerUnit;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Custom;
            st.spritePivot = piv;
            st.spriteMeshType = SpriteMeshType.FullRect; // 봉인석 타일 반복에 필요
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
            Debug.Log($"[BastetObjectArtApplier] 가져오기 설정 고침: {path}");
        }
        foreach (string name in UiNames)
        {
            string path = FindPath(U, name);
            if (path == null || !(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
            if (ti.textureType == TextureImporterType.Sprite && Mathf.Approximately(ti.spritePixelsPerUnit, 100f)
                && (!path.EndsWith("Bastet_UI_Button.png") || ti.spriteBorder != Vector4.zero)) continue;
            BastetArtImport.SetupUi(ti, path);
            ti.SaveAndReimport();
            Debug.Log($"[BastetObjectArtApplier] UI 가져오기 설정 고침: {path}");
        }
    }
}
#endif
