using UnityEngine;

// 그림 몇 장을 차례로 바꿔 보여 주는 작은 애니메이션 (물결, 갈대, 악어 대기 등)
// 붙이는 곳: SpriteRenderer가 있는 오브젝트. Frames에 그림 2장 이상
[RequireComponent(typeof(SpriteRenderer))]
public class SobekSpriteAnim : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [Range(0.5f, 12f)] [SerializeField] private float fps = 2f;
    [Tooltip("여러 개가 똑같이 깜빡이지 않게 시작 시간을 섞음")]
    [SerializeField] private bool randomOffset = true;

    private SpriteRenderer sr;
    private float offset;
    private int last = -1;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        offset = randomOffset ? Random.value * 10f : 0f;
    }

    private void Update()
    {
        if (frames == null || frames.Length < 2) return;
        int i = Mathf.FloorToInt((Time.time + offset) * fps) % frames.Length;
        if (i == last) return; // 바뀔 때만 대입
        last = i;
        sr.sprite = frames[i];
    }

    public void SetFrames(Sprite[] f, float speed)
    {
        frames = f;
        fps = speed;
        last = -1;
    }
}
