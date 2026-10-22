using System.Collections;
using UnityEngine;

// 연출용 효과 모음 (DriftFX를 여러 개 뿌림)
// - Puff: 발밑, 모래 더미에서 먼지가 퐁 (붓질, 착지)
// - Wind: 화면 왼쪽에서 오른쪽으로 모래바람이 쓸고 감 (코루틴, Cine.Run으로 따로 돌리기 좋음)
// - Sparkle: 금빛 반짝 (유물 발견)
// 그림은 모래 알갱이(Pro_FX_SandGrain_01~03)를 씀. sprites가 비면 아무것도 안 함
public static class CineFX
{
    public static string SortingLayer = "Default";
    public static int SortingOrder = 5;

    private static readonly Color Dust = new Color(0.92f, 0.82f, 0.6f, 0.9f);
    private static readonly Color Gold = new Color(1f, 0.83f, 0.35f, 1f);

    // 효과가 플레이어와 같은 정렬 레이어에서 플레이어보다 한 칸 앞에 그려지도록 (Cine.Begin이 부름)
    public static void MatchPlayer()
    {
        PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
        SpriteRenderer sr = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        if (sr == null) return;

        SortingLayer = sr.sortingLayerName;
        SortingOrder = sr.sortingOrder + 1;
    }

    public static void Puff(Sprite[] sprites, Vector3 pos, int count, float power = 1.2f)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            dir.y = Mathf.Abs(dir.y) * 0.7f + 0.2f; // 위, 옆으로 퍼짐
            Vector3 start = pos + (Vector3)(Random.insideUnitCircle * 0.15f);
            DriftFX.Spawn(Pick(sprites), start, dir * Random.Range(0.4f, 1f) * power, Random.Range(0.5f, 0.9f), Dust, SortingLayer, SortingOrder);
        }
    }

    public static IEnumerator Wind(Sprite[] sprites, float seconds, float perSecond)
    {
        Camera cam = Camera.main;
        if (cam == null || sprites == null || sprites.Length == 0) yield break;

        float t = 0f, spawn = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            spawn += perSecond * Time.deltaTime;
            for (; spawn >= 1f; spawn -= 1f) SpawnWindGrain(cam, sprites);
            yield return null;
        }
    }

    private static void SpawnWindGrain(Camera cam, Sprite[] sprites)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c = cam.transform.position;
        Vector3 start = new Vector3(c.x - halfW - 0.5f, c.y + Random.Range(-halfH, halfH), 0f);
        Vector2 vel = new Vector2(Random.Range(9f, 14f), Random.Range(-0.8f, 0.4f));
        DriftFX.Spawn(Pick(sprites), start, vel, halfW * 2f / vel.x + 0.3f, Dust, SortingLayer, SortingOrder + 20, 0f);
    }

    public static void Sparkle(Sprite[] sprites, Vector3 pos, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            DriftFX.Spawn(Pick(sprites), pos, dir * Random.Range(0.8f, 1.6f), Random.Range(0.4f, 0.7f), Gold, SortingLayer, SortingOrder + 1, 3f);
        }
    }

    private static Sprite Pick(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return null;
        return sprites[Random.Range(0, sprites.Length)];
    }
}
