using System;
using System.Collections.Generic;
using UnityEngine;

// 고고학 수첩 "메모" (라의 방 [노트 1~7] 같은 단서 글)
// - 퍼즐 코드가 남기는 로그 "[노트 4] 밤의 태양은..."를 받아서 번호 순서대로 저장
// - 오른쪽 위에는 짧은 알림만 뜨고(GameMessageUI), 긴 글은 J키 수첩 -> 메모 탭에서 다시 읽음
// - 한 판(실행) 동안만 기억 (업적과 달리 판을 넘어 저장하지 않음)
public class NotebookNotes : MonoBehaviour
{
    public const int RaNoteCount = 7;

    private static readonly SortedDictionary<int, string> notes = new SortedDictionary<int, string>();

    public static bool HasNew { get; private set; }
    public static IEnumerable<KeyValuePair<int, string>> All => notes;
    public static int Count => notes.Count;

    public static void MarkSeen() => HasNew = false;

    private void OnEnable() => Application.logMessageReceived += HandleLog;

    private void OnDisable() => Application.logMessageReceived -= HandleLog;

    private static void HandleLog(string msg, string stackTrace, LogType type)
    {
        if (type != LogType.Log || !msg.StartsWith("[노트")) return;
        try
        {
            int end = msg.IndexOf(']');
            if (end < 0) return;
            string digits = msg.Substring(3, end - 3).Trim();
            if (!int.TryParse(digits, out int number)) number = notes.Count + 1;
            string body = msg.Substring(end + 1).Trim();
            if (notes.ContainsKey(number) && notes[number] == body) return;
            notes[number] = body;
            HasNew = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[NotebookNotes] 메모 저장 실패: {e.Message}");
        }
    }
}
