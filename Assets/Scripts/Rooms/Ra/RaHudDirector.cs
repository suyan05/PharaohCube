using System;
using UnityEngine;
using UnityEngine.UI;

// 라의 방 화면 글자 정리 (씬에 1개, RaTemple에 붙음)
// 겹치던 것
//  - 목표 문구(위 가운데)가 연출의 검은 바 위에 그대로 떠 있음
//  - 횃불 안내(AnubisToast, 위 가운데)가 목표 문구에 가려짐
//  - 오른쪽 위 알림이 검은 바에 걸침
//  - 소지품 바(왼쪽 아래)가 대사창 밑에 비침
// 고친 방법 (다른 사람 UI 코드는 안 고치고 위치, 투명도만 바꿈)
//  - 연출, 대사, 아이템 팝업, 라의 대화, 클리어 컷, 플레이어가 묶여 있는 동안: 목표 문구와 소지품 바를 부드럽게 숨김
//  - 횃불 안내 상자는 목표 문구 아래로, 오른쪽 위 알림은 검은 바 아래로 내림
//  - 목표가 바뀌면 목표 상자가 한 번 통 튀면서 금빛으로 깜빡 (바뀐 걸 알아채게)
public class RaHudDirector : MonoBehaviour
{
    private const float SearchSeconds = 15f;

    [SerializeField, Min(0.1f)] private float fadeSpeed = 6f;
    [SerializeField] private Vector2 torchToastPos = new Vector2(0f, -118f);
    [SerializeField] private Vector2 torchToastSize = new Vector2(1000f, 100f);
    [SerializeField] private Vector2 alertToastPos = new Vector2(-30f, -128f);
    [SerializeField, Min(0.1f)] private float pulseSeconds = 0.6f;
    [SerializeField] private Color pulseColor = new Color(0.55f, 0.38f, 0.08f, 0.95f);

    private CanvasGroup objectiveGroup, inventoryGroup;
    private RectTransform objectiveBox;
    private Image objectiveImage;
    private Text objectiveText;
    private Color objectiveColor;
    private string lastObjective;
    private bool pulsePending;
    private float pulseT = -1f;

    private bool torchToastMoved, alertMoved;
    private RaDialogue raDialogue;
    private RaNightFinale finale;
    private PlayerController2D controller;
    private float searchTime, searchTimer;

    // 다른 라의 방 HUD(횃불 게이지 등)가 숨을지 정할 때 씀: 연출, 대사, 팝업, 퍼즐 화면, 플레이어가 묶여 있음
    public static bool Busy { get; private set; }

    private void LateUpdate()
    {
        try
        {
            FindParts();
            bool busy = IsBusy();
            Busy = busy || PuzzleOverlayFocus.IsActive || (controller != null && !controller.enabled);
            FadeGroup(objectiveGroup, busy);
            FadeGroup(inventoryGroup, busy);
            WatchObjective(busy);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaHudDirector] 화면 정리 오류 (정리 기능만 끔): {e}");
            enabled = false;
        }
    }

    private void OnDisable()
    {
        Busy = false;
    }

    // ================= 찾기 (UI는 각자 Start에서 코드로 만들어져서 조금 늦게 생김) =================
    private void FindParts()
    {
        if (searchTime > SearchSeconds) return;
        searchTime += Time.unscaledDeltaTime;
        searchTimer -= Time.unscaledDeltaTime;
        if (searchTimer > 0f) return;
        searchTimer = 0.25f;

        if (objectiveGroup == null) objectiveGroup = GroupOf("RaObjectiveCanvas");
        if (objectiveBox == null && objectiveGroup != null) FindObjectiveBox();
        if (inventoryGroup == null) inventoryGroup = GroupOf("InventoryCanvas");
        if (!torchToastMoved) torchToastMoved = MoveChild("Toast_Canvas", "ToastBox", torchToastPos, torchToastSize);
        if (!alertMoved) alertMoved = MoveChild("GameMessageCanvas", "ToastRoot", alertToastPos, Vector2.zero);
        FindOthers();
    }

    private void FindOthers()
    {
        if (raDialogue == null) raDialogue = FindFirstObjectByType<RaDialogue>(FindObjectsInactive.Include);
        if (finale == null) finale = FindFirstObjectByType<RaNightFinale>(FindObjectsInactive.Include);
        if (controller == null) controller = FindFirstObjectByType<PlayerController2D>();
    }

    private static CanvasGroup GroupOf(string canvasName)
    {
        GameObject go = GameObject.Find(canvasName);
        if (go == null) return null;
        CanvasGroup group = go.GetComponent<CanvasGroup>();
        if (group == null) group = go.AddComponent<CanvasGroup>();
        return group;
    }

    private void FindObjectiveBox()
    {
        Transform box = objectiveGroup.transform.Find("ObjectiveBox");
        if (box == null) return;
        objectiveBox = (RectTransform)box;
        objectiveImage = box.GetComponent<Image>();
        objectiveText = box.GetComponentInChildren<Text>(true);
        if (objectiveImage != null) objectiveColor = objectiveImage.color;
    }

    // size가 0이면 크기는 그대로
    private static bool MoveChild(string canvasName, string childName, Vector2 pos, Vector2 size)
    {
        GameObject canvas = GameObject.Find(canvasName);
        if (canvas == null) return false;
        var rt = canvas.transform.Find(childName) as RectTransform;
        if (rt == null) return false;

        rt.anchoredPosition = pos;
        if (size != Vector2.zero) rt.sizeDelta = size;
        return true;
    }

    // ================= 숨기기 =================
    private bool IsBusy()
    {
        if (Cine.Running || DialogueBox.IsOpen || ItemPopup.IsShowing) return true;
        if (raDialogue != null && raDialogue.IsPlaying) return true;
        if (finale != null && finale.IsPlaying) return true;
        return controller != null && !controller.enabled && !PuzzleOverlayFocus.IsActive;
    }

    private void FadeGroup(CanvasGroup group, bool hide)
    {
        if (group == null) return;
        group.alpha = Mathf.MoveTowards(group.alpha, hide ? 0f : 1f, fadeSpeed * Time.unscaledDeltaTime);
    }

    // ================= 목표가 바뀌면 통 =================
    private void WatchObjective(bool busy)
    {
        if (objectiveText == null) return;

        string now = objectiveText.text;
        if (now != lastObjective)
        {
            bool first = lastObjective == null;
            lastObjective = now;
            if (!first && !string.IsNullOrEmpty(now)) pulsePending = true;
        }

        if (pulsePending && !busy && objectiveGroup != null && objectiveGroup.alpha > 0.95f)
        {
            pulsePending = false;
            pulseT = 0f;
        }
        AnimatePulse();
    }

    private void AnimatePulse()
    {
        if (pulseT < 0f || objectiveBox == null) return;

        pulseT += Time.unscaledDeltaTime;
        float p = Mathf.Clamp01(pulseT / pulseSeconds);
        float k = Mathf.Sin(p * Mathf.PI);
        objectiveBox.localScale = Vector3.one * (1f + 0.08f * k);
        if (objectiveImage != null) objectiveImage.color = Color.Lerp(objectiveColor, pulseColor, k);

        if (p < 1f) return;
        objectiveBox.localScale = Vector3.one;
        if (objectiveImage != null) objectiveImage.color = objectiveColor;
        pulseT = -1f;
    }
}
