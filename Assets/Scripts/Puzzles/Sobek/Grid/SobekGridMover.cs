using UnityEngine;
using UnityEngine.InputSystem;

// 소베크 판 위에서만 쓰는 칸 단위 이동 (판에 들어가면 SobekBoard가 플레이어에 자동으로 붙임)
// - 팀 PlayerController2D(자유 이동)는 판 위에서 꺼 둠 -> 판에서 나가면 다시 켬
// - 방향키/WASD를 누르고 있으면 한 칸씩 계속 걸음, Z(또는 Backspace) = 한 걸음 되돌리기, R = 이 판 처음으로
// - 걷는 동안 Rigidbody2D 속도를 넣어서 팀 걷기 애니메이션(PlayerSpriteAnimator)이 그대로 돎
[DisallowMultipleComponent]
public class SobekGridMover : MonoBehaviour
{
    [Tooltip("한 칸 걷는 시간 (초)")]
    [Range(0.05f, 0.5f)] [SerializeField] private float stepTime = 0.16f;

    private SobekBoard board;
    private Rigidbody2D body;
    private PlayerController2D controller;
    private RigidbodyType2D savedBodyType;
    private bool stepping;
    private Vector2 stepTarget;
    private Vector2 stepDir;
    private float stepSpeed;

    public bool IsStepping => stepping;
    public float StepTime => stepTime;
    public SobekBoard Board => board;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController2D>();
    }

    // ================= 판 붙이기 / 떼기 =================
    public void Attach(SobekBoard target, Vector2 worldPos)
    {
        bool wasFree = board == null; // 판에서 판으로 바로 옮길 때(메인 A -> B)는 원래 값을 덮어쓰지 않음
        board = target;
        enabled = true;
        if (controller != null) controller.enabled = false;
        if (body != null)
        {
            if (wasFree) savedBodyType = body.bodyType;
            body.bodyType = RigidbodyType2D.Kinematic; // 판 위에서는 벽 충돌 없이 칸 규칙만 따름
            body.linearVelocity = Vector2.zero;
        }
        Teleport(worldPos);
    }

    public void Detach(Vector2 worldPos)
    {
        board = null;
        stepping = false;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.bodyType = savedBodyType;
        }
        Teleport(worldPos);
        if (controller != null) controller.enabled = !PlayerLock.IsLocked;
        enabled = false;
    }

    public void Teleport(Vector2 worldPos)
    {
        stepping = false;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.position = worldPos;
        }
        transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
    }

    // 한 칸 걷기 시작 (SobekBoard가 규칙 검사 후 부름)
    public void StepTo(Vector2 worldPos)
    {
        Vector2 from = body != null ? body.position : (Vector2)transform.position;
        Vector2 delta = worldPos - from;
        if (delta.sqrMagnitude < 0.0001f) { Teleport(worldPos); return; }
        stepTarget = worldPos;
        stepDir = delta.normalized;
        stepSpeed = delta.magnitude / stepTime;
        stepping = true;
    }

    // ================= 입력 =================
    private void Update()
    {
        if (board == null) return;
        if (PlayerLock.IsLocked || stepping || board.IsBusy) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.rKey.wasPressedThisFrame)
        {
            board.ResetByPlayer();
            return;
        }
        if (kb.zKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)
        {
            board.UndoByPlayer();
            return;
        }

        int dir = -1;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir = 0;
        else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir = 1;
        else if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir = 2;
        else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir = 3;
        if (dir >= 0) board.RequestStep(dir);
    }

    // PlayerLock이 풀릴 때 팀 이동을 다시 켜므로, 판 위에 있는 동안은 계속 꺼 둠
    private void LateUpdate()
    {
        if (board != null && controller != null && controller.enabled) controller.enabled = false;
    }

    private void FixedUpdate()
    {
        if (!stepping || body == null) return;
        if (PlayerLock.IsLocked) { body.linearVelocity = Vector2.zero; return; } // 팝업 중엔 멈춤

        Vector2 pos = body.position;
        float remain = Vector2.Distance(pos, stepTarget);
        if (remain <= stepSpeed * Time.fixedDeltaTime + 0.001f)
        {
            body.linearVelocity = Vector2.zero;
            body.position = stepTarget;
            stepping = false;
            return;
        }
        body.linearVelocity = stepDir * stepSpeed;
    }
}
