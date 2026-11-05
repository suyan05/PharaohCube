using System;
using UnityEngine;

// 단서 비석(ClueStele)을 읽으면 고고학 수첩 메모에 남김 (비석과 같은 오브젝트에 붙임)
// - 비석의 Flag On Read가 켜지는 순간 "[노트 N] 글" 로그를 남김 -> NotebookNotes가 받아서 수첩(J) 메모 탭에 저장
//   (라의 방 노트와 같은 방식. 오른쪽 위 짧은 알림도 같이 뜸)
public class HorusClueNote : MonoBehaviour
{
    [Header("이 플래그가 켜지면 기록 (비석의 Flag On Read와 같게)")]
    [SerializeField] private string readFlag = "";

    [Header("수첩 메모")]
    [SerializeField] private int noteId = 10;
    [SerializeField, TextArea] private string noteText = "";

    private bool subscribed;

    private void OnEnable() => TrySubscribe();

    private void Start() => TrySubscribe(); // GameManager가 늦게 준비된 경우

    private void TrySubscribe()
    {
        try
        {
            if (GameManager.Instance == null || subscribed) return;
            GameManager.Instance.OnFlagChanged += HandleFlag;
            subscribed = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[HorusClueNote] {name} 준비 오류: {e}");
        }
    }

    private void OnDisable()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
        subscribed = false;
    }

    private void HandleFlag(string flag)
    {
        if (string.IsNullOrEmpty(readFlag) || flag != readFlag) return;
        Debug.Log($"[노트 {noteId}] {noteText}");
    }
}
