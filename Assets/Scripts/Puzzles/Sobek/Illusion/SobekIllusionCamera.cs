using System;
using UnityEngine;

// 착시 퍼즐 카메라 (직교 아이소메트릭, 마우스로 돌림)
// - 드래그하는 동안은 자유롭게 돌고, 손을 떼면 가장 가까운 45°/135°/225°/315°로 착 붙음
// - 붙은 순간에만 길이 계산됨 (돌고 있는 동안은 이동 불가)
// 붙이는 곳: 카메라의 부모 오브젝트(회전 중심). 자식에 Camera
public class SobekIllusionCamera : MonoBehaviour
{
    // 정확한 아이소메트릭 각도 = atan(1/√2). 이 값이어야 칸이 정확히 겹쳐 보임 (바꾸지 말 것)
    public const float IsoPitch = 35.26439f;

    [SerializeField] private Camera cam;
    [Tooltip("카메라와 중심 사이 거리 (직교라 화면 크기에는 영향 없음, 블록이 잘리지 않을 만큼만)")]
    [Range(10f, 100f)] [SerializeField] private float distance = 40f;
    [Tooltip("마우스 1픽셀당 도는 각도")]
    [Range(0.05f, 1f)] [SerializeField] private float dragDegreesPerPixel = 0.3f;
    [Tooltip("손을 뗀 뒤 각도에 붙는 시간")]
    [Range(0.05f, 1f)] [SerializeField] private float snapTime = 0.22f;

    private float yaw = 225f;
    private float snapFrom, snapTo, snapT = 1f;
    private bool dragging;

    public event Action<int> Snapped;       // 각도에 붙음 (0~3)
    public event Action RotateStarted;      // 돌기 시작 (길 끊김)

    public Camera Cam => cam;
    public bool IsRotating => dragging || snapT < 1f;
    public int YawIndex => SobekIllusionGraph.YawIndex(yaw);
    public Vector3 Forward => cam != null ? cam.transform.forward : transform.forward;

    private void Awake()
    {
        if (cam == null) cam = GetComponentInChildren<Camera>();
    }

    public void SetYawImmediate(int index)
    {
        yaw = SobekIllusionGraph.YawDegrees(index);
        dragging = false;
        snapT = 1f;
        Apply();
    }

    public void BeginDrag()
    {
        if (dragging) return;
        dragging = true;
        snapT = 1f;
        RotateStarted?.Invoke();
    }

    public void Drag(float pixelsX) { if (dragging) { yaw += pixelsX * dragDegreesPerPixel; Apply(); } }

    public void EndDrag()
    {
        if (!dragging) return;
        dragging = false;
        StartSnap(SobekIllusionGraph.YawDegrees(SobekIllusionGraph.YawIndex(yaw)));
    }

    // Q/E: 90°씩
    public void Step(int dir)
    {
        if (IsRotating) return;
        RotateStarted?.Invoke();
        StartSnap(yaw + 90f * Mathf.Sign(dir));
    }

    private void StartSnap(float target)
    {
        // 가장 짧은 쪽으로 돌기
        target = yaw + Mathf.DeltaAngle(yaw, target);
        snapFrom = yaw;
        snapTo = target;
        snapT = 0f;
    }

    private void Update()
    {
        if (snapT >= 1f) return;
        snapT = Mathf.Min(1f, snapT + Time.deltaTime / Mathf.Max(0.01f, snapTime));
        float e = 1f - (1f - snapT) * (1f - snapT); // ease-out
        yaw = Mathf.Lerp(snapFrom, snapTo, e);
        if (snapT >= 1f)
        {
            yaw = Mathf.Repeat(snapTo, 360f);
            Apply();
            Debug.Log($"[SobekIllusionCamera] 각도 고정: {SobekIllusionGraph.YawDegrees(YawIndex)}°", this);
            Snapped?.Invoke(YawIndex);
            return;
        }
        Apply();
    }

    private void Apply()
    {
        transform.rotation = Quaternion.Euler(IsoPitch, yaw, 0f);
        if (cam != null)
        {
            cam.transform.localPosition = new Vector3(0f, 0f, -distance);
            cam.transform.localRotation = Quaternion.identity;
        }
    }

    private void OnValidate() { if (cam == null) cam = GetComponentInChildren<Camera>(); }

#if UNITY_EDITOR
    public void EditorSetup(Camera c, float size)
    {
        cam = c;
        cam.orthographic = true;
        cam.orthographicSize = size;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = distance * 2.5f;
        Apply();
    }
#endif
}
