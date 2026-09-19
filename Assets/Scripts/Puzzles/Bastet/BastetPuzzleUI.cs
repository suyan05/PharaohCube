using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BastetPuzzleUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private BastetPuzzleManager puzzleManager;

    [Header("버튼")]
    [SerializeField] private Button btnLeft;
    [SerializeField] private Button btnRight;
    [SerializeField] private Button btnCenter;

    private Button[] buttons;
    private readonly Color normalColor = Color.white;
    private readonly Color highlightColor = Color.yellow;

    private void Awake()
    {
        buttons = new Button[] { btnLeft, btnRight, btnCenter };
    }

    private void Start()
    {
        btnLeft.onClick.AddListener(() => puzzleManager.SubmitInput(0));
        btnRight.onClick.AddListener(() => puzzleManager.SubmitInput(1));
        btnCenter.onClick.AddListener(() => puzzleManager.SubmitInput(2));
    }

    private void OnEnable()
    {
        puzzleManager.OnPuzzleCleared += HandleCleared;
        puzzleManager.OnPuzzleFailed += HandleFailed;
        puzzleManager.OnShowStep += HandleShowStep;
        puzzleManager.OnShowPatternStart += HandleShowStart;
        puzzleManager.OnShowPatternEnd += HandleShowEnd;
    }

    private void OnDisable()
    {
        puzzleManager.OnPuzzleCleared -= HandleCleared;
        puzzleManager.OnPuzzleFailed -= HandleFailed;
        puzzleManager.OnShowStep -= HandleShowStep;
        puzzleManager.OnShowPatternStart -= HandleShowStart;
        puzzleManager.OnShowPatternEnd -= HandleShowEnd;
    }

    private void HandleShowStart()
    {
        SetButtonsInteractable(false);
    }

    private void HandleShowEnd()
    {
        SetButtonsInteractable(true);
    }

    private void HandleShowStep(int index)
    {
        StartCoroutine(FlashButton(buttons[index]));
    }

    private IEnumerator FlashButton(Button btn)
    {
        btn.image.color = highlightColor;
        yield return new WaitForSeconds(0.4f);
        btn.image.color = normalColor;
    }

    private void SetButtonsInteractable(bool value)
    {
        foreach (var btn in buttons)
            btn.interactable = value;
    }

    private void HandleCleared()
    {
        Debug.Log("[BastetPuzzleUI] 클리어! (연출은 나중에 추가 예정)");
    }

    private void HandleFailed()
    {
        Debug.Log("[BastetPuzzleUI] 오답! 패턴 다시 보여줍니다.");
    }
}