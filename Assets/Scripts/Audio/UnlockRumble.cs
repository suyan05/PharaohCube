using UnityEngine;

// 방(구역)이 열릴 때 쿠르릉 (화면 흔들림과 같이)
// - 낮게 울리는 쿠르릉 + 돌 부서지는 소리를 살짝 섞음 (노트북 스피커는 낮은 소리가 잘 안 들려서)
// - 1.2초 안에 여러 번 불려도 한 번만 (구역 연출과 "[Door] 열렸다" 알림이 겹칠 때)
public static class UnlockRumble
{
    private const float MinGap = 1.2f;
    private static float last = -10f;

    public static void Play(float volume = 1f)
    {
        if (Time.unscaledTime - last < MinGap) return;
        last = Time.unscaledTime;
        GameAudio.Play("sfx_rumble", volume, 0.9f);
        GameAudio.Play("sfx_sand_collapse", volume * 0.5f, 0.8f);
    }
}
