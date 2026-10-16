using System;
using System.Collections.Generic;
using UnityEngine;

// 메인 저울(심판) 실패 처벌 - 씬을 다시 불러오지 않고 진행만 되돌림
// 1) 서브 퍼즐 보상 4개를 회수 (망자의 심장은 남김)
//    -> 제단 잠금(AnubisAltarLock), 석문(AnubisItemGate), 안내 연출(AnubisZoneGuide)이
//       ItemInventory.OnItemConsumed를 받아 스스로 다시 잠기고 어두워짐
// 2) 서브 퍼즐 4개 + 메인 저울을 처음 상태로 (IAnubisRetryable)
// 3) 메인 저울 창을 닫고 플레이어를 A1 앞으로 이동
// 4) 양피지 조각, 사자의 서 단서는 그대로 남김 (이미 알아낸 정보는 유지)
// 5) OnPenaltyApplied 이벤트 -> 다음 단계(서브 퍼즐 변형 교체)에서 연결
public class AnubisJudgmentPenalty : MonoBehaviour
{
    [Header("회수할 보상 (서브 퍼즐 보상 4개)")]
    [SerializeField]
    private string[] itemsToTakeBack =
    {
        AnubisItemIds.BronzeBowl, AnubisItemIds.Obsidian, AnubisItemIds.ClayPot, AnubisItemIds.GoldJar
    };

    [Header("연결")]
    [Tooltip("메인 저울 제단 (Altar_A5_Main). 처벌 후 이 제단의 퍼즐 창을 닫음")]
    [SerializeField] private AnubisAltarStation mainAltarStation;
    [Tooltip("처벌 후 플레이어가 돌아갈 위치 (A1 제단 앞). 비우면 이동 안 함")]
    [SerializeField] private Transform respawnPoint;
    [Tooltip("비우면 Main Camera에서 자동으로 찾음")]
    [SerializeField] private CameraFollow2D cameraFollow;

    [Header("메시지")]
    [TextArea(2, 4)]
    [SerializeField] private string penaltyMessage = "암무트가 네 개의 보물을 삼켰습니다.\n시험의 제단이 다시 봉인되었습니다. 첫 번째 시험부터 다시 시작하십시오.";
    [SerializeField, Range(1f, 8f)] private float messageTime = 5f;

    // 변형 세트 교체 등 다음 단계에서 구독 (실패 횟수 전달)
    public event Action<int> OnPenaltyApplied;

    public int FailCount { get; private set; }

    private readonly List<IAnubisRetryable> retryables = new List<IAnubisRetryable>();

    private void Start()
    {
        CacheRetryables();
        if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
    }

    // 퍼즐 패널은 대부분 꺼져 있으니 비활성 오브젝트까지 포함해서 한 번만 찾아 둠
    private void CacheRetryables()
    {
        retryables.Clear();
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb is IAnubisRetryable r) retryables.Add(r);
        }
        Debug.Log($"[{name}] 되돌릴 퍼즐 {retryables.Count}개 찾음", this);
    }

    // 메인 저울의 "다시 도전" 버튼이 호출
    public void ApplyPenalty()
    {
        try
        {
            FailCount++;
            Debug.Log($"[{name}] 심판 실패 처벌 시작 ({FailCount}번째)", this);

            TakeBackRewards();
            ResetPuzzles();

            if (mainAltarStation != null) mainAltarStation.Close(); // 플레이어 이동 잠금도 풀림
            MovePlayerToRespawn();

            AnubisToast.Show(penaltyMessage, messageTime);
            OnPenaltyApplied?.Invoke(FailCount);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 실패 처벌 중 오류: {e}", this);
        }
    }

    private void TakeBackRewards()
    {
        if (ItemInventory.Instance == null)
        {
            Debug.LogError($"[{name}] ItemInventory가 없어 보상을 회수할 수 없음", this);
            return;
        }

        foreach (string id in itemsToTakeBack)
        {
            if (!string.IsNullOrEmpty(id) && ItemInventory.Instance.Has(id))
                ItemInventory.Instance.Consume(id); // -> OnItemConsumed -> 제단 / 석문 / 안내가 다시 잠김
        }
    }

    private void ResetPuzzles()
    {
        foreach (IAnubisRetryable r in retryables)
        {
            try
            {
                r.ResetForRetry();
            }
            catch (Exception e)
            {
                Debug.LogError($"[{name}] {r} 되돌리기 오류: {e}", this);
            }
        }
    }

    private void MovePlayerToRespawn()
    {
        if (respawnPoint == null) return;

        var player = FindAnyObjectByType<PlayerController2D>();
        if (player == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = respawnPoint.position;
        }
        player.transform.position = respawnPoint.position;

        if (cameraFollow != null) cameraFollow.SnapToTarget();
    }

    [ContextMenu("테스트: 실패 처벌 바로 적용 (Play 중)")]
    private void TestPenalty()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning($"[{name}] Play 중에만 사용", this);
            return;
        }
        ApplyPenalty();
    }

    // Scene 뷰: 되돌아갈 위치를 붉은 원으로 표시
    private void OnDrawGizmos()
    {
        if (respawnPoint == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.25f, 0.9f);
        Gizmos.DrawWireSphere(respawnPoint.position, 0.5f);
        Gizmos.DrawLine(respawnPoint.position + Vector3.up * 0.8f, respawnPoint.position + Vector3.down * 0.8f);
    }
}
