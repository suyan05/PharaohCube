using System;
using System.Collections;
using UnityEngine;

// 붕괴 연출 (기획서 4.1 / 세부 아트 요청서 1.3 컷 8)
// 1. 플레이어 발밑에 금 _01 -> _02 -> _03 (0.6초씩), 흔들림 약 -> 강
// 2. 모래 알갱이가 구멍으로 빨려 들어감
// 3. 암전 -> 다음 씬 (Prologue_Sphinx, 없으면 다음 번호 씬)
// 연출 동안 플레이어 조작 잠금
public class DigCollapseSequence : MonoBehaviour
{
    [Header("바닥 금 (Pro_FX_Crack_01~03)")]
    [SerializeField] private Sprite[] crackFrames;
    [SerializeField, Min(0.05f)] private float frameSeconds = 0.6f;
    [SerializeField] private string crackSortingLayer = "Default";
    [SerializeField] private int crackSortingOrder = -1;

    [Header("흔들림 (유닛)")]
    [SerializeField, Min(0f)] private float weakShake = 0.04f;
    [SerializeField, Min(0f)] private float strongShake = 0.2f;

    [Header("모래 알갱이 (Pro_FX_SandGrain_01~03)")]
    [SerializeField] private Sprite[] grainSprites;
    [SerializeField, Min(0)] private int grainCount = 28;
    [SerializeField] private float grainSpread = 2.5f;
    [SerializeField] private int grainSortingOrder = 1;

    [Header("끝")]
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.6f;
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
        PlayerLock.Lock(this);
        Vector3 feet = FindFeet();
        SpriteRenderer crack = CreateCrack(feet);

        yield return PlayCrack(crack);
        SpawnGrains(feet);
        CameraShake2D.Set(strongShake);

        yield return ScreenFader.FadeOut(fadeOutSeconds);
        CameraShake2D.Set(0f);
        LoadNext();
    }

    // 금이 커질수록 흔들림도 강해짐
    private IEnumerator PlayCrack(SpriteRenderer crack)
    {
        int count = crackFrames != null && crackFrames.Length > 0 ? crackFrames.Length : 3;
        for (int i = 0; i < count; i++)
        {
            if (crack != null && crackFrames != null && i < crackFrames.Length) crack.sprite = crackFrames[i];
            float t = count > 1 ? i / (float)(count - 1) : 1f;
            CameraShake2D.Set(Mathf.Lerp(weakShake, strongShake, t));
            yield return new WaitForSeconds(frameSeconds);
        }
    }

    private Vector3 FindFeet()
    {
        PlayerController2D player = FindAnyObjectByType<PlayerController2D>();
        if (player != null) return player.transform.position;

        Debug.LogWarning("[DigCollapseSequence] 플레이어를 못 찾음 -> 이 오브젝트 위치에서 붕괴");
        return transform.position;
    }

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
            PlayerLock.Unlock(this);
            playing = false;
        }
    }
}
