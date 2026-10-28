using System;
using UnityEngine;

// 작은 그림 하나가 떠다니다 사라지는 효과 (모래먼지, 바람, 반짝임 공용)
// DriftFX.Spawn(그림, 위치, 속도, 수명, 색, 정렬 레이어, 순서)
// - 마지막 40% 동안 서서히 투명해짐, drag로 점점 느려짐
public class DriftFX : MonoBehaviour
{
    private SpriteRenderer sr;
    private Vector2 velocity;
    private float life;
    private float maxLife;
    private float drag;
    private Color baseColor;

    public static void Spawn(Sprite sprite, Vector3 pos, Vector2 velocity, float life, Color color, string layer, int order, float drag = 1.5f)
    {
        if (sprite == null) return;

        try
        {
            var go = new GameObject("DriftFX");
            go.transform.position = pos;

            DriftFX fx = go.AddComponent<DriftFX>();
            fx.sr = go.AddComponent<SpriteRenderer>();
            fx.sr.sprite = sprite;
            fx.sr.sortingLayerName = layer;
            fx.sr.sortingOrder = YSortSystem.OrderFor(layer, order, pos); // [수정] 앞뒤 자동 정렬 중이면 그 위치 높이에 맞춤
            fx.sr.color = color;
            fx.Setup(velocity, life, color, drag);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DriftFX] 효과 생성 오류: {e}");
        }
    }

    private void Setup(Vector2 newVelocity, float newLife, Color color, float newDrag)
    {
        velocity = newVelocity;
        maxLife = life = Mathf.Max(0.05f, newLife);
        baseColor = color;
        drag = newDrag;
    }

    private void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        velocity *= Mathf.Clamp01(1f - drag * Time.deltaTime);
        transform.position += (Vector3)(velocity * Time.deltaTime);

        Color c = baseColor;
        c.a *= Mathf.Clamp01(life / (maxLife * 0.4f));
        sr.color = c;
    }
}
