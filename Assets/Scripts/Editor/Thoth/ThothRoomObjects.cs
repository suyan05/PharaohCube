using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 토트의 방 물건 놓기 (ThothRoomBuilder가 부름). 좌표는 ThothRoomMap 글자 그림 기준 (칸 하나 = 1 유닛)
// 그림은 Assets/Resources/Thoth/World/TH_*.png. 그림 가운데 = 오브젝트 위치
public static class ThothRoomObjects
{
    private const string WorldDir = "Assets/Resources/Thoth/World/";
    public static readonly Vector3 PlayerStart = new Vector3(18f, 3f, 0f);
    public static readonly Vector2 PlateCenter = new Vector2(18f, 18.8f);
    private static readonly string[] PlateGlyphs = { "pen", "moon", "ibis", "eye", "scale" }; // 위에서 시계 방향 (섞어 둠)
    private static string layer = "Default";
    private static Transform root;

    public static void Build(string sortingLayer)
    {
        layer = sortingLayer;
        root = new GameObject("ThothRoom").transform;
        BuildCorridor();
        BuildHall();
        BuildWestWing();
        BuildEastWing();
        BuildDirector();
        new GameObject("ThothFrescoUI").AddComponent<ThothFrescoUI>().transform.SetParent(root, false);
    }

    // ================= 남쪽 복도 =================
    private static void BuildCorridor()
    {
        Solid(Obj("Shelf_Corridor", new Vector2(16.5f, 4.6f), "TH_Shelf", 1f), 0.9f);
        GameObject s = Obj("Shelf_MoonFrag1", new Vector2(19.5f, 5.6f), "TH_Shelf", 1f);
        Solid(s, 0.9f);
        Pickup(s, "", -1, ThothIds.MoonFrag(1), "E: 책장 위 살펴보기", ThothLines.Me, "책장 위에 달 모양 금 조각이 있어.");
        Set(s.GetComponent<ThothPickup>(), "keepObject", true);
        Set(s.GetComponent<ThothPickup>(), "afterLines", new[] { ThothLines.Me, "토트의 서가. 달의 셈을 적은 책들이 꽂혀 있다." });
        Glow(s, new Vector2(0f, 0.45f), 0.5f, s.GetComponent<ThothPickup>());
        Obj("Door_South", new Vector2(18f, 1.2f), "TH_Door", 1.2f);
    }

