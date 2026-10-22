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

    private bool dim;            // 밤 희미한 빛 모드
    private bool pressed;        // [추가] 지금 밟고 있는지 (발을 떼야 다시 밟힘)
    private Collider2D playerCol; // [추가] 판 위에 들어온 플레이어 콜라이더
    private float lightIntensity = 1.2f;

    public event Action<BastetFootprintPlate> OnStepped;
    public int Index { get; set; }

    private void Awake()
    {
        if (glowLight != null) lightIntensity = glowLight.intensity; // 원래 밝기 기억
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
    }

    public void SetGlow(bool on, Color color)
    {
        // [수정] 꺼질 때 밤 모드면 희미한 빛으로
        if (!on && dim) { ShowDim(); return; }
        if (glow != null) { glow.enabled = on; glow.color = color; }
        if (glowLight != null) { glowLight.enabled = on; glowLight.color = color; glowLight.intensity = lightIntensity; }
    }

    // [추가] 밤 희미한 빛 켜기/끄기 (켜진 빛이 있으면 다음 SetGlow(false) 때 적용)
    public void SetDim(bool on)
    {
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
