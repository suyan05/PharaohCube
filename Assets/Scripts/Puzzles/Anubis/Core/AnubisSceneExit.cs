using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 플레이어가 닿으면 다음 씬으로 이동 (튜토리얼 → 실전 아누비스 방)
// Collider2D는 Is Trigger 체크 필수 (컴포넌트를 처음 붙일 때 자동 설정됨)
// [수정] 화면이 서서히 어두워지며 넘어감 -> 새 씬에서 다시 밝아짐
//  - 검은 화면은 씬이 바뀌어도 남는 임시 오브젝트(DontDestroyOnLoad)가 그리고, 다 밝아지면 스스로 사라짐
//  - 어두워지는 동안 플레이어 이동을 막음
[RequireComponent(typeof(Collider2D))]
public class AnubisSceneExit : MonoBehaviour
{
    [Header("이동할 씬")]
    [Tooltip("Build Profiles의 Scene List에 등록된 씬 이름")]
    [SerializeField] private string nextSceneName = "Puzzle_Test_Anubis_Tilemap";
    [SerializeField] private string playerTag = "Player";

    [Header("연출")]
    [TextArea(1, 3)]
    [SerializeField] private string exitMessage = "진짜 심판의 신전으로 내려갑니다...";
    [Tooltip("[추가] 화면이 완전히 어두워지는 시간 (초)")]
    [Range(0.2f, 4f)]
    [SerializeField] private float fadeOutTime = 1.5f;
    [Tooltip("[추가] 어두운 화면에서 글을 보여 주는 시간 (초)")]
    [Range(0f, 4f)]
    [SerializeField] private float holdTime = 1.0f;
    [Tooltip("[추가] 새 씬에서 다시 밝아지는 시간 (초)")]
    [Range(0.2f, 4f)]
    [SerializeField] private float fadeInTime = 1.2f;
    [Tooltip("[추가] 어두운 화면 가운데 글자 폰트 (MonoplexKR-Regular SDF). 비우면 TMP 기본 폰트")]
    [SerializeField] private TMP_FontAsset font;

    [Header("[추가] 나가기 전에 회수할 아이템")]
    [Tooltip("튜토리얼에서 얻은 보상을 지움. 소지품이 씬을 넘어 유지되는 경우(라 방 Managers) 실전 제단이 미리 열리는 것 방지")]
    [SerializeField]
    private string[] itemsToRemoveOnExit =
    {
        AnubisItemIds.GoldJar, AnubisItemIds.ClayPot, AnubisItemIds.Obsidian, AnubisItemIds.BronzeBowl,
        "SCROLL_ANUBIS_TUT_1", "SCROLL_ANUBIS_TUT_2", "SCROLL_ANUBIS_TUT_3", "SCROLL_ANUBIS_TUT_4"
    };

    private bool used;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !other.CompareTag(playerTag)) return;

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"[{name}] '{nextSceneName}' 씬이 Build Profiles의 Scene List에 없음", this);
            return;
        }

        used = true;
        LockPlayer(other.gameObject);
        RemoveTutorialItems();
        Debug.Log($"[{name}] '{nextSceneName}'(으)로 이동 시작 (화면 어두워짐)", this);

        // [수정] 바로 Invoke로 넘기지 않고, 씬이 바뀌어도 남는 페이더가 처리
        AnubisSceneFader.Begin(nextSceneName, exitMessage, font, fadeOutTime, holdTime, fadeInTime);
    }

    private void RemoveTutorialItems()
    {
        ItemInventory inv = ItemInventory.Instance;
        if (inv == null || itemsToRemoveOnExit == null) return;
        foreach (string id in itemsToRemoveOnExit)
        {
            if (!string.IsNullOrEmpty(id) && inv.Has(id)) inv.Consume(id);
        }
    }

    // 어두워지는 동안 움직이지 않게
    private static void LockPlayer(GameObject player)
    {
        var controller = player.GetComponentInParent<PlayerController2D>();
        if (controller != null) controller.enabled = false;
        var rb = player.GetComponentInParent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    [ContextMenu("테스트: 지금 바로 다음 씬으로 (Play 중)")]
    private void TestGo()
    {
        if (!Application.isPlaying || used) return;
        used = true;
        AnubisSceneFader.Begin(nextSceneName, exitMessage, font, fadeOutTime, holdTime, fadeInTime);
    }

    // Scene 뷰: 출구 범위 (하늘색)
    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
    }
}

// [추가] 화면 어둡게 -> 씬 불러오기 -> 다시 밝게 (씬 전환 동안만 존재하는 임시 오브젝트)
public class AnubisSceneFader : MonoBehaviour
{
    private Image black;
    private TextMeshProUGUI label;

    public static void Begin(string sceneName, string message, TMP_FontAsset font, float fadeOut, float hold, float fadeIn)
    {
        var go = new GameObject("AnubisSceneFader");
        DontDestroyOnLoad(go); // 씬이 바뀌어도 검은 화면 유지
        var fader = go.AddComponent<AnubisSceneFader>();
        fader.Build(message, font);
        fader.StartCoroutine(fader.Run(sceneName, fadeOut, hold, fadeIn));
    }

    private IEnumerator Run(string sceneName, float fadeOut, float hold, float fadeIn)
    {
        // 1) 서서히 어두워짐 (글자도 같이 나타남)
        yield return Fade(0f, 1f, fadeOut);
        if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

        // 2) 어두운 상태에서 씬 불러오기
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone) yield return null;
        yield return null; // 새 씬 첫 프레임(Start) 지나고

        // 3) 서서히 밝아짐 -> 사라짐
        yield return Fade(1f, 0f, fadeIn);
        Destroy(gameObject);
    }

    private IEnumerator Fade(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime; // 일시정지(timeScale 0)여도 진행
            SetAlpha(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / time)));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        black.color = new Color(0f, 0f, 0f, a);
        label.alpha = a;
    }

    private void Build(string message, TMP_FontAsset font)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 모든 UI 위
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>(); // 어두운 동안 뒤 UI 클릭 막기

        var bgGo = new GameObject("Black", typeof(RectTransform), typeof(Image));
        var bg = (RectTransform)bgGo.transform;
        bg.SetParent(transform, false);
        bg.anchorMin = Vector2.zero; // 화면 전체
        bg.anchorMax = Vector2.one;
        bg.offsetMin = bg.offsetMax = Vector2.zero;
        black = bgGo.GetComponent<Image>();
        black.raycastTarget = true;

        var textGo = new GameObject("Message", typeof(RectTransform));
        var trt = (RectTransform)textGo.transform;
        trt.SetParent(transform, false);
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(1400f, 200f);
        label = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = message;
        label.fontSize = 40;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.91f, 0.70f, 0.23f); // 금색
        label.raycastTarget = false;

        SetAlpha(0f);
    }
}
