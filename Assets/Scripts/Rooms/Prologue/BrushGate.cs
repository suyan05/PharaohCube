using System;
using UnityEngine;

// 발굴 구역 로프 입구 (기획서 2.2 마지막 줄)
// - 붓(TOOL_BRUSH)을 얻기 전: 입구가 막혀 있고, E로 말 걸면 "붓이 있어야 한다"
// - 붓을 얻으면(F_PRO_OBJ_CRATE_DONE 켜짐): 막는 콜라이더 + 입구 로프 그림이 꺼짐
// 이 오브젝트의 Collider2D(트리거 아님)가 입구를 막고, InteractionSystem도 이걸로 찾음
[RequireComponent(typeof(Collider2D))]
public class BrushGate : MonoBehaviour, IInteractable
{
    [SerializeField] private string requiredTool = "TOOL_BRUSH";
    [Tooltip("이 플래그가 켜질 때 다시 확인 (장비 상자 끝)")]
    [SerializeField] private string checkFlag = "F_PRO_OBJ_CRATE_DONE";
    [SerializeField] private string promptText = "로프 살펴보기";
    [SerializeField] private DialogueLine[] lockedLines = { new DialogueLine("", "발굴 구역이다. 붓이 있어야 한다.") };

    [Header("열리면 꺼질 것 (비우면 자기 콜라이더만)")]
    [SerializeField] private Collider2D blocker;
    [SerializeField] private GameObject closedVisual;

    private bool open;
    private bool subscribed;

    private void Start()
    {
        try
        {
            if (blocker == null) blocker = GetComponent<Collider2D>();
            Subscribe();
            Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError($"[BrushGate] 초기화 오류: {e}");
        }
    }

    private void Subscribe()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnFlagChanged += HandleFlag;
        subscribed = true;
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
    }

    private void HandleFlag(string flag)
    {
        if (flag == checkFlag) Refresh();
    }

    private void Refresh()
    {
        bool hasTool = ItemInventory.Instance != null && ItemInventory.Instance.Has(requiredTool);
        SetOpen(hasTool);
    }

    private void SetOpen(bool value)
    {
        open = value;
        if (blocker != null) blocker.enabled = !value;
        if (closedVisual != null) closedVisual.SetActive(!value);
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        return open ? null : $"E: {promptText}";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (open || DialogueBox.IsOpen || ItemPopup.IsShowing) return;
            if (DialogueBox.ClosedFrame == Time.frameCount) return;
            DialogueBox.Play(lockedLines);
        }
        catch (Exception e)
        {
            Debug.LogError($"[BrushGate] 조사 오류: {e}");
        }
    }
}
