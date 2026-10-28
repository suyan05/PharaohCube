using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// [임시] 중앙 홀 진행 (중앙 홀 맵, 아트가 오기 전까지)
// 1. 스핑크스 계단을 내려온 직후: 자막 -> 어두운 홀 -> 가운데 큐브가 희미하게 빛남
// 2. 큐브가 크게 빛나고 빛 덩어리가 화로로 흘러감 -> 화로에 불이 붙음 -> 목소리
// 3. 목표: 횃불 걸이에서 횃불 집기 (Inspectable, F_HALL_TORCH_TAKEN)
// 4. 목표: 화로에 대고 불 붙이기 (Inspectable, 길게 누르기 -> ITEM_TORCH -> 내 횃불 켜짐)
// 5. 목표: 북쪽 문으로 라의 신전에 들어가기 (SceneDoor, 횃불 필요)
// 6. [추가] 신전을 끝내고 돌아오면 (Routes 칸): 북쪽 문 앞에서 시작 -> 큐브 앞으로 걸어감
//    -> 내 몸에서 빛이 큐브로 흘러가 면이 깨어남 -> 목소리 -> 북쪽 문이 다음 신전으로 바뀜
public class HallTempDirector : MonoBehaviour
{
    [Header("연결 (배치 도구가 채움)")]
    [SerializeField] private HallFlame cube;
    [SerializeField] private HallFlame brazier;
    [SerializeField] private Inspectable rack;
    [SerializeField] private Inspectable brazierInspect;
    [SerializeField] private Transform door;
    [SerializeField] private Vector2 arrivePoint = new Vector2(9f, 0.8f);
    [SerializeField] private Vector2 walkPoint = new Vector2(9f, 3f);

    [Header("플래그")]
    [SerializeField] private string introFlag = "F_HALL_TEMP_INTRO";

    [Header("목표 문구")]
    [SerializeField] private string objRack = "횃불 걸이에서 횃불을 집자";
    [SerializeField] private string objBrazier = "화로에 횃불을 대고 불을 붙이자";
    [SerializeField] private string objDoor = "북쪽 문으로 라의 신전에 들어가자";

    [Header("신전에서 돌아왔을 때 (순서대로)")]
    [SerializeField] private HallRoute[] routes = HallRoute.Defaults();
    [Tooltip("돌아왔을 때 서 있는 곳 (북쪽 문 바로 앞)")]
    [SerializeField] private Vector2 returnPoint = new Vector2(9f, 8.6f);
    [Tooltip("큐브 앞까지 걸어갈 곳")]
    [SerializeField] private Vector2 returnWalkPoint = new Vector2(9f, 6.8f);

    [Header("대사")]
    [SerializeField] private HallTempLines lines = new HallTempLines();

    private string objective = "";
    private bool started;

    private void Awake()
    {
        ScreenFader.SetBlack();
    }

    private IEnumerator Start()
    {
        yield return null;
        HallRoute back = PendingReturn();
        if (back != null) yield return ReturnFrom(back);
        else if (!HasFlag(introFlag)) yield return Intro();
        else
        {
            LightRoom();
            yield return ScreenFader.FadeIn(0.8f);
        }

        ApplyRoute();
        EnableDoorAutoEnter();
        Subscribe();
        started = true;
        RefreshObjective();
    }

    private void Subscribe()
    {
        Cine.Ended += ShowObjective;
        if (rack != null) rack.OnCompleted += _ => RefreshObjective();
        if (brazierInspect != null) brazierInspect.OnCompleted += _ => StartCoroutine(AfterTorch());
    }

    private void OnDestroy()
    {
        Cine.Ended -= ShowObjective;
    }

    // ================= 첫 연출 =================
    private IEnumerator Intro()
    {
        Transform player = CineActor.Player();
        Cine.Begin(true);
        if (player != null) CineActor.Place(player, player.GetComponent<Rigidbody2D>(), arrivePoint);
        CineCamera.Focus(walkPoint);
        CineCamera.Zoom(4f);
        CineCamera.SnapZoom();

        yield return TitleCardUI.Show(lines.title, lines.subtitle, 2f);
        yield return ScreenFader.FadeIn(Cine.Skipping ? 0.2f : 1.4f);
        yield return CineActor.WalkTo(player, walkPoint, 1.6f);
        yield return EmoteBalloonUI.Show(player, "...", 0.9f);
        yield return Cine.Say(lines.arrive);

        yield return CubeToBrazier(player);
        yield return Cine.Say(lines.voice);
        yield return LookAtRack(player);
        Finish();
    }

