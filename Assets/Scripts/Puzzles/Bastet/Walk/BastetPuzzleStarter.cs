using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 바스테트 걷는 퍼즐 공용 부품
// ================= 시작 석상 (큰 고양이 석상 / 바스테트 신상) =================
[RequireComponent(typeof(Collider2D))]
public class BastetPuzzleStarter : MonoBehaviour, IInteractable
{
    [SerializeField] private BastetWalkPuzzleBase target;

    [Header("잠금 (앞 퍼즐 보상이 있어야 시작)")]
    [SerializeField] private string requiredItemId = "";
    [TextArea(1, 2)]
    [SerializeField] private string lockedMessage = "석상이 아직 잠들어 있다. 앞의 시험을 먼저 통과하자.";
    [Tooltip("[추가] 켜면 처음 깨울 때 필요 아이템을 넣고(사라짐) 열림 상태를 플래그로 기억 (B5 신상: 발톱 인장)")]
    [SerializeField] private bool consumeRequiredItem = false;
    [Tooltip("[추가] 아이템을 넣은 뒤 켜지는 플래그 (비우면 F_BASTET_STARTER_<이름>_OPEN)")]
    [SerializeField] private string unlockFlag = "";

    [Header("앞발 휘두르기 (선택: 앞발 오브젝트)")]
    [SerializeField] private Transform paw;
    [SerializeField] private float swingAngle = 35f;
    [SerializeField] private float swingTime = 0.35f;

    [Header("[추가] 앞발 그림 교체 (아트가 오면 연결. 넣으면 회전 대신 그림을 바꿈)")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [Tooltip("Bastet_Obj_PawStatue_PawDown")]
    [SerializeField] private Sprite pawDownSprite;
    [Tooltip("Bastet_Obj_PawStatue_PawUp")]
    [SerializeField] private Sprite pawUpSprite;
    [Range(0.1f, 1f)] [SerializeField] private float pawUpTime = 0.3f;

    private Coroutine swingRoutine;
    private WaitForSeconds waitPawUp;

    public BastetWalkPuzzleBase Target => target;
    private string UnlockFlag => string.IsNullOrEmpty(unlockFlag) ? $"F_BASTET_STARTER_{name}_OPEN" : unlockFlag;
    private bool FlagOpen => consumeRequiredItem && GameManager.Instance != null && GameManager.Instance.HasFlag(UnlockFlag);
    private bool HasItem => ItemInventory.Instance != null && ItemInventory.Instance.Has(requiredItemId);
    private bool Unlocked => string.IsNullOrEmpty(requiredItemId) || FlagOpen || HasItem; // [수정] 넣은 뒤에는 플래그로 열림

    public string GetPrompt()
    {
        if (target == null) return "";
        if (!Unlocked) return "E: 잠든 석상 (잠김)";
        if (consumeRequiredItem && !FlagOpen) return "E: 석상에 흔적 넣기"; // [추가]
        return target.StarterPrompt;
    }

    public void Interact(GameObject player)
    {
        if (target == null || PuzzleOverlayFocus.IsActive) return;
        if (!Unlocked)
        {
            AnubisToast.Show(lockedMessage);
            Debug.Log($"[BastetPuzzleStarter] {name} 잠김: {requiredItemId} 필요", this);
            return;
        }
        // [추가] 처음 깨울 때 아이템을 넣고 사라지게
        if (consumeRequiredItem && !FlagOpen && !string.IsNullOrEmpty(requiredItemId))
        {
            if (ItemInventory.Instance != null) ItemInventory.Instance.Consume(requiredItemId);
            if (GameManager.Instance != null) GameManager.Instance.SetFlag(UnlockFlag);
            AnubisToast.Show("흔적을 석상에 바쳤다. 석상이 깨어난다!", 2.5f);
            Debug.Log($"[BastetPuzzleStarter] {name}: {requiredItemId} 넣음 -> {UnlockFlag}", this);
        }
        if (bodyRenderer != null && pawUpSprite != null && pawDownSprite != null)
        {
            if (swingRoutine != null) StopCoroutine(swingRoutine);
            swingRoutine = StartCoroutine(PawSwap()); // [추가] 그림 교체 방식
        }
        else if (paw != null)
        {
            if (swingRoutine != null) StopCoroutine(swingRoutine);
            swingRoutine = StartCoroutine(Swing());
        }
        else if (bodyRenderer != null && swingRoutine == null)
        {
            swingRoutine = StartCoroutine(Bounce()); // [추가] 그림 1장뿐일 때: 석상이 살짝 움찔
        }
        target.Begin();
    }

    // [추가] _PawUp 그림을 잠깐 보여 주고 _PawDown으로
    private IEnumerator PawSwap()
    {
        bodyRenderer.sprite = pawUpSprite;
        if (waitPawUp == null) waitPawUp = new WaitForSeconds(pawUpTime); // 처음 한 번만 만들고 재사용
        yield return waitPawUp;
        bodyRenderer.sprite = pawDownSprite;
        swingRoutine = null;
    }

    // [추가] 몸통을 살짝 눌렀다 펴기 (그림 1장일 때 반응)
    private IEnumerator Bounce()
    {
        Transform t = bodyRenderer.transform;
        Vector3 baseScale = t.localScale;
        float time = 0f;
        while (time < swingTime)
        {
            time += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(time / swingTime) * Mathf.PI);
            t.localScale = new Vector3(baseScale.x * (1f + 0.06f * k), baseScale.y * (1f - 0.08f * k), baseScale.z);
            yield return null;
        }
        t.localScale = baseScale;
        swingRoutine = null;
    }

    private IEnumerator Swing()
    {
        Quaternion start = paw.localRotation;
        Quaternion hit = start * Quaternion.Euler(0f, 0f, -swingAngle);
        float t = 0f;
        while (t < swingTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / swingTime) * Mathf.PI); // 내려쳤다 돌아옴
            paw.localRotation = Quaternion.Slerp(start, hit, k);
            yield return null;
        }
        paw.localRotation = start;
        swingRoutine = null;
    }

#if UNITY_EDITOR
    public void EditorSetup(BastetWalkPuzzleBase t, Transform p) { target = t; if (p != null) paw = p; }
    public void EditorSetLock(string item, string msg) { requiredItemId = item; if (!string.IsNullOrEmpty(msg)) lockedMessage = msg; }
    public void EditorSetConsume(bool consume, string flag) { consumeRequiredItem = consume; unlockFlag = flag; } // [추가]
    // [추가] 그림 연결: 몸통 그림 + 앞발 그림 2장(없으면 null). 그림 몸통을 쓰면 네모 앞발(paw)은 뺌
    public void EditorSetArt(SpriteRenderer body, Sprite up, Sprite down) { bodyRenderer = body; pawUpSprite = up; pawDownSprite = down; paw = null; }
#endif
}
