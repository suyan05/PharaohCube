#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// [추가] 메뉴: Tools/Bastet/★ 그림 붙이기 (석상·발판)
// 걷는 퍼즐(B1·B2·B5)의 네모·동그라미 임시 그림을 Assets/image/Bastet/Puzzle/ 그림으로 바꿈
//  - PawStatue (B1 시작 석상)         <- Bastet_Obj_PawStatue      (64x96px = 2x3칸)
//  - BigCatStatue (B2 큰 석상)         <- Bastet_Obj_EyeCatBig      (64x96px)
//  - SmallCat_0~5 / RingCat_0~5 (눈 석상) <- Bastet_Obj_EyeCat      (32x64px = 1x2칸)
//  - Footprint_0~6 / RingStep_0~5 (발판) <- Bastet_Floor_Footprint  (32x32px = 1칸, 48 메뉴는 48x48px)
//  - 눈이 켜질 때는 _Eyes_Light(흰색) 그림에 코드가 색을 입힘 (B2 정답 청록, 틀림 빨강 등)
// 여러 번 눌러도 됨 (임시 그림일 때만 위치를 발 기준으로 내림)
public static class BastetArtApplier
{
    private const string F = BastetArtImport.Folder;

    // 마지막에 고른 발판 크기 기억 (맵 배치 메뉴를 다시 눌러도 유지)
    public static bool BigPlate
    {
        get => EditorPrefs.GetBool("Bastet_BigPlate", false);
        set => EditorPrefs.SetBool("Bastet_BigPlate", value);
    }

    [MenuItem("Tools/Bastet/★ 그림 붙이기 (석상·발판 1칸)", priority = 2)]
    private static void MenuSmall() => ApplyMenu(false);

    [MenuItem("Tools/Bastet/그림 붙이기 (발판 크게 1.5칸)", priority = 3)]
    private static void MenuBig() => ApplyMenu(true);

    private static void ApplyMenu(bool bigPlate)
    {
        BigPlate = bigPlate;
        var log = new StringBuilder("[BastetArtApplier] 그림 붙이기\n");
        int n = Apply(bigPlate, log);
        Debug.Log(log.ToString());
        if (n < 0)
            EditorUtility.DisplayDialog("그림 붙이기", $"그림을 찾지 못했습니다.\n{F} 폴더에 Bastet_Obj_PawStatue.png 등이 있는지 확인해 주세요.", "확인");
        else
            EditorUtility.DisplayDialog("그림 붙이기", $"{n}개 오브젝트에 그림을 붙였습니다.\n자세한 내용은 Console을 확인해 주세요. 확인 후 Ctrl+S", "확인");
    }

    // 반환: 바꾼 개수 (그림이 없으면 -1). MapFit/전체 세팅 메뉴에서도 부름
    public static int Apply(bool bigPlate, StringBuilder log)
    {
        string plateName = bigPlate ? "Bastet_Floor_Footprint_48" : "Bastet_Floor_Footprint";
        FixImport("Bastet_Obj_PawStatue", "Bastet_Obj_PawStatue_Eyes_Light", "Bastet_Obj_EyeCatBig", "Bastet_Obj_EyeCatBig_Eyes_Light",
                  "Bastet_Obj_EyeCat", "Bastet_Obj_EyeCat_Eyes_Light", plateName, plateName + "_Glow_Light");

        Sprite paw = Load("Bastet_Obj_PawStatue"), pawEyes = Load("Bastet_Obj_PawStatue_Eyes_Light");
        Sprite big = Load("Bastet_Obj_EyeCatBig"), bigEyes = Load("Bastet_Obj_EyeCatBig_Eyes_Light");
        Sprite small = Load("Bastet_Obj_EyeCat"), smallEyes = Load("Bastet_Obj_EyeCat_Eyes_Light");
        Sprite plate = Load(plateName), plateGlow = Load(plateName + "_Glow_Light");
        if (paw == null && big == null && small == null && plate == null)
        {
            log.AppendLine($"- 그림 없음: {F}");
            return -1;
        }

        int count = 0;
        // 시작 석상 (B1 앞발 석상 / B2 큰 석상). B5 신상은 그림이 오면 같은 방식으로 추가
        foreach (var st in Object.FindObjectsByType<BastetPuzzleStarter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Sprite body = st.name == "PawStatue" ? paw : st.name == "BigCatStatue" ? big : null;
            Sprite eyes = st.name == "PawStatue" ? pawEyes : bigEyes;
            if (body == null) { log.AppendLine($"- {st.name}: 그림 없음 -> 임시 그림 유지"); continue; }
            SpriteRenderer sr = Statue(st.gameObject, body, eyes);
            Undo.RecordObject(st, "그림");
            st.EditorSetArt(sr, null, null); // 그림 1장: E를 누르면 석상이 살짝 움찔
            EditorUtility.SetDirty(st);
            count++;
            log.AppendLine($"- {st.name}: {body.name}");
        }
        // 눈 석상 (B2 작은 석상 6개, B5 둘레 석상 6개)
        if (small != null)
        {
            foreach (var es in Object.FindObjectsByType<BastetEyeStatue>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Statue(es.gameObject, small, smallEyes);
                count++;
            }
            log.AppendLine($"- 눈 석상: {small.name}");
        }
        // 발판 (B1 발자국 7개, B5 둘레 발판 6개)
        if (plate != null)
        {
            float half = plate.bounds.extents.x; // 32px = 0.5칸, 48px = 0.75칸
            foreach (var p in Object.FindObjectsByType<BastetFootprintPlate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Plate(p, plate, plateGlow, half);
                count++;
            }
            log.AppendLine($"- 발판: {plate.name} (밟힘 반지름 {half * 0.7f:0.00}칸)");
        }
        if (count > 0) EditorSceneManager.MarkAllScenesDirty();
        return count;
    }

