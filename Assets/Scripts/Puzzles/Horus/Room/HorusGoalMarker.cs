using System;
using UnityEngine;
using UnityEngine.UI;

// 지금 목표 위에 금빛 별이 둥둥 떠 있게 + 목표가 화면 밖이면 화면 가장자리에 화살표
// - 목표는 HorusObjectiveGuide의 현재 단계 Target (퍼즐 본체)
// - 어두운 방에서도 보이게 Unlit + 작은 빛. 연출(Cine) 중에는 숨김
public class HorusGoalMarker : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private HorusObjectiveGuide guide;
    [SerializeField] private Sprite markerSprite;

    [Header("모양")]
    [SerializeField] private float height = 0.6f;
    [SerializeField] private float size = 0.55f;
    [SerializeField] private float bob = 0.15f;
    [SerializeField] private Color color = new Color(1f, 0.85f, 0.4f, 1f);
    [Tooltip("화면 가장자리에서 화살표까지 거리 (화면 픽셀)")]
    [SerializeField] private float edgeMargin = 70f;

    private SpriteRenderer marker;
    private RectTransform arrow;
    private Image arrowImage;

    private void Start()
    {
        try
        {
            if (guide == null) guide = FindFirstObjectByType<HorusObjectiveGuide>();
            HorusFx.MatchPlayer();
            marker = HorusFx.Spawn("GoalMarker", markerSprite, transform.position, size, color, HorusFx.Above);
            marker.transform.SetParent(transform, true);
            RaGlowFx.PointLight(marker.transform, Vector3.zero, color, 1.6f, 0.6f);
            BuildArrow();
        }
        catch (Exception e)
        {
            Debug.LogError($"[HorusGoalMarker] 준비 오류: {e}");
        }
    }

    private void BuildArrow()
    {
        RectTransform canvas = UIBuild.CreateCanvas(transform, "GoalArrowCanvas", 80, out GameObject canvasGo);
        canvasGo.GetComponent<GraphicRaycaster>().enabled = false;
        arrowImage = UIBuild.Image(canvas, "GoalArrow", Vector2.zero, new Vector2(48f, 48f), ArrowSprite(), color);
        arrowImage.raycastTarget = false;
        arrowImage.color = color;
        arrow = arrowImage.rectTransform;
    }

    private void LateUpdate()
    {
        if (marker == null || guide == null) return;
        SpriteRenderer target = guide.CurrentTarget;
        bool show = target != null && !Cine.Running;
        marker.enabled = show;
        if (!show)
        {
            if (arrowImage != null) arrowImage.enabled = false;
            return;
        }
        Bounds b = target.bounds;
        float t = Time.time;
        marker.transform.position = new Vector3(b.center.x, b.max.y + height + Mathf.Sin(t * 3f) * bob, 0f);
        marker.transform.Rotate(0f, 0f, 40f * Time.deltaTime);
        UpdateArrow(marker.transform.position);
    }

    // 목표가 화면 밖이면 그쪽 가장자리에 화살표 (목표 방향으로 회전)
    private void UpdateArrow(Vector3 world)
    {
        Camera cam = Camera.main;
        if (cam == null || arrowImage == null) return;
        Vector3 vp = cam.WorldToViewportPoint(world);
        bool inside = vp.x > 0.02f && vp.x < 0.98f && vp.y > 0.02f && vp.y < 0.98f;
        arrowImage.enabled = !inside;
        if (inside) return;
        Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
        Vector2 dir = ((Vector2)cam.WorldToScreenPoint(world) - center).normalized;
        Vector2 half = center - Vector2.one * edgeMargin;
        float k = Mathf.Min(Mathf.Abs(half.x / Mathf.Max(Mathf.Abs(dir.x), 0.001f)), Mathf.Abs(half.y / Mathf.Max(Mathf.Abs(dir.y), 0.001f)));
        arrow.position = center + dir * k;
        arrow.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 6f);
        arrow.localScale = new Vector3(pulse, pulse, 1f);
    }

    // 오른쪽을 가리키는 삼각형 그림 (코드로 만듦)
    private static Sprite ArrowSprite()
    {
        const int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float half = (1f - x / (float)n) * n * 0.5f;
                bool on = Mathf.Abs(y - n * 0.5f) < half;
                tex.SetPixel(x, y, on ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }
}
