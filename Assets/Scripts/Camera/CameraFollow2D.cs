using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 쯔꾸르(RPG Maker) 스타일 2D 카메라.
/// - 플레이어를 부드럽게 따라간다.
/// - 맵(Ground Tilemap) 가장자리에 닿으면 더 이상 바깥을 보여주지 않는다.
/// - 맵이 화면보다 작은 방향은 맵 중앙에 고정한다.
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

    [Header("도트 떨림 방지")]
    [SerializeField] private bool snapToPixel = true;
    [SerializeField, Min(1)] private int pixelsPerUnit = 32;

    [Header("맵 경계 (Ground Tilemap 넣기)")]
    [SerializeField] private Tilemap boundsTilemap;

    private Camera cam;
    private Vector3 velocity;
    private Bounds mapBounds;
    private bool hasBounds;

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
            Vector3 desired = ClampToMap(GetTargetPosition());
            Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            transform.position = snapToPixel ? SnapPixel(next) : next;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CameraFollow2D] 카메라 이동 실패: {e.Message}");
            enabled = false;
        }
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
        transform.position = snapToPixel ? SnapPixel(pos) : pos;
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