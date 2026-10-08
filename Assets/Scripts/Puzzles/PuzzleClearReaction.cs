using System;
using UnityEngine;
using UnityEngine.Events;

// 퍼즐 클리어 -> 스테이션 스프라이트를 _Active로 교체 + 인스펙터에서 연결한 이벤트 실행
// 씬을 다시 불러와도 GameManager 플래그를 보고 클리어 상태를 복원함
// PuzzleBase를 상속한 퍼즐이면 어디든 재사용 가능 (P1, P3, P4 스테이션 공용)
public class PuzzleClearReaction : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private PuzzleBase puzzle;
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("클리어 후 스프라이트 (예: Station_Sub_P1_P3_P4_Active)")]
    [SerializeField] private Sprite activeSprite;

    [Header("복원용 플래그 - 퍼즐의 clearFlag와 똑같이 입력")]
    [SerializeField] private string clearedFlag = "";

    [Header("방금 클리어했을 때 (연출 O)")]
    [SerializeField] private UnityEvent onCleared;

    [Header("씬 재진입 시 이미 클리어 상태일 때 (연출 X)")]
    [SerializeField] private UnityEvent onRestored;

    private bool applied;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        if (puzzle == null)
        {
            Debug.LogWarning($"[{name}] PuzzleClearReaction: puzzle 연결 안 됨");
            return;
        }
        puzzle.OnSuccess += HandleSuccess;
    }

    private void OnDisable()
    {
        if (puzzle != null)
        {
            puzzle.OnSuccess -= HandleSuccess;
        }
    }

    private void Start()
    {
        if (IsAlreadyCleared())
        {
            ApplyCleared(true);
        }
    }

    private void HandleSuccess()
    {
        ApplyCleared(false);
    }

    private bool IsAlreadyCleared()
    {
        if (string.IsNullOrEmpty(clearedFlag)) return false;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"[{name}] GameManager가 씬에 없음 -> 복원 건너뜀");
            return false;
        }
        return GameManager.Instance.HasFlag(clearedFlag);
    }

    private void ApplyCleared(bool restored)
    {
        if (applied) return; // 두 번 실행 방지
        applied = true;

        try
        {
            SwapSprite();

            if (restored) onRestored?.Invoke();
            else onCleared?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 클리어 반영 중 오류: {e}");
        }
    }

    private void SwapSprite()
    {
        if (targetRenderer == null || activeSprite == null)
        {
            Debug.LogWarning($"[{name}] targetRenderer 또는 activeSprite 비어 있음 -> 스프라이트 교체 생략");
            return;
        }
        targetRenderer.sprite = activeSprite;
    }
}