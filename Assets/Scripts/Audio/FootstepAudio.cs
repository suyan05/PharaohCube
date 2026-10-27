using UnityEngine;

// 플레이어 발소리 (GameAudioDirector가 플레이어에 자동으로 붙임)
// - 실제로 움직이고 있을 때만 (Rigidbody2D 속도), 걷는 속도에 맞춰 간격 조절
// - 바닥: sand = 모래 밟는 소리(프롤로그) / stone = 돌바닥(신전)
// - 연출 중 자동 걷기(CineActor.WalkTo)도 속도가 있어서 같이 소리 남
[RequireComponent(typeof(Rigidbody2D))]
public class FootstepAudio : MonoBehaviour
{
    public string Surface = "sand";

    [SerializeField] private float minSpeed = 0.4f;
    [SerializeField] private float stepDistance = 0.85f; // 이만큼 걸을 때마다 한 발
    [SerializeField, Range(0f, 1f)] private float volume = 0.45f;

    private Rigidbody2D body;
    private float walked;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (body == null || Time.timeScale <= 0f) return;

        float speed = body.linearVelocity.magnitude;
        if (speed < minSpeed)
        {
            walked = stepDistance * 0.7f; // 멈췄다 다시 걸으면 첫발이 금방 나오게
            return;
        }

        walked += speed * Time.deltaTime;
        if (walked < stepDistance) return;
        walked = 0f;
        GameAudio.PlayRandom($"sfx_step_{Surface}_", 3, volume, 0.1f);
    }
}
