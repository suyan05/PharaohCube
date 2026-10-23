using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// [히든] 라의 불씨가 숨어 있는 "태양의 배" 모형 (서쪽 방 왼쪽 아래 구석)
// - 낮: 그냥 배 모형. 조사하면 받침대 글귀 "라의 불은 해가 진 뒤에도 배 안에서 꺼지지 않는다" (복선, F_RA_BOAT_SEEN)
// - 밤(F_RA_NIGHT_OPEN): 신전 불이 다 꺼지면 배 안에서만 작은 불빛이 깜빡임 + 불똥이 피어오름 (어둠 속 단서)
// - 밤에 E 길게 -> 불씨가 배에서 떠올라 내 횃불로 날아와 합쳐지는 연출 -> 라의 불씨 획득 (ITEM_RA_EMBER, 금테 팝업)
// - 불씨 효과는 PlayerTorchTemp가 이미 처리: 시야 1.3배, 시간으로 안 꺼짐, 꺼져도 E 2초로 어디서든 다시 붙임
[RequireComponent(typeof(Collider2D))]
public class RaSunBoat : MonoBehaviour, IInteractable
{
    [Header("아이템, 플래그")]
    [SerializeField] private string emberItemId = "ITEM_RA_EMBER";
    [SerializeField] private string seenFlag = "F_RA_BOAT_SEEN";
    [SerializeField] private string foundFlag = "F_RA_EMBER_FOUND";

    [Header("연결 (배치 도구가 채움)")]
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Light2D emberLight;
    [SerializeField] private SpriteRenderer emberGlow;
    [Tooltip("배 그림 기준 불씨 자리 (선실 입구)")]
    [SerializeField] private Vector3 emberLocalPos = new Vector3(0f, 0.67f, 0f);

    [Header("불빛")]
    [SerializeField] private Color emberColor = new Color(1f, 0.62f, 0.25f);
    [SerializeField, Min(0f)] private float glowIntensity = 1.6f;
    [SerializeField, Min(0.05f)] private float sparkEvery = 0.45f;

    [Header("길게 누르기")]
    [SerializeField, Min(0.1f)] private float holdSeconds = 1.5f;
    [SerializeField] private float holdRange = 1.9f;

    [Header("대사")]
    [SerializeField] private DialogueLine[] dayLines = RaSunBoatLines.Day;
    [SerializeField] private DialogueLine[] emptyLines = RaSunBoatLines.Empty;
    [SerializeField] private DialogueLine[] foundLines = RaSunBoatLines.Found;

    private bool holding, taking;
    private float progress, sparkTimer;
    private Transform player;

    private bool Available => RaPhaseState.IsNight && !HasFlag(foundFlag) && !HasEmber();

