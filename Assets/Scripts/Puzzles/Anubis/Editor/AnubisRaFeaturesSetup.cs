#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 라 방에 있는 방 연출 기능을 아누비스 실전 씬에 한 번에 붙이는 메뉴
// Tools/Anubis/실전: 라 방 기능 적용 (목표, 신상 대화, 비석, 소지품, 입장 연출, 힌트)
// - 여러 번 눌러도 이미 있는 것은 새로 만들지 않고 연결만 다시 함 (문구를 고친 .asset은 덮어쓰지 않음)
// - 라 방 스크립트(RaDialogue, CluePanel, ClueStele, InventoryHUD, RaIntroCutscene, PuzzleHintButton)는 수정 없이 그대로 사용
public static class AnubisRaFeaturesSetup
{
    private const string DataRoot = "Assets/Scripts/Puzzles/Anubis/Data";
    private const string ClueFolder = DataRoot + "/Clues";
    private const string HintFolder = DataRoot + "/Hints";

    private const string RoomRootName = "AnubisRoomUI";
    private const string StoryCanvasName = "AnubisStoryCanvas";
    private const string GameManagerName = "GameManager_AnubisTest";
    private const string HintObjectName = "HintButton";

    [MenuItem("Tools/Anubis/실전: 라 방 기능 적용 (목표, 신상 대화, 비석, 소지품, 입장 연출, 힌트)")]
    public static void Apply()
    {
        var log = new List<string>();
        TMP_FontAsset font = FindFont();

        EnsureGameManager(log);
        RaDialogue dialogue;
        CluePanel cluePanel;
        EnsureStoryCanvas(log, out dialogue, out cluePanel);
        GameObject room = EnsureRoomRoot(log, font);
        EnsureShrine(log, dialogue);
        EnsureSteles(log, cluePanel);
        EnsureHintButtons(log);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = room;

        Debug.Log("[라 방 기능 적용] 완료 (Ctrl+S로 씬 저장)\n- " + string.Join("\n- ", log));
    }

