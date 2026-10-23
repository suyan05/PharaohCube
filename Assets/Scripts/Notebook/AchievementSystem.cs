using System;
using System.Collections.Generic;
using UnityEngine;

// 고고학 수첩 업적 관리 (게임 전체에 1개, NotebookBootstrap이 자동으로 만듦)
// - 0.5초마다 플래그, 아이템을 보고 조건이 맞으면 Unlock (퍼즐 쪽 코드를 안 고쳐도 됨)
// - 다른 방은 원하는 순간에 AchievementSystem.Unlock("ACH_BAS_CLEAR") 한 줄로 직접 줄 수도 있음
// - 판을 넘어 저장 (기획서: 미라 엔딩 후 새로 시작해도 남음). 지금은 PlayerPrefs ([확인 필요] 팀 저장 방식이 정해지면 옮기기)
// - 새로 얻은 카드는 "새 것" 표시 (수첩 탭 빨간 점) -> 수첩에서 카드를 보면 지워짐
public class AchievementSystem : MonoBehaviour
{
    private const string SaveKey = "PC_ACH_UNLOCKED";
    private const string NewKey = "PC_ACH_NEW";

    public static AchievementSystem Instance { get; private set; }
    public static event Action<AchievementDef> Unlocked;

    private readonly HashSet<string> unlocked = new HashSet<string>();
    private readonly HashSet<string> fresh = new HashSet<string>();
    private float timer;

    public int UnlockedCount => unlocked.Count;
    public bool HasNew => fresh.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        Load();
    }

    private void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = 0.5f;
        try
        {
            CheckAll();
        }
        catch (Exception e)
        {
            Debug.LogError($"[AchievementSystem] 확인 오류: {e}");
        }
    }

    // ================= 밖에서 부르는 것 =================
    public static void Unlock(string id)
    {
        if (Instance == null || string.IsNullOrEmpty(id) || Instance.unlocked.Contains(id)) return;
        AchievementDef def = AchievementData.All.Find(a => a.id == id);
        if (def == null)
        {
            Debug.LogWarning($"[AchievementSystem] 없는 업적 ID: {id}");
            return;
        }
        Instance.unlocked.Add(id);
        Instance.fresh.Add(id);
        Instance.Save();
        Debug.Log($"[Achievement] 유물 발굴: {def.relic} ({def.title})");
        Unlocked?.Invoke(def);
    }

    public bool IsUnlocked(string id) => unlocked.Contains(id);

    public bool IsNew(string id) => fresh.Contains(id);

    public bool ChapterHasNew(string chapter)
    {
        foreach (AchievementDef a in AchievementData.All) if (a.chapter == chapter && fresh.Contains(a.id)) return true;
        return false;
    }

    public void MarkSeen(string id)
    {
        if (fresh.Remove(id)) Save();
    }

    // ================= 조건 확인 =================
    private void CheckAll()
    {
        foreach (AchievementDef a in AchievementData.All)
        {
            if (!unlocked.Contains(a.id) && Met(a)) Unlock(a.id);
        }
    }

    private bool Met(AchievementDef a)
    {
        if (a.allOthers) return unlocked.Count >= AchievementData.All.Count - 1;
        bool hasRule = a.anyFlags.Length > 0 || a.allFlags.Length > 0 || a.allItems.Length > 0;
        if (!hasRule) return false; // 조건이 없는 업적은 Unlock()으로만
        if (a.anyFlags.Length > 0 && !AnyFlag(a.anyFlags)) return false;
        if (a.allFlags.Length > 0 && !AllFlags(a.allFlags)) return false;
        if (a.noneFlags.Length > 0 && AnyFlag(a.noneFlags)) return false;
        return a.allItems.Length == 0 || AllItems(a.allItems);
    }

    private static bool AnyFlag(string[] flags)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return false;
        foreach (string f in flags) if (gm.HasFlag(f)) return true;
        return false;
    }

    private static bool AllFlags(string[] flags)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return false;
        foreach (string f in flags) if (!gm.HasFlag(f)) return false;
        return true;
    }

    private static bool AllItems(string[] items)
    {
        ItemInventory inv = ItemInventory.Instance;
        if (inv == null) return false;
        foreach (string i in items) if (!inv.Has(i)) return false;
        return true;
    }

    // ================= 저장 =================
    private void Load()
    {
        Split(PlayerPrefs.GetString(SaveKey, ""), unlocked);
        Split(PlayerPrefs.GetString(NewKey, ""), fresh);
    }

    private void Save()
    {
        PlayerPrefs.SetString(SaveKey, string.Join(",", unlocked));
        PlayerPrefs.SetString(NewKey, string.Join(",", fresh));
        PlayerPrefs.Save();
    }

    private static void Split(string text, HashSet<string> into)
    {
        foreach (string s in text.Split(',')) if (!string.IsNullOrEmpty(s)) into.Add(s);
    }

    [ContextMenu("테스트: 업적 전부 지우기")]
    private void ResetAll()
    {
        unlocked.Clear();
        fresh.Clear();
        Save();
        Debug.Log("[AchievementSystem] 업적 기록을 모두 지움");
    }
}
