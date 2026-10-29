using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 시연용 치트키 T + 시연 순서 고정 (빌드에서도 동작. 시연이 끝나면 AllowInBuild = false)
// 순서: 타이틀 -> 발굴 현장 -> 스핑크스의 꿈 -> 중앙 홀 -> 라의 방 -> 중앙 홀 -> 바스테트의 방 -> 중앙 홀 -> 아누비스의 방
//
// [T 키]
// - 라의 방 (낮): 낮 P1~P4를 푼 상태로 -> 바로 북쪽 태양 제단(낮 메인 퍼즐)을 풀 수 있음
// - 라의 방 (밤): 밤 P1~P4를 푼 상태로 -> 바로 북쪽 밤의 제단(밤 메인 퍼즐)을 풀 수 있음
// - 바스테트의 방: 서브 방 1~4를 푼 상태 + 신상의 방 문 열림 -> 바로 메인 퍼즐(해와 달의 기억)
// - 아누비스의 방: 서브 퍼즐 보상 4개 + 양피지 조각 전부 -> 바로 메인 저울
// - 라, 바스테트는 플래그를 켠 뒤 씬을 다시 불러옴 (모든 방, 횃불, 문이 저장된 상태대로 정리됨)
//
// - 프롤로그 (발굴 현장, 스핑크스의 꿈): 연출 중이면 연출 건너뛰기, 대화창만 떠 있으면 대화 건너뛰기
//   (선택지 대화와 아이템 팝업은 건너뛰지 않음 -> E로 직접)
//
// [순서 고정]
// - 바스테트 엔딩이 끝나면 아누비스 튜토리얼 대신 중앙 홀로 (중앙 홀 북쪽 문 -> 아누비스의 방)
// - 중앙 홀 -> 바스테트, 아누비스도 튜토리얼 방 없이 바로 실전 방 (HallRoute 기본값)
public class DemoCheat : MonoBehaviour
{
    public static bool AllowInBuild = true;

    private const KeyCode CheatKey = KeyCode.T;
    private const float Cooldown = 1.5f;
    private const string HallScene = "CentralRoom_Temp";

    private static readonly string[] RaDayFlags =
    {
        "F_RA_TEMPLE_ENTERED", "F_RA_INTRO_PLAYED", "F_RA_FIRST_TORCH_LIT", "F_RA_SHRINE_FIRST_TALKED",
        "F_RA_CIRCUIT_1", "F_RA_P1_DONE", "F_RA_CIRCUIT_2", "F_RA_P3_DONE",
        "F_RA_CIRCUIT_3", "F_RA_P4_DONE", "F_RA_CIRCUIT_4"
    };

    private static readonly string[] RaNightFlags =
    {
        "F_RA_SHRINE_NIGHT_TALKED", "F_RA_NIGHT_P1_DONE", "F_RA_NIGHT_P3_DONE", "F_RA_NIGHT_P4_DONE"
    };

    private static readonly string[] BastetFlags =
    {
        "F_BASTET_INTRO_PLAYED", "F_BASTET_GODDESS_INTRO", "F_CLUE_BASTET_HALL_READ",
        "F_BASTET_B1_CLEAR", "F_BASTET_B2_CLEAR", "F_BASTET_B3_CLEAR", "F_BASTET_B4_CLEAR",
        "FLAG_BASTET_P1", "FLAG_BASTET_P2", "FLAG_BASTET_P3", "FLAG_BASTET_P4",
        "F_BASTET_GATE_Gate_Room2_OPEN", "F_BASTET_GATE_Gate_Room3_OPEN",
        "F_BASTET_GATE_Gate_Room4_OPEN", "F_BASTET_GATE_Gate_T_OPEN"
    };

    private static DemoCheat instance;
    private float nextAllowed;
    private Text toastText;
    private CanvasGroup toastGroup;
    private float toastTimer;

