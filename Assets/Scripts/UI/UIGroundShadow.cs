using UnityEngine;
using UnityEngine.UI;

// 퍼즐 화면(UI) 그림이 배경 위에 "붕 떠 보이는" 것을 줄이는 컴포넌트
// 1) 그림자: 같은 그림을 어둡게 복사해 살짝 아래-오른쪽에 깔아 줌 (회전하는 톱니도 그림자가 같이 돎)
// 2) 바닥 그림자: 그림 발밑에 납작한 타원 그림자 (놓여 있는 물체용). 들어 올려지면 작고 옅어짐
// 3) 색 맞추기: 배경 조명(따뜻한 갈색)에 맞게 그림 색을 살짝 어둡게 곱함
// 4) 축 핀: 톱니/도르래 가운데에 고정 핀을 그려 벽에 박혀 있는 느낌
// 붙이는 곳: 퍼즐 패널 안의 그림(Image) 오브젝트. Tools/Anubis/퍼즐 그림 바닥에 붙이기 메뉴가 자동으로 붙임
[RequireComponent(typeof(Image))]
public class UIGroundShadow : MonoBehaviour
{
    public enum Mode
    {
        Hanging,   // 매달린 물체 (심장, 추): 그림자만
        Standing,  // 바닥에 놓인 물체 (기둥, 받침): 그림자 + 발밑 타원
        Lifted,    // 들어 올려지는 물체 (유물): 발밑 타원이 바닥에 남고, 올라갈수록 작아짐
        Mounted    // 벽에 박힌 물체 (톱니, 도르래): 그림자 + 가운데 축 핀
    }

    [Tooltip("발밑 타원, 축 핀에 쓰는 동그라미 그림 (메뉴가 자동으로 Knob 넣음)")]
    [SerializeField] private Sprite circleSprite;

    [Header("종류")]
    [SerializeField] private Mode mode = Mode.Hanging;

    [Header("그림자 (그림 복사본)")]
    [SerializeField] private bool dropShadow = true;
    [Tooltip("그림자 위치 (UI 픽셀). 아래-오른쪽 = 왼쪽 위 조명")]
    [SerializeField] private Vector2 shadowOffset = new Vector2(7f, -9f);
    [SerializeField] private Color shadowColor = new Color(0.08f, 0.05f, 0.02f, 0.55f);

    [Header("발밑 타원 그림자 (Standing / Lifted)")]
    [Range(0.2f, 1.5f)] [SerializeField] private float groundWidth = 0.9f;   // 그림 너비 대비
    [Range(0.05f, 0.5f)] [SerializeField] private float groundHeight = 0.16f; // 그림 너비 대비
    [Tooltip("발밑 그림자를 그림 아래 끝에서 얼마나 내릴지 (UI 픽셀)")]
    [SerializeField] private float groundOffsetY = 4f;
    [SerializeField] private Color groundColor = new Color(0f, 0f, 0f, 0.5f);
    [Tooltip("Lifted: 이 높이(UI 픽셀)만큼 올라가면 그림자가 가장 작아짐")]
    [SerializeField] private float maxLiftHeight = 300f;

    [Header("색 맞추기 (배경 조명)")]
    [SerializeField] private bool applyTint = true;
    [Tooltip("그림 색에 곱함. 흰색 = 그대로. 따뜻하고 살짝 어둡게 = 배경에 녹아듦")]
    [SerializeField] private Color ambientTint = new Color(0.9f, 0.84f, 0.74f, 1f);

    [Header("축 핀 (Mounted)")]
    [Range(0.05f, 0.4f)] [SerializeField] private float pinSize = 0.16f; // 그림 너비 대비
    [SerializeField] private Color pinColor = new Color(0.16f, 0.11f, 0.06f, 1f);

    private Image target;
    private RectTransform rt;
    private RectTransform shadowRt, groundRt;
    private Image shadowImg, groundImg;
    private float startY;
    private Vector2 groundBaseSize;
    private bool built;

    private void Awake()
    {
        target = GetComponent<Image>();
        rt = (RectTransform)transform;
        Build();
    }

