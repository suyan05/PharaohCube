using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 라의 방 첫 입장 연출 (씬에 1개, 빈 오브젝트에 붙임)
// 순서: 조작 잠금 -> 방 전체로 줌아웃 (불 켜진 곳만 밝게 보임) -> 잠깐 정지 + 안내
//       -> 플레이어로 줌인 -> 조작 해제
// - 한 번 보면 F_RA_INTRO_PLAYED 플래그가 켜져서 다시 안 나옴
// - 아무 키나 누르면 스킵 (바로 플레이어 화면으로)
public class RaIntroCutscene : MonoBehaviour
{
    // GameMessageUI가 이 말머리로 시작하는 로그를 오른쪽 위 알림으로 띄움
    private const string MessageTag = "[Door] ";

    [Header("연결 (비우면 Main Camera에서 자동으로 찾음)")]
    [SerializeField] private CameraFollow2D cameraFollow;

    [Header("한 번만 재생")]
    [SerializeField] private string playedFlag = "F_RA_INTRO_PLAYED";

    [Header("줌아웃 화면")]
    [Tooltip("줌아웃 때 비출 곳. 비우면 맵 가운데")]
    [SerializeField] private Transform overviewPoint;
    [Tooltip("줌아웃 Size. 0이면 맵 전체가 들어오게 자동 계산")]
    [SerializeField, Min(0f)] private float overviewSize = 0f;
    [Tooltip("자동 계산할 때 맵 바깥 여백 (유닛)")]
    [SerializeField, Min(0f)] private float mapPadding = 0.5f;

    [Header("타이밍 (초)")]
    [SerializeField, Min(0f)] private float startDelay = 0.4f;
    [SerializeField, Min(0.1f)] private float zoomOutTime = 1.4f;
    [SerializeField, Min(0f)] private float holdTime = 2.2f;
    [SerializeField, Min(0.1f)] private float zoomInTime = 1.2f;

    [Header("안내 / 스킵")]
    [Tooltip("줌아웃 상태에서 오른쪽 위에 띄울 알림 (비우면 없음)")]
    [SerializeField, TextArea] private string overviewMessage = "불이 켜진 곳만 갈 수 있다. 빛을 따라가 보자.";
    [SerializeField] private bool allowSkip = true;

    [Header("연출 중 투명하게 숨길 UI (목표 문구 RaObjectiveGuide 등)")]
    [SerializeField] private GameObject[] hideWhilePlaying;

    private readonly Dictionary<CanvasGroup, float> hiddenGroups = new Dictionary<CanvasGroup, float>();
    private PlayerController2D playerController;
    private InteractionSystem playerInteraction;
    private Rigidbody2D playerRb;
    private bool playing;
    private bool skipRequested;
    private float elapsed;

    // ================= 시작 =================
    private void Start()
    {
        try
        {
            if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
            if (cameraFollow == null)
            {
                Debug.LogWarning($"[{name}] CameraFollow2D를 못 찾아서 시작 연출을 건너뜀");
                return;
            }
            if (AlreadyPlayed()) return;

            StartCoroutine(Play());
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 시작 연출 오류: {e}");
        }
    }

    private bool AlreadyPlayed()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"[{name}] GameManager가 없어서 재생 기록을 못 남김 (이번엔 재생)");
            return false;
        }
        return GameManager.Instance.HasFlag(playedFlag);
    }

    // 연출이 시작되고 0.3초 뒤부터 스킵 가능 (입장 순간 누르던 키로 바로 넘어가는 것 방지)
    private void Update()
    {
        if (!playing || !allowSkip || skipRequested) return;

        elapsed += Time.deltaTime;
        if (elapsed > 0.3f && Input.anyKeyDown) skipRequested = true;
    }

    private void OnDisable()
    {
        if (!playing) return;
        StopAllCoroutines();
        SnapBackToPlayer();
        Finish();
    }

    // ================= 연출 =================
    private IEnumerator Play()
    {
        playing = true;
        skipRequested = false;
        elapsed = 0f;
        SetPlayerLocked(true);
        SetOtherUIHidden(true);

        yield return WaitOrSkip(startDelay);

        if (!skipRequested)
        {
            cameraFollow.FocusOn(OverviewPoint());
            cameraFollow.SetZoom(OverviewSize(), zoomOutTime * 0.5f);
            ShowMessage();
        }
        yield return WaitOrSkip(zoomOutTime + holdTime);

        if (skipRequested)
        {
            SnapBackToPlayer();
        }
        else
        {
            cameraFollow.ReleaseFocus();
            cameraFollow.ResetZoom(zoomInTime * 0.5f);
            yield return WaitOrSkip(zoomInTime);
            if (skipRequested) SnapBackToPlayer();
        }

        Finish();
    }

    private IEnumerator WaitOrSkip(float seconds)
    {
        float t = 0f;
        while (t < seconds && !skipRequested)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    private Vector3 OverviewPoint()
    {
        return overviewPoint != null ? overviewPoint.position : cameraFollow.GetMapCenter();
    }

    private float OverviewSize()
    {
        return overviewSize > 0f ? overviewSize : cameraFollow.GetFitMapSize(mapPadding);
    }

    // 스킵 / 중단: 애니메이션 없이 바로 플레이어 화면
    private void SnapBackToPlayer()
    {
        if (cameraFollow == null) return;
        cameraFollow.ReleaseFocus();
        cameraFollow.ResetZoom();
        cameraFollow.SnapZoom();
        cameraFollow.SnapToTarget();
    }

    private void Finish()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && !gm.HasFlag(playedFlag)) gm.SetFlag(playedFlag);

        SetOtherUIHidden(false);
        SetPlayerLocked(false);
        playing = false;
    }

    private void ShowMessage()
    {
        if (string.IsNullOrEmpty(overviewMessage)) return;
        Debug.Log(MessageTag + overviewMessage);
    }

    // ================= 플레이어 잠금 / UI 숨기기 =================
    // 이동(PlayerController2D)과 E키(InteractionSystem)를 같이 막음 (RaZoneLighting과 같은 방식)
    private void SetPlayerLocked(bool locked)
    {
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController2D>();
            if (playerController == null) return;
            playerInteraction = playerController.GetComponent<InteractionSystem>();
            playerRb = playerController.GetComponent<Rigidbody2D>();
        }

        playerController.enabled = !locked;
        if (playerInteraction != null) playerInteraction.enabled = !locked;
        if (locked && playerRb != null) playerRb.linearVelocity = Vector2.zero;
    }

    // 다른 UI는 끄지 않고 투명하게만 (그쪽 스크립트가 멈추지 않게)
    private void SetOtherUIHidden(bool hide)
    {
        if (hideWhilePlaying == null) return;

        foreach (GameObject go in hideWhilePlaying)
        {
            if (go == null) continue;

            CanvasGroup group = go.GetComponent<CanvasGroup>();
            if (group == null) group = go.AddComponent<CanvasGroup>();

            if (hide)
            {
                if (!hiddenGroups.ContainsKey(group)) hiddenGroups[group] = group.alpha;
                group.alpha = 0f;
            }
            else if (hiddenGroups.TryGetValue(group, out float prev))
            {
                group.alpha = prev;
            }
        }
        if (!hide) hiddenGroups.Clear();
    }

    // ================= 테스트용 (Play 중에만) =================
    [ContextMenu("테스트: 시작 연출 다시 보기")]
    private void TestReplay()
    {
        if (!Application.isPlaying || playing || cameraFollow == null) return;
        StartCoroutine(Play());
    }
}