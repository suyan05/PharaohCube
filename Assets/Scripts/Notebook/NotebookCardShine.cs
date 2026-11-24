using UnityEngine;

// 황금 카드 반짝임: 왼쪽 밖 -> 오른쪽 밖으로 지나가고 잠깐 쉼 (수첩은 시간이 멈춰도 움직이게 unscaled)
public class NotebookCardShine : MonoBehaviour
{
    private const float Sweep = 0.9f, Rest = 2.4f;
    private RectTransform rt, card;
    private float clock;

    private void Awake()
    {
        rt = (RectTransform)transform;
        card = (RectTransform)transform.parent;
        clock = Random.value * (Sweep + Rest);
    }

    private void Update()
    {
        clock = (clock + Time.unscaledDeltaTime) % (Sweep + Rest);
        float w = card.sizeDelta.x;
        float u = Mathf.Clamp01(clock / Sweep);
        rt.anchoredPosition = new Vector2(Mathf.Lerp(-w, w, u), 0f);
    }
}
