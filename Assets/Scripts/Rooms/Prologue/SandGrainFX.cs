using System;
using UnityEngine;

// 모래 알갱이 하나 (붕괴 때 구멍으로 빨려 들어감)
// SandGrainFX.Spawn(그림, 시작 위치, 빨려 들어갈 위치, 정렬 레이어, 순서)
public class SandGrainFX : MonoBehaviour
{
    private Vector3 target;
    private float speed;
    private float life;

    public static void Spawn(Sprite sprite, Vector3 from, Vector3 to, string sortingLayer, int sortingOrder)
    {
        if (sprite == null) return;

        try
        {
            var go = new GameObject("SandGrain");
            go.transform.position = from;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;

            SandGrainFX fx = go.AddComponent<SandGrainFX>();
            fx.target = to;
            fx.speed = UnityEngine.Random.Range(1.2f, 2.6f);
            fx.life = 2f;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SandGrainFX] 알갱이 생성 오류: {e}");
        }
    }

    private void Update()
    {
        life -= Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // 구멍에 가까워질수록 작아짐
        float dist = Vector3.Distance(transform.position, target);
        transform.localScale = Vector3.one * Mathf.Clamp01(dist / 0.8f + 0.2f);

        if (life <= 0f || dist < 0.05f) Destroy(gameObject);
    }
}
