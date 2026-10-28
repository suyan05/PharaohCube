using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 바스테트 걷는 퍼즐 공용 부품
// ================= 밟는 발자국 =================
[RequireComponent(typeof(Collider2D))]
public class BastetFootprintPlate : MonoBehaviour
{
    [Header("빛 (자식 오브젝트, 평소에는 꺼짐)")]
    [SerializeField] private SpriteRenderer glow;
    [SerializeField] private Light2D glowLight;
    [SerializeField] private string playerTag = "Player";

    [Header("[추가] 밟기 판정 (옆 판이 같이 눌리는 것 방지)")]
    [Tooltip("플레이어 발끝(콜라이더 아래쪽 가운데)이 판 가운데에서 이 거리(칸) 안에 들어와야 밟힘")]
    [Range(0.1f, 1f)] [SerializeField] private float stepRadius = 0.35f;
    [Tooltip("발끝 위치를 콜라이더 아래쪽에서 위로 올리는 정도 (칸)")]
    [Range(0f, 0.5f)] [SerializeField] private float footLift = 0.08f;

    [Header("[추가] 밤에 꺼진 판도 희미하게 (B5 밤 단계)")]
    [Range(0f, 1f)] [SerializeField] private float dimAlpha = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float dimLightIntensity = 0.15f;
    [SerializeField] private Color dimColor = new Color(0.94f, 0.86f, 0.76f); // 크림 #EFDCC2

    [Header("[추가] 밟았을 때 반응 (눌렸는지 확실히 보이게)")]
    [Tooltip("밟는 순간 판 그림이 이 색으로 어두워졌다 돌아옴 (돌판이 가라앉는 느낌)")]
    [SerializeField] private Color pressTint = new Color(0.55f, 0.5f, 0.45f);
    [Range(0.05f, 0.6f)] [SerializeField] private float pressTime = 0.22f;
    [Tooltip("밟을 때 '턱' 소리")]
    [SerializeField] private bool pressSound = true;
    [Tooltip("Pulse() 때 빛이 몇 배로 번쩍였다가 원래 밝기로 돌아오는지")]
    [Range(1f, 5f)] [SerializeField] private float pulseBoost = 2.6f;
    [Range(0.1f, 1f)] [SerializeField] private float pulseTime = 0.45f;

    private SpriteRenderer body;   // [추가] 판 그림 (가라앉는 색)
    private Color bodyColor = Color.white;
    private Coroutine pressRoutine, pulseRoutine;
    private Vector3 glowScale = Vector3.one; // [추가] 빛 그림 원래 크기 (번쩍 중간에 끊겨도 커진 채로 남지 않게)

    private bool dim;            // 밤 희미한 빛 모드
    private bool pressed;        // [추가] 지금 밟고 있는지 (발을 떼야 다시 밟힘)
    private Collider2D playerCol; // [추가] 판 위에 들어온 플레이어 콜라이더
    private float lightIntensity = 1.2f;

    public event Action<BastetFootprintPlate> OnStepped;
    public int Index { get; set; }

