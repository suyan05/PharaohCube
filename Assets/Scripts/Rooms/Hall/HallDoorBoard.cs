using System;
using UnityEngine;

// [추가] 중앙 홀(타일맵)의 신전 문 여러 개를 한 번에 관리 (HallTempDirector가 부름)
// - 차례인 신전 문 하나만 열림. 나머지는 잠김 (아직 차례가 아님 / 이미 끝낸 신전)
// - 아무 신전도 안 끝냈으면 라의 문 (횃불 필요)
// - 다음 신전의 문이 홀에 없으면 (아누비스: 소베크를 지나 내려감) -> 방금 끝낸 신전의 문이 다음 신전으로 이어짐
// 붙이는 곳: 중앙 홀 HallSystems (메뉴 PharaohCube -> Hall -> 중앙 홀(타일맵) 연결하기 가 채움)
public class HallDoorBoard : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public string god = "라";
        public SceneDoor door;
    }

    [SerializeField] private Entry[] doors = new Entry[0];
    [SerializeField] private string firstGod = "라";
    [SerializeField] private string torchItem = "ITEM_TORCH";

    private static readonly DialogueLine[] NotYet =
    {
        new DialogueLine("나", "문이 굳게 닫혀 있다. 아직 이 신의 차례가 아닌 것 같아.")
    };
    private static readonly DialogueLine[] Done =
    {
        new DialogueLine("나", "이 신전의 빛은 이미 큐브에 담았어.")
    };

    // 열린 문을 돌려줌 (연출에서 카메라가 비출 곳). 없으면 null
    public Transform Apply(HallRoute latest, Func<string, bool> cleared)
    {
        try
        {
            Entry open = FindOpen(latest);
            foreach (Entry e in doors) Lock(e, open, cleared);
            foreach (Entry e in doors) SetGlow(e, e == open);
            if (open == null) return null;
            Unlock(open, latest);
            return open.door.transform;
        }
        catch (Exception e)
        {
            Debug.LogError($"[HallDoorBoard] 문 정리 오류: {e}");
            return null;
        }
    }

    public void SetAutoEnter(bool on)
    {
        foreach (Entry e in doors) if (e != null && e.door != null) e.door.SetAutoEnter(on);
    }

    private Entry FindOpen(HallRoute latest)
    {
        if (latest == null) return Find(e => e.god == firstGod);
        if (string.IsNullOrEmpty(latest.nextScene)) return null; // 마지막 신전까지 끝남
        Entry byScene = Find(e => e.door.NextScene == latest.nextScene);
        return byScene ?? Find(e => e.god == latest.god);
    }

    private void Lock(Entry e, Entry open, Func<string, bool> cleared)
    {
        if (e == null || e.door == null || e == open) return;
        e.door.SetLocked(true, cleared != null && cleared(e.god) ? Done : NotYet);
    }

    private void Unlock(Entry open, HallRoute latest)
    {
        open.door.SetLocked(false);
        if (latest == null)
        {
            open.door.SetRequiredItem(torchItem);
            return;
        }
        open.door.Retarget(latest.nextPrompt, latest.nextScene);
        if (!latest.needTorch) open.door.SetRequiredItem("");
        Debug.Log($"[HallDoorBoard] {open.god}의 문 -> {latest.nextScene}");
    }

    // [추가] 열린 문만 그 신의 색으로 은은하게 빛남 (문 아래 'Glow' 자식, 메뉴가 만듦)
    private static void SetGlow(Entry e, bool on)
    {
        if (e == null || e.door == null) return;
        Transform glow = e.door.transform.Find("Glow");
        if (glow != null && glow.gameObject.activeSelf != on) glow.gameObject.SetActive(on);
    }

    private Entry Find(Predicate<Entry> match)
    {
        foreach (Entry e in doors)
        {
            if (e != null && e.door != null && match(e)) return e;
        }
        return null;
    }

#if UNITY_EDITOR
    public void EditorSetup(Entry[] list) => doors = list;
#endif
}
