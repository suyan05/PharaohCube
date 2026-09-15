using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FadeController : MonoBehaviour
{
    public static FadeController Instance { get; private set; }

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.8f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (fadeImage == null)
        {
            fadeImage = GetComponent<Image>();
            if (fadeImage == null) fadeImage = GetComponentInChildren<Image>();
        }
    }

    private void Start()
    {
        FadeIn();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        FadeIn();
    }

    public void FadeIn(Action onComplete = null)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1f, 0f, onComplete));
    }

    public void FadeOut(Action onComplete = null)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(0f, 1f, onComplete));
    }

    public void FadeOutAndLoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        FadeOut(() =>
        {
            SceneManager.LoadScene(sceneName);
        });
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, Action onComplete)
    {
        if (fadeImage == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        gameObject.SetActive(true);
        fadeImage.enabled = true;
        fadeImage.raycastTarget = (endAlpha > 0.5f);

        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            color.a = Mathf.Lerp(startAlpha, endAlpha, smoothT);
            fadeImage.color = color;
            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;

        if (endAlpha <= 0f)
        {
            fadeImage.raycastTarget = false;
            fadeImage.enabled = false;
        }

        onComplete?.Invoke();
    }
}