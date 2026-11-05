using UnityEngine;

// 바람에 실린 먼지 한 톨 (스스로 움직이다 사라짐)
public class HorusDust : MonoBehaviour
{
    private Vector3 velocity;
    private SpriteRenderer sr;
    private float life, age;
    private Color color;

    public static void Spawn(Vector3 pos, Vector2 dir, Color color)
    {
        SpriteRenderer r = HorusFx.Spawn("WallDust", FxSprites.Dot, pos, UnityEngine.Random.Range(0.05f, 0.1f), color, HorusFx.Above - 10);
        HorusDust d = r.gameObject.AddComponent<HorusDust>();
        d.sr = r;
        d.color = color;
        d.life = UnityEngine.Random.Range(1.2f, 2f);
        d.velocity = (Vector3)(dir * UnityEngine.Random.Range(0.6f, 1.1f)) + Vector3.up * UnityEngine.Random.Range(-0.1f, 0.15f);
    }

    private void Update()
    {
        age += Time.deltaTime;
        transform.position += velocity * Time.deltaTime + Vector3.up * Mathf.Sin(age * 5f) * 0.004f;
        if (sr != null) sr.color = new Color(color.r, color.g, color.b, color.a * (1f - age / life));
        if (age >= life) Destroy(gameObject);
    }
}
