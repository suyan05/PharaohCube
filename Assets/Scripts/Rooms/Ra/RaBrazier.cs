using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 라의 성소 화로 (꺼지지 않는 불, 신전 가운데 1개)
// - E: 꺼진 내 횃불에 다시 불 붙이기 (다 타서 재가 됐어도 됨)
// - 주변 brightRadius 안은 "밝은 곳" -> 횃불 시간이 줄지 않음 (RaTorchTimer)
// - 밤에도 안 꺼짐 (RaTorchManager는 벽 횃불만 끔). 색은 밤 분위기가 푸르게 바꿈
[RequireComponent(typeof(Collider2D))]
public class RaBrazier : MonoBehaviour, IInteractable
{
    private static readonly List<RaBrazier> all = new List<RaBrazier>();

    [SerializeField] private Light2D fireLight;
    [SerializeField, Min(0f)] private float intensity = 1.3f;
    [SerializeField] private float brightRadius = 3.5f;
    [SerializeField] private Color sparkColor = new Color(1f, 0.7f, 0.3f);
    [SerializeField] private string message = "성소 화로의 불을 횃불에 옮겨 붙였다.";

    private SpriteRenderer body;
    private float sparkTimer;

    public static bool AnyNear(Vector3 pos)
    {
        foreach (RaBrazier b in all)
        {
            if (b == null) continue;
            if ((b.transform.position - pos).sqrMagnitude <= b.brightRadius * b.brightRadius) return true;
        }
        return false;
    }

    private void OnEnable() => all.Add(this);

    private void OnDisable() => all.Remove(this);

    private void Awake()
    {
        body = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (fireLight != null)
        {
            float n = Mathf.PerlinNoise(Time.time * 3.2f, 7.7f);
            fireLight.intensity = intensity * (0.85f + n * 0.3f);
        }
        sparkTimer -= Time.deltaTime;
        if (sparkTimer > 0f || body == null) return;
        sparkTimer = UnityEngine.Random.Range(0.25f, 0.6f);
        RaGlowFx.Spark(transform.position + new Vector3(0f, 0.9f, 0f), sparkColor, body.sortingLayerName, body.sortingOrder + 1);
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        ITorchLight t = TorchLink.Current;
        return t != null && t.IsLit ? null : "E: 화로에서 횃불 붙이기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            ITorchLight t = TorchLink.Current;
            if (t == null || t.IsLit) return;
            t.Relight(message);
            CineFX.Puff(FxSprites.Dots, transform.position + Vector3.up * 0.8f, 8, 1f, sparkColor);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaBrazier] 횃불 붙이기 오류: {e}");
        }
    }
}
