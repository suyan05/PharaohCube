using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 불이 붙는 물건 (화로, 큐브 빛) 공용: 그림 꺼짐/켜짐 + Light 2D 밝기 + 일렁임
// - Ignite(): 0 -> 번쩍 -> 원래 밝기 (0.8초), 불똥
// - SetInstant(true/false): 연출 없이 바로
// - Pulse(배율, 초): 잠깐 밝아졌다 돌아옴 (큐브가 빛을 내뿜을 때)
public class HallFlame : MonoBehaviour
{
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private Sprite offSprite;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Light2D lightSource;
    [SerializeField, Min(0f)] private float litIntensity = 1.4f;
    [SerializeField, Range(0f, 0.5f)] private float flicker = 0.15f;
    [SerializeField] private bool startLit;
    [SerializeField] private Color sparkColor = new Color(1f, 0.75f, 0.3f);

    private bool lit;
    private float boost = 1f;

    public bool IsLit => lit;

    private void Awake()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        if (offSprite == null && target != null) offSprite = target.sprite;
        SetInstant(startLit);
    }

    private void Update()
    {
        if (!lit || lightSource == null) return;
        float n = Mathf.PerlinNoise(Time.time * 3f, transform.position.x * 0.37f);
        lightSource.intensity = litIntensity * boost * (1f - flicker + n * flicker * 2f);
    }

    public void SetInstant(bool on)
    {
        lit = on;
        boost = 1f;
        if (target != null && (on ? onSprite : offSprite) != null) target.sprite = on ? onSprite : offSprite;
        if (lightSource != null)
        {
            lightSource.gameObject.SetActive(on);
            lightSource.intensity = on ? litIntensity : 0f;
        }
    }

    public IEnumerator Ignite()
    {
        SetInstant(true);
        CineFX.Puff(FxSprites.Dots, transform.position + Vector3.up * 0.6f, 14, 1.4f, sparkColor);
        yield return Pulse(2.2f, 0.8f);
    }

    public IEnumerator Pulse(float peak, float seconds)
    {
        if (!lit) SetInstant(true);
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float p = t / Mathf.Max(0.01f, seconds);
            boost = p < 0.35f ? Mathf.Lerp(0.2f, peak, p / 0.35f) : Mathf.Lerp(peak, 1f, (p - 0.35f) / 0.65f);
            yield return null;
        }
        boost = 1f;
    }
}
