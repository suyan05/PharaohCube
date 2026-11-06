using System.Collections;
using UnityEngine;

// 아펩의 그림자 (실체 없음, 보랏빛 검은 뱀 그림자. 기획서 6장)
// - 숨은 방이 열릴 때 계단 쪽에서 솟았다가 물결치며 멀리 달아나고 흐려짐 (조작 없는 짧은 연출)
// 붙이는 곳: 그림자 SpriteRenderer 오브젝트 (평소엔 꺼 둠)
[RequireComponent(typeof(SpriteRenderer))]
public class SobekApepShadow : MonoBehaviour
{
    [Tooltip("달아나는 방향·거리 (월드 단위)")]
    [SerializeField] private Vector2 fleeOffset = new Vector2(-9f, -2f);
    [Range(0.5f, 4f)] [SerializeField] private float fleeTime = 1.8f;
    [Range(0f, 1f)] [SerializeField] private float maxAlpha = 0.8f;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;
    }

    public IEnumerator Flee()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        Vector3 start = transform.position;
        sr.enabled = true;
        AnubisToast.Show("…쉬익. 보랏빛 그림자가 계단 아래에서 빠져나간다!", 2f);
        // 솟구침
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            SetAlpha(maxAlpha * t / 0.4f);
            transform.localScale = new Vector3(1f, Mathf.Lerp(0.3f, 1f, t / 0.4f), 1f);
            yield return null;
        }
        // 물결치며 달아남
        for (float t = 0f; t < fleeTime; t += Time.deltaTime)
        {
            float k = t / fleeTime;
            transform.position = start + (Vector3)(fleeOffset * k) + new Vector3(0f, Mathf.Sin(k * 18f) * 0.25f, 0f);
            SetAlpha(maxAlpha * (1f - k));
            yield return null;
        }
        sr.enabled = false;
        transform.position = start;
        transform.localScale = Vector3.one;
    }

    private void SetAlpha(float a)
    {
        Color c = sr.color; c.a = a; sr.color = c;
    }
}
