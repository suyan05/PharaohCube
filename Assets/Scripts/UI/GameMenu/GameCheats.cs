using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// [추가] 설정 창 "치트키"에서 누르는 치트 모음 (시연·테스트용)
// - 모든 서브 퍼즐 완료: 지금 방의 서브 퍼즐을 푼 상태로 -> 바로 메인 퍼즐
//     라·바스테트·호루스·아누비스 = 사랑님 DemoCheat(T 키)와 같은 처리를 그대로 부름 (그 파일은 안 고침)
//     소베크 허브 = 서브 4개 클리어 + 부적 4개 + 다리 통나무 모두 열림 / 소베크 퍼즐 씬 = 지금 구역 클리어
// - 소베크 메인 퍼즐 완료: 메인 퍼즐 씬이면 남은 구역을 모두 풀고 마무리 연출 / 허브면 메인을 깬 상태로 다시 불러와 물 빠짐 연출
// - 모든 메인 퍼즐 완료: 라(낮·밤)·바스테트·호루스·소베크·아누비스 메인을 모두 깬 상태 + 형판 6장 -> 지금 방을 다시 불러옴
// - 다음 스테이지로 이동: Build Profiles 씬 목록의 다음 씬 (F12와 같음)
// - 비석 모두 해석: 씬의 비석들을 읽음·해독한 것으로 + 볼록렌즈
// - 횃불 지급: 횃불 아이템 + 꺼졌거나 재가 된 횃불을 다시 켬
// - 아이템 전부 지급: 아이템 목록(ItemCatalog)에 있는 것 + 라·호루스·소베크·아누비스 아이템 전부
// - 라 방 F9 / F10: 밤으로 전환 / 지금 낮·밤 보기 (RaPhaseDebug를 잠깐 붙여서 부름)
// - 이동 속도 +5: 누를 때마다 +5, 씬이 바뀌어도 유지 ("원래대로"로 0)
// 빌드에서 끄려면 AllowInBuild = false (에디터에서는 항상 켜짐)
public static class GameCheats
{
    public const bool AllowInBuild = true;
    public static bool Enabled => Application.isEditor || Debug.isDebugBuild || AllowInBuild;

    public const float SpeedStep = 5f;
    public static float SpeedBonus { get; private set; }