    // ================= 1. GameManager (플래그) =================
    // 아누비스 테스트 씬에는 GameManager가 없어서 목표/비석/대화 플래그가 동작하지 않음
    // Managers 안에 넣으면 GameManager가 Managers 통째로 DontDestroyOnLoad / 중복 삭제하므로 따로 최상위에 만듦
    private static void EnsureGameManager(List<string> log)
    {
        if (Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include) != null)
        {
            log.Add("GameManager: 이미 있음");
            return;
        }
        var go = new GameObject(GameManagerName);
        Undo.RegisterCreatedObjectUndo(go, "GameManager 추가");
        go.AddComponent<GameManager>();
        log.Add($"GameManager: '{GameManagerName}' 새로 만듦 (최상위 오브젝트로 둘 것)");
    }

    // ================= 2. 대사창 + 비석 화면 =================
    private static void EnsureStoryCanvas(List<string> log, out RaDialogue dialogue, out CluePanel cluePanel)
    {
        dialogue = Object.FindFirstObjectByType<RaDialogue>(FindObjectsInactive.Include);
        cluePanel = Object.FindFirstObjectByType<CluePanel>(FindObjectsInactive.Include);
        if (dialogue != null && cluePanel != null)
        {
            log.Add("대사창/비석 화면: 이미 있음");
            return;
        }

        GameObject canvasGo = GameObject.Find(StoryCanvasName);
        if (canvasGo == null)
        {
            canvasGo = new GameObject(StoryCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "대사 캔버스");
            canvasGo.layer = 5;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 65; // 퍼즐(50), 토스트(55), 양피지 창(60)보다 위
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (dialogue == null)
        {
            RectTransform rt = CreateStretchChild(canvasGo.transform, "Dialogue_Overlay");
            dialogue = rt.gameObject.AddComponent<RaDialogue>();
            var so = new SerializedObject(dialogue);
            SetString(so, "speakerName", "아누비스");
            SetObject(so, "dialogBoxSprite", FindSprite("UI_Dialog_Box"));
            SetObject(so, "nextSprite", FindSprite("UI_Dialog_Next"));
            // 아누비스 초상화가 오면 넣을 자리 (없으면 파란 네모로 보임)
            SetObject(so, "portraitDefault", FindSprite("UI_Portrait_Anubis_Default"));
            SetObject(so, "portraitGlow", FindSprite("UI_Portrait_Anubis_Glow"));
            SetObject(so, "portraitNight", FindSprite("UI_Portrait_Anubis_Stern"));
            so.ApplyModifiedPropertiesWithoutUndo();
            log.Add("대사창: Dialogue_Overlay (RaDialogue, 이름 '아누비스') 만듦");
        }

        if (cluePanel == null)
        {
            RectTransform rt = CreateStretchChild(canvasGo.transform, "Clue_Overlay");
            cluePanel = rt.gameObject.AddComponent<CluePanel>();
            var so = new SerializedObject(cluePanel);
            SetObject(so, "frameSprite", FindSprite("UI_Journal_Frame"));
            so.ApplyModifiedPropertiesWithoutUndo();
            log.Add("비석 화면: Clue_Overlay (CluePanel) 만듦");
        }
    }

    // ================= 3. 목표 / 안내 문구 / 소지품 / 입장 연출 / 진행 플래그 =================
    private static GameObject EnsureRoomRoot(List<string> log, TMP_FontAsset font)
    {
        GameObject room = GameObject.Find(RoomRootName);
        if (room == null)
        {
            room = new GameObject(RoomRootName);
            Undo.RegisterCreatedObjectUndo(room, "아누비스 방 UI");
            log.Add($"'{RoomRootName}' 오브젝트 만듦");
        }

        var guide = GetOrAdd<AnubisObjectiveGuide>(room, log, "목표 안내 띠");
        SetFont(guide, font);

        var prompt = GetOrAdd<AnubisPromptUI>(room, log, "아래 안내 문구 (E: …)");
        SetFont(prompt, font);

        GetOrAdd<AnubisRoomProgress>(room, log, "메인 저울 통과 플래그");

        var hud = GetOrAdd<InventoryHUD>(room, log, "소지품 바");
        FillInventoryEntries(hud);

        var intro = GetOrAdd<RaIntroCutscene>(room, log, "입장 연출 (맵 줌아웃)");
        var iso = new SerializedObject(intro);
        SetString(iso, "playedFlag", "F_ANUBIS_INTRO_PLAYED");
        SetString(iso, "overviewMessage", "불이 켜진 구역만 갈 수 있다. 먼저 아누비스 신상에게 가 보자.");
        iso.ApplyModifiedPropertiesWithoutUndo();

        return room;
    }

    // 소지품 바: 라 방 아이템 대신 아누비스 보물 5개 (아이콘은 메인 저울 유물 그림을 그대로 사용)
    private static void FillInventoryEntries(InventoryHUD hud)
    {
        var items = new (string id, string label, string relic)[]
        {
            (AnubisItemIds.Heart, "망자의 심장 (H)", "Relic_Heart"),
            (AnubisItemIds.GoldJar, "황금 단지 (A)", "Relic_GoldJar"),
            (AnubisItemIds.ClayPot, "흙의 단지 (C)", "Relic_ClayPot"),
            (AnubisItemIds.Obsidian, "흑요석 추 (D)", "Relic_Obsidian"),
            (AnubisItemIds.BronzeBowl, "청동 그릇 (B)", "Relic_BronzeBowl")
        };

        var so = new SerializedObject(hud);
        SerializedProperty entries = so.FindProperty("entries");
        if (entries == null) return;

        entries.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            SerializedProperty e = entries.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("itemId").stringValue = items[i].id;
            e.FindPropertyRelative("displayName").stringValue = items[i].label;
            e.FindPropertyRelative("icon").objectReferenceValue = FindRelicSprite(items[i].relic);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================= 4. 아누비스 신상 =================
    private static void EnsureShrine(List<string> log, RaDialogue dialogue)
    {
        AnubisShrine shrine = Object.FindFirstObjectByType<AnubisShrine>(FindObjectsInactive.Include);
        if (shrine == null)
        {
            GameObject target = Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<SpriteRenderer>() != null
                ? Selection.activeGameObject
                : GameObject.Find("statue_0");
            if (target == null)
            {
                log.Add("신상: statue_0을 못 찾음 -> 신상 오브젝트를 선택하고 메뉴를 다시 실행");
                return;
            }
            Undo.AddComponent<AnubisShrine>(target);
            shrine = target.GetComponent<AnubisShrine>();
            log.Add($"신상: '{target.name}'에 AnubisShrine 붙임");
        }

        EnsureCollider(shrine.gameObject);
        SpriteRenderer bubble = EnsureBubble(shrine.gameObject);

        var so = new SerializedObject(shrine);
        SetObject(so, "dialogue", dialogue);
        SetObject(so, "newBubble", bubble);
        SetObject(so, "penalty", Object.FindFirstObjectByType<AnubisJudgmentPenalty>(FindObjectsInactive.Include));
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================= 5. 비석 단서 =================
    private static void EnsureSteles(List<string> log, CluePanel panel)
    {
        EnsureFolder(ClueFolder);

        // (오브젝트 이름, 옆에 둘 제단, 단서 ID, 제목, 본문)
        var steles = new (string obj, string altar, string id, string title, string body)[]
        {
            ("Slate_0", null, "ANUBIS_HALL", "심판의 전당",
                "죽은 자의 심장은 진실의 깃털(마아트)과 함께 저울에 오른다.\n네 개의 시험에서 보물을 얻고, 흩어진 양피지 조각으로 그 무게를 밝혀라.\n모든 보물의 무게는 자연수다."),
            ("Stele_A1", "Altar_A1_Sand", "ANUBIS_A1", "나트론의 방",
                "미라를 만들 때 쓰는 신성한 소금, 나트론.\n모래는 항아리에서 항아리로만 옮겨지고, 한 번 붓기 시작하면 멈추지 않는다.\n바닥의 양피지 조각을 모아 규칙을 읽어라."),
            ("Stele_A2", "Altar_A2_Gear", "ANUBIS_A2", "태엽의 방",
                "석관은 사람의 힘만으로는 들어 올릴 수 없었다.\n톱니바퀴는 힘을 바꾸고, 움직 도르래는 힘을 두 배로 만든다.\n필요한 만큼만 힘을 키워라. 모자라도 넘쳐도 줄은 끊어진다."),
            ("Stele_A3", "Altar_A3_Lever", "ANUBIS_A3", "지렛대의 방",
                "아누비스는 심장을 저울에 달기 전, 먼저 지렛대로 그 무게를 잰다.\n무게만 보지 말고, 축에서 얼마나 떨어져 있는지도 함께 보아라."),
            ("Stele_A4", "Altar_A4_MagicSquare", "ANUBIS_A4", "42인의 심판관",
                "망자는 42명의 심판관 앞에서 죄가 없음을 고백한다.\n아홉 신의 상징에는 숫자가 숨어 있다.\n가로, 세로, 대각선 어느 쪽으로 더해도 같은 합이 되게 하라.")
        };

        GameObject template = GameObject.Find("Slate_0");
        foreach (var s in steles)
        {
            ClueData data = EnsureClueData(s.id, s.title, s.body);

            GameObject go = GameObject.Find(s.obj);
            if (go == null)
            {
                go = CreateSteleObject(s.obj, s.altar, template);
                if (go == null)
                {
                    log.Add($"비석 '{s.obj}': 옆에 둘 '{s.altar}'를 못 찾아 건너뜀");
                    continue;
                }
                log.Add($"비석 '{s.obj}' 만듦 ({s.altar} 왼쪽, 위치는 Scene에서 옮기기)");
            }

            ClueStele stele = go.GetComponent<ClueStele>();
            if (stele == null) stele = Undo.AddComponent<ClueStele>(go);

            EnsureCollider(go);
            SpriteRenderer bubble = EnsureBubble(go);

            var so = new SerializedObject(stele);
            SetObject(so, "data", data);
            SetObject(so, "panel", panel);
            SetObject(so, "newBubble", bubble);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        log.Add($"비석 단서 {steles.Length}개 연결 (문구: {ClueFolder})");
    }

    private static GameObject CreateSteleObject(string name, string altarName, GameObject template)
    {
        GameObject altar = string.IsNullOrEmpty(altarName) ? null : GameObject.Find(altarName);
        if (altar == null) return null;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "비석 만들기");
        go.transform.position = altar.transform.position + new Vector3(-1.8f, 0.3f, 0f);
        if (altar.transform.parent != null) go.transform.SetParent(altar.transform.parent, true);

        var sr = go.AddComponent<SpriteRenderer>();
        var src = template != null ? template.GetComponent<SpriteRenderer>() : null;
        if (src != null)
        {
            sr.sprite = src.sprite;
            sr.sortingLayerID = src.sortingLayerID;
            sr.sortingOrder = src.sortingOrder;
            go.transform.localScale = template.transform.localScale;
        }
        else sr.sprite = FindSprite("Slate");
        return go;
    }

    private static ClueData EnsureClueData(string id, string title, string body)
    {
        string path = $"{ClueFolder}/Clue_{id}.asset";
        var data = AssetDatabase.LoadAssetAtPath<ClueData>(path);
        if (data != null) return data; // 이미 있으면 사용자가 고친 문구를 지키기 위해 덮어쓰지 않음

        data = ScriptableObject.CreateInstance<ClueData>();
        AssetDatabase.CreateAsset(data, path);
        var so = new SerializedObject(data);
        SetString(so, "clueId", id);
        so.FindProperty("day.title").stringValue = title;
        so.FindProperty("day.body").stringValue = body;
        so.FindProperty("day.illustrationTint").colorValue = Color.white;
        so.ApplyModifiedPropertiesWithoutUndo();
        return data;
    }

    // ================= 6. 퍼즐 화면 힌트 버튼 (우제트의 눈) =================
    private static void EnsureHintButtons(List<string> log)
    {
        EnsureFolder(HintFolder);

        var list = new (string panel, string hintId, string doneItem, string doneFlag, string[] hints)[]
        {
            ("Panel_Sand", "A1_Sand", AnubisItemIds.GoldJar, "", new[]
            {
                "붓기는 받는 항아리가 가득 차거나 붓는 항아리가 빌 때까지 멈추지 않는다. 작은 항아리를 계량컵처럼 써 보자.",
                "가득 찬 항아리에서 작은 항아리를 채우면 '큰 용량 - 작은 용량'만큼 남는다. 이 차이로 목표 양을 만든다.",
                "목표에서 막내(가장 작은) 항아리는 비어 있어야 한다. 계량에 쓴 뒤 마지막에 다른 항아리로 비워 두자."
            }),
            ("Panel_Gear", "A2_Gear", AnubisItemIds.ClayPot, "", new[]
            {
                "가운데 바퀴는 방향만 바꾼다. 힘 = 마지막 바퀴의 이 수 ÷ 첫 바퀴의 이 수.",
                "움직 도르래 하나마다 힘이 2배가 된다. 전체 힘 = 톱니 비율 × 2 × 2 …",
                "12→24는 2배, 12→36은 3배, 24→36은 1.5배. 목표 힘을 톱니 비율과 도르래 2배로 나누어 맞춰라."
            }),
            ("Panel_Lever", "A3_Lever", AnubisItemIds.Obsidian, "", new[]
            {
                "지렛대는 (무게 × 축에서의 거리)가 양쪽에서 같을 때 평형을 이룬다.",
                "심장 하나 = 흑요석 추 두 개. 왼쪽의 힘은 2 × (심장이 걸린 눈금 번호).",
                "오른쪽 힘은 (추 개수 × 눈금 번호). 추 두 개를 심장과 같은 번호의 눈금에 걸면 평형이다."
            }),
            ("Panel_MagicSquare", "A4_MagicSquare", AnubisItemIds.BronzeBowl, "", new[]
            {
                "상징 = 숫자 대응표는 A4 양피지에 있다. 가로, 세로, 대각선 8줄의 합이 모두 같아야 한다.",
                "1~9로 만드는 3×3 마방진은 한 줄의 합이 15, 가운데는 항상 5(자칼)다.",
                "짝수(2, 4, 6, 8)는 네 모서리, 홀수(1, 3, 7, 9)는 변의 가운데에 놓인다. 마주 보는 두 칸의 합은 10."
            }),
            ("Panel_MainScale", "A5_MainScale", "", AnubisObjectiveGuide.FlagMainDone, new[]
            {
                "저울 왼쪽 '사자의 서'에 비문 4개가 모두 있는지 보자. 모든 무게는 자연수다.",
                "B = 5C, H = 2D를 나머지 두 비문 (C + A = H, (B + C) - (D + A) = 1)에 넣어 보자.",
                "정리하면 7C - 3D = 1. 가장 작은 자연수 해는 C = 1, D = 2.",
                "H 4, A 3, B 5, C 1, D 2. 합이 7이 되는 보물을 왼쪽 접시에 올려라. (답은 여러 개)"
            })
        };

        Sprite normal = FindSprite("UI_Hint_Normal");
        Sprite hover = FindSprite("UI_Hint_Hover");
        Sprite disabled = FindSprite("UI_Hint_Disabled");

        int made = 0;
        foreach (var h in list)
        {
            GameObject panel = FindInScene(h.panel);
            if (panel == null)
            {
                log.Add($"힌트: '{h.panel}'를 못 찾아 건너뜀");
                continue;
            }

            AnubisHintData data = EnsureHintData(h.hintId, h.hints);

            Transform child = panel.transform.Find(HintObjectName);
            RectTransform rt = child != null ? (RectTransform)child : CreateStretchChild(panel.transform, HintObjectName);
            rt.SetAsLastSibling(); // 패널 안 다른 UI보다 위에

            AnubisPuzzleHints provider = rt.GetComponent<AnubisPuzzleHints>();
            if (provider == null) provider = Undo.AddComponent<AnubisPuzzleHints>(rt.gameObject);
            var pso = new SerializedObject(provider);
            SetObject(pso, "data", data);
            SetString(pso, "doneItemId", h.doneItem);
            SetString(pso, "doneFlag", h.doneFlag);
            pso.ApplyModifiedPropertiesWithoutUndo();

            PuzzleHintButton button = rt.GetComponent<PuzzleHintButton>();
            if (button == null) button = Undo.AddComponent<PuzzleHintButton>(rt.gameObject);
            var bso = new SerializedObject(button);
            SetObject(bso, "provider", provider);
            SetObject(bso, "normalSprite", normal);
            SetObject(bso, "hoverSprite", hover);
            SetObject(bso, "disabledSprite", disabled);
            // 닫기 버튼(760, 440) 왼쪽에 둠
            SetVector2(bso, "buttonPos", new Vector2(590f, 430f));
            SetVector2(bso, "messagePos", new Vector2(0f, 330f));
            bso.ApplyModifiedPropertiesWithoutUndo();
            made++;
        }
        log.Add($"힌트 버튼 {made}개 연결 (문구: {HintFolder})");
    }

    private static AnubisHintData EnsureHintData(string id, string[] hints)
    {
        string path = $"{HintFolder}/Hint_Anubis_{id}.asset";
        var data = AssetDatabase.LoadAssetAtPath<AnubisHintData>(path);
        if (data != null) return data; // 고친 문구 보호

        data = ScriptableObject.CreateInstance<AnubisHintData>();
        data.EditorSetHints(hints);
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    // ================= 도우미 =================
    private static T GetOrAdd<T>(GameObject go, List<string> log, string label) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c != null) return c;

        // 다른 오브젝트에 이미 있으면 (라 방에서 복사해 온 경우) 그것을 씀
        T existing = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (existing != null)
        {
            log.Add($"{label}: 이미 '{existing.name}'에 있음");
            return existing;
        }

        c = Undo.AddComponent<T>(go);
        log.Add($"{label}: {typeof(T).Name} 붙임");
        return c;
    }

    // 상호작용(E키)은 Collider2D가 있어야 찾음. 없을 때만 그림 크기로 트리거를 만듦
    private static void EnsureCollider(GameObject go)
    {
        if (go.GetComponent<Collider2D>() != null) return;

        var col = Undo.AddComponent<BoxCollider2D>(go);
        col.isTrigger = true;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            col.size = sr.sprite.bounds.size;
            col.offset = sr.sprite.bounds.center;
        }
    }

    // 머리 위 "!" 말풍선 (라 방과 같은 UI_Prompt_New)
    private static SpriteRenderer EnsureBubble(GameObject owner)
    {
        Transform t = owner.transform.Find("NewBubble");
        SpriteRenderer sr;
        if (t != null) sr = t.GetComponent<SpriteRenderer>();
        else
        {
            var go = new GameObject("NewBubble");
            Undo.RegisterCreatedObjectUndo(go, "말풍선");
            go.transform.SetParent(owner.transform, false);
            sr = go.AddComponent<SpriteRenderer>();
        }
        if (sr == null) return null;

        sr.sprite = FindSprite("UI_Prompt_New");
        var parentSr = owner.GetComponent<SpriteRenderer>();
        float top = 1.5f;
        if (parentSr != null)
        {
            sr.sortingLayerID = parentSr.sortingLayerID;
            sr.sortingOrder = parentSr.sortingOrder + 1;
            if (parentSr.sprite != null) top = parentSr.sprite.bounds.max.y + 0.35f;
        }
        sr.transform.localPosition = new Vector3(0f, top, 0f);
        sr.enabled = false; // 스크립트가 켬
        return sr;
    }

    private static RectTransform CreateStretchChild(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, name);
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; // 화면 전체 (해상도가 바뀌어도 꽉 참)
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    // 꺼져 있는 오브젝트(퍼즐 패널)도 찾음
    private static GameObject FindInScene(string name)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == name && t.gameObject.scene.IsValid()) return t.gameObject;
        }
        return null;
    }

    private static Sprite FindRelicSprite(string relicName)
    {
        GameObject relic = FindInScene(relicName);
        Image img = relic != null ? relic.GetComponent<Image>() : null;
        return img != null ? img.sprite : null;
    }

    private static Sprite FindSprite(string fileName)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{fileName} t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != fileName) continue;

            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (o is Sprite s) return s; // Sprite Mode가 Multiple이어도 첫 장
            }
        }
        return null;
    }

    private static TMP_FontAsset FindFont()
    {
        foreach (string guid in AssetDatabase.FindAssets("MonoplexKR-Regular SDF t:TMP_FontAsset"))
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (f != null) return f;
        }
        return null;
    }

    private static void SetFont(Component c, TMP_FontAsset font)
    {
        if (font == null) return;
        var so = new SerializedObject(c);
        SetObject(so, "font", font);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void SetObject(SerializedObject so, string prop, Object value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null && value != null) p.objectReferenceValue = value;
    }

    private static void SetString(SerializedObject so, string prop, string value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null) p.stringValue = value;
    }

    private static void SetVector2(SerializedObject so, string prop, Vector2 value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null) p.vector2Value = value;
    }
}
#endif
