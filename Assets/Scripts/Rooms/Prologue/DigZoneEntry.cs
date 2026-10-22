using System;
using System.Collections;
using UnityEngine;

// B구역(로프 안)에 처음 들어갔을 때 짧은 연출
// - 몇 걸음 더 들어감 -> 카메라가 구역 가운데를 살짝 당겨서 보여줌 -> "?" -> 대사 -> 조작 복귀
// - 한 번만 (F_PRO_ZONE_ENTERED)
// 이 오브젝트의 트리거 콜라이더가 구역 안쪽을 덮고 있어야 함
[RequireComponent(typeof(Collider2D))]
public class DigZoneEntry : MonoBehaviour
{
    [SerializeField] private string doneFlag = "F_PRO_ZONE_ENTERED";
    [SerializeField] private Vector2 stepInto = new Vector2(19.8f, 6f);
    [SerializeField] private Vector2 zoneCenter = new Vector2(22f, 5f);
    [SerializeField, Min(1f)] private float zoomSize = 4.5f;
    [SerializeField] private DialogueLine[] lines =
    {
        new DialogueLine("나", "바람에 표층이 쓸려나가서... 모래가 봉긋하게 솟은 데가 세 군데."),
        new DialogueLine("나", "붓으로 조심조심 털어보자.")
    };

    private bool played;

    private void Start()
    {
        played = GameManager.Instance != null && GameManager.Instance.HasFlag(doneFlag);
        Collider2D col = GetComponent<Collider2D>();
        if (!col.isTrigger) Debug.LogWarning($"[{name}] 콜라이더가 트리거가 아님 -> Is Trigger 켜기");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        try
        {
            if (played || Cine.Running) return;
            if (other.GetComponentInParent<PlayerController2D>() == null) return;

            played = true;
            if (GameManager.Instance != null) GameManager.Instance.SetFlag(doneFlag);
            StartCoroutine(Play(other.GetComponentInParent<PlayerController2D>().transform));
        }
        catch (Exception e)
        {
            Debug.LogError($"[DigZoneEntry] 구역 입장 연출 오류: {e}");
        }
    }

    private IEnumerator Play(Transform player)
    {
        Cine.Begin(false);
        yield return CineActor.WalkTo(player, stepInto, 2f);

        CineCamera.Focus(zoneCenter);
        CineCamera.Zoom(zoomSize, 1f);
        yield return Cine.Wait(1f);
        yield return EmoteBalloonUI.Show(player, "?", 0.8f);
        yield return Cine.Say(lines);
        Cine.End();
    }
}
