using UnityEngine;

// [추가] 씬 배경음: 씬에 하나 두면 씬이 시작될 때 이 곡으로 바뀜 (같은 곡이면 끊기지 않고 이어짐)
// 붙이는 곳: 씬의 빈 오브젝트 (예: "BGM")
public class SceneBgm : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;
    [Range(0f, 5f)] [SerializeField] private float fadeSeconds = 1.5f;
    [Tooltip("클립이 비어 있으면 이전 씬 배경음을 끄기")]
    [SerializeField] private bool stopIfEmpty = false;

    private void Start()
    {
        if (clip != null) SoundManager.PlayBgm(clip, volume, fadeSeconds);
        else if (stopIfEmpty) SoundManager.StopBgm(fadeSeconds);
    }

    [ContextMenu("테스트: 지금 재생")]
    private void TestPlay()
    {
        if (Application.isPlaying) SoundManager.PlayBgm(clip, volume, fadeSeconds);
    }
}
