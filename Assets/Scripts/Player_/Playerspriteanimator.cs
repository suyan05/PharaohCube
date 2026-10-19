using System;
using UnityEngine;

// 플레이어 도트 애니메이션 (Animator 없이 스크립트로 그림만 바꿈)
// - Rigidbody2D 속도로 방향(아래/왼쪽/오른쪽/위)과 걷는 중인지 판단 -> PlayerController2D는 그대로
// - 걷기: 방향별 프레임을 walkPattern 순서로 재생 (쯔꾸르식 3프레임 = 0,1,2,1)
// - 멈춤: 마지막으로 본 방향의 idleFrame
// - E키 조사: InteractionSystem.OnInteracted -> 조사 프레임을 잠깐 보여줌
// Player 오브젝트(SpriteRenderer가 있는 곳)에 붙임
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSpriteAnimator : MonoBehaviour
{
    public enum Facing { Down, Left, Right, Up }

    [Header("걷기 프레임 (방향마다 시트 조각을 순서대로 넣기)")]
    [SerializeField] private Sprite[] downFrames;
    [SerializeField] private Sprite[] leftFrames;
    [SerializeField] private Sprite[] rightFrames;
    [SerializeField] private Sprite[] upFrames;
    [Tooltip("왼쪽 프레임이 비어 있으면 오른쪽 프레임을 좌우로 뒤집어서 씀")]
    [SerializeField] private bool flipRightForLeft = true;

    [Header("걷기 재생")]
    [Tooltip("프레임 번호 재생 순서 (0부터). 3프레임 시트는 0,1,2,1 / 4프레임 시트는 0,1,2,3")]
    [SerializeField] private int[] walkPattern = { 0, 1, 2, 1 };
    [Tooltip("멈췄을 때 보여줄 프레임 번호 (3프레임 시트는 보통 가운데 1)")]
    [SerializeField] private int idleFrame = 1;
    [SerializeField, Min(1f)] private float framesPerSecond = 8f;
    [Tooltip("이 속도보다 느리면 멈춘 것으로 봄")]
    [SerializeField, Min(0f)] private float moveThreshold = 0.05f;

    [Header("조사 동작 (E키)")]
    [SerializeField] private Sprite[] interactFrames;
    [Tooltip("켜면 조사 프레임이 방향별 1장씩 (아래, 왼쪽, 오른쪽, 위 순서로 넣기). 끄면 넣은 순서대로 재생")]
    [SerializeField] private bool interactPerDirection = false;
    [SerializeField, Min(0.05f)] private float interactTime = 0.4f;
    [Tooltip("켜면 왼쪽 조사 때 오른쪽 조사 그림(Element 2)을 좌우로 뒤집어서 씀 (왼쪽 그림이 없을 때)")]
    [SerializeField] private bool interactFlipRightForLeft = true;
    [Tooltip("켜면 E를 누를 때 조사하는 대상 쪽으로 몸을 돌림")]
    [SerializeField] private bool faceTargetOnInteract = true;
    [Tooltip("켜면 E를 누를 때 방향과 그림 이름을 Console에 찍음 (프레임 순서 확인용, 다 맞추면 끄기)")]
    [SerializeField] private bool debugInteractLog = false;

    [Header("연결 (비우면 같은 오브젝트에서 찾음)")]
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private InteractionSystem interaction;
    [SerializeField] private Facing startFacing = Facing.Down;

    private SpriteRenderer sr;
    private Facing facing;
    private float walkTimer;
    private float interactTimer;
    private bool warnedEmpty;

    public Facing CurrentFacing => facing;

    // ================= 초기화 =================
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (interaction == null) interaction = GetComponent<InteractionSystem>();
        facing = startFacing;
    }

    private void OnEnable()
    {
        if (interaction != null) interaction.OnInteracted += HandleInteracted;
    }

    private void OnDisable()
    {
        if (interaction != null) interaction.OnInteracted -= HandleInteracted;
    }

    private void HandleInteracted(IInteractable target)
    {
        try
        {
            if (faceTargetOnInteract && target is Component c) FaceToward(c.transform.position);
            if (!HasFrames(interactFrames)) return;

            interactTimer = interactTime;
            if (debugInteractLog) LogInteractFrame();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 조사 동작 오류: {e}");
        }
    }

    // 대상과의 차이가 더 큰 축으로 방향을 정함 (걷기 방향 판단과 같은 방식)
    private void FaceToward(Vector3 worldPos)
    {
        Vector2 diff = worldPos - transform.position;
        if (diff.sqrMagnitude < 0.0001f) return;

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y)) facing = diff.x > 0f ? Facing.Right : Facing.Left;
        else facing = diff.y > 0f ? Facing.Up : Facing.Down;
    }

    // 방향별 조사 그림 번호 (아래 0 / 왼쪽 1 / 오른쪽 2 / 위 3)
    // 왼쪽은 설정에 따라 오른쪽 그림을 뒤집어서 씀
    private int InteractIndex(out bool flip)
    {
        flip = false;
        if (facing == Facing.Left && interactFlipRightForLeft)
        {
            flip = true;
            return (int)Facing.Right;
        }
        return (int)facing;
    }

    private void LogInteractFrame()
    {
        bool flip = false;
        int index = interactPerDirection ? InteractIndex(out flip) : 0;
        Sprite s = interactFrames[Mathf.Clamp(index, 0, interactFrames.Length - 1)];
        string name = s != null ? s.name : "비어 있음";
        Debug.Log($"[PlayerAnim] 조사 방향: {facing} -> Element {index} = {name}{(flip ? " (좌우 뒤집음)" : "")}");
    }

    // ================= 매 프레임 =================
    // 이동은 FixedUpdate라서, 그 결과를 보고 그리도록 LateUpdate에서 처리
    private void LateUpdate()
    {
        try
        {
            bool moving = UpdateFacing();

            if (interactTimer > 0f) ShowInteract();
            else if (moving) ShowWalk();
            else ShowIdle();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 플레이어 애니메이션 오류: {e}");
            enabled = false; // 매 프레임 같은 오류 도배 방지
        }
    }

    // 속도가 있으면 더 크게 움직이는 축으로 방향을 정함. 멈춰 있으면 방향 유지
    private bool UpdateFacing()
    {
        if (body == null) return false;

        Vector2 v = body.linearVelocity;
        if (v.sqrMagnitude < moveThreshold * moveThreshold) return false;

        if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) facing = v.x > 0f ? Facing.Right : Facing.Left;
        else facing = v.y > 0f ? Facing.Up : Facing.Down;
        return true;
    }

    private void ShowWalk()
    {
        Sprite[] frames = GetFrames(out bool flip);
        if (walkPattern == null || walkPattern.Length == 0)
        {
            SetFrame(frames, idleFrame, flip);
            return;
        }

        walkTimer += Time.deltaTime;
        int step = Mathf.FloorToInt(walkTimer * framesPerSecond) % walkPattern.Length;
        SetFrame(frames, walkPattern[step], flip);
    }

    private void ShowIdle()
    {
        walkTimer = 0f;
        Sprite[] frames = GetFrames(out bool flip);
        SetFrame(frames, idleFrame, flip);
    }

    private void ShowInteract()
    {
        interactTimer -= Time.deltaTime;

        if (interactPerDirection)
        {
            int dirIndex = InteractIndex(out bool flip);
            SetFrame(interactFrames, dirIndex, flip);
            return;
        }

        float progress = 1f - Mathf.Clamp01(interactTimer / interactTime);
        int index = Mathf.FloorToInt(progress * interactFrames.Length);
        SetFrame(interactFrames, index, false);
    }

    // ================= 도우미 =================
    private Sprite[] GetFrames(out bool flip)
    {
        flip = false;
        switch (facing)
        {
            case Facing.Up: return upFrames;
            case Facing.Right: return rightFrames;
            case Facing.Left:
                if (HasFrames(leftFrames) || !flipRightForLeft) return leftFrames;
                flip = true;
                return rightFrames;
            default: return downFrames;
        }
    }

    private void SetFrame(Sprite[] frames, int index, bool flip)
    {
        if (!HasFrames(frames))
        {
            if (!warnedEmpty) Debug.LogWarning($"[{name}] {facing} 방향 프레임이 비어 있음 -> Inspector에서 시트 조각 넣기");
            warnedEmpty = true;
            return;
        }

        Sprite s = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        if (s != null) sr.sprite = s;
        sr.flipX = flip;
    }

    private static bool HasFrames(Sprite[] frames)
    {
        return frames != null && frames.Length > 0;
    }
}