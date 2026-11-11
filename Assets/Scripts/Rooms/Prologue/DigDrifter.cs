using UnityEngine;

// 발굴 현장에 떠다니는 것 하나 (모래바람 줄기, 굴러가는 마른 풀). DigAtmosphere가 만들고, 수명이 끝나면 스스로 사라짐
// - velocity: 초당 이동(칸), life: 수명(초), 처음과 끝에 서서히 나타나고 사라짐
// - rollStep > 0 이면 그 시간마다 90도씩 돌고 통통 튐 (마른 풀. 도트가 뭉개지지 않게 90도 단위로만 돌림)
public class DigDrifter : MonoBehaviour
{
    private SpriteRenderer sr;
    private Vector2 velocity;
    private float life, age, rollStep, rollTimer, baseY, maxAlpha;

    public static DigDrifter Spawn(Sprite sprite, Vector3 pos, Vector2 velocity, float life, string layer, int order, float alpha, float rollStep = 0f)
    {
        if (sprite == null) return null;
        var go = new GameObject("DigDrifter");
        go.transform.position = pos;
        DigDrifter d = go.AddComponent<DigDrifter>();
        d.sr = go.AddComponent<SpriteRenderer>();
        d.sr.sprite = sprite;
        d.sr.sortingLayerName = layer;
        d.sr.sortingOrder = order;
        d.sr.color = new Color(1f, 1f, 1f, 0f);
        d.velocity = velocity;
        d.life = Mathf.Max(0.1f, life);
        d.rollStep = rollStep;
        d.baseY = pos.y;
        d.maxAlpha = alpha;
        return d;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= life)
        {
            Destroy(gameObject);
            return;
        }
        baseY += velocity.y * Time.deltaTime;
        float hop = rollStep > 0f ? Mathf.Abs(Mathf.Sin(age * 6f)) * 0.18f : 0f;
        transform.position = new Vector3(transform.position.x + velocity.x * Time.deltaTime, baseY + hop, 0f);
        float fade = Mathf.Min(1f, Mathf.Min(age / 0.25f, (life - age) / 0.25f));
        sr.color = new Color(1f, 1f, 1f, maxAlpha * Mathf.Clamp01(fade));
        Roll();
    }

    private void Roll()
    {
        if (rollStep <= 0f) return;
        rollTimer += Time.deltaTime;
        if (rollTimer < rollStep) return;
        rollTimer = 0f;
        transform.Rotate(0f, 0f, -90f);
    }
}
