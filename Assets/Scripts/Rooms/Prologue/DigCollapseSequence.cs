using System;
using System.Collections;
using UnityEngine;

// 붕괴 연출 (기획서 4.1 / 세부 아트 요청서 1.3 컷 8) - SandPileGroup이 스카라브 뒤에 부름
// 1. 땅이 낮게 울림 -> "?" "...응?"
// 2. 무전기 다급한 경고 (끊김)
// 3. 발밑에 금 _01 -> _02 -> _03 (0.6초씩), 흔들림 약 -> 강, 모래 알갱이가 구멍으로
// 4. "!!" -> 플레이어가 구멍으로 빨려 떨어짐 -> 암전 -> 정적 -> 다음 씬 (Prologue_Sphinx)
// Cine 연출 상태는 SandPileGroup에서 이미 시작된 상태로 들어옴 (조작 잠금 + 검은 바 유지)
public class DigCollapseSequence : MonoBehaviour
{
    [Header("바닥 금 (Pro_FX_Crack_01~03)")]
    [SerializeField] private Sprite[] crackFrames;
    [SerializeField, Min(0.05f)] private float frameSeconds = 0.6f;
    [SerializeField] private string crackSortingLayer = "Default";
    [SerializeField] private int crackSortingOrder = -1;

    [Header("흔들림 (유닛)")]
    [SerializeField, Min(0f)] private float weakShake = 0.03f;
    [SerializeField, Min(0f)] private float strongShake = 0.22f;

    [Header("모래 알갱이 (Pro_FX_SandGrain_01~03)")]
    [SerializeField] private Sprite[] grainSprites;
    [SerializeField, Min(0)] private int grainCount = 28;
    [SerializeField] private float grainSpread = 2.5f;
    [SerializeField] private int grainSortingOrder = 1;

    [Header("소리 (비워도 됨)")]
    [SerializeField] private AudioClip rumbleSfx;
    [SerializeField] private AudioClip radioStaticSfx;
    [SerializeField] private AudioClip collapseSfx;

    [Header("대사")]
    [SerializeField] private DialogueLine[] rumbleLines = { new DialogueLine("나", "...응? 땅이 울리는데?") };
    [SerializeField]
    private DialogueLine[] radioWarnLines =
    {
        new DialogueLine("무전 (탐사대장)", "치직- B구역 아래는- 치지직- 빈 공간일 수도- 당장 나와-!")
    };
    [SerializeField] private DialogueLine[] fallLines = { new DialogueLine("나", "어, 어어...?!") };

    [Header("끝")]
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.5f;
    [SerializeField, Min(0f)] private float blackHoldSeconds = 1f;
    [SerializeField] private string nextScene = "Prologue_Sphinx";

    private bool playing;

    public void Play()
    {
        if (playing) return;
        playing = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        Transform player = CineActor.Player();
        Vector3 feet = player != null ? player.position : transform.position;

        yield return Warning(player, feet);
        SpriteRenderer crack = CreateCrack(feet);
        CineAudio.Play(collapseSfx);
        yield return PlayCrack(crack);
        yield return Fall(player, feet);
        LoadNext();
    }

    // ================= 장면들 =================
    private IEnumerator Warning(Transform player, Vector3 feet)
    {
        CineCamera.Focus(feet);
        CineAudio.Play(rumbleSfx);
        yield return CineCamera.Rumble(1f, 0f, weakShake);
        yield return EmoteBalloonUI.Show(player, "?", 0.7f);
        yield return Cine.Say(rumbleLines);

        CineAudio.Play(radioStaticSfx);
        Cine.Run(CineCamera.Rumble(4f, weakShake, weakShake * 1.5f));
        yield return Cine.Say(radioWarnLines);
    }

    // 금이 커질수록 흔들림도 강해짐
    private IEnumerator PlayCrack(SpriteRenderer crack)
    {
        int count = crackFrames != null && crackFrames.Length > 0 ? crackFrames.Length : 3;
        for (int i = 0; i < count; i++)
        {
            if (crack != null && crackFrames != null && i < crackFrames.Length) crack.sprite = crackFrames[i];
            float t = count > 1 ? i / (float)(count - 1) : 1f;
            yield return CineCamera.Rumble(frameSeconds, Mathf.Lerp(weakShake, strongShake, t * 0.7f), Mathf.Lerp(weakShake, strongShake, t));
        }
    }

    private IEnumerator Fall(Transform player, Vector3 feet)
    {
        SpawnGrains(feet);
        EmoteBalloonUI.Pop(player, "!!", 0.6f);
        yield return Cine.Say(fallLines);

        CineCamera.Zoom(3.5f, 0.5f);
        Cine.Run(CineCamera.Rumble(fadeOutSeconds + 0.6f, strongShake, strongShake));
        yield return CineActor.Sink(player, 0.6f, 0.6f);
        yield return ScreenFader.FadeOut(fadeOutSeconds);
        yield return CineAudio.StopLoop(0.3f);
        yield return new WaitForSeconds(blackHoldSeconds);
    }

    // ================= 도우미 =================
    private SpriteRenderer CreateCrack(Vector3 feet)
    {
        try
        {
            var go = new GameObject("Crack");
            go.transform.position = feet;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = crackSortingLayer;
            sr.sortingOrder = crackSortingOrder;
            return sr;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DigCollapseSequence] 바닥 금 생성 오류: {e}");
            return null;
        }
    }

    private void SpawnGrains(Vector3 feet)
    {
        if (grainSprites == null || grainSprites.Length == 0) return;

        for (int i = 0; i < grainCount; i++)
        {
            Sprite sprite = grainSprites[UnityEngine.Random.Range(0, grainSprites.Length)];
            Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.6f, grainSpread);
            SandGrainFX.Spawn(sprite, feet + (Vector3)offset, feet, crackSortingLayer, grainSortingOrder);
        }
    }

    private void LoadNext()
    {
        try
        {
            SceneFlow.LoadByNameOrNext(nextScene);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DigCollapseSequence] 다음 씬 이동 오류: {e}");
            Cine.End();
            playing = false;
        }
    }
}