    // ================= 석상 =================
    private static SpriteRenderer Statue(GameObject go, Sprite body, Sprite eyes)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        var col = go.GetComponent<BoxCollider2D>();
        Transform t = go.transform;
        Undo.RecordObject(t, "그림");
        if (sr != null) Undo.RecordObject(sr, "그림");

        // 임시 네모(가운데 기준)였으면 발 위치로 내림 (그림은 바닥 가운데 기준)
        if (sr != null && IsPlaceholder(sr.sprite))
            t.localPosition -= new Vector3(0f, t.localScale.y * 0.5f, 0f);
        t.localScale = Vector3.one;

        if (sr != null)
        {
            sr.sprite = body;
            sr.color = Color.white;
            sr.drawMode = SpriteDrawMode.Simple;
        }
        // 단단한 부분 = 받침대만 (위쪽 몸통 뒤로는 지나갈 수 있게)
        if (col != null)
        {
            Undo.RecordObject(col, "그림");
            float w = body.bounds.size.x;
            col.size = new Vector2(Mathf.Max(0.4f, w * 0.75f), 0.55f);
            col.offset = new Vector2(0f, 0.28f);
        }
        // 눈 빛: 흰 그림 + Light2D가 눈 위치에 오도록
        Transform e = t.Find("Eyes");
        if (e != null && eyes != null)
        {
            Undo.RecordObject(e, "그림");
            e.localScale = Vector3.one;
            e.localPosition = (Vector3)((eyes.pivot - body.pivot) / body.pixelsPerUnit);
            var er = e.GetComponent<SpriteRenderer>();
            if (er != null)
            {
                Undo.RecordObject(er, "그림");
                er.sprite = eyes;
                er.sortingOrder = (sr != null ? sr.sortingOrder : 0) + 1;
            }
        }
        // 네모 앞발은 그림에 이미 있으므로 끔
        Transform paw = t.Find("Paw");
        if (paw != null) Undo.RecordObject(paw.gameObject, "그림");
        if (paw != null) paw.gameObject.SetActive(false);
        EditorUtility.SetDirty(go);
        return sr;
    }

    // ================= 발판 =================
    private static void Plate(BastetFootprintPlate p, Sprite baseSprite, Sprite glowSprite, float half)
    {
        Transform t = p.transform;
        var sr = p.GetComponent<SpriteRenderer>();
        Undo.RecordObject(t, "그림");
        t.localScale = Vector3.one;
        if (sr != null)
        {
            Undo.RecordObject(sr, "그림");
            sr.sprite = baseSprite;
            sr.color = Color.white;
        }
        Transform g = t.Find("Glow");
        if (g != null && glowSprite != null)
        {
            Undo.RecordObject(g, "그림");
            g.localScale = Vector3.one;
            g.localPosition = Vector3.zero;
            var gr = g.GetComponent<SpriteRenderer>();
            if (gr != null) { Undo.RecordObject(gr, "그림"); gr.sprite = glowSprite; }
        }
        // 트리거 = 판 크기, 실제 밟힘 = 발끝이 판 가운데 70% 안
        var cc = p.GetComponent<CircleCollider2D>();
        if (cc != null) { Undo.RecordObject(cc, "그림"); cc.radius = half; cc.isTrigger = true; }
        Undo.RecordObject(p, "그림");
        p.EditorSetStepRadius(half * 0.7f);
        EditorUtility.SetDirty(p);
    }

    // ================= 도우미 =================
    private static bool IsPlaceholder(Sprite s) => s == null || s.name == "UISprite" || s.name == "Knob";

    private static Sprite Load(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(F + file + ".png");

    // 자동 설정(BastetArtImport)보다 그림이 먼저 들어와 기본값으로 가져온 경우 바로잡음
    private static void FixImport(params string[] files)
    {
        foreach (string file in files)
        {
            string path = F + file + ".png";
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
            bool ok = ti.textureType == TextureImporterType.Sprite && Mathf.Approximately(ti.spritePixelsPerUnit, BastetArtImport.PixelsPerUnit)
                      && ti.filterMode == FilterMode.Point && ti.textureCompression == TextureImporterCompression.Uncompressed;
            if (ok) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = BastetArtImport.PixelsPerUnit;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Custom;
            st.spritePivot = BastetArtImport.PivotFor(file);
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
            Debug.Log($"[BastetArtApplier] 가져오기 설정 고침: {path}");
        }
    }
}
#endif
