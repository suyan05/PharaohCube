using System;
using UnityEngine;
using UnityEngine.UI;

// 플래그에 따라 그림 한 장을 켜고 끔 (퍼즐 화면 안에서 씀)
// - H1 먼지 덮개 (H1_02): F_HO_H1_UNLOCKED가 꺼져 있는 동안 보임 + 클릭 막음
// - H1 완성 매 그림 (H1_03): F_HO_H1_DONE이 켜지면 보임
[RequireComponent(typeof(Image))]
public class HorusFlagImage : MonoBehaviour
{
    [Header("이 플래그를 보고 켜고 끔")]
    [SerializeField] private string flag = "";

    [Tooltip("켜짐 = 플래그가 켜졌을 때 보임 / 꺼짐 = 플래그가 꺼져 있을 때 보임")]
    [SerializeField] private bool showWhenFlagOn = true;

    private Image image;
    private bool subscribed;

    private void OnEnable()
    {
        try
        {
            image = GetComponent<Image>();
            Refresh();
            if (GameManager.Instance == null || subscribed) return;
            GameManager.Instance.OnFlagChanged += HandleFlag;
            subscribed = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[HorusFlagImage] {name} 준비 오류: {e}");
        }
    }

    private void OnDisable()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
        subscribed = false;
    }

    private void HandleFlag(string changed)
    {
        if (changed == flag) Refresh();
    }

    private void Refresh()
    {
        if (image == null) return;
        bool on = GameManager.Instance != null && !string.IsNullOrEmpty(flag) && GameManager.Instance.HasFlag(flag);
        image.enabled = on == showWhenFlagOn;
    }
}