    private void Build()
    {
        if (built || rt.parent == null) return;
        built = true;

        if (applyTint) target.color *= ambientTint;

        Sprite circle = circleSprite; // 비어 있으면 네모로 그려짐

        // 그림 복사본 그림자 (같은 부모, 바로 뒤 순서)
        if (dropShadow)
        {
            shadowImg = CreateSibling("DropShadow_" + name, target.sprite);
            shadowImg.color = shadowColor;
            shadowImg.preserveAspect = target.preserveAspect;
            shadowImg.type = target.type;
            shadowRt = shadowImg.rectTransform;
        }

        // 발밑 타원
        if (mode == Mode.Standing || mode == Mode.Lifted)
        {
            groundImg = CreateSibling("GroundShadow_" + name, circle);
            groundImg.color = groundColor;
            groundRt = groundImg.rectTransform;
            float w = rt.rect.width * Mathf.Abs(rt.localScale.x);
            groundBaseSize = new Vector2(w * groundWidth, w * groundHeight);
            groundRt.pivot = new Vector2(0.5f, 0.5f); // 타원 가운데가 바닥선에 오게
            groundRt.sizeDelta = groundBaseSize;
            groundRt.localScale = Vector3.one;
            groundRt.localRotation = Quaternion.identity;
            startY = rt.anchoredPosition.y;
        }

        // 축 핀 (그림 위, 자식으로 붙여 같이 돎)
        if (mode == Mode.Mounted)
        {
            var pinGo = new GameObject("AxlePin", typeof(RectTransform), typeof(Image));
            pinGo.layer = gameObject.layer;
            var pin = pinGo.GetComponent<Image>();
            pin.sprite = circle;
            pin.color = pinColor;
            pin.raycastTarget = false;
            var prt = pin.rectTransform;
            prt.SetParent(rt, false);
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            float s = rt.rect.width * pinSize;
            prt.sizeDelta = new Vector2(s, s);
            prt.anchoredPosition = Vector2.zero;
        }

        Follow();
    }

    private Image CreateSibling(string objName, Sprite sprite)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false; // 버튼 클릭을 막지 않게
        var srt = img.rectTransform;
        srt.SetParent(rt.parent, false);
        srt.SetSiblingIndex(rt.GetSiblingIndex()); // 그림 바로 뒤에 그려짐
        srt.anchorMin = rt.anchorMin;
        srt.anchorMax = rt.anchorMax;
        srt.pivot = rt.pivot;
        srt.sizeDelta = rt.sizeDelta;
        return img;
    }

    // 그림이 움직이고 돌고 켜지고 꺼지는 것을 따라감 (할당 없음)
    private void LateUpdate()
    {
        if (built) Follow();
    }

    private void Follow()
    {
        bool visible = target.enabled && gameObject.activeInHierarchy;

        if (shadowRt != null)
        {
            if (shadowImg.enabled != visible) shadowImg.enabled = visible;
            shadowRt.anchoredPosition = rt.anchoredPosition + shadowOffset;
            shadowRt.localRotation = rt.localRotation;
            shadowRt.localScale = rt.localScale;
            shadowRt.sizeDelta = rt.sizeDelta;
            if (shadowImg.sprite != target.sprite) shadowImg.sprite = target.sprite; // 그림이 바뀌면 같이
        }

        if (groundRt != null)
        {
            if (groundImg.enabled != visible) groundImg.enabled = visible;
            float h = rt.rect.height * Mathf.Abs(rt.localScale.y);
            // 그림 아래 끝 = (기준 y) - 높이 x 피벗. Lifted는 처음 바닥 위치에 그림자를 남김
            float baseY = mode == Mode.Lifted ? startY : rt.anchoredPosition.y;
            float bottomY = baseY - h * rt.pivot.y;
            groundRt.anchoredPosition = new Vector2(rt.anchoredPosition.x + rt.rect.width * (0.5f - rt.pivot.x), bottomY - groundOffsetY);

            float k = 1f;
            if (mode == Mode.Lifted && maxLiftHeight > 0f)
                k = Mathf.Lerp(1f, 0.45f, Mathf.Clamp01((rt.anchoredPosition.y - startY) / maxLiftHeight));
            groundRt.sizeDelta = groundBaseSize * k;
            Color c = groundColor;
            c.a *= k;
            groundImg.color = c;
        }
    }

    // 꺼질 때 같이 숨김 (LateUpdate가 안 돌기 때문)
    private void OnDisable()
    {
        if (shadowImg != null) shadowImg.enabled = false;
        if (groundImg != null) groundImg.enabled = false;
    }

    private void OnDestroy()
    {
        if (shadowImg != null) Destroy(shadowImg.gameObject);
        if (groundImg != null) Destroy(groundImg.gameObject);
    }

#if UNITY_EDITOR
    public void EditorSetMode(Mode m) => mode = m;
    public void EditorSetCircle(Sprite s) => circleSprite = s;

    // Scene 뷰: 발밑 그림자 위치 미리보기
    private void OnDrawGizmosSelected()
    {
        var r = transform as RectTransform;
        if (r == null) return;
        Vector3[] c = new Vector3[4];
        r.GetWorldCorners(c);
        Gizmos.color = new Color(0f, 0f, 0f, 0.8f);
        Gizmos.DrawLine(c[0], c[3]); // 그림 아래 끝 = 바닥선
    }
#endif
}
