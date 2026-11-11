using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 호루스 방의 단서 비석이 어둠에 묻혀 안 보이던 문제 -> 비석마다 은은한 빛 + 머리 위에 떠 있는 금빛 표식
// - 다른 방은 방이 나뉘어 있어 비석이 눈에 띄지만, 호루스는 큰 홀 하나라 비석이 배경에 섞였음
// - HorusRuntimeTune이 씬이 열릴 때 비석(OBJ_HS*, 경고 석판)에 자동으로 붙임
public class HorusSteleBeacon : MonoBehaviour
{
    private static Sprite diamond;
    private Light2D glow;
    private Transform mark;
    private float clock, topY;

    private void Start()
    {
        try
        {
            SpriteRenderer host = GetComponentInChildren<SpriteRenderer>();
            topY = host != null ? host.bounds.max.y - transform.position.y : 1f;
            BuildGlow();
            BuildMark(host);
            clock = UnityEngine.Random.value * 6f;
        }
        catch (Exception e)
        {
            Debug.LogError($"[비석 표식] {name}: {e.Message}");
            enabled = false;
        }
    }

    private void BuildGlow()
    {
        var go = new GameObject("SteleGlow", typeof(Light2D));
        go.transform.position = transform.position + Vector3.up * (topY * 0.5f);
        go.transform.SetParent(transform, true);
        glow = go.GetComponent<Light2D>();
        glow.lightType = Light2D.LightType.Point;
        glow.color = new Color(1f, 0.9f, 0.62f);
        glow.pointLightOuterRadius = 2.6f;
        glow.pointLightInnerRadius = 0.5f;
        glow.falloffIntensity = 0.85f;
        glow.intensity = 0.7f;
    }

    private void BuildMark(SpriteRenderer host)
    {
        var go = new GameObject("SteleMark");
        go.transform.position = transform.position + Vector3.up * (topY + 0.3f);
        go.transform.SetParent(transform, true);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Diamond();
        sr.color = new Color(1f, 0.84f, 0.4f, 1f);
        if (host != null)
        {
            sr.sortingLayerID = host.sortingLayerID;
            sr.sortingOrder = host.sortingOrder + 30; // 기둥이나 다른 물건에 가려지지 않게
        }
        mark = go.transform;
    }

    // 표식은 둥실둥실, 빛은 천천히 숨 쉬듯
    private void Update()
    {
        clock += Time.deltaTime;
        if (mark != null) mark.position = transform.position + Vector3.up * (topY + 0.3f + Mathf.Sin(clock * 2.4f) * 0.07f);
        if (glow != null) glow.intensity = 0.6f + 0.25f * Mathf.Sin(clock * 1.6f);
    }

    // 7x7 도트 마름모
    private static Sprite Diamond()
    {
        if (diamond != null) return diamond;
        var tex = new Texture2D(7, 7, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < 7; y++)
        {
            for (int x = 0; x < 7; x++)
            {
                int d = Mathf.Abs(x - 3) + Mathf.Abs(y - 3);
                tex.SetPixel(x, y, d > 3 ? Color.clear : (d == 3 ? new Color(0.45f, 0.3f, 0.1f, 1f) : Color.white));
            }
        }
        tex.Apply();
        diamond = Sprite.Create(tex, new Rect(0, 0, 7, 7), new Vector2(0.5f, 0.5f), 32f);
        return diamond;
    }
}
