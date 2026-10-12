using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 쯔꾸르(RPG Maker) 스타일 2D 카메라.
/// - 플레이어를 부드럽게 따라간다.
/// - 맵(Ground Tilemap) 가장자리에 닿으면 더 이상 바깥을 보여주지 않는다.
/// - 맵이 화면보다 작은 방향은 맵 중앙에 고정한다.
/// - Shake()로 화면 흔들림 (따라가는 위치와 따로 계산해서 흔들림이 누적되지 않음)
/// - FocusOn()으로 다른 곳을 잠깐 비추고, ReleaseFocus()로 플레이어에게 돌아옴
/// Main Camera에 붙여서 사용.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("따라갈 대상 (비워두면 'Player' 태그로 자동 검색)")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";

    [Header("움직임")]
    [Tooltip("0이면 딱 붙어서 따라감. 0.1~0.2 정도가 쯔꾸르 느낌")]
    [SerializeField, Min(0f)] private float smoothTime = 0.12f;

    [Header("다른 곳 비추기 (FocusOn)")]
    [Tooltip("다른 곳으로 갈 때와 돌아올 때 부드러운 정도. 클수록 천천히 움직임")]
    [SerializeField, Min(0f)] private float focusSmoothTime = 0.35f;

    [Header("도트 떨림 방지")]
    [SerializeField] private bool snapToPixel = true;
    [SerializeField, Min(1)] private int pixelsPerUnit = 32;

    [Header("맵 경계 (Ground Tilemap 넣기)")]
    [SerializeField] private Tilemap boundsTilemap;

    private Camera cam;
    private Vector3 velocity;
    private Bounds mapBounds;
    private bool hasBounds;

    // 흔들림을 빼고 "따라가기만 한" 위치 (흔들림이 다음 프레임 계산에 섞이지 않게 따로 보관)
    private Vector3 followPos;
    private bool hasFollowPos;

    // 화면 흔들림
    private float shakeTime;
    private float shakeDuration;
    private float shakeStrength;

    // 다른 곳 비추기
    private bool hasFocus;
    private Vector3 focusPoint;
    private bool returningFromFocus;

    public bool IsFocusing => hasFocus;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (!cam.orthographic)
        {
            Debug.LogWarning("[CameraFollow2D] 카메라 Projection이 Orthographic이 아닙니다. 2D용으로 바꿔주세요.");
        }
        RefreshBounds();
    }

    private void Start()
    {
        TryFindTarget();
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null && !TryFindTarget()) return;

        try
        {
            Vector3 from = hasFollowPos ? followPos : transform.position;
            Vector3 desired = ClampToMap(GetGoalPosition());
            followPos = Vector3.SmoothDamp(from, desired, ref velocity, CurrentSmoothTime(desired));
            hasFollowPos = true;

            Vector3 next = followPos + ShakeOffset();
            transform.position = snapToPixel ? SnapPixel(next) : next;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CameraFollow2D] 카메라 이동 실패: {e.Message}");
            enabled = false;
        }
    }

    // 비추는 중이거나 플레이어에게 돌아오는 중이면 천천히, 평소에는 원래 속도
    private float CurrentSmoothTime(Vector3 desired)
    {
        if (hasFocus) return focusSmoothTime;
        if (!returningFromFocus) return smoothTime;

        // 거의 다 돌아왔으면 평소 속도로 복귀
        if ((desired - followPos).sqrMagnitude < 0.01f) returningFromFocus = false;
        return focusSmoothTime;
    }

    /// <summary>방(씬)이 바뀌거나 맵을 새로 그렸을 때 외부에서 호출.</summary>
    public void RefreshBounds()
    {
        hasBounds = false;
        if (boundsTilemap == null) return;

        boundsTilemap.CompressBounds();
        Bounds local = boundsTilemap.localBounds;
        if (local.size == Vector3.zero) return;

        local.center = boundsTilemap.transform.TransformPoint(local.center);
        mapBounds = local;
        hasBounds = true;
    }

    /// <summary>순간이동(문 통과 등) 직후 부드러운 이동 없이 바로 맞출 때 호출.</summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        velocity = Vector3.zero;
        Vector3 pos = ClampToMap(GetTargetPosition());
        followPos = pos;
        hasFollowPos = true;
        transform.position = snapToPixel ? SnapPixel(pos) : pos;
    }

    /// <summary>화면 흔들림. duration초 동안 strength(유닛)만큼 흔들리고 점점 약해짐.</summary>
    public void Shake(float duration, float strength)
    {
        if (duration <= 0f) return;
        shakeDuration = duration;
        shakeTime = duration;
        shakeStrength = Mathf.Max(0f, strength);
    }

    /// <summary>플레이어 대신 worldPoint를 비춤 (ReleaseFocus 전까지 유지).</summary>
    public void FocusOn(Vector3 worldPoint)
    {
        focusPoint = worldPoint;
        hasFocus = true;
        returningFromFocus = false;
    }

    /// <summary>다시 플레이어를 따라감 (천천히 돌아옴).</summary>
    public void ReleaseFocus()
    {
        if (!hasFocus) return;
        hasFocus = false;
        returningFromFocus = true;
    }

    private Vector3 ShakeOffset()
    {
        if (shakeTime <= 0f) return Vector3.zero;

        shakeTime -= Time.deltaTime;
        float fade = Mathf.Clamp01(shakeTime / shakeDuration); // 끝으로 갈수록 약하게
        Vector2 r = Random.insideUnitCircle * shakeStrength * fade;
        return new Vector3(r.x, r.y, 0f);
    }

    private bool TryFindTarget()
    {
        if (string.IsNullOrEmpty(targetTag)) return false;
        try
        {
            GameObject found = GameObject.FindWithTag(targetTag);
            if (found != null) target = found.transform;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"[CameraFollow2D] '{targetTag}' 태그가 프로젝트에 없습니다.");
            targetTag = string.Empty;
        }
        return target != null;
    }

    // 비추는 중이면 그 지점, 아니면 플레이어
    private Vector3 GetGoalPosition()
    {
        if (!hasFocus) return GetTargetPosition();
        return new Vector3(focusPoint.x, focusPoint.y, transform.position.z);
    }

    private Vector3 GetTargetPosition()
    {
        return new Vector3(target.position.x, target.position.y, transform.position.z);
    }

    private Vector3 ClampToMap(Vector3 pos)
    {
        if (!hasBounds) return pos;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        pos.x = ClampAxis(pos.x, mapBounds.min.x, mapBounds.max.x, halfW);
        pos.y = ClampAxis(pos.y, mapBounds.min.y, mapBounds.max.y, halfH);
        return pos;
    }

    private static float ClampAxis(float value, float min, float max, float halfView)
    {
        // 맵이 화면보다 작으면 맵 중앙에 고정
        if (max - min <= halfView * 2f) return (min + max) * 0.5f;
        return Mathf.Clamp(value, min + halfView, max - halfView);
    }

    private Vector3 SnapPixel(Vector3 pos)
    {
        float unit = 1f / pixelsPerUnit;
        pos.x = Mathf.Round(pos.x / unit) * unit;
        pos.y = Mathf.Round(pos.y / unit) * unit;
        return pos;
    }
}