    // ================= 자동 생성 =================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (!Application.isEditor && !AllowInBuild) return;
        if (instance != null) return;
        var go = new GameObject("DemoCheat");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DemoCheat>();
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BuildToast();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (instance == this) instance = null;
    }

    private void Update()
    {
        UpdateToast();
        if (!Input.GetKeyDown(CheatKey)) return;

        try
        {
            string scene = SceneManager.GetActiveScene().name;
            if (IsPrologue(scene))
            {
                SkipPrologue();
                return;
            }
            if (Time.unscaledTime < nextAllowed) return;
            if (Cine.Running || DialogueBox.IsOpen || ItemPopup.IsShowing) return;
            nextAllowed = Time.unscaledTime + Cooldown;
            HandleCheat(scene);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DemoCheat] 치트 오류: {e}");
        }
    }

    // ================= 프롤로그 T = 건너뛰기 =================
    private static bool IsPrologue(string scene)
    {
        return scene.Contains("Prologue_Dig") || scene.Contains("Prologue_Sphinx");
    }

    // 연출 중 -> 연출 전체 건너뛰기 (ESC 길게와 같음). 이미 건너뛰는 중이거나 연출 밖 -> 대화창 건너뛰기
    private void SkipPrologue()
    {
        if (Cine.Running && !Cine.Skipping)
        {
            CineSkip.RequestNow();
            Toast("[치트] 연출 건너뛰기");
            return;
        }
        DialogueBox.SkipToEnd();
    }

    // ================= T =================
    private void HandleCheat(string scene)
    {
        if (scene.Contains("Ra_Tilemap")) CheatRa();
        else if (scene.Contains("Bastet_Tliemap") || scene.Contains("Bastet_Tilemap")) CheatBastet();
        else if (scene.Contains("Anubis_Tilemap")) CheatAnubis();
        else Toast("이 방에는 치트가 없습니다");
    }

    private void CheatRa()
    {
        bool night = RaPhaseState.IsNight;
        if (!night && HasFlag("F_RA_P5_DONE"))
        {
            Toast("낮은 이미 끝났다. 라의 대화를 기다리자");
            return;
        }
        if (night && HasFlag("F_RA_NIGHT_P5_DONE"))
        {
            Toast("밤도 이미 끝났다");
            return;
        }

        SetFlags(night ? RaNightFlags : RaDayFlags);
        WallTorch.MarkAllLit();
        Toast(night ? "[치트] 밤의 메인 퍼즐로 (북쪽 밤의 제단)" : "[치트] 낮의 메인 퍼즐로 (북쪽 태양 제단)");
        ReloadScene();
    }

    private void CheatBastet()
    {
        if (HasFlag("F_BASTET_MAIN_DONE"))
        {
            Toast("바스테트 메인 퍼즐은 이미 끝났다");
            return;
        }
        SetFlags(BastetFlags);
        GrantItem("ITEM_BASTET_CLAW");
        WallTorch.MarkAllLit();
        Toast("[치트] 바스테트 메인 퍼즐로 (신상의 방)");
        ReloadScene();
    }

    // 아누비스는 다시 불러오지 않음 (제단이 인벤토리를 바로 봄)
    private void CheatAnubis()
    {
        GrantItem(AnubisItemIds.GoldJar);
        GrantItem(AnubisItemIds.ClayPot);
        GrantItem(AnubisItemIds.Obsidian);
        GrantItem(AnubisItemIds.BronzeBowl);

        int scrolls = 0;
        foreach (AnubisScrollPiece piece in FindObjectsByType<AnubisScrollPiece>(FindObjectsSortMode.None))
        {
            piece.Interact(null);
            scrolls++;
        }
        Toast($"[치트] 아누비스 보상 4개 + 양피지 {scrolls}개 -> 메인 저울로");
    }

    private static void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ================= 순서 고정 =================
    // 바스테트 엔딩 다음 = 중앙 홀 (원래는 아누비스 튜토리얼). 씬 파일은 그대로 두고 실행 중에만 바꿈
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        try
        {
            if (!scene.name.Contains("Bastet_Tliemap") && !scene.name.Contains("Bastet_Tilemap")) return;
            int changed = 0;
            changed += OverrideNextScene<BastetEnding>("nextSceneName");
            changed += OverrideNextScene<BastetSunMoonWalk>("nextSceneName");
            Debug.Log($"[DemoCheat] 바스테트 다음 씬 -> {HallScene} ({changed}곳)");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DemoCheat] 순서 고정 오류: {e}");
        }
    }

    private static int OverrideNextScene<T>(string fieldName) where T : MonoBehaviour
    {
        int count = 0;
        FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null || field.FieldType != typeof(string)) return 0;
        foreach (T c in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            field.SetValue(c, HallScene);
            count++;
        }
        return count;
    }

    // ================= 도우미 =================
    private static bool HasFlag(string flag)
    {
        return GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }

    private static void SetFlags(string[] flags)
    {
        if (GameManager.Instance == null) return;
        foreach (string f in flags)
        {
            if (!GameManager.Instance.HasFlag(f)) GameManager.Instance.SetFlag(f);
        }
    }

    private static void GrantItem(string id)
    {
        if (ItemInventory.Instance != null && !ItemInventory.Instance.Has(id)) ItemInventory.Instance.Grant(id);
    }

    // ================= 화면 위 짧은 안내 =================
    private void BuildToast()
    {
        RectTransform canvas = UIBuild.CreateCanvas(transform, "DemoCheatCanvas", 250, out GameObject canvasGo);
        canvasGo.GetComponent<GraphicRaycaster>().enabled = false;
        RectTransform box = UIBuild.Rect(canvas, "Toast", new Vector2(0f, 380f), new Vector2(900f, 56f));
        toastGroup = box.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;
        UIBuild.Image(box, "Bg", Vector2.zero, new Vector2(900f, 56f), null, new Color(0f, 0f, 0f, 0.7f));
        toastText = UIBuild.Text(box, "Text", PuzzleUITheme.GetBodyFont(), 26, Vector2.zero, new Vector2(880f, 50f), new Color(1f, 0.85f, 0.35f));
    }

    private void Toast(string message)
    {
        Debug.Log($"[DemoCheat] {message}");
        if (toastText == null) return;
        toastText.text = message;
        toastTimer = 2.5f;
    }

    private void UpdateToast()
    {
        if (toastGroup == null) return;
        toastTimer -= Time.unscaledDeltaTime;
        toastGroup.alpha = Mathf.Clamp01(toastTimer * 2f);
    }
}