    // ================= 1) 모든 서브 퍼즐 완료 =================
    public static void ClearSubPuzzles()
    {
        string scene = SceneManager.GetActiveScene().name;
        Debug.Log($"[GameCheats] 모든 서브 퍼즐 완료 ({scene})");
        try
        {
            if (scene == SobekIds.HubScene) { SobekHub(); return; }
            if (scene.StartsWith("Sobek_")) { SobekPuzzleScene(); return; }
            if (scene.Contains("Ra_Tilemap") || scene.Contains("Bastet_Tliemap") || scene.Contains("Bastet_Tilemap")
                || scene.Contains("Horus") || scene.Contains("Anubis_Tilemap"))
            {
                CallDemoCheat(scene);
                return;
            }
            CheatRunner.Toast("이 방에는 서브 퍼즐 치트가 없습니다");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameCheats] 서브 퍼즐 완료 오류: {e}");
        }
    }

    // 사랑님 DemoCheat의 방별 처리(T 키)를 그대로 부름. 빌드에서 DemoCheat가 꺼져 있어도 여기서 하나 만듦
    private static void CallDemoCheat(string scene)
    {
        DemoCheat demo = UnityEngine.Object.FindFirstObjectByType<DemoCheat>();
        if (demo == null)
        {
            var go = new GameObject("DemoCheat (치트키 메뉴)");
            UnityEngine.Object.DontDestroyOnLoad(go);
            demo = go.AddComponent<DemoCheat>();
        }
        MethodInfo m = typeof(DemoCheat).GetMethod("HandleCheat", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (m == null) { CheatRunner.Toast("DemoCheat.HandleCheat를 찾지 못함"); return; }
        m.Invoke(demo, new object[] { scene });
    }

    private static void SobekHub()
    {
        foreach (string f in SobekIds.AmuletClearFlags) SetFlag(f);
        foreach (string id in new[] { "S2", "S3", "S4" }) SetFlag($"F_SOBEK_LOCK_{id}_OPEN");
        foreach (string id in SobekIds.Amulets) Grant(id);
        CheatRunner.Toast("[치트] 소베크 서브 퍼즐 4개 완료 + 부적 4개 -> 큰 문(소베크의 입)으로");
        ReloadScene();
    }

    private static void SobekPuzzleScene()
    {
        SobekIllusionFlow flow = UnityEngine.Object.FindFirstObjectByType<SobekIllusionFlow>();
        if (flow == null) { CheatRunner.Toast("착시 퍼즐을 찾지 못했습니다"); return; }
        flow.CheatClearCurrent();
        CheatRunner.Toast("[치트] 지금 구역 클리어");
    }

    // ================= 1-2) 소베크 메인 퍼즐 완료 =================
    public static void ClearSobekMain()
    {
        string scene = SceneManager.GetActiveScene().name;
        Debug.Log($"[GameCheats] 소베크 메인 퍼즐 완료 ({scene})");
        if (scene == SobekIds.MainScene)
        {
            SobekIllusionFlow flow = UnityEngine.Object.FindFirstObjectByType<SobekIllusionFlow>();
            if (flow == null) { CheatRunner.Toast("메인 퍼즐을 찾지 못했습니다"); return; }
            flow.CheatClearAll(); // 소베크 대사 -> 형판 공개·큐브 -> 허브 (물 빠짐)
            CheatRunner.Toast("[치트] 소베크 메인 퍼즐 완료");
            return;
        }
        if (scene == SobekIds.HubScene)
        {
            if (GameManager.Instance != null && GameManager.Instance.HasFlag(SobekIds.FlagMainDone)) { CheatRunner.Toast("소베크 메인 퍼즐은 이미 끝났습니다"); return; }
            // 서브 4개 + 통나무 + 큰 문에 부적을 넣은 것까지 -> 메인 클리어
            foreach (string f in SobekIds.AmuletClearFlags) SetFlag(f);
            foreach (string id in new[] { "S2", "S3", "S4" }) SetFlag($"F_SOBEK_LOCK_{id}_OPEN");
            SetFlag(SobekIds.FlagDoorOpen);
            SetFlag(SobekIds.FlagMainADone);
            for (int i = 0; i < 3; i++) SetFlag("F_SOBEK_MAIN_STAGE_" + i);
            SetFlag(SobekIds.FlagMainDone);
            SetFlag(SobekIds.FlagTabletReady);
            Grant(SobekIds.Plate);
            CheatRunner.Toast("[치트] 소베크 메인 퍼즐 완료 + 악어 형판 -> 신전의 물이 빠진다");
            ReloadScene(); // 허브가 메인 클리어 상태로 다시 열리며 물 빠짐 연출
            return;
        }
        CheatRunner.Toast("소베크의 방(허브 또는 메인 퍼즐)에서만 쓸 수 있습니다");
    }

    // ================= 1-3) 모든 메인 퍼즐 완료 =================
    // 방마다 "메인을 깼다"로 보는 플래그 (중앙 홀 HallRoute, 각 방 진행 코드 기준)
    private static readonly string[] MainDoneFlags =
    {
        "F_RA_P5_DONE", RaPhaseState.NightFlag, "F_RA_NIGHT_P5_DONE", "F_RA_PLATE_GIVEN", // 라: 낮 메인 -> 밤 -> 밤 메인 -> 태양 형판
        "F_BASTET_MAIN_DONE", "F_BASTET_PLATE_GIVEN",                                     // 바스테트
        "F_HO_H5_DONE",                                                                   // 호루스 (H5 호루스의 하늘)
        SobekIds.FlagDoorOpen, SobekIds.FlagMainADone, SobekIds.FlagMainDone, SobekIds.FlagTabletReady, // 소베크
        "F_ANUBIS_MAIN_DONE",                                                             // 아누비스 (심판의 저울)
    };
    private static readonly string[] AllPlates = { "PLATE_RA", "PLATE_BASTET", "PLATE_THOTH", "PLATE_HORUS", "PLATE_SOBEK", "PLATE_ANUBIS" };

    public static void ClearAllMains()
    {
        string scene = SceneManager.GetActiveScene().name;
        Debug.Log($"[GameCheats] 모든 메인 퍼즐 완료 ({scene})");
        int set = 0;
        foreach (string f in MainDoneFlags) if (SetFlagNew(f)) set++;
        for (int i = 0; i < 3; i++) SetFlag("F_SOBEK_MAIN_STAGE_" + i);
        int plates = 0;
        foreach (string id in AllPlates) if (Grant(id)) plates++;

        // 소베크 메인 퍼즐 씬이면 그 자리에서 마무리 연출까지 (형판 공개 -> 허브 물 빠짐)
        if (scene == SobekIds.MainScene)
        {
            SobekIllusionFlow flow = UnityEngine.Object.FindFirstObjectByType<SobekIllusionFlow>();
            if (flow != null) { flow.CheatClearAll(); CheatRunner.Toast($"[치트] 모든 메인 퍼즐 완료 (플래그 {set}개, 형판 {plates}장)"); return; }
        }
        CheatRunner.Toast($"[치트] 모든 메인 퍼즐 완료 (플래그 {set}개, 형판 {plates}장) -> 방을 다시 불러옴");
        ReloadScene(); // 방이 깬 상태로 다시 정리되게 (중앙 홀이면 큐브·문 상태도)
    }

    // ================= 2) 다음 스테이지 =================
    public static void NextStage()
    {
        Debug.Log("[GameCheats] 다음 스테이지로 이동");
        if (!SceneFlow.LoadNext()) CheatRunner.Toast("다음 씬이 없습니다 (Build Profiles 씬 목록 확인)");
    }

    // ================= 3) 비석 모두 해석 =================
    // 방마다 비석 스크립트가 달라서, 이름에 Stele가 들어간 스크립트의 읽음/해독 플래그를 이름으로 찾아 켬
    private static readonly string[] FlagProps = { "ReadFlag", "DecodedFlag" };
    private static readonly string[] FlagFields = { "readFlag", "flagOnRead" };

    public static void DecodeAllSteles()
    {
        Grant("TOOL_LENS");
        int count = 0;
        const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            Type t = mb.GetType();
            if (!t.Name.Contains("Stele") && t.Name != "NoteStone") continue;
            bool any = false;
            try
            {
                if (mb is BastetClueStele bastet) { bastet.TestMarkDecoded(); any = true; }
                foreach (string p in FlagProps)
                {
                    PropertyInfo pi = t.GetProperty(p, bf);
                    if (pi != null && pi.PropertyType == typeof(string) && SetFlag(pi.GetValue(mb) as string)) any = true;
                }
                foreach (string f in FlagFields)
                {
                    FieldInfo fi = t.GetField(f, bf);
                    if (fi != null && fi.FieldType == typeof(string) && SetFlag(fi.GetValue(mb) as string)) any = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameCheats] 비석 {mb.name} ({t.Name}) 건너뜀: {e.Message}");
            }
            if (any) { count++; Debug.Log($"[GameCheats] 비석 해석: {mb.name} ({t.Name})", mb); }
        }
        CheatRunner.Toast(count > 0 ? $"[치트] 비석 {count}개를 해석한 것으로 처리 + 볼록렌즈" : "이 방에서 비석을 찾지 못했습니다");
    }

    // ================= 4) 횃불 =================
    public static void GiveTorch()
    {
        Grant("ITEM_TORCH");
        ITorchLight torch = TorchLink.Current;
        if (torch != null) torch.Relight("");
        CheatRunner.Toast(torch != null ? "[치트] 횃불 지급 + 불 붙임" : "[치트] 횃불 아이템 지급 (이 방에는 플레이어 횃불이 없음)");
        Debug.Log($"[GameCheats] 횃불 지급 (플레이어 횃불 {(torch != null ? "다시 켬" : "없음")})");
    }

    // ================= 5) 아이템 전부 =================
    public static void GiveAllItems()
    {
        if (ItemInventory.Instance == null) { CheatRunner.Toast("이 씬에는 인벤토리가 없습니다"); return; }
        var ids = new HashSet<string>();
        try
        {
            // 아이템 목록 (ItemCatalog의 items는 private -> 이름으로 읽음)
            ItemCatalog cat = ItemCatalog.Main;
            FieldInfo fi = cat != null ? typeof(ItemCatalog).GetField("items", BindingFlags.Instance | BindingFlags.NonPublic) : null;
            if (fi != null && fi.GetValue(cat) is List<ItemDefinition> list)
                foreach (ItemDefinition d in list) if (d != null && !string.IsNullOrEmpty(d.id)) ids.Add(d.id);
        }
        catch (Exception e) { Debug.LogWarning($"[GameCheats] 아이템 목록 읽기 실패: {e.Message}"); }
        foreach (string id in ItemIds.All) ids.Add(id);
        foreach (string id in HorusItemIds.All) ids.Add(id);
        foreach (string id in SobekIds.Amulets) ids.Add(id);
        ids.Add(SobekIds.Plate); ids.Add(SobekIds.Tooth);
        foreach (string id in new[] { AnubisItemIds.Heart, AnubisItemIds.GoldJar, AnubisItemIds.ClayPot, AnubisItemIds.Obsidian, AnubisItemIds.BronzeBowl }) ids.Add(id);

        int added = 0;
        foreach (string id in ids) if (Grant(id)) added++;
        CheatRunner.Toast($"[치트] 아이템 {added}개 새로 지급 (전체 {ids.Count}개)");
        Debug.Log($"[GameCheats] 아이템 전부 지급: 새로 {added}개 / 목록 {ids.Count}개");
    }

    // ================= 6) 라 방 F9 / F10 =================
    private static bool InRaRoom => SceneManager.GetActiveScene().name.Contains("Ra_Tilemap") || SceneManager.GetActiveScene().name.Contains("Test_Ra");

    public static void RaNight()
    {
        if (!InRaRoom) { CheatRunner.Toast("라의 방에서만 쓸 수 있습니다"); return; }
        if (RaPhaseState.IsNight) { CheatRunner.Toast("이미 밤입니다"); return; }
        RaPhaseDebug dbg = UnityEngine.Object.FindFirstObjectByType<RaPhaseDebug>();
        if (dbg == null) dbg = new GameObject("RaPhaseDebug (치트키 메뉴)").AddComponent<RaPhaseDebug>();
        MethodInfo m = typeof(RaPhaseDebug).GetMethod("SwitchToNight", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (m == null) { CheatRunner.Toast("RaPhaseDebug.SwitchToNight를 찾지 못함"); return; }
        m.Invoke(dbg, null);
        CheatRunner.Toast("[치트] 라의 방: 낮을 끝내고 밤으로");
        ReloadScene(); // 방 전체가 밤 상태로 다시 정리되게
    }

    public static void RaPhase()
    {
        string now = RaPhaseState.IsNight ? "밤" : "낮";
        CheatRunner.Toast($"라의 방은 지금 {now}입니다");
        Debug.Log($"[GameCheats] 라의 방 지금: {RaPhaseState.Current}");
    }

    // ================= 7) 이동 속도 =================
    public static void SpeedUp()
    {
        SpeedBonus += SpeedStep;
        int n = ApplySpeed();
        CheatRunner.Toast(n > 0 ? $"[치트] 이동 속도 +{SpeedBonus:0}" : $"[치트] 이동 속도 +{SpeedBonus:0} (이 씬에는 플레이어 없음)");
    }

    public static void SpeedReset()
    {
        SpeedBonus = 0f;
        ApplySpeed();
        CheatRunner.Toast("[치트] 이동 속도 원래대로");
    }

    // 플레이어 원래 속도(처음 본 값)는 표시 컴포넌트에 기억 -> 원래 + 보너스
    public static int ApplySpeed()
    {
        FieldInfo fi = typeof(PlayerController2D).GetField("moveSpeed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (fi == null) return 0;
        int n = 0;
        foreach (PlayerController2D p in UnityEngine.Object.FindObjectsByType<PlayerController2D>(FindObjectsSortMode.None))
        {
            var mark = p.GetComponent<CheatSpeedMark>();
            if (mark == null)
            {
                if (SpeedBonus <= 0f) continue; // 한 번도 안 바꿨으면 손대지 않음
                mark = p.gameObject.AddComponent<CheatSpeedMark>();
                mark.baseSpeed = (float)fi.GetValue(p);
            }
            fi.SetValue(p, mark.baseSpeed + SpeedBonus);
            n++;
            Debug.Log($"[GameCheats] 이동 속도 {mark.baseSpeed} + {SpeedBonus} = {mark.baseSpeed + SpeedBonus}", p);
        }
        return n;
    }

    // ================= 도우미 =================
    private static bool SetFlag(string f)
    {
        if (string.IsNullOrEmpty(f) || GameManager.Instance == null) return false;
        if (!GameManager.Instance.HasFlag(f)) GameManager.Instance.SetFlag(f);
        return true;
    }

    // 새로 켠 플래그만 true (몇 개 바뀌었는지 알림용)
    private static bool SetFlagNew(string f)
    {
        if (string.IsNullOrEmpty(f) || GameManager.Instance == null || GameManager.Instance.HasFlag(f)) return false;
        GameManager.Instance.SetFlag(f);
        return true;
    }

    private static bool Grant(string id)
    {
        if (string.IsNullOrEmpty(id) || ItemInventory.Instance == null || ItemInventory.Instance.Has(id)) return false;
        ItemInventory.Instance.Grant(id);
        return true;
    }

    private static void ReloadScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}

// [추가] 치트 이동 속도: 플레이어의 원래 속도를 기억해 둠 (씬마다 플레이어에 붙음)
public class CheatSpeedMark : MonoBehaviour
{
    public float baseSpeed;
}

// [추가] 치트 도우미: 메뉴를 닫은 다음 프레임에 치트 실행 + 화면 위 짧은 알림 + 씬이 바뀌면 이동 속도 다시 적용
public class CheatRunner : MonoBehaviour
{
    private static CheatRunner instance;
    private TextMeshProUGUI text;
    private CanvasGroup group;
    private float showUntil;

    public static CheatRunner Get()
    {
        if (instance != null) return instance;
        var go = new GameObject("CheatRunner");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<CheatRunner>();
        return instance;
    }

    public static void Run(Action action, bool nextFrame)
    {
        CheatRunner r = Get();
        if (!nextFrame) { Safe(action); return; }
        r.StartCoroutine(r.Later(action));
    }

    private IEnumerator Later(Action action)
    {
        yield return null; // 메뉴가 닫히고 일시 정지가 풀린 다음
        Safe(action);
    }

    private static void Safe(Action action)
    {
        try { action?.Invoke(); }
        catch (Exception e) { Debug.LogError($"[GameCheats] 치트 오류: {e}"); }
    }

    public static void Toast(string msg)
    {
        Debug.Log($"[GameCheats] {msg}");
        Get().Show(msg);
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (GameCheats.SpeedBonus > 0f) StartCoroutine(ApplyLater());
    }

    private IEnumerator ApplyLater()
    {
        yield return null; // 플레이어 Start 뒤
        GameCheats.ApplySpeed();
    }

    // 화면 위쪽 가운데 알림 (일시 정지 중에도 보이게 맨 위 캔버스, 시간 정지와 상관없이 사라짐)
    private void Show(string msg)
    {
        if (text == null)
        {
            Canvas c = MenuUI.NewCanvas("CheatToast", 1400, true);
            c.transform.SetParent(transform, false);
            group = c.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            RectTransform bg = MenuUI.Rect("Bg", c.transform);
            bg.anchorMin = bg.anchorMax = new Vector2(0.5f, 1f);
            bg.pivot = new Vector2(0.5f, 1f);
            bg.anchoredPosition = new Vector2(0f, -40f);
            bg.sizeDelta = new Vector2(1200f, 70f);
            var img = bg.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.75f);
            img.raycastTarget = false;
            text = MenuUI.Text("Text", bg, null, 30f, MenuUI.Theme.accentColor, TextAlignmentOptions.Center);
            MenuUI.Stretch(text.rectTransform, 10f);
        }
        text.text = msg;
        group.alpha = 1f;
        showUntil = Time.unscaledTime + 2.8f;
    }

    private void Update()
    {
        if (group == null || group.alpha <= 0f) return;
        if (Time.unscaledTime > showUntil) group.alpha = Mathf.MoveTowards(group.alpha, 0f, Time.unscaledDeltaTime * 2f);
    }
}
