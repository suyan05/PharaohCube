using UnityEngine;

// 빛 덩어리 하나: 천천히 떠다니며 밝아졌다 사라짐
public class DreamGlowOrb : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color color;
    private Vector2 drift;
    private float life, maxLife;

    public void Setup(SpriteRenderer renderer, Color c, float size, float seconds)
    {
        sr = renderer;
        color = c;
        transform.localScale = Vector3.one * size;
        drift = Random.insideUnitCircle * 0.15f;
        maxLife = life = seconds;
        sr.color = Color.clear;
    }

    private void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(drift * Time.deltaTime);
        float p = 1f - life / maxLife;
        Color c = color;
        c.a *= Mathf.Sin(p * Mathf.PI); // 0 -> 최대 -> 0
        sr.color = c;
    }
}