    // ================= IInteractable =================
    public string GetPrompt()
    {
        if (taking) return null;
        return Available ? "E 길게: 배 안을 들여다보기" : "E: 배 모형 살펴보기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (taking || Cine.Running || DialogueBox.IsOpen || ItemPopup.IsShowing) return;
            if (DialogueBox.ClosedFrame == Time.frameCount) return;

            if (Available)
            {
                player = who != null ? who.transform : null;
                holding = true;
                return;
            }
            bool empty = HasFlag(foundFlag) || HasEmber();
            DialogueBox.Play(empty ? emptyLines : dayLines, () => SetFlagOnce(seenFlag));
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaSunBoat] 조사 오류: {e}");
        }
    }

    // ================= 매 프레임 =================
    private void Update()
    {
        if (!taking) UpdateGlow();
        UpdateHold();
    }

    private void UpdateGlow()
    {
        bool on = Available;
        float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 2.4f) + 0.1f * Mathf.PerlinNoise(Time.time * 4f, 3.1f);
        float target = on ? glowIntensity * pulse : 0f;
        if (emberLight != null) emberLight.intensity = Mathf.MoveTowards(emberLight.intensity, target, Time.deltaTime * 2f);
        SetGlowAlpha(on ? 0.55f * pulse : 0f, false);

        if (!on || body == null) return;
        sparkTimer -= Time.deltaTime;
        if (sparkTimer > 0f) return;
        sparkTimer = sparkEvery * UnityEngine.Random.Range(0.6f, 1.4f);
        RaGlowFx.Spark(EmberWorld(), emberColor, body.sortingLayerName, body.sortingOrder + 2);
    }

    private void UpdateHold()
    {
        if (!holding && progress <= 0f) return;

        bool pressing = holding && Input.GetKey(KeyCode.E) && InRange() && !PlayerLock.IsLocked && Available;
        if (!pressing) holding = false;

        progress = Mathf.Clamp01(progress + (pressing ? 1f : -1.5f) * Time.deltaTime / holdSeconds);
        if (progress > 0f) HoldRingUI.Show(transform.position, progress);
        else HoldRingUI.Hide();
        if (progress < 1f) return;

        holding = false;
        progress = 0f;
        HoldRingUI.Hide();
        StartCoroutine(TakeEmber());
    }

    private bool InRange()
    {
        return player != null && Vector2.Distance(player.position, transform.position) <= holdRange;
    }

    // ================= 불씨 얻기 연출 =================
    private IEnumerator TakeEmber()
    {
        taking = true;
        Transform p = CineActor.Player();
        Cine.Begin(false);

        yield return Reveal();
        yield return RaEmberFlight.Fly(EmberWorld(), p, emberColor, body);
        SetGlowAlpha(0f, true);
        if (emberLight != null) emberLight.intensity = 0f;
        yield return Receive(p);

        Cine.End();
        taking = false;
    }

    // 카메라가 배로 다가가고, 배 안의 불이 크게 숨쉬듯 밝아짐
    private IEnumerator Reveal()
    {
        CineCamera.Focus(EmberWorld());
        CineCamera.Zoom(2.6f, 1.2f);
        yield return Cine.Wait(0.9f);

        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(t / 1.2f * Mathf.PI * 0.5f);
            if (emberLight != null) emberLight.intensity = glowIntensity * (1f + 1.5f * k);
            SetGlowAlpha(0.6f + 0.4f * k, true);
            if (body != null && UnityEngine.Random.value < 0.35f) RaGlowFx.Spark(EmberWorld(), emberColor, body.sortingLayerName, body.sortingOrder + 2);
            yield return null;
        }
    }

    // 불씨가 내 횃불에 스며듦 -> 금빛 번쩍 -> 횃불이 더 크게 -> 팝업 -> 혼잣말
    private IEnumerator Receive(Transform p)
    {
        StartCoroutine(ScreenTintUI.Flash(new Color(1f, 0.75f, 0.35f, 0.55f), 0.08f, 0.1f, 0.6f));
        CineCamera.Shake(0.25f, 0.05f);
        if (TorchLink.Current != null && !TorchLink.Current.IsLit) TorchLink.Current.Relight("");
        if (p != null) CineCamera.Focus(p.position + Vector3.up * 0.5f);

        bool closed = false;
        ItemPopup.Grant(emberItemId, () => closed = true);
        while (!closed) yield return null;

        SetFlagOnce(foundFlag);
        SetFlagOnce(seenFlag);
        if (p != null) yield return EmoteBalloonUI.Show(p, "!", 0.8f);
        yield return Cine.Say(foundLines);
    }

    // ================= 도우미 =================
    private Vector3 EmberWorld()
    {
        return transform.TransformPoint(emberLocalPos);
    }

    private void SetGlowAlpha(float alpha, bool instant)
    {
        if (emberGlow == null) return;
        Color c = emberGlow.color;
        c.a = instant ? alpha : Mathf.MoveTowards(c.a, alpha, Time.deltaTime * 2f);
        emberGlow.color = c;
    }

    private bool HasEmber()
    {
        return ItemInventory.Instance != null && ItemInventory.Instance.Has(emberItemId);
    }

    private static bool HasFlag(string flag)
    {
        return !string.IsNullOrEmpty(flag) && GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }

    private static void SetFlagOnce(string flag)
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && !string.IsNullOrEmpty(flag) && !gm.HasFlag(flag)) gm.SetFlag(flag);
    }
}
