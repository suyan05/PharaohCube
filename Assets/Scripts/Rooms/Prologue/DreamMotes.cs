using UnityEngine;

// 꿈 공간에 천천히 떠오르는 빛 알갱이 (Pro_FX_DreamMote, 세부 아트 요청서 2.6)
// 카메라 화면 안 아무 곳에서 생겨서 위로 천천히 떠오르다 사라짐. 씬에 하나만 두면 됨
public class DreamMotes : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;
    [SerializeField, Min(0f)] private float perSecond = 3f;
    [SerializeField] private Color color = new Color(0.91f, 0.86f, 1f, 0.85f);

    [Header("정렬 (배치 도구가 플레이어 기준으로 채움)")]
    [SerializeField] private string sortingLayer = "Default";
    [SerializeField] private int sortingOrder = 30;

    private float pending;

    private void Update()
    {
        if (sprites == null || sprites.Length == 0) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        pending += perSecond * Time.deltaTime;
        for (; pending >= 1f; pending -= 1f) Spawn(cam);
    }

    private void Spawn(Camera cam)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c = cam.transform.position;

        Vector3 pos = new Vector3(c.x + Random.Range(-halfW, halfW), c.y + Random.Range(-halfH, halfH * 0.6f), 0f);
        Vector2 vel = new Vector2(Random.Range(-0.12f, 0.12f), Random.Range(0.15f, 0.35f));
        Sprite sprite = sprites[Random.Range(0, sprites.Length)];
        DriftFX.Spawn(sprite, pos, vel, Random.Range(3f, 5f), color, sortingLayer, sortingOrder, 0f);
    }
}
