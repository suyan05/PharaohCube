using System;
using System.Collections;
using UnityEngine;

// 발굴 구역 모래 더미 3개 묶음 (기획서 2.2)
// - 각 모래 더미는 Inspectable (붓 필요 + 길게 누르기 + Big/Small -> Gone 그림)
//   모래 더미 Inspectable의 Grant Items / Lines는 비워둠 -> 결과는 여기서 정함
// - 털은 순서대로: 1번째 = "깨진 토기 조각이다." / 2번째 = "또 토기다."
// - 마지막(세 번째)으로 털은 것이 항상 스카라브 (어느 더미든 상관없이)
//   -> 그림을 _Scarab으로 바꾸고 COL_SCARAB 획득 팝업 -> 붕괴 연출
public class SandPileGroup : MonoBehaviour
{
    [Header("모래 더미 (Inspectable 3개)")]
    [SerializeField] private Inspectable[] piles;

    [Header("결과")]
    [SerializeField] private DialogueLine[] firstLines = { new DialogueLine("", "깨진 토기 조각이다.") };
    [SerializeField] private DialogueLine[] laterLines = { new DialogueLine("", "또 토기다.") };
    [SerializeField] private string scarabItem = "COL_SCARAB";
    [Tooltip("Pro_Obj_SandPile_Scarab")]
    [SerializeField] private Sprite scarabSprite;
    [Tooltip("스카라브 팝업 뒤에 나올 대사 (비우면 바로 붕괴)")]
    [SerializeField] private DialogueLine[] scarabLines;

    [Header("붕괴")]
    [SerializeField] private DigCollapseSequence collapse;

    private int clearedCount;
    private bool scarabFound;

    private void OnEnable()
    {
        if (piles == null) return;
        foreach (Inspectable pile in piles)
        {
            if (pile != null) pile.OnCompleted += HandleCleared;
        }
    }

    private void OnDisable()
    {
        if (piles == null) return;
        foreach (Inspectable pile in piles)
        {
            if (pile != null) pile.OnCompleted -= HandleCleared;
        }
    }

    private void Start()
    {
        if (piles == null || piles.Length == 0) Debug.LogWarning("[SandPileGroup] 모래 더미가 비어 있음");
        if (collapse == null) Debug.LogWarning("[SandPileGroup] DigCollapseSequence가 비어 있음 -> 스카라브 뒤 붕괴가 안 됨");
    }

    private void HandleCleared(Inspectable pile)
    {
        try
        {
            if (scarabFound) return;
            clearedCount++;

            if (clearedCount >= piles.Length) StartCoroutine(FindScarab(pile));
            else StartCoroutine(Inspectable.PlayLines(clearedCount == 1 ? firstLines : laterLines));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SandPileGroup] 모래 더미 결과 오류: {e}");
        }
    }

    private IEnumerator FindScarab(Inspectable pile)
    {
        scarabFound = true;
        ShowScarabSprite(pile);
        ObjectiveBoxUI.Hide();

        bool closed = false;
        ItemPopup.Grant(scarabItem, () => closed = true);
        while (!closed) yield return null;

        yield return Inspectable.PlayLines(scarabLines);
        StartCollapse();
    }

    private void ShowScarabSprite(Inspectable pile)
    {
        if (pile == null || scarabSprite == null) return;
        SpriteRenderer sr = pile.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = scarabSprite;
    }

    private void StartCollapse()
    {
        if (collapse != null) collapse.Play();
        else Debug.LogError("[SandPileGroup] 붕괴 연출이 연결 안 됨 -> 다음 씬으로 못 넘어감 (F12로 넘기기)");
    }
}