    // ================= 중앙 홀 =================
    private static void BuildHall()
    {
        GameObject beam = Obj("Floor_Moonbeam", new Vector2(18f, 15.5f), "TH_Moonlight", 1.25f);
        beam.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.45f);
        Hint(beam, YSortHint.Mode.AlwaysBelow);
        Light(beam, new Color(0.7f, 0.85f, 1f), 1.1f, 6f);
        GameObject dial = Obj("MoonDial", new Vector2(18f, 14.2f), "TH_Dial_Locked", 1f);
        Box(dial, new Vector2(1.7f, 0.9f), new Vector2(0f, -0.5f), false);
        Set(dial.AddComponent<ThothMoonDial>(), "art", dial.GetComponent<SpriteRenderer>());
        GameObject stele = Obj("Stele", new Vector2(14.5f, 16.5f), "TH_Stele", 1f);
        Solid(stele, 0.8f);
        Set(stele.AddComponent<ThothReadable>(), "key", "stele");
        Set(stele.GetComponent<ThothReadable>(), "prompt", "E: 비석 읽기");
        BuildIbis();
        BuildGateArea();
        BuildNicheC();
        Solid(Obj("Stack_HallW", new Vector2(11.6f, 9.1f), "TH_Stack_A", 1f), 2.6f);
        Solid(Obj("Stack_HallE", new Vector2(24.4f, 9.1f), "TH_Stack_B", 1f), 2.6f);
        Obj("Shelf_Wall1", new Vector2(16f, 22.5f), "TH_Shelf", 1f);
        Obj("Shelf_Wall2", new Vector2(24.5f, 22.5f), "TH_Shelf", 1f);
    }

    private static void BuildIbis()
    {
        GameObject ibis = Obj("IbisStatue", new Vector2(21.5f, 16.8f), "TH_Ibis", 1f);
        Solid(ibis, 0.8f);
        Pickup(ibis, "B", 2, "", "E: 따오기 상 살펴보기", ThothLines.Me, "따오기 부리 끝에 벽화 조각이 걸려 있었어. 서쪽 벽화를 되살리니 고개를 숙였나 봐.");
        ThothPickup p = ibis.GetComponent<ThothPickup>();
        Set(p, "keepObject", true);
        Set(p, "requireFlag", ThothIds.FrescoDone("A"));
        Set(p, "lockedLines", new[] { ThothLines.Me, "토트의 새, 따오기 상. 부리가 서쪽 서고를 가리키고 있어." });
        Set(p, "afterLines", new[] { ThothLines.Me, "따오기 상. 이제 동쪽 서고를 바라보고 있다." });
        Glow(ibis, new Vector2(-0.3f, 0.5f), 0.45f, p);
    }

    private static void BuildGateArea()
    {
        GameObject gate = Obj("GateOfWisdom", new Vector2(18f, 23.75f), "TH_Gate_Closed", 1.75f);
        // E 받는 곳은 문 발치 (글자판보다 가까워야 문이 골라짐)
        var door = new GameObject("GateDoor");
        door.transform.SetParent(root, false);
        door.transform.position = new Vector2(18f, 22.15f);
        Box(door, new Vector2(1.6f, 0.4f), Vector2.zero, true);
        Set(door.AddComponent<ThothGate>(), "art", gate.GetComponent<SpriteRenderer>());
        GameObject panel = Obj("SpellPanel", new Vector2(22f, 23f), "TH_Spell_Panel", 1f);
        var center = new GameObject("PlateCenter").transform;
        center.SetParent(root, false);
        center.position = PlateCenter;
        ThothGlyphSpell spell = panel.AddComponent<ThothGlyphSpell>();
        Set(spell, "panelArt", panel.GetComponent<SpriteRenderer>());
        Set(spell, "plateCenter", center);
        for (int i = 0; i < 5; i++) BuildPlate(i);
    }

    private static void BuildPlate(int i)
    {
        float a = (90f - i * 72f) * Mathf.Deg2Rad;
        Vector2 pos = PlateCenter + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 1.45f;
        GameObject plate = Obj("Floor_Plate_" + PlateGlyphs[i], pos, "TH_Plate_" + PlateGlyphs[i], 1f);
        Hint(plate, YSortHint.Mode.AlwaysBelow);
        Box(plate, new Vector2(1f, 1f), Vector2.zero, true);
        ThothGlyphPlate p = plate.AddComponent<ThothGlyphPlate>();
        Set(p, "glyph", PlateGlyphs[i]);
        Set(p, "art", plate.GetComponent<SpriteRenderer>());
    }

    // 북쪽 벽감 (벽화 C) + 막는 돌판 + 달빛 속 조각 3개
    private static void BuildNicheC()
    {
        GameObject slab = Obj("NicheSlab", new Vector2(13f, 23f), "TH_Slab", 1f);
        Box(slab, new Vector2(4f, 1.2f), new Vector2(0f, -0.4f), false);
        BuildFresco("C", new Vector2(13f, 26.125f));
        Vector2[] spots = { new Vector2(16.6f, 15.1f), new Vector2(19.4f, 15.3f), new Vector2(18f, 16.9f) };
        int[] idx = ThothFrescoData.ScatteredOf("C");
        for (int i = 0; i < spots.Length && i < idx.Length; i++)
        {
            GameObject p = Obj("Floor_PieceC" + idx[i], spots[i], "TH_Piece_C", 1f);
            Box(p, new Vector2(0.8f, 0.8f), Vector2.zero, true);
            Hint(p, YSortHint.Mode.AlwaysBelow);
            Pickup(p, "C", idx[i], "", "E: 달빛 속 조각 줍기", ThothLines.Me, "달빛 속에 벽화 조각이 떨어져 있어.");
            Set(p.GetComponent<ThothPickup>(), "requireFlag", ThothIds.RevealDone);
            Set(p.GetComponent<ThothPickup>(), "hideUntilFlag", true);
            Glow(p, Vector2.zero, 0.6f, p.GetComponent<ThothPickup>());
        }
    }

    // ================= 서쪽 서고 (벽화 A) =================
    private static void BuildWestWing()
    {
        BuildFresco("A", new Vector2(4.5f, 21.125f));
        GameObject stack = Obj("Stack_West", new Vector2(3f, 15f), "TH_Stack_A", 1f);
        Solid(stack, 2.6f);
        ScatterPiece("A", 1, new Vector2(3f, 13.6f), "E: 책장 틈 살펴보기", "책장 틈에 벽화 조각이 끼어 있었어.", false);
        GameObject pile = Obj("Stack_Scrolls", new Vector2(6.6f, 17.2f), "TH_Stack_B", 1f);
        Solid(pile, 2.6f);
        ScatterPiece("A", 5, new Vector2(6.6f, 15.8f), "E: 두루마리 더미 뒤지기", "두루마리 사이에 벽화 조각이 섞여 있었어.", false);
        ScatterPiece("A", 6, new Vector2(1.7f, 10.7f), "E: 먼지 털기", "먼지 밑에서 벽화 조각이 나왔어.", true);
        Obj("Shelf_West", new Vector2(7.6f, 20.5f), "TH_Shelf", 1f);
    }

    // ================= 동쪽 서고 (벽화 B, 책상, 숨은 홈) =================
    private static void BuildEastWing()
    {
        BuildFresco("B", new Vector2(28.9f, 21.125f));
        GameObject desk = Obj("ScribeDesk", new Vector2(31f, 15f), "TH_Desk", 1f);
        Box(desk, new Vector2(2.6f, 0.9f), new Vector2(0f, -0.5f), false);
        desk.AddComponent<ThothDesk>();
        Solid(Obj("Stack_East", new Vector2(28.6f, 12.2f), "TH_Stack_A", 1f), 2.6f);
        ScatterPiece("B", 7, new Vector2(28.6f, 10.8f), "E: 책장 꼭대기 살펴보기", "책장 꼭대기에 벽화 조각이 올려져 있었어.", false);
        GameObject dust = Obj("Floor_Dust_MoonFrag3", new Vector2(34.2f, 10.7f), "TH_Dust", 1.2f);
        Hint(dust, YSortHint.Mode.AlwaysBelow);
        Box(dust, new Vector2(1f, 0.7f), Vector2.zero, true);
        Pickup(dust, "", -1, ThothIds.MoonFrag(3), "E: 먼지 털기", ThothLines.Me, "먼지 밑에서 달 모양 금 조각이 나왔어.");
        Set(dust.GetComponent<ThothPickup>(), "hold", true);
        Glow(dust, Vector2.zero, 0.5f, dust.GetComponent<ThothPickup>());
        BuildNiche(new Vector2(32.9f, 21f));
        Obj("Shelf_East", new Vector2(34.5f, 17.6f), "TH_Shelf", 1f);
    }

    private static void BuildNiche(Vector2 pos)
    {
        GameObject niche = Obj("HiddenNiche", pos, "TH_Niche", 1f);
        Box(niche, new Vector2(3.6f, 0.6f), new Vector2(0f, -0.7f), false);
        var sockets = new List<SpriteRenderer>();
        for (int i = 0; i < 3; i++)
        {
            GameObject s = Obj("Socket" + (i + 1), Vector2.zero, "TH_MoonFrag_" + (i + 1), 1f);
            s.transform.SetParent(niche.transform, false);
            s.transform.localPosition = new Vector3(-0.6f + i * 0.6f, 0.05f, 0f);
            s.GetComponent<SpriteRenderer>().sortingOrder = 1;
            sockets.Add(s.GetComponent<SpriteRenderer>());
        }
        Set(niche.AddComponent<ThothHiddenNiche>(), "sockets", sockets.ToArray());
    }

    private static void BuildFresco(string key, Vector2 pos)
    {
        GameObject wall = Obj("Fresco" + key, pos, "TH_Fresco" + key + "_Wall", 1f);
        Box(wall, new Vector2(3.4f, 0.6f), new Vector2(0f, -0.85f), false);
        ThothFrescoWall f = wall.AddComponent<ThothFrescoWall>();
        Set(f, "fresco", key);
        Set(f, "art", wall.GetComponent<SpriteRenderer>());
        Set(f, "glint", Glow(wall, Vector2.zero, 2.2f, null));
    }

    private static void ScatterPiece(string fresco, int index, Vector2 pos, string prompt, string line, bool hold)
    {
        GameObject p = Obj(hold ? "Floor_Dust_" + fresco + index : "Piece_" + fresco + index, pos, hold ? "TH_Dust" : "TH_Piece_" + fresco, hold ? 1.2f : 1f);
        Box(p, new Vector2(0.8f, 0.8f), Vector2.zero, true);
        if (hold) Hint(p, YSortHint.Mode.AlwaysBelow);
        Pickup(p, fresco, index, "", prompt, ThothLines.Me, line);
        Set(p.GetComponent<ThothPickup>(), "hold", hold);
        Glow(p, Vector2.zero, 0.5f, p.GetComponent<ThothPickup>());
    }

    private static void BuildDirector()
    {
        var go = new GameObject("ThothDirector");
        go.transform.SetParent(root, false);
        ThothDirector d = go.AddComponent<ThothDirector>();
        Set(d, "moonbeam", Find("Floor_Moonbeam").GetComponent<SpriteRenderer>());
        Set(d, "beamLight", Find("Floor_Moonbeam").GetComponentInChildren<Light2D>());
        Set(d, "slab", Find("NicheSlab").transform);
        Set(d, "gate", Find("GateDoor").GetComponent<ThothGate>());
        Set(d, "skylight", Find("Floor_Moonbeam").transform);
    }

    // ================= 도우미 =================
    private static GameObject Find(string name) => root.Find(name).gameObject;

    private static GameObject Obj(string name, Vector2 pos, string sprite, float scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WorldDir + sprite + ".png");
        if (sr.sprite == null) Debug.LogWarning($"[ThothRoomObjects] 그림 없음: {WorldDir}{sprite}.png");
        sr.sortingLayerName = layer;
        return go;
    }

    private static BoxCollider2D Box(GameObject go, Vector2 size, Vector2 offset, bool trigger)
    {
        BoxCollider2D b = go.AddComponent<BoxCollider2D>();
        float s = go.transform.localScale.x;
        b.size = size / s;
        b.offset = offset / s;
        b.isTrigger = trigger;
        return b;
    }

    // 서 있는 물건: 그림 아랫부분만 막힘 (뒤로 지나갈 수 있게)
    private static void Solid(GameObject go, float width)
    {
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        float h = sr.sprite != null ? sr.sprite.bounds.size.y * go.transform.localScale.y : 1f;
        Box(go, new Vector2(width, 0.6f), new Vector2(0f, -h * 0.5f + 0.3f), false);
    }

    private static void Hint(GameObject go, YSortHint.Mode mode)
    {
        go.AddComponent<YSortHint>().mode = mode;
    }

    private static SpriteRenderer Glow(GameObject parent, Vector2 local, float scale, ThothPickup pickup)
    {
        GameObject g = Obj("Glint", Vector2.zero, "TH_Glow", 1f);
        g.transform.SetParent(parent.transform, false);
        g.transform.localPosition = local;
        g.transform.localScale = Vector3.one * scale / parent.transform.localScale.x;
        SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
        sr.color = new Color(1f, 0.86f, 0.5f, 0.4f);
        sr.sortingOrder = 2;
        if (pickup != null)
        {
            Set(pickup, "glint", sr);
            Set(pickup, "art", parent.GetComponent<SpriteRenderer>());
        }
        return sr;
    }

    private static void Light(GameObject parent, Color color, float intensity, float radius)
    {
        var go = new GameObject("MoonLight");
        go.transform.SetParent(parent.transform, false);
        Light2D l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.pointLightOuterRadius = radius;
        l.pointLightInnerRadius = radius * 0.3f;
    }

    private static void Pickup(GameObject go, string fresco, int piece, string item, string prompt, params string[] lines)
    {
        ThothPickup p = go.AddComponent<ThothPickup>();
        p.Setup(fresco, piece, item, prompt, lines);
        EditorUtility.SetDirty(p);
    }

    // private [SerializeField] 칸 채우기
    public static void Set(Object target, string field, object value)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogWarning($"[ThothRoomObjects] {target.GetType().Name}에 '{field}' 칸이 없음");
            return;
        }
        Assign(p, value);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Assign(SerializedProperty p, object value)
    {
        if (value is string s) p.stringValue = s;
        else if (value is bool b) p.boolValue = b;
        else if (value is float f) p.floatValue = f;
        else if (value is int i) p.intValue = i;
        else if (value is string[] arr) AssignArray(p, arr.Length, (e, k) => e.stringValue = arr[k]);
        else if (value is Object[] objs) AssignArray(p, objs.Length, (e, k) => e.objectReferenceValue = objs[k]);
        else p.objectReferenceValue = value as Object;
    }

    private static void AssignArray(SerializedProperty p, int count, System.Action<SerializedProperty, int> set)
    {
        p.arraySize = count;
        for (int k = 0; k < count; k++) set(p.GetArrayElementAtIndex(k), k);
    }
}
