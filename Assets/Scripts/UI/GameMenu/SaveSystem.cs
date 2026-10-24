using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// [추가] 이어하기용 저장 (씬 이름 + 플래그 + 소지품)
// - 저장 위치: Application.persistentDataPath/pharaohcube_save.json
// - 자동 저장: 게임 씬에 들어갈 때, 일시 정지 메뉴에서 시작 화면으로 갈 때, 게임을 끌 때
// - 이어하기: 마지막 씬의 처음부터 시작 (플래그·소지품은 그대로라 푼 퍼즐은 풀린 상태)
// GameManager / ItemInventory 코드는 건드리지 않고 안쪽 목록(activeFlags / items)을 읽고 씀
public static class SaveSystem
{
    [Serializable]
    private class SaveData
    {
        public int version = 1;
        public string scene;
        public string savedAt;
        public List<string> flags = new List<string>();
        public List<string> items = new List<string>();
    }

    private const string FileName = "pharaohcube_save.json";
    private static SaveData pending;       // 이어하기: 다음 씬이 열리면 넣을 값
    private static bool pendingClear;      // 새 게임: 다음 씬이 열리면 비움

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
    public static bool HasSave => File.Exists(FilePath);

    // 시작 화면 "마지막 저장" 표시용
    public static string LastSaveInfo()
    {
        SaveData d = Read();
        return d == null ? "" : d.savedAt;
    }

    // ================= 저장 =================
    public static void SaveNow()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (GameMenuBootstrap.IsMenuScene(scene)) return; // 시작 화면에서는 저장 안 함
        try
        {
            var d = new SaveData { scene = scene, savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm") };
            HashSet<string> flags = FlagSet(), items = ItemSet();
            if (flags != null) d.flags.AddRange(flags);
            if (items != null) d.items.AddRange(items);
            File.WriteAllText(FilePath, JsonUtility.ToJson(d, true));
            Debug.Log($"[SaveSystem] 저장: {scene} (플래그 {d.flags.Count}, 소지품 {d.items.Count}) -> {FilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] 저장 실패: {e.Message}");
        }
    }

    public static void Delete()
    {
        try { if (HasSave) File.Delete(FilePath); }
        catch (Exception e) { Debug.LogError($"[SaveSystem] 삭제 실패: {e.Message}"); }
    }

    // ================= 이어하기 / 새 게임 =================
    // 반환: 이동할 씬 이름 (없으면 null)
    public static string PrepareContinue()
    {
        SaveData d = Read();
        if (d == null || string.IsNullOrEmpty(d.scene)) return null;
        pending = d;
        pendingClear = false;
        return d.scene;
    }

    public static void PrepareNewGame()
    {
        Delete();
        pending = null;
        pendingClear = true;
        ClearSets(); // 이미 살아 있는 매니저(씬을 넘어 유지)도 비움
    }

    // GameMenuBootstrap이 씬이 열릴 때마다 부름 (씬 오브젝트 Awake 다음, Start 전)
    public static void OnSceneLoaded(Scene scene)
    {
        if (GameMenuBootstrap.IsMenuScene(scene.name)) return;
        if (pendingClear) { ClearSets(); pendingClear = false; }
        if (pending != null)
        {
            Restore(pending);
            pending = null;
        }
        SaveNow(); // 씬에 들어올 때마다 자동 저장
    }

    private static void Restore(SaveData d)
    {
        HashSet<string> flags = FlagSet(), items = ItemSet();
        if (flags != null) { flags.Clear(); foreach (string f in d.flags) flags.Add(f); }
        if (items != null) { items.Clear(); foreach (string i in d.items) items.Add(i); }
        Debug.Log($"[SaveSystem] 이어하기 복원: {d.scene} (플래그 {d.flags.Count}, 소지품 {d.items.Count})");
    }

    private static void ClearSets()
    {
        FlagSet()?.Clear();
        ItemSet()?.Clear();
    }

    private static SaveData Read()
    {
        try
        {
            return HasSave ? JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) : null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] 저장 파일 읽기 실패: {e.Message}");
            return null;
        }
    }

    // ================= 팀 매니저 안쪽 목록 (이벤트 없이 조용히 읽고 씀) =================
    private static HashSet<string> FlagSet() => GetSet(GameManager.Instance, "activeFlags");
    private static HashSet<string> ItemSet() => GetSet(ItemInventory.Instance, "items");

    private static HashSet<string> GetSet(object owner, string field)
    {
        if (owner == null) return null;
        FieldInfo fi = owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (fi == null)
        {
            Debug.LogWarning($"[SaveSystem] {owner.GetType().Name}.{field} 없음 (이름이 바뀌었으면 SaveSystem도 수정)");
            return null;
        }
        return fi.GetValue(owner) as HashSet<string>;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/UI/저장 파일 지우기 (이어하기 초기화)")]
    private static void EditorDelete()
    {
        Delete();
        Debug.Log($"[SaveSystem] 저장 파일 삭제: {FilePath}");
    }

    [UnityEditor.MenuItem("Tools/UI/저장 폴더 열기")]
    private static void EditorReveal() => UnityEditor.EditorUtility.RevealInFinder(Application.persistentDataPath);
#endif
}
