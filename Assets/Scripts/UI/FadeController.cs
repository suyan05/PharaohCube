using System;
using System.Collections;
using UnityEngine;

public class FadeController : MonoBehaviour
{
    public static FadeController Instance { get; private set; }

    [Header("UI Reference")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 3f; // 기준 3초

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 씬이 바뀌어도 파괴되지 않고 유지되도록 설정
            DontDestroyOnLoad(gameObject.transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Update()
    {
        // 키보드 F1을 누르면 페이드 아웃, F2를 누르면 페이드 인
        if (Input.GetKeyDown(KeyCode.F1))
        {
            FadeOut(() => Debug.Log("페이드 아웃 완료"));
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            FadeIn(() => Debug.Log("페이드 인 완료"));
        }
    }
    // 화면이 점점 어두워짐
    public void FadeOut(Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(0f, 1f, onComplete, true));
    }

    // 화면이 점점 밝아짐
    public void FadeIn(Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1f, 0f, onComplete, false));
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, Action onComplete, bool isFadeOut)
    {
        // 페이드 진행 중에는 마우스 클릭 차단
        fadeCanvasGroup.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            // 문서 명세 곡선 적용
            float curveT = isFadeOut
                ? Mathf.SmoothStep(0f, 1f, t)
                : Mathf.Sin(t * Mathf.PI * 0.5f);

            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, curveT);
            yield return null;
        }

        fadeCanvasGroup.alpha = endAlpha;
        // 완전히 어두워진 상태가 아니면 입력 차단 해제
        fadeCanvasGroup.blocksRaycasts = (endAlpha > 0.9f);
        onComplete?.Invoke();
    }

    [ContextMenu("테스트: 페이드 아웃")]
    private void TestFadeOut() => FadeOut(() => Debug.Log("페이드 아웃 완료"));

    [ContextMenu("테스트: 페이드 인")]
    private void TestFadeIn() => FadeIn(() => Debug.Log("페이드 인 완료"));
}