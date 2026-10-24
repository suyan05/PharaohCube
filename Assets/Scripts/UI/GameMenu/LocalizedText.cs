using TMPro;
using UnityEngine;

// [추가] TMP 글자에 붙이면 언어가 바뀔 때 자동으로 글자가 바뀜
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Loc 표의 키 (예: title.newgame)")]
    [SerializeField] private string key;

    private TMP_Text text;

    public void SetKey(string k)
    {
        key = k;
        Refresh();
    }

    private void OnEnable()
    {
        GameSettings.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => GameSettings.LanguageChanged -= Refresh;

    private void Refresh()
    {
        if (text == null) text = GetComponent<TMP_Text>();
        if (text != null && !string.IsNullOrEmpty(key)) text.text = Loc.Get(key);
    }
}