    private void Awake()
    {
        if (glowLight != null) lightIntensity = glowLight.intensity; // 원래 밝기 기억
        body = GetComponent<SpriteRenderer>(); // [추가]
        if (body != null) bodyColor = body.color;
        if (glow != null) glowScale = glow.transform.localScale;
    }

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    // [수정] 콜라이더가 닿기만 하면 밟히던 방식 -> 발끝이 판 가운데 가까이 와야 밟힘
    private void OnTriggerEnter2D(Collider2D other)
    {
        bool isPlayer = other.CompareTag(playerTag) ||
                        (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag));
        if (!isPlayer) return;
        playerCol = other;
        CheckStep();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other == playerCol) CheckStep();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != playerCol) return;
        playerCol = null;
        pressed = false;
    }

    // 발끝 = 플레이어 콜라이더 아래쪽 가운데 (할당 없음)
    private void CheckStep()
    {
        if (playerCol == null) return;
        Bounds b = playerCol.bounds;
        Vector2 foot = new Vector2(b.center.x, b.min.y + footLift);
        float sq = (foot - (Vector2)transform.position).sqrMagnitude;
        if (!pressed && sq <= stepRadius * stepRadius)
        {
            pressed = true;
            PressFx(); // [추가] 밟힌 판이 '턱' 가라앉음 + 소리 (퍼즐과 상관없이 항상)
            OnStepped?.Invoke(this);
        }
        else if (pressed && sq > stepRadius * stepRadius * 2.25f)
        {
            pressed = false; // 판 안에서 충분히 벗어나면 다시 밟을 수 있음
        }
    }

    private void OnDisable()
    {
        playerCol = null;
        pressed = false;
        pressRoutine = pulseRoutine = null; // [추가] 꺼지면 코루틴도 멈춤
        if (body != null) body.color = bodyColor;
        if (glow != null) glow.transform.localScale = glowScale;
        if (glowLight != null) glowLight.intensity = lightIntensity;
    }

    // ================= [추가] 밟았을 때 반응 =================
    private void PressFx()
    {
        if (pressSound) BastetSfx.Press();
        if (body == null || !isActiveAndEnabled) return;
        if (pressRoutine != null) StopCoroutine(pressRoutine);
        pressRoutine = StartCoroutine(PressRoutine());
    }

    // 판 그림이 확 어두워졌다가 원래 색으로 (콜라이더 크기는 안 건드림 -> 다시 밟힘 판정에 영향 없음)
    private IEnumerator PressRoutine()
    {
        float t = 0f;
        while (t < pressTime)
        {
            t += Time.deltaTime;
            body.color = Color.Lerp(pressTint * bodyColor, bodyColor, Mathf.SmoothStep(0f, 1f, t / pressTime));
            yield return null;
        }
        body.color = bodyColor;
        pressRoutine = null;
    }

    private void StopPulse()
    {
        if (pulseRoutine == null) return;
        StopCoroutine(pulseRoutine);
        pulseRoutine = null;
        if (glow != null) glow.transform.localScale = glowScale;
    }

    /// <summary>[추가] 빛을 켜면서 한 번 번쩍 (맞게 밟음, 정답 표시용). 빛은 켜진 채로 남음</summary>
    public void Pulse(Color color)
    {
        SetGlow(true, color);
        if (glowLight == null || !isActiveAndEnabled) return;
        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        float t = 0f;
        Transform g = glow != null ? glow.transform : null;
        Vector3 baseScale = glowScale;
        while (t < pulseTime)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / pulseTime); // 1 -> 0
            glowLight.intensity = lightIntensity * (1f + (pulseBoost - 1f) * k);
            if (g != null) g.localScale = baseScale * (1f + 0.25f * k); // 빛 그림도 살짝 커졌다 돌아옴
            yield return null;
        }
        glowLight.intensity = lightIntensity;
        if (g != null) g.localScale = baseScale;
        pulseRoutine = null;
    }

    public void SetGlow(bool on, Color color)
    {
        StopPulse(); // [추가] 번쩍 중이면 멈추고 원래 크기로
        // [수정] 꺼질 때 밤 모드면 희미한 빛으로
        if (!on && dim) { ShowDim(); return; }
        if (glow != null) { glow.enabled = on; glow.color = color; }
        if (glowLight != null) { glowLight.enabled = on; glowLight.color = color; glowLight.intensity = lightIntensity; }
    }

    // [추가] 밤 희미한 빛 켜기/끄기 (켜진 빛이 있으면 다음 SetGlow(false) 때 적용)
    public void SetDim(bool on)
    {
        StopPulse(); // [추가]
        dim = on;
        if (on) ShowDim();
        else SetGlow(false, Color.white);
    }

    private void ShowDim()
    {
        Color c = dimColor;
        c.a = dimAlpha;
        if (glow != null) { glow.enabled = true; glow.color = c; }
        if (glowLight != null) { glowLight.enabled = dimLightIntensity > 0f; glowLight.color = dimColor; glowLight.intensity = dimLightIntensity; }
    }

    // [추가] Scene 뷰: 밟히는 범위 (노랑)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, stepRadius);
    }

#if UNITY_EDITOR
    public void EditorSetup(SpriteRenderer g, Light2D l) { glow = g; glowLight = l; }
    public void EditorSetStepRadius(float r) { stepRadius = r; } // [추가]
#endif
}
