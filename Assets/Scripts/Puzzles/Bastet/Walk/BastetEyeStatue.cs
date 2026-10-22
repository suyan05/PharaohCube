using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 바스테트 걷는 퍼즐 공용 부품
// ================= 작은 고양이 석상 =================
public interface IBastetStatueOwner
{
    string PromptFor(int index);
    void OnStatuePressed(int index);
}

[RequireComponent(typeof(Collider2D))]
public class BastetEyeStatue : MonoBehaviour, IInteractable
{
    [Header("눈 빛 (자식 오브젝트, 평소에는 꺼짐)")]
    [SerializeField] private SpriteRenderer eyes;
    [SerializeField] private Light2D eyeLight;
    [Tooltip("횃불을 먹을 때 흔들 몸통 (비우면 이 오브젝트)")]
    [SerializeField] private Transform body;

    [Header("[추가] 그림 교체 (아트가 오면 연결, 비우면 생략)")]
    [Tooltip("석상 본체 SpriteRenderer")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [Tooltip("횃불을 삼킬 때 그림 (Bastet_Obj_EyeCat_Mouth)")]
    [SerializeField] private Sprite mouthSprite;
    private Sprite normalSprite;

    private IBastetStatueOwner owner;
    private int index;
    private Coroutine eatRoutine;
    private Vector3 bodyBase;

    public void Init(IBastetStatueOwner o, int i)
    {
        owner = o;
        index = i;
        if (body == null) body = transform;
        bodyBase = body.localPosition;
        if (bodyRenderer != null) normalSprite = bodyRenderer.sprite;
    }

    public string GetPrompt() => owner != null ? owner.PromptFor(index) : "";

    public void Interact(GameObject player)
    {
        if (owner != null && !PuzzleOverlayFocus.IsActive) owner.OnStatuePressed(index);
    }

    public void SetEyes(bool on, Color color)
    {
        if (eyes != null) { eyes.enabled = on; eyes.color = color; }
        if (eyeLight != null) { eyeLight.enabled = on; eyeLight.color = color; }
    }

    // 틀렸을 때: 석상이 횃불을 먹는 연출 (몸통 흔들림 + 빨간 눈)
    public void EatTorch(Color wrongColor, float time)
    {
        if (eatRoutine != null) StopCoroutine(eatRoutine);
        eatRoutine = StartCoroutine(EatRoutine(wrongColor, time));
    }

    private IEnumerator EatRoutine(Color wrongColor, float time)
    {
        SetEyes(true, wrongColor);
        if (bodyRenderer != null && mouthSprite != null) bodyRenderer.sprite = mouthSprite; // [추가] 입 벌림
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float shake = Mathf.Sin(t * 60f) * 0.06f * (1f - t / time);
            body.localPosition = bodyBase + new Vector3(shake, 0f, 0f);
            yield return null;
        }
        body.localPosition = bodyBase;
        if (bodyRenderer != null && normalSprite != null) bodyRenderer.sprite = normalSprite; // [추가] 원래 그림
        SetEyes(false, wrongColor);
        eatRoutine = null;
    }

#if UNITY_EDITOR
    public void EditorSetup(SpriteRenderer e, Light2D l, Transform b) { eyes = e; eyeLight = l; body = b; }
#endif
}
