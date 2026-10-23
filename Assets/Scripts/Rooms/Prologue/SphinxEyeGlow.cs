using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 스핑크스 눈빛 연출 (SphinxHead가 말할 때 켜고 끔)
// - 눈 그림(EyeGlow)을 "조명 영향 안 받는" 재질로 한 겹 더 그림 -> 화면이 어두워져도 눈은 쨍하게 빛남 (+ Bloom 번짐)
// - 눈마다 금빛 후광 + 두 눈을 가로지르는 빛줄기 + 얼굴 뒤 큰 은은한 빛
// - Point Light 2D(Eye Light): 얼굴과 주변 모래를 금빛으로 비춤
// - 켜져 있는 동안 살짝 일렁임
// 그림 없이 코드로 만든 부드러운 원을 씀. 배치 도구가 자식 오브젝트와 연결을 다 만들어 줌
public class SphinxEyeGlow : MonoBehaviour
{
    [Header("연결 (배치 도구가 채움)")]
    [SerializeField] private SpriteRenderer eyeOverlay;
    [SerializeField] private SpriteRenderer[] halos;
    [SerializeField] private SpriteRenderer beam;
    [SerializeField] private SpriteRenderer aura;
    [SerializeField] private Light2D eyeLight;
    [Tooltip("Sprite-Unlit-Default (비우면 실행 중에 찾음)")]
    [SerializeField] private Material unlitMaterial;

    [Header("세기")]
    [SerializeField, Min(0f)] private float fadeSeconds = 0.5f;
    [SerializeField, Min(0f)] private float lightIntensity = 2.2f;
    [SerializeField, Range(0f, 1f)] private float haloAlpha = 0.7f;
    [SerializeField, Range(0f, 1f)] private float beamAlpha = 0.45f;
    [SerializeField, Range(0f, 1f)] private float auraAlpha = 0.22f;

    private static Sprite softCircle;
    private float amount;   // 0 = 꺼짐, 1 = 다 켜짐
    private float target;

    public void SetOn(bool on)
    {
        target = on ? 1f : 0f;
    }

    private void Awake()
    {
        try
        {
            ApplyLook();
            Apply(0f, 0f);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SphinxEyeGlow] 준비 오류: {e}");
        }
    }

    private void Update()
    {
        float step = fadeSeconds > 0f ? Time.deltaTime / fadeSeconds : 1f;
        amount = Mathf.MoveTowards(amount, target, step);
        float wobble = 0.85f + Mathf.Sin(Time.time * 5f) * 0.1f + Mathf.Sin(Time.time * 11.3f) * 0.05f;
        Apply(amount, wobble);
    }

    private void Apply(float a, float wobble)
    {
        SetAlpha(eyeOverlay, a);
        if (halos != null) foreach (SpriteRenderer h in halos) SetAlpha(h, a * haloAlpha * wobble);
        SetAlpha(beam, a * beamAlpha * wobble);
        SetAlpha(aura, a * auraAlpha);
        if (eyeLight != null) eyeLight.intensity = a * lightIntensity * wobble;
    }

    private static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = Mathf.Clamp01(alpha);
        sr.color = c;
    }

    // 후광, 빛줄기, 오라는 부드러운 원 + 조명 무시 재질
    private void ApplyLook()
    {
        Material mat = unlitMaterial != null ? unlitMaterial : FindUnlit();
        foreach (SpriteRenderer sr in new[] { eyeOverlay, beam, aura })
        {
            if (sr != null && mat != null) sr.sharedMaterial = mat;
        }
        if (halos != null) foreach (SpriteRenderer h in halos) if (h != null && mat != null) h.sharedMaterial = mat;

        Sprite soft = SoftCircle();
        if (beam != null) beam.sprite = soft;
        if (aura != null) aura.sprite = soft;
        if (halos != null) foreach (SpriteRenderer h in halos) if (h != null) h.sprite = soft;
    }

    private static Material FindUnlit()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) Debug.LogWarning("[SphinxEyeGlow] Sprite-Unlit-Default를 못 찾음 -> 눈빛이 어두울 수 있음");
        return shader != null ? new Material(shader) : null;
    }

    private static Sprite SoftCircle()
    {
        if (softCircle != null) return softCircle;

        const int size = 64;
        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            float d = Vector2.Distance(new Vector2(i % size + 0.5f, i / size + 0.5f), new Vector2(32f, 32f)) / 32f;
            float a = Mathf.Clamp01(1f - d);
            pixels[i] = new Color32(255, 255, 255, (byte)(a * a * 255f));
        }
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(pixels);
        tex.Apply();
        softCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return softCircle;
    }
}