    private IEnumerator CubeToBrazier(Transform player)
    {
        if (cube != null)
        {
            CineCamera.Focus(cube.transform.position + Vector3.up);
            CineCamera.Zoom(5f, 1.2f);
            yield return Cine.Wait(1f);
            yield return cube.Pulse(3f, 1.2f);
        }
        yield return EmoteBalloonUI.Show(player, "!", 0.6f);
        if (cube != null && brazier != null) yield return LightStream(cube.transform.position + Vector3.up * 1.2f, brazier.transform.position + Vector3.up * 0.8f);
        if (brazier != null)
        {
            CineCamera.Shake(0.4f, 0.06f);
            yield return brazier.Ignite();
        }
    }

    // 큐브에서 화로까지 빛 덩어리가 날아감 (부드러운 원 + 작은 빛)
    private IEnumerator LightStream(Vector3 from, Vector3 to)
    {
        GameObject orb = CreateOrb(from);
        CineCamera.Focus((from + to) * 0.5f);
        float t = 0f;
        while (t < 1.1f && orb != null)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / 1.1f);
            orb.transform.position = Vector3.Lerp(from, to, p) + Vector3.up * Mathf.Sin(p * Mathf.PI) * 1.2f;
            CineFX.Sparkle(FxSprites.Dots, orb.transform.position, 1);
            yield return null;
        }
        if (orb != null) Destroy(orb);
        CineCamera.Focus(to);
    }

    private static GameObject CreateOrb(Vector3 at)
    {
        var go = new GameObject("LightOrb");
        go.transform.position = at;
        go.transform.localScale = Vector3.one * 1.4f;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FxSprites.Soft;
        sr.color = new Color(1f, 0.85f, 0.45f, 0.9f);
        sr.sortingOrder = 50;

        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(1f, 0.8f, 0.45f);
        light.pointLightOuterRadius = 3f;
        light.intensity = 1.5f;
        return go;
    }

    private IEnumerator LookAtRack(Transform player)
    {
        if (rack != null)
        {
            CineCamera.Focus(rack.transform.position + Vector3.up);
            yield return Cine.Wait(1.2f);
        }
        yield return Cine.Say(lines.needTorch);
    }

    private void Finish()
    {
        if (GameManager.Instance != null && !HasFlag(introFlag)) GameManager.Instance.SetFlag(introFlag);
        Cine.End();
    }

    // ================= 횃불 얻은 뒤 =================
    private IEnumerator AfterTorch()
    {
        Cine.Begin(false);
        yield return Cine.Say(lines.gotTorch);
        if (door != null)
        {
            CineCamera.Focus(door.position + Vector3.up);
            yield return Cine.Wait(1.3f);
        }
        yield return Cine.Say(lines.toDoor);
        RefreshObjective();
        Cine.End();
    }

    // ================= 신전에서 돌아왔을 때 =================
    // 끝냈는데 아직 돌아온 연출을 안 본 신전 (앞에서부터)
    private HallRoute PendingReturn()
    {
        if (routes == null) return null;
        foreach (HallRoute r in routes)
        {
            if (r != null && HasFlag(r.clearFlag) && !HasFlag(r.returnedFlag)) return r;
        }
        return null;
    }

    // 끝낸 신전 중 가장 마지막 (북쪽 문이 이 신전의 다음으로 감)
    private HallRoute LatestCleared()
    {
        HallRoute latest = null;
        if (routes == null) return null;
        foreach (HallRoute r in routes)
        {
            if (r != null && HasFlag(r.clearFlag)) latest = r;
        }
        return latest;
    }

    private IEnumerator ReturnFrom(HallRoute route)
    {
        Transform player = CineActor.Player();
        Cine.Begin(true);
        LightRoom();
        if (player != null) CineActor.Place(player, player.GetComponent<Rigidbody2D>(), returnPoint);
        CineCamera.Focus(returnWalkPoint);
        CineCamera.Zoom(4.5f);
        CineCamera.SnapZoom();

        yield return TitleCardUI.Show(lines.title, route.faceTitle, 1.6f);
        yield return ScreenFader.FadeIn(Cine.Skipping ? 0.2f : 1.2f);
        if (player != null) yield return CineActor.WalkTo(player, returnWalkPoint, 1.6f);
        yield return Cine.Say(lines.returnArrive);
        yield return FaceAwakens(player);
        yield return Cine.Say(route.cubeLines);
        yield return LookAtDoor();

        SetFlagOnce(route.returnedFlag);
        SetFlagOnce(introFlag); // 홀 첫 연출을 건너뛰고 바로 온 경우(테스트)에도 다시 안 나오게
        Cine.End();
    }

    // 내 몸에서 빛이 큐브로 흘러감 -> 큐브가 크게 빛남 + 흔들림 + 쿠르릉
    private IEnumerator FaceAwakens(Transform player)
    {
        if (cube == null) yield break;
        Vector3 top = cube.transform.position + Vector3.up * 1.2f;
        if (player != null) yield return LightStream(player.position + Vector3.up * 0.8f, top);

        CineCamera.Zoom(4f, 1f);
        CineCamera.Shake(0.6f, 0.08f);
        GameAudio.Play("sfx_rumble", 0.9f);
        GameAudio.Play("sfx_dream_shimmer", 0.7f);
        StartCoroutine(ScreenTintUI.Flash(new Color(1f, 0.85f, 0.5f, 0.45f), 0.08f, 0.1f, 0.6f));
        yield return cube.Pulse(3.5f, 1.6f);
    }

    private IEnumerator LookAtDoor()
    {
        if (door == null) yield break;
        CineCamera.Focus(door.position + Vector3.up);
        CineCamera.Shake(0.4f, 0.05f);
        GameAudio.Play("sfx_rumble", 0.6f, 1.2f);
        yield return Cine.Wait(1.3f);
    }

    // 화로는 계속 켜져 있고, 신전을 하나라도 끝냈으면 큐브도 켜진 채로
    private void LightRoom()
    {
        if (brazier != null) brazier.SetInstant(true);
        if (cube != null && LatestCleared() != null) cube.SetInstant(true);
    }

    // [추가] 북쪽 문은 E 없이 닿기만 하면 들어감
    private void EnableDoorAutoEnter()
    {
        if (door == null) return;
        SceneDoor sceneDoor = door.GetComponent<SceneDoor>();
        if (sceneDoor != null) sceneDoor.SetAutoEnter(true);
    }

    // 북쪽 문을 다음 신전으로 바꿈
    private void ApplyRoute()
    {
        HallRoute r = LatestCleared();
        if (r == null || door == null) return;
        SceneDoor sceneDoor = door.GetComponent<SceneDoor>();
        if (sceneDoor != null) sceneDoor.Retarget(r.nextPrompt, r.nextScene);
        else Debug.LogWarning("[HallTempDirector] 북쪽 문에 SceneDoor가 없어서 다음 신전으로 못 바꿈");
    }

    // ================= 목표 =================
    private void RefreshObjective()
    {
        HallRoute cleared = LatestCleared();
        if (cleared != null) objective = cleared.objective;
        else if (ItemInventory.Instance != null && ItemInventory.Instance.Has("ITEM_TORCH")) objective = objDoor;
        else if (rack != null && rack.IsDone) objective = objBrazier;
        else objective = objRack;
        ShowObjective();
    }

    private void ShowObjective()
    {
        if (!started || Cine.Running) return;
        try
        {
            ObjectiveBoxUI.Show(objective);
        }
        catch (Exception e)
        {
            Debug.LogError($"[HallTempDirector] 목표 표시 오류: {e}");
        }
    }

    private static bool HasFlag(string flag)
    {
        return !string.IsNullOrEmpty(flag) && GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }

    private static void SetFlagOnce(string flag)
    {
        if (string.IsNullOrEmpty(flag) || GameManager.Instance == null || GameManager.Instance.HasFlag(flag)) return;
        GameManager.Instance.SetFlag(flag);
    }
}
