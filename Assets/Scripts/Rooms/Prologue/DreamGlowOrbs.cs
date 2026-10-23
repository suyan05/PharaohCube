using UnityEngine;

// 꿈 공간에 크고 흐릿한 빛 덩어리가 천천히 떠다님 (그림 없이 코드로 부드러운 원을 만들어 씀)
// - 화면 안에 Max Orbs개까지, 생겼다가 서서히 밝아지고 다시 사라짐
// - 보라, 분홍, 금빛 중 하나. 아주 연하게 (알파 0.08~0.2)
public class DreamGlowOrbs : MonoBehaviour
{
    private static Sprite softCircle;

    [SerializeField, Min(0)] private int maxOrbs = 7;
    [SerializeField] private Color[] colors =
    {
        new Color(0.78f, 0.7f, 1f, 0.16f),
        new Color(1f, 0.72f, 0.9f, 0.12f),
        new Color(1f, 0.85f, 0.5f, 0.1f)
    };

    [Header("정렬 (배치 도구가 플레이어 기준으로 채움)")]
    [SerializeField] private string sortingLayer = "Default";
    [SerializeField] private int sortingOrder = 25;

    private float nextSpawn;

    private void Update()
    {
        if (Time.time < nextSpawn || transform.childCount >= maxOrbs) return;
        nextSpawn = Time.time + Random.Range(0.6f, 1.6f);

        Camera cam = Camera.main;
        if (cam != null) Spawn(cam);
    }

    private void Spawn(Camera cam)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c = cam.transform.position;
        Vector3 pos = new Vector3(c.x + Random.Range(-halfW, halfW), c.y + Random.Range(-halfH, halfH), 0f);

        var go = new GameObject("GlowOrb");
        go.transform.SetParent(transform, false);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SoftCircle();
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder;

        Color color = colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white;
        go.AddComponent<DreamGlowOrb>().Setup(sr, color, Random.Range(1.5f, 4f), Random.Range(5f, 9f));
    }

    // 가운데가 밝고 바깥으로 갈수록 투명한 원 (한 번만 만들어 재사용)
    private static Sprite SoftCircle()
    {
        if (softCircle != null) return softCircle;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            float d = Vector2.Distance(new Vector2(i % size + 0.5f, i / size + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) / (size * 0.5f);
            float a = Mathf.Clamp01(1f - d);
            pixels[i] = new Color32(255, 255, 255, (byte)(a * a * 255f));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        softCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return softCircle;
    }
}
